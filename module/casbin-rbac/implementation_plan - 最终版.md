# Casbin-RBAC 性能优化实施计划 — 最终版

基于四轮专家审查 + 一轮交叉评估的全部结论，本计划汇总所有需要实施的修改点。

---

## 修改文件清单（6 个文件）

```
module/casbin-rbac/
├── SharpFort.CasbinRbac.Application.Contracts/
│   └── IServices/IMenuService.cs                       (新增 WarmupCacheAsync 契约)
├── SharpFort.CasbinRbac.Application/
│   ├── Services/System/MenuService.cs                   (缓存+N+1消除+双读消除+安全校验+异常处理)
│   └── SharpFortCasbinRbacApplicationModule.cs         (启动预热+异常保护)
├── SharpFort.CasbinRbac.Domain/
│   ├── Managers/ICasbinPolicyManager.cs                 (新增 ReloadAllPoliciesAsync 接口)
│   ├── Managers/CasbinPolicyManager.cs                 (9写方法增量重构+锁收窄+UOW回退+domain过滤)
│   └── Managers/CasbinSeedService.cs                   (统一调用 ReloadAllPoliciesAsync)
```

---

## 修改 1：ICasbinPolicyManager.cs — 接口新增方法

**文件**：`SharpFort.CasbinRbac.Domain/Managers/ICasbinPolicyManager.cs`

**操作**：在接口末尾添加 `ReloadAllPoliciesAsync` 方法签名。

```csharp
/// <summary>
/// 手动触发全量策略重载（仅供系统初始化、分布式同步或运维手动触发）。
/// 内部持有全局写锁以保证线程安全。
/// </summary>
Task ReloadAllPoliciesAsync();
```

---

## 修改 2：CasbinPolicyManager.cs — 核心重构

**文件**：`SharpFort.CasbinRbac.Domain/Managers/CasbinPolicyManager.cs`

### 2.1 架构变更

| 项目 | 旧设计 | 新设计 |
|------|--------|--------|
| DB 写入 | 锁内执行 | 锁外执行（利用 DB 事务隔离） |
| 内存同步 | 全量 `LoadPolicyAsync()` | 精准增量 API（微秒级） |
| 写锁 | 保护 DB + 内存 | 仅保护 Enforcer 内存操作 |
| UOW 判定 | 有 UOW → OnCompleted(LoadPolicy)，无 UOW → 同步 LoadPolicy | 有 UOW → OnCompleted(增量)，无 UOW → ReloadAllPoliciesAsync 兜底 |

### 2.2 全部 9 个写方法的具体修改

#### 写操作 1：AddRoleForUserAsync — 为用户分配角色 (g)

DB 写入在锁外，内存同步通过 `AddGroupingPolicyAsync` 增量添加。

