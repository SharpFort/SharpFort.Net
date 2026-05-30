# Casbin-RBAC 性能优化 — 最终实施计划

基于多轮专家审查 + 独立交叉评估的全部结论，所有已知 BUG 已确认修复方案，设计约束已明确记录。本计划为可执行的最终版。

---

## 修改文件清单（6 个文件）

```
module/casbin-rbac/
├── SharpFort.CasbinRbac.Application.Contracts/
│   └── IServices/IMenuService.cs                       (新增 WarmupCacheAsync)
├── SharpFort.CasbinRbac.Application/
│   ├── Services/System/MenuService.cs                   (缓存+N+1+双读+安全+异常)
│   └── SharpFortCasbinRbacApplicationModule.cs         (启动预热+异常保护)
├── SharpFort.CasbinRbac.Domain/
│   ├── Managers/ICasbinPolicyManager.cs                 (新增 ReloadAllPoliciesAsync)
│   ├── Managers/CasbinPolicyManager.cs                 (9写方法增量重构)
│   └── Managers/CasbinSeedService.cs                   (统一重载入口)
```

---

## 1. ICasbinPolicyManager.cs — 接口新增方法

**文件**：`SharpFort.CasbinRbac.Domain/Managers/ICasbinPolicyManager.cs`

在接口末尾添加：

```csharp
/// <summary>
/// 带全局写锁的全量策略重载（供系统初始化、运维手动触发及数据迁移使用）。
/// </summary>
Task ReloadAllPoliciesAsync();
```

---

## 2. CasbinPolicyManager.cs — 核心重构

**文件**：`SharpFort.CasbinRbac.Domain/Managers/CasbinPolicyManager.cs`

### 2.1 架构变更摘要

| 项目 | 旧设计 | 新设计 |
|------|--------|--------|
| DB 写入 | `_writeLock` 锁内 | 锁外（DB 事务自行隔离） |
| 内存同步 | 全量 `LoadPolicyAsync()` | 精准增量 API（微秒级） |
| 写锁 | 保护 DB + 内存 | 仅保护 Enforcer 内存 |
| UOW 路径 | `OnCompleted(LoadPolicy)` | `OnCompleted(增量)` |
| 无 UOW 路径 | 同步 `LoadPolicy` | `SyncOrFallback(增量, 全量兜底)` |

### 2.2 删除的旧代码

- 整个 `TriggerMemorySync()` 方法
- `syncKey` 去重逻辑
- DB 写入上的 `_writeLock` 保护

### 2.3 新增的辅助方法

```csharp
/// <summary>
/// 无 UOW 时的一致性兜底：先尝试增量同步，失败则全量重载。
/// </summary>
private async Task SyncOrFallback(Func<Task> incrementalSync)
{
    try { await incrementalSync(); }
    catch { await ReloadAllPoliciesAsync(); }
}

/// <summary>
/// 带全局写锁的全量策略重载。
/// </summary>
public async Task ReloadAllPoliciesAsync()
{
    await _writeLock.WaitAsync();
    try { await _enforcer.LoadPolicyAsync(); }
    finally { _writeLock.Release(); }
}
```

### 2.4 9 个写方法详细修改

#### 写操作 1：AddRoleForUserAsync — 分配角色

DB 写入（锁外）→ 内存 `AddGroupingPolicyAsync`（锁内）。UOW 判定 + 无 UOW 兜底。

```csharp
public async Task AddRoleForUserAsync(User user, Role role)
{
    string sub = GetUserSubject(user.Id);
    string roleSub = GetRoleSubject(role.RoleCode!);
    string domain = GetTenantDomain(user.TenantId);

    await _roleRepository._Db.Insertable(
        new CasbinRule { PType = "g", V0 = sub, V1 = roleSub, V2 = domain })
        .ExecuteCommandAsync();

    Func<Task> syncAction = async () =>
    {
        await _writeLock.WaitAsync();
        try { await _enforcer.AddGroupingPolicyAsync(sub, roleSub, domain); }
        finally { _writeLock.Release(); }
    };

    IUnitOfWork? uow = _unitOfWorkManager.Current;
    if (uow != null) { uow.OnCompleted(syncAction); }
    else { await SyncOrFallback(syncAction); }
}
```

