# Casbin-RBAC 审核答疑（第三批 — Q23-Q32）

> 日期：2026-05-19
> 基于：CODE_AUDIT_REPORT.md（V2）+ CODE_AUDIT_REPORT_V3.md（V3）

---

## Q23: B-01 登录拦截逻辑与注释矛盾——新用户注册后如何授予"默认角色"？

### 当前链路

```
注册 → AccountManager.RegisterAsync()
  → UserManager.CreateAsync() → 发布 UserCreateEventArgs
    → UserInfoHandler → SetDefautRoleAsync()
      → 查询 roleCode == "default" 的角色
      → GiveUserSetRoleAsync() → 分配角色 + Casbin 同步
```

然后用户登录时：
```
AccountManager.GetTokenByUserIdAsync()
  → 检查 RoleCodes.Count == 0 → 抛异常 "LOGIN_ERR_004"
  → 检查 PermissionCodes.Count == 0 → 抛异常 "LOGIN_ERR_005"
```

**问题**：即使 `SetDefautRoleAsync` 成功分配了默认角色，如果该角色**没有关联任何菜单**（即 `PermissionCodes` 为空），登录仍然被拦截。

### 解决方案

#### 架构策略

```
┌─────────────────────────────────────────────────┐
│ 角色体系                                         │
│  sys-admin   — 超级管理员（全权限，不可删除）        │
│  developer   — 开发/运营（管理员分配）               │
│  common      — 普通用户（注册默认获得）              │
│  ...其他自定义角色                                  │
└─────────────────────────────────────────────────┘
```

#### Step 1 — 修改登录拦截逻辑

**文件**: `AccountManager.cs:58-66`

```csharp
// 修改前：
// 临时注释，允许无角色/无权限用户登录
if (userInfo.RoleCodes.Count == 0)
{
    throw new UserFriendlyException(UserConst.No_Role, code: "LOGIN_ERR_004");
}
if (userInfo.PermissionCodes.Count == 0)
{
    throw new UserFriendlyException(UserConst.No_Permission, code: "LOGIN_ERR_005");
}

// 修改后：
// 允许无角色/无权限用户登录（他们只能看到基础UI，无法操作任何业务接口）
// Casbin 中间件会自动拦截所有未授权的 API 请求
// 注：如果确实需要阻止完全无权限的用户登录，取消下方注释
// if (userInfo.RoleCodes.Count == 0)
// {
//     throw new UserFriendlyException(UserConst.No_Role, code: "LOGIN_ERR_004");
// }
```

**理由**：安全兜底由 Casbin 中间件提供。即使登录成功，没有 Casbin 策略的用户访问任何业务 API 都会收到 403。

#### Step 2 — 确保"默认角色"存在并拥有基础菜单

**数据库种子**（或首次部署时手动配置）：

```sql
-- 确保存在 "default" 角色
INSERT INTO casbin_sys_role (id, role_code, role_name, data_scope, state, order_num)
VALUES (gen_random_uuid(), 'default', '普通用户', 5, true, 999)
ON CONFLICT (role_code) DO NOTHING;
```

**给 default 角色分配基础菜单**（通过管理后台操作）：
- `/api/app/account`（获取个人信息）
- `/api/app/account/logout`（退出登录）
- `/api/app/account/update-password`（修改密码）
- `/api/app/account/vue3router`（前端路由）

这些是**所有登录用户都需要的端点**，不分配则前端无法正常工作。

#### Step 3 — 区分"管理员创建的用户"

管理员通过 `UserService.CreateAsync` 创建用户时，**不调用** `SetDefautRoleAsync`，而是由管理员在创建后手动分配角色。这已由当前代码支持（`GiveUserSetRoleAsync`）。

**建议**：`UserService.CreateAsync` 中增加一个参数区分"管理员创建"和"注册创建"：
- 管理员创建 → 不自动分配默认角色
- 注册创建 → 自动分配默认角色（已实现）

---

## Q24: B-02 多租户迁移——按租户数量/开关判断 domain

### 分析

当前配置：`"EnabledSaasMultiTenancy": true`，但实际仅一个租户。

### 推荐方案：始终使用 tenant_id，null 时 fallback "default"

**不需要判断租户数量**。直接以 `tenant_id` 区分域，逻辑最简洁：

```
tenant_id == null  →  domain = "default"
tenant_id != null  →  domain = tenant_id.ToString()
```

这与 `CasbinPolicyManager.GetTenantDomain` 已有的逻辑完全一致：