```csharp
public async Task AddRoleForUserAsync(User user, Role role)
{
    string sub = GetUserSubject(user.Id);
    string roleSub = GetRoleSubject(role.RoleCode!);
    string domain = GetTenantDomain(user.TenantId);

    // DB 写入（锁外）
    await _roleRepository._Db.Insertable(new CasbinRule { PType = "g", V0 = sub, V1 = roleSub, V2 = domain })
        .ExecuteCommandAsync();

    // 内存增量同步
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

#### 写操作 2：RemoveRoleForUserAsync — 移除用户的角色 (g)

DB 精确删除，内存通过 `RemoveGroupingPolicyAsync` 增量删除。

```csharp
public async Task RemoveRoleForUserAsync(User user, Role role)
{
    string sub = GetUserSubject(user.Id);
    string roleSub = GetRoleSubject(role.RoleCode!);
    string domain = GetTenantDomain(user.TenantId);

    // DB 删除（锁外）
    await _roleRepository._Db.Deleteable<CasbinRule>()
        .Where(x => x.PType == "g" && x.V0 == sub && x.V1 == roleSub && x.V2 == domain)
        .ExecuteCommandAsync();

    // 内存增量同步
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

#### 写操作 3：SetUserRolesAsync — 全量覆盖用户的角色 (g)

**【关键修复】** DB 和内存双侧均保留 domain 过滤。内存侧使用"先枚举旧规则 → 按 domain 过滤 → 逐个精确删除"方案，避免空串字面量陷阱。

```csharp
public async Task SetUserRolesAsync(User user, List<Role> roles)
{
    string sub = GetUserSubject(user.Id);
    string domain = GetTenantDomain(user.TenantId);

    // DB 清空 + 重新插入（锁外，保留 domain 过滤）
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

    // 内存增量同步（锁内：枚举旧域内规则 → 逐个精确删除 → 批量添加新规则）
    Func<Task> syncAction = async () =>
    {
        await _writeLock.WaitAsync();
        try
        {
            // 先枚举该用户在内存中的所有 g-rule，按 domain 过滤后逐个删除
            var oldRules = _enforcer.GetFilteredGroupingPolicy(0, sub);
            foreach (var rule in oldRules)
            {
                if (rule.Count() >= 3 && rule.ElementAt(2) == domain)
                    await _enforcer.RemoveGroupingPolicyAsync(rule.ElementAt(0), rule.ElementAt(1), rule.ElementAt(2));
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

#### 写操作 4：SetRolePermissionsAsync — 设置角色的权限 (p)

```csharp
public async Task SetRolePermissionsAsync(Role role, List<Menu> menus)
{
    string roleSub = GetRoleSubject(role.RoleCode!);
    string domain = GetTenantDomain(role.TenantId);

    // DB 清空 + 重新插入（锁外）
    await _roleRepository._Db.Deleteable<CasbinRule>()
        .Where(x => x.PType == "p" && x.V0 == roleSub && x.V1 == domain)
        .ExecuteCommandAsync();

    List<CasbinRule> newRules = new();
    foreach (Menu menu in menus)
    {
        if (string.IsNullOrWhiteSpace(menu.ApiUrl)) continue;
        string methods = string.IsNullOrWhiteSpace(menu.ApiMethod) ? "*" : menu.ApiMethod;
        newRules.Add(new CasbinRule { PType = "p", V0 = roleSub, V1 = domain, V2 = menu.ApiUrl, V3 = methods });
    }

    if (newRules.Count > 0)
        await _roleRepository._Db.Insertable(newRules).ExecuteCommandAsync();

    // 内存增量同步（锁内）
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
    {
        PType = "p", V0 = roleSub, V1 = domain, V2 = "*", V3 = "*"
    }).ExecuteCommandAsync();

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

角色级 g-rule 清理使用 `RemoveFilteredGroupingPolicyAsync(1, roleSub, domain)`，精确匹配 V1=roleSub, V2=domain。

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

#### 写操作 7：CleanRolePoliciesByRoleCodeAsync — 按角色编码清理策略

与 CleanRolePoliciesAsync 对称。

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

低频运维操作，内存侧直接使用 `ReloadAllPoliciesAsync()` 全量重载作为安全兜底。

```csharp
public async Task MigrateRoleCodeAsync(string oldRoleCode, string newRoleCode, Guid? tenantId)
{
    string domain = GetTenantDomain(tenantId);

    // DB 更新（锁外）
    await _roleRepository._Db.Updateable<CasbinRule>()
        .SetColumns(it => new CasbinRule { V1 = newRoleCode })
        .Where(x => x.PType == "g" && x.V1 == oldRoleCode && x.V2 == domain)
        .ExecuteCommandAsync();

    await _roleRepository._Db.Updateable<CasbinRule>()
        .SetColumns(it => new CasbinRule { V0 = newRoleCode })
        .Where(x => x.PType == "p" && x.V0 == oldRoleCode && x.V1 == domain)
        .ExecuteCommandAsync();

    // 内存同步：全量重载兜底
    Func<Task> syncAction = ReloadAllPoliciesAsync;

    IUnitOfWork? uow = _unitOfWorkManager.Current;
    if (uow != null) { uow.OnCompleted(syncAction); }
    else { await syncAction(); }
}
```

#### 写操作 9：CleanUserPoliciesAsync — 清理用户所有策略 (g)

**【关键修复】** DB 和内存双侧均保留 domain 过滤。内存侧使用"枚举+过滤+逐个删除"方案。

```csharp
public async Task CleanUserPoliciesAsync(Guid userId, Guid? tenantId)
{
    string sub = GetUserSubject(userId);
    string domain = GetTenantDomain(tenantId);

    // DB 删除（锁外，保留 domain 过滤）
    await _roleRepository._Db.Deleteable<CasbinRule>()
        .Where(x => x.PType == "g" && x.V0 == sub && x.V2 == domain)
        .ExecuteCommandAsync();

    // 内存增量同步（锁内：枚举域内规则 → 逐个精确删除）
    Func<Task> syncAction = async () =>
    {
        await _writeLock.WaitAsync();
        try
        {
            var oldRules = _enforcer.GetFilteredGroupingPolicy(0, sub);
            foreach (var rule in oldRules)
            {
                if (rule.Count() >= 3 && rule.ElementAt(2) == domain)
                    await _enforcer.RemoveGroupingPolicyAsync(rule.ElementAt(0), rule.ElementAt(1), rule.ElementAt(2));
            }
        }
        finally { _writeLock.Release(); }
    };

    IUnitOfWork? uow = _unitOfWorkManager.Current;
    if (uow != null) { uow.OnCompleted(syncAction); }
    else { await SyncOrFallback(syncAction); }
}
```

### 2.3 辅助方法

```csharp
/// <summary>
/// 无 UOW 时的一致性兜底：先尝试增量同步，失败则全量重载。
/// 覆盖后台任务、种子数据等无事务场景。
/// </summary>
private async Task SyncOrFallback(Func<Task> incrementalSync)
{
    try
    {
        await incrementalSync();
    }
    catch
    {
        // 增量失败时回退到全量重载保证最终一致
        await ReloadAllPoliciesAsync();
    }
}

/// <summary>
/// 带全局写锁的全量策略重载入口。
/// 供系统初始化、Redis Watcher 回调、运维手动触发使用。
/// </summary>
public async Task ReloadAllPoliciesAsync()
{
    await _writeLock.WaitAsync();
    try { await _enforcer.LoadPolicyAsync(); }
    finally { _writeLock.Release(); }
}
```

### 2.4 移除的旧代码

- 删除 `TriggerMemorySync()` 方法及其 `syncKey` 去重逻辑
- 删除所有 `_writeLock` 对 DB 写入的保护（锁仅保留在内存同步中）

---

## 修改 3：CasbinSeedService.cs — 统一重载入口

**文件**：`SharpFort.CasbinRbac.Domain/Managers/CasbinSeedService.cs`

构造函数注入 `ICasbinPolicyManager`，Phase 4 改用 `_casbinPolicyManager.ReloadAllPoliciesAsync()`：

```csharp
public partial class CasbinSeedService(
    IEnforcer enforcer,
    ISqlSugarRepository<Role> roleRepo,
    ICasbinPolicyManager casbinPolicyManager,
    ILogger<CasbinSeedService> logger) : DomainService
{
    // ...
}

// Phase 4:
await _casbinPolicyManager.ReloadAllPoliciesAsync();
```

---

## 修改 4：IMenuService.cs — 缓存预热契约

**文件**：`SharpFort.CasbinRbac.Application.Contracts/IServices/IMenuService.cs`

```csharp
public interface IMenuService : ISfCrudAppService<...>
{
    /// <summary>
    /// 本地高速缓存预热（尽力而为，失败不阻断启动）
    /// </summary>
    Task WarmupCacheAsync();
}
```

---

## 修改 5：MenuService.cs — 读/写全面优化

**文件**：`SharpFort.CasbinRbac.Application/Services/System/MenuService.cs`

### 5.1 缓存基础设施

```csharp
// 注入 IMemoryCache（由 ABP AbpCachingModule 自动注册）
private readonly IMemoryCache _memoryCache = memoryCache;

// 无锁原子版本号（单实例 static 即可满足需求）
private static long _menuSchemaVersion = 1;

private void InvalidateMenuCache() => Interlocked.Increment(ref _menuSchemaVersion);

private string GetCachedKeyPrefix()
{
    long v = Interlocked.Read(ref _menuSchemaVersion);
    return $"Menuv{v}:";
}
```

### 5.2 GetListAsync — 版本化缓存 + 缓存键冲突修复

```csharp
public override async Task<PagedResultDto<MenuGetListOutputDto>> GetListAsync(MenuGetListInputVo input)
{
    string keyPrefix = GetCachedKeyPrefix();
    string searchName = input.MenuName ?? "*";
    string stateKey = input.State?.ToString() ?? "all";  // 隔离 true / false / all
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
            .Where(m => SqlFunc.Subqueryable<RoleMenu>().Where(rm => rm.RoleId == roleId && rm.MenuId == m.Id).Any())
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
        return await base.GetAsync(id);
    })!;
}
```

### 5.5 CreateAsync — 创建 + 缓存失效

```csharp
public override async Task<MenuGetOutputDto> CreateAsync(MenuCreateInputVo input)
    => await CreateInternalAsync(input, invalidateCache: true);

private async Task<MenuGetOutputDto> CreateInternalAsync(MenuCreateInputVo input, bool invalidateCache)
{
    // 校验逻辑同旧版
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

### 5.6 PostImportExcelAsync — 批量导入 + 单次失效 + 异常保护

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
        InvalidateMenuCache(); // 无论如何只失效一次
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

    // A. 读旧实体（1 次 DB 读）
    Menu oldMenu = await _repository.GetByIdAsync(id)
        ?? throw new EntityNotFoundException(typeof(Menu), id);

    bool isApiChanged = oldMenu.ApiUrl != input.ApiUrl
        || (oldMenu.ApiMethod?.ToUpper(CultureInfo.InvariantCulture) ?? "")
        != (input.ApiMethod?.ToUpper(CultureInfo.InvariantCulture) ?? "");

    // B. 显式补齐权限和输入校验（绕过 base.UpdateAsync 时必须手动调用）
    await CheckUpdatePolicyAsync();
    await CheckUpdateInputDtoAsync(oldMenu, input);

    // C. 直接在已加载实体上更新（1 次 DB 写，无二次读）
    await MapToEntityAsync(input, oldMenu);
    await _repository.UpdateAsync(oldMenu, autoSave: true);
    MenuGetOutputDto result = await MapToGetOutputDtoAsync(oldMenu);

    // D. 缓存失效
    InvalidateMenuCache();

    // E. API 变更时批量刷新 Casbin 策略（共 4 次额外 DB 读）
    if (isApiChanged)
    {
        List<Guid> roleIds = await _roleMenuRepository._DbQueryable
            .Where(x => x.MenuId == id).Select(x => x.RoleId).Distinct().ToListAsync();

        if (roleIds.Count > 0)
        {
            List<Role> roles = await _roleRepository.GetListAsync(x => roleIds.Contains(x.Id));

            var mappings = await _roleMenuRepository._DbQueryable
                .Where(x => roleIds.Contains(x.RoleId))
                .Select(x => new { x.RoleId, x.MenuId }).ToListAsync();

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
    List<Guid> affectedRoleIds = await _roleMenuRepository._DbQueryable
        .Where(x => ids.Contains(x.MenuId)).Select(x => x.RoleId).Distinct().ToListAsync();

    await base.DeleteAsync(ids);
    InvalidateMenuCache();

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

## 修改 6：SharpFortCasbinRbacApplicationModule.cs — 启动预热

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

## 设计约束记录（运维参考）

| 约束 | 说明 |
|------|------|
| 单实例缓存 | `_menuSchemaVersion` 为进程内 `static` 字段，仅对当前实例有效。多实例部署时各实例独立维护版本号，缓存失效不同步。若未来启用多实例，需通过 Redis pub/sub 或 ABP 分布式事件总线广播失效通知。 |
| Redis Watcher 兼容性 | 增量更新不经过 Adapter 的 `SavePolicy`，因此 Redis Watcher 不会收到通知。当前 `EnableRedisWatcher: false` 无影响。若未来开启多实例 Redis Watcher，需在 `syncAction` 执行后显式调用 `watcher.Update()` 通知其他实例。 |

---

## 验证计划

### 读性能

- `GetListAsync` 首次调用：10-15ms（DB 查询 + 缓存填充）
- `GetListAsync` 重复调用：<0.1ms（IMemoryCache 命中）
- `GetAsync` 同上

### 写性能

- `UpdateAsync`（无 API 变更）：3-5ms（1 读 + 1 写 + 权限校验）
- `UpdateAsync`（API 变更 + 多角色）：<15ms（1 读 + 1 写 + 4 批量读 + 增量内存同步）

### 一致性

- 写操作后立即读：缓存已失效，首次读回源 DB，数据一致
- 批量导入异常：finally 块保证缓存失效，最终一致
- 无 UOW 路径：`SyncOrFallback` 兜底，增量失败则全量重载