#### 写操作 2：RemoveRoleForUserAsync — 移除角色

DB 精确删除 → 内存 `RemoveGroupingPolicyAsync`。

```csharp
public async Task RemoveRoleForUserAsync(User user, Role role)
{
    string sub = GetUserSubject(user.Id);
    string roleSub = GetRoleSubject(role.RoleCode!);
    string domain = GetTenantDomain(user.TenantId);

    await _roleRepository._Db.Deleteable<CasbinRule>()
        .Where(x => x.PType == "g" && x.V0 == sub && x.V1 == roleSub && x.V2 == domain)
        .ExecuteCommandAsync();

    Func<Task> syncAction = async () =>
    {
        await _writeLock.WaitAsync();
        try { await _enforcer.RemoveGroupingPolicyAsync(sub, roleSub, domain); }
        finally { _writeLock.Release(); }
    };

    IUnitOfWork? uow = _unitOfWorkManager.Current;
    if (uow != null) { uow.OnCompleted(syncAction); }
    else { await SyncOrFallback(syncAction); }
}
```

#### 写操作 3：SetUserRolesAsync — 全量覆盖角色 **[含 domain 过滤修复]**

DB 清空（带 domain）→ 重新插入。内存侧：**枚举旧域内规则 → 按 domain 过滤 → 逐个精确删除 → 批量添加新规则**。
此方案避免了 `RemoveFilteredGroupingPolicyAsync(0, sub, "", domain)` 中 `""` 作为字面量匹配不到任何规则的问题。

```csharp
public async Task SetUserRolesAsync(User user, List<Role> roles)
{
    string sub = GetUserSubject(user.Id);
    string domain = GetTenantDomain(user.TenantId);

    // DB（锁外，保留 domain 过滤）
    await _roleRepository._Db.Deleteable<CasbinRule>()
        .Where(x => x.PType == "g" && x.V0 == sub && x.V2 == domain)
        .ExecuteCommandAsync();

    if (roles.Count > 0)
    {
        List<CasbinRule> newRules = roles.Select(r => new CasbinRule
        {
            PType = "g", V0 = sub, V1 = GetRoleSubject(r.RoleCode!), V2 = domain
        }).ToList();
        await _roleRepository._Db.Insertable(newRules).ExecuteCommandAsync();
    }

    // 内存（锁内：枚举 + domain 过滤 + 逐个精确删除）
    Func<Task> syncAction = async () =>
    {
        await _writeLock.WaitAsync();
        try
        {
            var oldRules = _enforcer.GetFilteredGroupingPolicy(0, sub);
            foreach (var rule in oldRules)
            {
                if (rule.Count() >= 3 && rule.ElementAt(2) == domain)
                    await _enforcer.RemoveGroupingPolicyAsync(
                        rule.ElementAt(0), rule.ElementAt(1), rule.ElementAt(2));
            }

            if (roles.Count > 0)
            {
                List<List<string>> groupings = roles.Select(r =>
                    new List<string> { sub, GetRoleSubject(r.RoleCode!), domain }).ToList();
                await _enforcer.AddGroupingPoliciesAsync(groupings);
            }
        }
        finally { _writeLock.Release(); }
    };

    IUnitOfWork? uow = _unitOfWorkManager.Current;
    if (uow != null) { uow.OnCompleted(syncAction); }
    else { await SyncOrFallback(syncAction); }
}
```

#### 写操作 4：SetRolePermissionsAsync — 设置角色权限 (p)

DB 清空 + 重新插入 → 内存 `RemoveFilteredPolicyAsync` + `AddPoliciesAsync`。