```csharp
// CasbinPolicyManager.cs:36-40（已有逻辑）
private string GetTenantDomain(Guid? tenantId)
{
    Guid? finalTenantId = tenantId ?? _currentTenant.Id;
    return finalTenantId?.ToString() ?? "default";
}
```

**修复 CasbinSeedService 时，复用这个逻辑即可**。单租户场景下 `tenant_id` 为 null，自然 fallback 到 `"default"`，与现有行为兼容。

---

## Q25: B-03 Casbin 模型中 sys-admin 硬编码——启用配置项 + 是否需同时出现在两处？

### 先回答"是否需同时出现"

**不需要**。两者是独立机制：

| 位置 | 作用 | 独立于 |
|------|------|--------|
| 模型 Matcher: `g(r.sub, "sys-admin", r.dom)` | **快速路径**：跳过所有策略匹配，直接放行 | 策略表 |
| 策略表: `p, sys-admin, default, *, *` | **策略匹配**：通过正常 Matcher 路径匹配 | 模型 |

二者是 OR 关系（`||`）。**移除模型的快速路径后，策略表中的 `*, *` 通配符策略仍然生效**——它走正常 Matcher 路径 `keyMatch2(r.obj, "*")` 匹配所有资源。

### 换句话说

当前模型：
```
m = g(r.sub, "sys-admin", r.dom)  ← 硬编码快速路径
    ||
    (g(r.sub, p.sub, r.dom) && r.dom == p.dom && keyMatch2(r.obj, p.obj) && ...)
    ← 策略匹配（通配符 * 在这里生效）
```

移除快速路径后：
```
m = (g(r.sub, p.sub, r.dom) && r.dom == p.dom && keyMatch2(r.obj, p.obj) && ...)
```

只要策略表中有 `p, sys-admin, default, *, *`，`keyMatch2(r.obj, "*")` 匹配任意请求路径，效果等价。

### 修复方案

**Step 1 — 模型动态化**

将模型文件改为构建时注入配置值。在 `SharpFortCasbinRbacSqlSugarCoreModule` 注册 Enforcer 时：

```csharp
// 读取模型文件内容
string modelContent = File.ReadAllText(modelPath);

// 从配置读取超管角色代码
string superAdminRoleCode = casbinOptions.SuperAdminRoleCode ?? "sys-admin";

// ★ 如果配置了快速路径（可选启用）
if (casbinOptions.EnableSuperAdminBypass)  // 新增配置项
{
    // 替换占位符
    modelContent = modelContent.Replace("${SuperAdminRoleCode}", superAdminRoleCode);
}

// 使用字符串创建 Enforcer（而非文件路径）
Enforcer enforcer = new(DefaultModel.CreateFromText(modelContent), adapter);
```

**模型文件** 改为：
```
[matchers]
# 超管快速路径（由构建时注入 SuperAdminRoleCode，若未配置则此行为注释）
# m = g(r.sub, "${SuperAdminRoleCode}", r.dom) || (正常路径...)

# 标准路径：全部走策略表匹配
m = (g(r.sub, p.sub, r.dom) && r.dom == p.dom && keyMatch2(r.obj, p.obj) && (r.act == p.act || p.act == "*"))
```

**Step 2 — `CasbinOptions` 增加开关**：

```csharp
public class CasbinOptions
{
    public string SuperAdminRoleCode { get; set; } = "sys-admin";
    
    /// <summary>
    /// 是否启用 Casbin 模型中的超级管理员快速路径。
    /// 启用后超管跳过策略匹配（性能更优，但硬编码了角色码）。
    /// 禁用后超管权限完全由 casbin_rule 表中的 *, * 通配符策略控制。
    /// 默认: false（推荐）
    /// </summary>
    public bool EnableSuperAdminBypass { get; set; } = false;
}
```

---

## Q26: B-04 模型 Matcher 冗余快速路径——改动是否太大？

### 我需要说服你，这恰恰是**最安全**的改动

#### "Casbin 很强大，也很脆弱" ——你说得对

Casbin 的 Matcher 是权限系统的**灵魂**。改错了，整个系统的权限全部失效。

#### 但当前硬编码的快速路径才是真正脆弱的地方

**场景 1：有人误改了 seed 数据**
```
当前：sys-admin 角色被误删除了 role_code
→ 模型快速路径 g(r.sub, "sys-admin", r.dom)
→ 匹配的是 ROLE CODE，不是角色 ID
→ seed 数据改了 role_code → 快速路径失效 → 超管被锁
→ 而且排查困难：模型文件里藏着 "sys-admin"，谁会想到去那里查？
```

**场景 2：需要重命名超管角色**
```
需求：把 sys-admin 改名为 super-admin
→ 需要改：模型文件 + 数据库 seed + 配置 + 前端代码
→ 漏改任何一处 → 权限系统出现不可调试的异常
```

#### 只用策略表 `*, *` 管理超管——更安全的理由

1. **权限统一入口**：所有权限（包括超管）都在 casbin_rule 表中，`SELECT * FROM casbin_rule WHERE v0 = 'sys-admin'` 一条 SQL 看清全部
2. **可调试**：`_enforcer.EnforceAsync()` 调试时可以看到完整的匹配路径
3. **可回滚**：误操作了超管权限 → 修改策略表 → 调用 `ReloadPolicy` → 秒级恢复
4. **可审计**：操作日志记录在 casbin_rule 的变更历史中

#### 安全兜底

**我们保留被注释的原代码**。模型文件中：
```ini
# === 备份：硬编码快速路径（如策略表损坏导致超管被锁，取消注释并重启） ===
# m = g(r.sub, "sys-admin", r.dom) || (g(r.sub, p.sub, r.dom) && ...)

# === 当前生效：纯策略驱动 ===
m = (g(r.sub, p.sub, r.dom) && r.dom == p.dom && keyMatch2(r.obj, p.obj) && (r.act == p.act || p.act == "*"))
```

**最坏情况恢复**：策略表被清空 → 超管无法登录 → SSH 到服务器 → 取消模型文件中的注释 → 重启 → 超管恢复 → 重建策略表 → 重新注释。

这个紧急恢复流程比调试一个硬编码在模型文件里的角色码要简单得多。

#### 结论

**这不是"大改动"，而是"去掉冗余"**。`InitAdminPermissionAsync` 已经写好了 `*, *` 策略。模型中的快速路径只是重复做了同一件事，而且方式更不透明。**去掉它，系统反而更健壮**。

---

## Q27: B-05 RoleCode 修改时缺少事务原子性——修复

### 问题

```csharp
// RoleService.cs:128-135
if (oldRoleCode != entity.RoleCode)
{
    await _casbinPolicyManager.CleanRolePoliciesByRoleCodeAsync(oldRoleCode, entity.TenantId);
    // ↑ Casbin DB 操作：DELETE FROM casbin_rule
}

await _repository.UpdateAsync(entity);
// ↑ 业务 DB 操作：UPDATE casbin_sys_role

await _roleManager.GiveRoleSetMenuAsync([id], input.MenuIds ?? []);
// ↑ Casbin DB 操作：DELETE + INSERT INTO casbin_rule
```

如果 `GiveRoleSetMenuAsync` 失败，Casbin 旧策略已删除，新策略未插入 → **角色权限丢失**。

### 修复方案

**调整执行顺序 + 增加中间变量**：

```csharp
public override async Task<RoleGetOutputDto> UpdateAsync(Guid id, RoleUpdateInputVo input)
{
    Role entity = await _repository.GetByIdAsync(id);

    bool isExist = await _repository._DbQueryable
        .Where(x => x.Id != entity.Id)
        .Where(x => x.TenantId == entity.TenantId)  // ★ B-07 修复一起做
        .AnyAsync(x => x.RoleCode == input.RoleCode || x.RoleName == input.RoleName);
    if (isExist)
    {
        throw new UserFriendlyException(RoleConst.Exist);
    }

    string oldRoleCode = entity.RoleCode!;

    await MapToEntityAsync(input, entity);

    // ★ Step 1: 先更新业务表（失败则全部回滚）
    await _repository.UpdateAsync(entity);

    // ★ Step 2: 再处理 Casbin 策略
    // 先设置新策略（SetRolePermissionsAsync 会清理旧的 p 规则并插入新的）
    await _roleManager.GiveRoleSetMenuAsync([id], input.MenuIds ?? []);

    // ★ Step 3: 如果 RoleCode 变更了，清理以旧 RoleCode 为标识的残留策略
    if (oldRoleCode != entity.RoleCode)
    {
        await _casbinPolicyManager.CleanRolePoliciesByRoleCodeAsync(oldRoleCode, entity.TenantId);
    }

    // 全部成功 → UOW 提交（ABP 自动管理）

    RoleGetOutputDto dto = await MapToGetOutputDtoAsync(entity);
    return dto;
}
```