```csharp
public async Task SetRolePermissionsAsync(Role role, List<Menu> menus)
{
    string roleSub = GetRoleSubject(role.RoleCode!);
    string domain = GetTenantDomain(role.TenantId);

    await _roleRepository._Db.Deleteable<CasbinRule>()
        .Where(x => x.PType == "p" && x.V0 == roleSub && x.V1 == domain)
        .ExecuteCommandAsync();

    List<CasbinRule> newRules = new();
    foreach (Menu menu in menus)
    {
        if (string.IsNullOrWhiteSpace(menu.ApiUrl)) continue;
        string methods = string.IsNullOrWhiteSpace(menu.ApiMethod) ? "*" : menu.ApiMethod;
        newRules.Add(new CasbinRule
            { PType = "p", V0 = roleSub, V1 = domain, V2 = menu.ApiUrl, V3 = methods });
    }

    if (newRules.Count > 0)
        await _roleRepository._Db.Insertable(newRules).ExecuteCommandAsync();

    Func<Task> syncAction = async () =>
    {
        await _writeLock.WaitAsync();
        try
        {
            await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);
            if (menus.Count > 0)
            {
                List<List<string>> policies = new();
                foreach (Menu menu in menus)
                {
                    if (string.IsNullOrWhiteSpace(menu.ApiUrl)) continue;
                    string methods = string.IsNullOrWhiteSpace(menu.ApiMethod) ? "*" : menu.ApiMethod;
                    policies.Add(new List<string> { roleSub, domain, menu.ApiUrl, methods });
                }
                await _enforcer.AddPoliciesAsync(policies);
            }
        }
        finally { _writeLock.Release(); }
    };

    IUnitOfWork? uow = _unitOfWorkManager.Current;
    if (uow != null) { uow.OnCompleted(syncAction); }
    else { await SyncOrFallback(syncAction); }
}
```

#### 写操作 5：InitAdminPermissionAsync — 初始化管理员权限 (p)

```csharp
public async Task InitAdminPermissionAsync(Role adminRole)
{
    string roleSub = GetRoleSubject(adminRole.RoleCode!);
    string domain = GetTenantDomain(adminRole.TenantId);

    await _roleRepository._Db.Deleteable<CasbinRule>()
        .Where(x => x.PType == "p" && x.V0 == roleSub && x.V1 == domain)
        .ExecuteCommandAsync();

    await _roleRepository._Db.Insertable(new CasbinRule
        { PType = "p", V0 = roleSub, V1 = domain, V2 = "*", V3 = "*" })
        .ExecuteCommandAsync();

    Func<Task> syncAction = async () =>
    {
        await _writeLock.WaitAsync();
        try
        {
            await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);
            await _enforcer.AddPolicyAsync(roleSub, domain, "*", "*");
        }
        finally { _writeLock.Release(); }
    };

    IUnitOfWork? uow = _unitOfWorkManager.Current;
    if (uow != null) { uow.OnCompleted(syncAction); }
    else { await SyncOrFallback(syncAction); }
}
```

#### 写操作 6：CleanRolePoliciesAsync — 清理角色所有策略 (p & g)

角色级 g-rule 清理：`RemoveFilteredGroupingPolicyAsync(1, roleSub, domain)` — 精确匹配 V1=roleSub, V2=domain。

```csharp
public async Task CleanRolePoliciesAsync(Role role)
{
    string roleSub = GetRoleSubject(role.RoleCode!);
    string domain = GetTenantDomain(role.TenantId);

    await _roleRepository._Db.Deleteable<CasbinRule>()
        .Where(x => (x.PType == "p" && x.V0 == roleSub && x.V1 == domain)
                 || (x.PType == "g" && x.V1 == roleSub && x.V2 == domain))
        .ExecuteCommandAsync();

    Func<Task> syncAction = async () =>
    {
        await _writeLock.WaitAsync();
        try
        {
            await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);
            await _enforcer.RemoveFilteredGroupingPolicyAsync(1, roleSub, domain);
        }
        finally { _writeLock.Release(); }
    };

    IUnitOfWork? uow = _unitOfWorkManager.Current;
    if (uow != null) { uow.OnCompleted(syncAction); }
    else { await SyncOrFallback(syncAction); }
}
```

#### 写操作 7：CleanRolePoliciesByRoleCodeAsync — 按角色编码清理

与 CleanRolePoliciesAsync 对称，参数来自外部传入。