**关键变化**：
1. 业务表更新放在最前面（它是最关键的）
2. Casbin 策略**先设置新的再清理旧的**（避免"删了旧的但新的没插进去"的窗口期）
3. ABP UOW 自动包裹整个方法，异常时全部回滚

---

## Q28: B-06 Casbin 大小写一致性问题——最优方案

### 为什么不能"强制所有接口小写"

ASP.NET Core 的路由匹配本身就是**大小写不敏感**的（除非配置了 `LowercaseUrls = false`）。所以 `/api/User` 和 `/api/user` 都能路由到同一个 Controller。

但 Casbin 的 `keyMatch2` 是**大小写敏感**的。所以即使请求到达了 Controller，Casbin 中间件可能在前面就返回了 403。

### 最优方案：中间件统一转小写（对开发者透明）

**文件**: `CasbinAuthorizationMiddleware.cs:76-81`

```csharp
// 修改前：
// string? obj = path; //.ToLower(); // Decided to keep case for now

// 修改后：
// ★ 统一转小写以确保 keyMatch2 匹配一致性
// API 路径在 ASP.NET Core 中大小写不敏感，但 Casbin keyMatch2 大小写敏感
string? obj = path?.ToLowerInvariant();
```

同时在数据库侧确保 `ApiUrl` 存储为小写：

**文件**: `MenuService.cs:35-43`（CreateAsync 中已有 ApiMethod 转大写逻辑，补充 ApiUrl 转小写）

```csharp
// ★ ApiUrl 统一转小写
if (!string.IsNullOrWhiteSpace(input.ApiUrl))
{
    input.ApiUrl = input.ApiUrl.ToLowerInvariant();
}
```

### 为什么这是最优方案

| 方案 | 优点 | 缺点 |
|------|------|------|
| 中间件转小写（推荐） | 对前端/开发者透明，零侵入 | 需要同时规范数据库存储 |
| 自定义 keyMatch2 函数 | Casbin 侧控制 | 需要修改模型文件，维护成本高 |
| 强制端点小写 | ASP.NET 原生支持 | 改变现有路由行为，可能影响前端 |
| 不做修复 | 零改动 | Linux 部署时 `api/User` ≠ `api/user` |

### 不需要"大写时报错"

因为 ASP.NET Core 的路由系统本身接受大小写混合的请求，在中间件层静默转小写即可，不需要额外的警告或报错。

---

## Q29: B-07 跨租户唯一性检查——修复

### 问题

```csharp
// RoleService.cs:85
bool isExist = await _repository.IsAnyAsync(x =>
    x.RoleCode == input.RoleCode || x.RoleName == input.RoleName);
// ↑ 缺少 TenantId 过滤
```

### 修复

**CreateAsync**：
```csharp
// 修改前：
bool isExist = await _repository.IsAnyAsync(x =>
    x.RoleCode == input.RoleCode || x.RoleName == input.RoleName);

// 修改后：
Guid? currentTenantId = CurrentTenant.Id;
bool isExist = await _repository._DbQueryable
    .Where(x => x.TenantId == currentTenantId)  // ★ 加租户过滤
    .AnyAsync(x => x.RoleCode == input.RoleCode || x.RoleName == input.RoleName);
```

**UpdateAsync**（行 117，同样问题）：
```csharp
// 修改前：
bool isExist = await _repository._DbQueryable
    .Where(x => x.Id != entity.Id)
    .AnyAsync(x => x.RoleCode == input.RoleCode || x.RoleName == input.RoleName);

// 修改后：
bool isExist = await _repository._DbQueryable
    .Where(x => x.Id != entity.Id)
    .Where(x => x.TenantId == entity.TenantId)  // ★ 加租户过滤
    .AnyAsync(x => x.RoleCode == input.RoleCode || x.RoleName == input.RoleName);
```

---

## Q30: B-08 用户删除时策略清理路径不一致——详细解释

### 当前代码

```csharp
// UserService.cs:264-271
public override async Task DeleteAsync(Guid id)
{
    // ★ 路径 A（旧式）：直接操作 Enforcer
    await _enforcer.RemoveFilteredGroupingPolicyAsync(0, id.ToString());  // 仅内存
    await _enforcer.SavePolicyAsync();  // 全量保存到 DB

    await base.DeleteAsync(id);
}
```

### 与之对比的标准路径