```csharp
public async Task CleanRolePoliciesByRoleCodeAsync(string roleCode, Guid? tenantId)
{
    string roleSub = GetRoleSubject(roleCode);
    string domain = GetTenantDomain(tenantId);

    await _roleRepository._Db.Deleteable<CasbinRule>()
        .Where(x => (x.PType == "p" && x.V0 == roleSub && x.V1 == domain)
                 || (x.PType == "g" && x.V1 == roleSub && x.V2 == domain))
        .ExecuteCommandAsync();

    Func<Task> syncAction = async () =>
    {
        await _writeLock.WaitAsync();
        try
        {
            await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);
            await _enforcer.RemoveFilteredGroupingPolicyAsync(1, roleSub, domain);
        }
        finally { _writeLock.Release(); }
    };

    IUnitOfWork? uow = _unitOfWorkManager.Current;
    if (uow != null) { uow.OnCompleted(syncAction); }
    else { await SyncOrFallback(syncAction); }
}
```

#### 写操作 8：MigrateRoleCodeAsync — 迁移角色编码

低频运维操作，内存侧直接用 `ReloadAllPoliciesAsync` 全量重载兜底。

```csharp
public async Task MigrateRoleCodeAsync(string oldRoleCode, string newRoleCode, Guid? tenantId)
{
    string domain = GetTenantDomain(tenantId);

    await _roleRepository._Db.Updateable<CasbinRule>()
        .SetColumns(it => new CasbinRule { V1 = newRoleCode })
        .Where(x => x.PType == "g" && x.V1 == oldRoleCode && x.V2 == domain)
        .ExecuteCommandAsync();

    await _roleRepository._Db.Updateable<CasbinRule>()
        .SetColumns(it => new CasbinRule { V0 = newRoleCode })
        .Where(x => x.PType == "p" && x.V0 == oldRoleCode && x.V1 == domain)
        .ExecuteCommandAsync();

    Func<Task> syncAction = ReloadAllPoliciesAsync;

    IUnitOfWork? uow = _unitOfWorkManager.Current;
    if (uow != null) { uow.OnCompleted(syncAction); }
    else { await syncAction(); }
}
```

#### 写操作 9：CleanUserPoliciesAsync — 清理用户策略 **[含 domain 过滤修复]**

DB 和内存双侧保留 domain 过滤。内存侧：枚举 + domain 过滤 + 逐个精确删除。

```csharp
public async Task CleanUserPoliciesAsync(Guid userId, Guid? tenantId)
{
    string sub = GetUserSubject(userId);
    string domain = GetTenantDomain(tenantId);

    await _roleRepository._Db.Deleteable<CasbinRule>()
        .Where(x => x.PType == "g" && x.V0 == sub && x.V2 == domain)
        .ExecuteCommandAsync();

    Func<Task> syncAction = async () =>
    {
        await _writeLock.WaitAsync();
        try
        {
            var oldRules = _enforcer.GetFilteredGroupingPolicy(0, sub);
            foreach (var rule in oldRules)
            {
                if (rule.Count() >= 3 && rule.ElementAt(2) == domain)
                    await _enforcer.RemoveGroupingPolicyAsync(
                        rule.ElementAt(0), rule.ElementAt(1), rule.ElementAt(2));
            }
        }
        finally { _writeLock.Release(); }
    };

    IUnitOfWork? uow = _unitOfWorkManager.Current;
    if (uow != null) { uow.OnCompleted(syncAction); }
    else { await SyncOrFallback(syncAction); }
}
```

---

## 3. CasbinSeedService.cs — 统一重载入口

**文件**：`SharpFort.CasbinRbac.Domain/Managers/CasbinSeedService.cs`

### 3.1 构造函数注入

```csharp
public partial class CasbinSeedService(
    IEnforcer enforcer,
    ISqlSugarRepository<Role> roleRepo,
    ICasbinPolicyManager casbinPolicyManager,  // 新增
    ILogger<CasbinSeedService> logger) : DomainService
{
    private readonly ICasbinPolicyManager _casbinPolicyManager = casbinPolicyManager;
    // ...其余不变
}
```

### 3.2 Phase 4 重载替换

```csharp
// 旧：await _enforcer.LoadPolicyAsync();
// 新：
await _casbinPolicyManager.ReloadAllPoliciesAsync();
```

---

## 4. IMenuService.cs — 缓存预热契约

**文件**：`SharpFort.CasbinRbac.Application.Contracts/IServices/IMenuService.cs`

```csharp
public interface IMenuService : ISfCrudAppService<MenuGetOutputDto, MenuGetListOutputDto,
    Guid, MenuGetListInputVo, MenuCreateInputVo, MenuUpdateInputVo>
{
    /// <summary>
    /// 本地高速缓存预热（尽力而为，失败不阻断启动）
    /// </summary>
    Task WarmupCacheAsync();
}
```

---

## 5. MenuService.cs — 读/写全面优化

**文件**：`SharpFort.CasbinRbac.Application/Services/System/MenuService.cs`

### 5.1 缓存基础设施

```csharp
// 构造函数追加 IMemoryCache memoryCache 参数注入
private readonly IMemoryCache _memoryCache = memoryCache;

// 无锁原子版本号（单实例 static 即可）
private static long _menuSchemaVersion = 1;

private void InvalidateMenuCache() => Interlocked.Increment(ref _menuSchemaVersion);

private string GetCachedKeyPrefix()
    => $"Menuv{Interlocked.Read(ref _menuSchemaVersion)}:";
```

### 5.2 GetListAsync — 版本化缓存 + 缓存键状态隔离

```csharp
public override async Task<PagedResultDto<MenuGetListOutputDto>> GetListAsync(MenuGetListInputVo input)
{
    string keyPrefix = GetCachedKeyPrefix();
    string searchName = input.MenuName ?? "*";
    string stateKey = input.State?.ToString() ?? "all";  // true / false / all 三态隔离
    string cacheKey = $"{keyPrefix}List:{input.MenuSource}:{stateKey}:{searchName}";

    return await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

        List<Menu> entities = await _repository._DbQueryable
            .WhereIF(!string.IsNullOrEmpty(input.MenuName), x => x.MenuName!.Contains(input.MenuName!))
            .WhereIF(input.State is not null, x => x.State == input.State)
            .Where(x => x.MenuSource == input.MenuSource)
            .OrderBy(x => x.OrderNum)
            .OrderBy(x => x.CreationTime)
            .ToListAsync();

        int total = entities.Count;
        var dtos = await MapToGetListOutputDtosAsync(entities);
        return new PagedResultDto<MenuGetListOutputDto>(total, dtos);
    }) ?? new PagedResultDto<MenuGetListOutputDto>();
}
```

### 5.3 GetListRoleIdAsync — 缓存

```csharp
public async Task<List<MenuGetListOutputDto>> GetListRoleIdAsync(Guid roleId)
{
    string cacheKey = $"{GetCachedKeyPrefix()}RoleList:{roleId}";

    return await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
        List<Menu> entities = await _repository._DbQueryable
            .Where(m => SqlFunc.Subqueryable<RoleMenu>()
                .Where(rm => rm.RoleId == roleId && rm.MenuId == m.Id).Any())
            .ToListAsync();
        return await MapToGetListOutputDtosAsync(entities);
    }) ?? [];
}
```

### 5.4 GetAsync — 缓存

```csharp
public override async Task<MenuGetOutputDto> GetAsync(Guid id)
{
    string cacheKey = $"{GetCachedKeyPrefix()}Detail:{id}";

    return await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
        return await base.GetAsync(id);  // 不存在时抛 EntityNotFoundException
    })!;
}
```

### 5.5 CreateAsync — 创建 + 缓存失效

```csharp
public override async Task<MenuGetOutputDto> CreateAsync(MenuCreateInputVo input)
    => await CreateInternalAsync(input, invalidateCache: true);

private async Task<MenuGetOutputDto> CreateInternalAsync(MenuCreateInputVo input, bool invalidateCache)
{
    // 校验逻辑保持不变（ApiMethod 默认 GET、ApiUrl 转小写、{param} 拒绝）
    if (!string.IsNullOrWhiteSpace(input.ApiUrl) && string.IsNullOrWhiteSpace(input.ApiMethod))
        input.ApiMethod = "GET";
    if (!string.IsNullOrWhiteSpace(input.ApiUrl))
    {
        if (input.ApiUrl.Contains('{'))
            throw new UserFriendlyException("ApiUrl 不支持 {param} 格式...");
        input.ApiUrl = input.ApiUrl.ToLowerInvariant();
    }
    if (!string.IsNullOrEmpty(input.ApiMethod))
        input.ApiMethod = input.ApiMethod.ToUpper(CultureInfo.InvariantCulture);

    var result = await base.CreateAsync(input);

    if (invalidateCache) InvalidateMenuCache();
    return result;
}
```