```csharp
// CasbinPolicyManager.SetUserRolesAsync —— 路径 B（标准式）
public async Task SetUserRolesAsync(User user, List<Role> roles)
{
    // 1. DB: 精确删除 + 批量插入
    await _roleRepository._Db.Deleteable<CasbinRule>()
        .Where(x => x.PType == "g" && x.V0 == sub && x.V2 == domain)
        .ExecuteCommandAsync();
    await _roleRepository._Db.Insertable(newRules).ExecuteCommandAsync();

    // 2. 内存: RemoveGroupingPolicy + AddGroupingPolicies
    foreach (string r in oldRoles) { await _enforcer.RemoveGroupingPolicyAsync(...); }
    await _enforcer.AddGroupingPoliciesAsync(policies);

    // 3. TriggerMemorySync
}
```

### 两条路径差异

| 维度 | 路径 A（旧） | 路径 B（标准） |
|------|------------|--------------|
| DB 删除 | ❌ 无（只删内存） | ✅ `Deleteable<CasbinRule>().Where(...)` |
| DB 保存 | `SavePolicyAsync()`（全量） | `Insertable()`（精确插入） |
| 内存操作 | `RemoveFilteredGroupingPolicyAsync` | `RemoveGroupingPolicyAsync` |
| 一致性 | 差（全量保存可能覆盖其他并发操作） | 好（精确操作） |

### 最优方案

为 `CasbinPolicyManager` 新增 `CleanUserPoliciesAsync` 方法，然后在 `UserService.DeleteAsync` 中调用它：

**Step 1 — ICasbinPolicyManager 新增接口**：

```csharp
/// <summary>清理用户所有的 Casbin 策略（删除用户时调用）</summary>
Task CleanUserPoliciesAsync(Guid userId, Guid? tenantId);
```

**Step 2 — CasbinPolicyManager 新增实现**：

```csharp
public async Task CleanUserPoliciesAsync(Guid userId, Guid? tenantId)
{
    string sub = GetUserSubject(userId);
    string domain = GetTenantDomain(tenantId);

    // 1. 持久化：删除该用户的所有 g 规则
    await _roleRepository._Db.Deleteable<CasbinRule>()
        .Where(x => x.PType == "g" && x.V0 == sub && x.V2 == domain)
        .ExecuteCommandAsync();

    // 2. 内存更新
    IEnumerable<string> roles = _enforcer.GetRolesForUserInDomain(sub, domain);
    foreach (string r in roles)
    {
        await _enforcer.RemoveGroupingPolicyAsync(sub, r, domain);
    }

    // 3. 触发同步（如果 Q17 已实施则此行不执行 LoadPolicy）
    TriggerMemorySync();
}
```

**Step 3 — UserService 改为统一路径**：

```csharp
public override async Task DeleteAsync(Guid id)
{
    // ★ 使用标准路径
    User? user = await _repository.GetByIdAsync(id);
    if (user != null)
    {
        await _casbinPolicyManager.CleanUserPoliciesAsync(user.Id, user.TenantId);
    }

    await base.DeleteAsync(id);
}
```

这样 `UserService` 不再直接依赖 `IEnforcer`，可以移除该注入。

---

## Q31: B-09 注册用户名 ls_ 前缀限制下沉到领域层

### 为什么是问题

```csharp
// AccountService.cs:318（Application 层）
if (input.UserName.StartsWith("ls_", StringComparison.Ordinal))
{
    throw new UserFriendlyException("注册账号不能以ls_字符开头");
}
```

`ls_` 前缀限制是**领域规则**（哪些用户名合法），不属于应用层逻辑。应用层负责调用，领域层负责规则校验。

### 为什么"ls_"

`ls_` 大概率是第三方登录（OAuth）自动生成的临时用户名前缀。OAuth 登录成功但本地没有对应用户时，系统自动创建一个 `ls_<openid>` 的临时账号。然后用户可以通过"绑定"操作与原账号合并。如果用户自己注册了 `ls_xxx`，会和 OAuth 自动创号冲突。

### 修复方案

**Step 1 — 下沉到 `UserManager.ValidateUserName`**（领域层）：

```csharp
// UserManager.cs:142-161
private static void ValidateUserName(User input)
{
    if (input.UserName is UserConst.Admin or UserConst.TenantAdmin)
    {
        throw new UserFriendlyException("用户名无效注册！");
    }

    // ★ 新增：第三方登录预留前缀
    if (input.UserName!.StartsWith("ls_", StringComparison.Ordinal))
    {
        throw new UserFriendlyException("注册账号不能以ls_字符开头");
    }

    if (input.UserName!.Length < 2)
    {
        throw new UserFriendlyException("账号名需大于等于2位！");
    }

    // ★ 将 "ls_" 也定义为常量
    string pattern = @"^[a-zA-Z0-9_]+$";
    bool isMatch = Regex.IsMatch(input.UserName, pattern);
    if (!isMatch)
    {
        throw new UserFriendlyException("用户名不能包含除【字母】与【数字】与【_】的其他字符");
    }
}
```