### 5.6 PostImportExcelAsync — 批量导入 + try-finally

```csharp
public override async Task PostImportExcelAsync(List<MenuCreateInputVo> input)
{
    try
    {
        foreach (var item in input)
            await CreateInternalAsync(item, invalidateCache: false);
    }
    finally
    {
        InvalidateMenuCache();  // 无论是否异常，单次失效
    }
}
```

### 5.7 UpdateAsync — 消除双读 + 补齐校验 + 批量消除 N+1

```csharp
public override async Task<MenuGetOutputDto> UpdateAsync(Guid id, MenuUpdateInputVo input)
{
    // 前置校验
    if (!string.IsNullOrWhiteSpace(input.ApiUrl) && string.IsNullOrWhiteSpace(input.ApiMethod))
        input.ApiMethod = "GET";
    if (!string.IsNullOrWhiteSpace(input.ApiUrl))
    {
        if (input.ApiUrl.Contains('{'))
            throw new UserFriendlyException("ApiUrl 不支持 {param} 格式...");
        input.ApiUrl = input.ApiUrl.ToLowerInvariant();
    }

    // (1) 读旧实体 —— 第 1 次 DB 读
    Menu oldMenu = await _repository.GetByIdAsync(id)
        ?? throw new EntityNotFoundException(typeof(Menu), id);

    bool isApiChanged = oldMenu.ApiUrl != input.ApiUrl
        || (oldMenu.ApiMethod?.ToUpper(CultureInfo.InvariantCulture) ?? "")
        != (input.ApiMethod?.ToUpper(CultureInfo.InvariantCulture) ?? "");

    // (2) 显式补齐权限和输入校验（绕过 base.UpdateAsync 时不能省略）
    await CheckUpdatePolicyAsync();
    await CheckUpdateInputDtoAsync(oldMenu, input);

    // (3) 直接在已加载实体上映射更新 —— 第 1 次 DB 写（无二次读）
    await MapToEntityAsync(input, oldMenu);
    await _repository.UpdateAsync(oldMenu, autoSave: true);
    MenuGetOutputDto result = await MapToGetOutputDtoAsync(oldMenu);

    // (4) 缓存失效
    InvalidateMenuCache();

    // (5) API 变更时批量刷新 Casbin 策略
    if (isApiChanged)
    {
        // 第 2 次 DB 读：获取关联角色 ID
        List<Guid> roleIds = await _roleMenuRepository._DbQueryable
            .Where(x => x.MenuId == id).Select(x => x.RoleId).Distinct().ToListAsync();

        if (roleIds.Count > 0)
        {
            // 第 3 次 DB 读：角色实体
            List<Role> roles = await _roleRepository.GetListAsync(x => roleIds.Contains(x.Id));

            // 第 4 次 DB 读：角色-菜单关联
            var mappings = await _roleMenuRepository._DbQueryable
                .Where(x => roleIds.Contains(x.RoleId))
                .Select(x => new { x.RoleId, x.MenuId }).ToListAsync();

            // 第 5 次 DB 读：菜单实体
            List<Guid> allMenuIds = mappings.Select(x => x.MenuId).Distinct().ToList();
            List<Menu> allMenus = await _repository.GetListAsync(x => allMenuIds.Contains(x.Id));

            // 内存归类 → 批量触发增量同步
            var roleMenusMap = mappings.GroupBy(x => x.RoleId)
                .ToDictionary(g => g.Key,
                    g => g.Select(x => allMenus.First(m => m.Id == x.MenuId)).ToList());

            foreach (Role role in roles)
            {
                if (roleMenusMap.TryGetValue(role.Id, out List<Menu>? menus))
                    await _casbinPolicyManager.SetRolePermissionsAsync(role, menus);
            }
        }
    }

    return result;
}
```