**Step 2 — `UserConst` 中定义常量**：

```csharp
public class UserConst
{
    // ... 现有常量 ...

    /// <summary>第三方登录临时账号前缀</summary>
    public const string OAuthTempPrefix = "ls_";
}
```

**Step 3 — 从 `AccountService` 中移除**：

删除 `AccountService.cs:317-320` 的 `StartsWith("ls_")` 检查（因为 `RegisterAsync` → `UserManager.CreateAsync` 内部已调用 `ValidateUserName`）。

---

## Q32: `[Authorize(Roles = "admin")]` 能否放入数据库/Setting Management？

### 短答案

**已经不需要了**。根据 Q6 的结论，迁移接口应该走 Casbin 策略表控制，而不是 `[Authorize(Roles = ...)]`。

### 详细分析

#### 为什么 `[Authorize(Roles = ...)]` 不能简单地放进数据库

`[Authorize(Roles = "sys-admin")]` 中的角色检查由 **ASP.NET Core 的 `UseAuthorization()` 中间件**执行，它通过 JWT Claims 中的 Role Claim 来验证。

这段逻辑发生在 Casbin 执行**之前**（在管道末尾的 `UseAuthorization()`）。要将它"配置化"，有两个选择：

| 方案 | 实现 | 复杂度 |
|------|------|--------|
| ABP Setting Management | 自定义 `IAuthorizationHandler`，从 ISettingProvider 读取允许的角色 | 中高 |
| Casbin 策略表（推荐） | 不加 `[Authorize(Roles = ...)]`，让 Casbin 决定谁能访问 | 低 |

#### 推荐：走 Casbin 策略表（与 Q6 一致）

```csharp
// CasbinMigrationService.cs:26
// [AllowAnonymous]  ← 删除
// [Authorize(Roles = "sys-admin")]  ← 不需要加
public async Task<object> MigrateAllAsync()
```

**权限控制在数据库**：

```sql
-- 仅 sys-admin 角色可访问迁移接口
INSERT INTO casbin_rule (ptype, v0, v1, v2, v3)
VALUES ('p', 'sys-admin', 'default', '/api/app/casbin-migration/migrate-all', 'POST');
```

**好处**：
1. 不需要改代码（只删一行 `[AllowAnonymous]`）
2. sys-admin 角色由 `SuperAdminRoleCode` 配置控制
3. 权限可在管理后台可视化配置
4. 不引入 `[Authorize(Roles = ...)]` 这种双轨制

**安全验证**：如果没有 casbin_rule 策略，即使已登录用户访问该接口，Casbin 中间件也会返回 403。**这与"仅限超级管理员"的安全目标完全一致**。

---

## 附：Q23-Q32 问题状态总览

| Q | V2/V3 编号 | 结论 |
|---|-----------|------|
| Q23 | B-01 | 移除登录拦截 + 确保 default 角色有基础菜单 |
| Q24 | B-02 | 始终用 `tenantId?.ToString() ?? "default"`，无需判断租户数量 |
| Q25 | B-03 | sys-admin **不需要**同时出现在模型和策略表。启用 `SuperAdminRoleCode` 配置项，模型中去掉硬编码 |
| Q26 | B-04 | 去掉快速路径反而更安全——权限统一、可调试、可回滚。保留注释兜底 |
| Q27 | B-05 | 调整执行顺序：先业务 UPDATE → 再 Casbin 写新策略 → 最后清理旧策略 |
| Q28 | B-06 | 中间件 `path.ToLowerInvariant()` + DB 存储小写，对开发者透明 |
| Q29 | B-07 | CreateAsync/UpdateAsync 均加 `TenantId` 过滤 |
| Q30 | B-08 | 新增 `CasbinPolicyManager.CleanUserPoliciesAsync`，UserService 不再直接操 Enforcer |
| Q31 | B-09 | 下沉到 `UserManager.ValidateUserName`，定义 `UserConst.OAuthTempPrefix` |
| Q32 | V3 S-01 | 不加 `[Authorize(Roles = ...)]`，走 Casbin 策略表控制（与 Q6 一致） |