### 5.8 DeleteAsync — 消除 N+1

```csharp
public override async Task DeleteAsync(IEnumerable<Guid> ids)
{
    // (1) 获取受影响的角色 ID
    List<Guid> affectedRoleIds = await _roleMenuRepository._DbQueryable
        .Where(x => ids.Contains(x.MenuId)).Select(x => x.RoleId).Distinct().ToListAsync();

    // (2) 物理删除
    await base.DeleteAsync(ids);

    // (3) 缓存失效
    InvalidateMenuCache();

    // (4) 批量刷新受影响角色的策略
    if (affectedRoleIds.Count > 0)
    {
        List<Role> roles = await _roleRepository.GetListAsync(x => affectedRoleIds.Contains(x.Id));

        var mappings = await _roleMenuRepository._DbQueryable
            .Where(x => affectedRoleIds.Contains(x.RoleId))
            .Select(x => new { x.RoleId, x.MenuId }).ToListAsync();

        List<Guid> allMenuIds = mappings.Select(x => x.MenuId).Distinct().ToList();
        List<Menu> allMenus = await _repository.GetListAsync(x => allMenuIds.Contains(x.Id));

        var roleMenusMap = mappings.GroupBy(x => x.RoleId)
            .ToDictionary(g => g.Key,
                g => g.Select(x => allMenus.First(m => m.Id == x.MenuId)).ToList());

        foreach (Role role in roles)
        {
            List<Menu> menus = roleMenusMap.TryGetValue(role.Id, out List<Menu>? mList) ? mList : [];
            await _casbinPolicyManager.SetRolePermissionsAsync(role, menus);
        }
    }
}
```

### 5.9 WarmupCacheAsync — 预热

```csharp
public async Task WarmupCacheAsync()
{
    await GetListAsync(new MenuGetListInputVo { MenuSource = MenuSource.Ruoyi });
    await GetListAsync(new MenuGetListInputVo { MenuSource = MenuSource.Pure });
}
```

---

## 6. SharpFortCasbinRbacApplicationModule.cs — 启动预热

**文件**：`SharpFort.CasbinRbac.Application/SharpFortCasbinRbacApplicationModule.cs`

```csharp
public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
{
    try
    {
        IMenuService menuService = context.ServiceProvider.GetRequiredService<IMenuService>();
        await menuService.WarmupCacheAsync();
    }
    catch (Exception ex)
    {
        ILogger<SharpFortCasbinRbacApplicationModule>? logger =
            context.ServiceProvider.GetService<ILogger<SharpFortCasbinRbacApplicationModule>>();
        logger?.LogWarning(ex, "菜单本地缓存预热失败（可能数据库未就绪），缓存将在首次请求时自动按需加载");
    }
}
```

---

## 设计约束记录

| 约束 | 说明 |
|------|------|
| 单实例缓存 | `_menuSchemaVersion` 为进程内 `static` 字段，多实例部署时各实例缓存失效不同步。若未来启用多实例，需通过 Redis pub/sub 或 ABP 分布式事件总线广播失效通知。 |
| Redis Watcher | 增量更新不经过 Adapter 的 `SavePolicy`（`AutoSave=false`），Watcher 不会收到通知。当前 `EnableRedisWatcher: false` 无影响。若未来开启多实例 Redis Watcher，需在 `syncAction` 后显式调用 `watcher.Update()` 通知其他实例。 |

---

## 验证计划

| 测试场景 | 预期结果 |
|----------|----------|
| `GetListAsync` 首次调用 | 10-15ms（DB 查询 + 缓存填充） |
| `GetListAsync` 缓存命中 | <0.1ms |
| `GetAsync` 缓存命中 | <0.1ms |
| `UpdateAsync`（无 API 变更） | 3-5ms（1 读 + 1 写 + 权限校验） |
| `UpdateAsync`（API 变更 + 多角色） | <15ms（1 读 + 1 写 + 4 批量读 + 增量内存同步） |
| 缓存键隔离 | State=null / false / true 三者缓存独立 |
| 批量导入异常 | finally 保证缓存失效 |
| 无 UOW 路径 | SyncOrFallback 兜底，增量失败则全量重载 |
