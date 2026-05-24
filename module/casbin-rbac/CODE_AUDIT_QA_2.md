# Casbin-RBAC 审核答疑（第二批 — Q9-Q22）

> 日期：2026-05-19
> 基于：CODE_AUDIT_REPORT.md（V2）+ CODE_AUDIT_REPORT_V3.md（V3）

---

## Q9: ABP UOW 与 SqlSugar Casbin 操作的事务隔离风险——如何解决？

### 先验证，再修

这个问题的根因取决于：**`_roleRepository._Db` 返回的 `SqlSugarClient` 是否与 ABP UOW 共享同一个数据库连接和事务**。

#### 验证步骤

在 `CasbinPolicyManager` 任意写方法中加临时日志：

```csharp
// 验证代码（确认后删除）
IUnitOfWork? uow = _unitOfWorkManager.Current;
var db = _roleRepository._Db;
_logger.LogInformation(
    "UOW TransactionId: {UowId}, Db ClientHash: {DbHash}, Db Ado: {AdoHash}",
    uow?.Id,
    db.GetHashCode(),
    db.Ado.GetHashCode());
```

**判断标准**：
- 如果 `_roleRepository._Db` 的 `SqlSugarClient` 是同一个**SqlSugarScope**实例（Singleton），且 UOW 也通过该 Scope 管理连接 → **天然安全**，无需修复
- 如果每次调用产生新的 Client/Connection → 需要修复

#### 如果是第一种情况（大概率）

ABP + SqlSugar 集成中，`ISqlSugarDbContext.SqlSugarClient` 通常返回 `SqlSugarScope`（Singleton），UOW 通过 Scope 的 `BeginTran()`/`CommitTran()` 管理事务。`CasbinPolicyManager` 中使用 `_roleRepository._Db` 做 `Insertable/Deleteable` 操作会**自动复用当前线程的事务**。

**结论：无需修复**。仅确认并关闭此问题。

#### 如果是第二种情况（小概率）

在 `CasbinPolicyManager` 注入 `ISqlSugarDbContext` 直接获取 Client，或通过 `IUnitOfWorkManager` 的 `OnCompleted` 注册 Casbin DB 操作：

```csharp
public class CasbinPolicyManager(
    IEnforcer enforcer,
    IUnitOfWorkManager unitOfWorkManager,
    ISqlSugarRepository<Role> roleRepository,
    ISqlSugarDbContext dbContext,       // ★ 直接注入 DbContext
    ICurrentTenant currentTenant) : DomainService, ICasbinPolicyManager
{
    // 使用 dbContext.SqlSugarClient 替代 _roleRepository._Db
}
```

---

## Q10: JWT 密钥迁移到环境变量

### 方案

**Step 1 — 修改 `appsettings.json`**：移除硬编码值，保留占位符：

```json
"JwtOptions": {
    "Issuer": "https://ccnetcore.com",
    "Audience": "https://ccnetcore.com",
    "SecurityKey": "",
    "ExpiresMinuteTime": 86400
},
"RefreshJwtOptions": {
    "Issuer": "https://yi.ccnetcore.com",
    "Audience": "https://yi.ccnetcore.com",
    "SecurityKey": "",
    "ExpiresMinuteTime": 172800
}
```

**Step 2 — 环境变量**（开发环境 `.env` 或 launchSettings，生产环境 docker/k8s secrets）：

```bash
JwtOptions__SecurityKey=<随机生成64位以上密钥>
RefreshJwtOptions__SecurityKey=<另一个随机64位以上密钥>
```

ASP.NET Core 的 Configuration 系统自动将环境变量 `JwtOptions__SecurityKey` 映射到 `JwtOptions:SecurityKey`（双下划线 = 层级分隔）。

**Step 3 — 生成安全随机密钥**（PowerShell）：

```powershell
# 生成 512-bit 随机密钥
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
```

**Step 4 — 启动时校验**（在 `SharpFortCasbinRbacDomainModule.ConfigureServices` 或启动检查中）：

```csharp
string? jwtKey = configuration["JwtOptions:SecurityKey"];
if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT SecurityKey 未配置或长度不足。请设置环境变量 JwtOptions__SecurityKey。");
}
```

---

## Q11: 默认管理员密码——是否还在使用？

### 验证结论：**已废弃，可安全移除**

#### 证据

**1. `UserDataSeed.cs`（种子数据）— 整文件被注释**

```csharp
// 文件: SharpFort.CasbinRbac.SqlSugarCore/DataSeeds/UserDataSeed.cs
// 全部 92 行代码均被 // 注释，包括：
// EncryPassword = new EncryPasswordValueObject(_options.AdminPassword),  // 行31
// EncryPassword = new EncryPasswordValueObject("123456"),                 // 行51
```

**2. `RoleDataSeed.cs`（种子数据）— 整文件被注释**

```csharp
// 全部 80 行代码均被 // 注释
```

**3. `AdminPassword` 唯一引用点**

| 位置 | 状态 |
|------|------|
| `RbacOptions.cs:8` | 属性定义（含默认值 `"123456"`） |
| `appsettings.json:98` | 配置值 `"123456"` |
| `UserDataSeed.cs:31` | **已注释** |
| `test/` 目录 | 测试配置 |

**4. 当前管理员创建路径**

管理员用户是通过前端界面手动创建的（`UserService.CreateAsync` → 调用 `entitiy.SetPassword(input.Password)`），密码由创建者输入。`AdminPassword` 没有参与任何运行时逻辑。

#### 关于 Swagger 登录

Swagger 使用的是 JWT Bearer 认证（`SfTokenAuthorizationFilter`），用户先在登录接口 `/api/app/account/login` 获取 Token，然后在 Swagger 中填入 Token。**与 `AdminPassword` 无关**。

#### 建议操作

```csharp
// RbacOptions.cs — 标记为废弃
/// <summary>
/// [已废弃] 超级管理员默认密码。
/// 当前管理员通过 UI 创建，密码由操作者指定。此字段不再使用。
/// </summary>
[Obsolete("管理员通过 UI 创建，密码由操作者输入，此字段不再使用")]
public string AdminPassword { get; set; } = "123456";

// TenantAdminPassword 同样：
[Obsolete("同上")]
public string TenantAdminPassword { get; set; } = "123456";
```

appsettings.json 中直接删除对应行或注释掉。

---

## Q12: JWT 无黑名单——能否用内存实现？如何在 Casbin 中间件中增加 JTI 校验？

### 现状

`PostLogout()` 仅清除用户缓存（`_userCache.RemoveAsync`）。JWT Token 本身没有任何撤销机制，Token 在有效期内始终可用（最长 60 天）。且当前 `CreateToken` 方法**没有颁发 JTI**。

### 方案：内存级 JWT 黑名单

#### Step 1 — 颁发 Token 时添加 JTI

**文件**: `AccountManager.cs:83-98`

```csharp
private string CreateToken(List<KeyValuePair<string, string>> kvs)
{
    SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_jwtOptions.SecurityKey));
    SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);
    List<Claim> claims = [.. kvs.Select(x => new Claim(x.Key, x.Value.ToString()))];

    // ★ 新增 JTI（JWT ID）用于黑名单
    string jti = Guid.NewGuid().ToString("N");
    claims.Add(new Claim(JwtRegisteredClaimNames.Jti, jti));

    JwtSecurityToken token = new(
       issuer: _jwtOptions.Issuer,
       audience: _jwtOptions.Audience,
       claims: claims,
       expires: DateTime.Now.AddMinutes(_jwtOptions.ExpiresMinuteTime),
       notBefore: DateTime.Now,
       signingCredentials: creds);
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

#### Step 2 — 内存黑名单服务

**新建文件**: `SharpFort.CasbinRbac.Domain/Authorization/JwtBlacklist.cs`

```csharp
using System.Collections.Concurrent;

namespace SharpFort.CasbinRbac.Domain.Authorization
{
    /// <summary>
    /// 内存级 JWT 黑名单。
    /// 注意：仅适用于单实例部署。多实例请改用 Redis。
    /// </summary>
    public class JwtBlacklist
    {
        // Key: jti, Value: 过期时间（UTC）
        private readonly ConcurrentDictionary<string, DateTime> _blacklist = new();

        /// <summary>将 Token 加入黑名单</summary>
        /// <param name="jti">JWT ID</param>
        /// <param name="tokenExpiresAt">Token 原始过期时间</param>
        public void Revoke(string jti, DateTime tokenExpiresAt)
        {
            _blacklist[jti] = tokenExpiresAt;
            // 定期清理过期条目（每 100 次调用触发一次）
            if (_blacklist.Count % 100 == 0)
            {
                CleanupExpired();
            }
        }

        /// <summary>检查 JTI 是否在黑名单中</summary>
        public bool IsRevoked(string jti)
        {
            if (_blacklist.TryGetValue(jti, out DateTime expiresAt))
            {
                if (DateTime.UtcNow < expiresAt)
                {
                    return true; // 仍在黑名单中
                }
                // 已过期，移除
                _blacklist.TryRemove(jti, out _);
            }
            return false;
        }

        private void CleanupExpired()
        {
            DateTime now = DateTime.UtcNow;
            foreach (KeyValuePair<string, DateTime> entry in _blacklist)
            {
                if (now >= entry.Value)
                {
                    _blacklist.TryRemove(entry.Key, out _);
                }
            }
        }
    }
}
```

#### Step 3 — 注册为 Singleton

**文件**: `SharpFortCasbinRbacDomainModule.cs`

```csharp
context.Services.AddSingleton<JwtBlacklist>();
```

#### Step 4 — 登出时拉黑

**文件**: `AccountService.cs:430-443`

```csharp
public async Task<bool> PostLogout()
{
    Guid? userId = _currentUser.Id;
    if (userId is null) return false;

    // ★ 拉黑当前 Token
    string? jti = _currentUser.FindClaim(JwtRegisteredClaimNames.Jti)?.Value;
    if (!string.IsNullOrEmpty(jti))
    {
        // 从 JWT claims 获取过期时间
        string? expClaim = _currentUser.FindClaim("exp")?.Value;
        if (expClaim != null)
        {
            DateTime expTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).UtcDateTime;
            _jwtBlacklist.Revoke(jti, expTime);
        }
    }

    await _userCache.RemoveAsync(new UserInfoCacheKey(userId.Value));
    return true;
}
```

#### Step 5 — Casbin 中间件增加 JTI 校验（无需新中间件）

**文件**: `CasbinAuthorizationMiddleware.cs:26-107`

在步骤 2（认证检查）之前插入 JTI 检查：

```csharp
public async Task InvokeAsync(HttpContext context, RequestDelegate next)
{
    string? path = context.Request.Path.Value;

    // 0. IgnoreUrls 检查（不变）
    // ...

    // 1. AllowAnonymous 检查（不变）
    // ...

    // ★ 1.5 JWT 黑名单检查（在认证检查之前）
    if (_currentUser.IsAuthenticated)
    {
        string? jti = _currentUser.FindClaim(JwtRegisteredClaimNames.Jti)?.Value;
        if (!string.IsNullOrEmpty(jti) && _jwtBlacklist.IsRevoked(jti))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers["X-Token-Revoked"] = "true";
            return;
        }
    }

    // 2. 认证检查（不变）
    // ...
}
```

#### 权衡

| 维度 | 内存方案 | Redis 方案 |
|------|---------|-----------|
| 部署要求 | 单实例 | 多实例 |
| 重启影响 | 黑名单丢失（可接受——用户重登即可） | 持久化 |
| 复杂度 | 极低 | 需 Redis 服务 |
| 当前适用 | ✅ 开发/单机部署 | 生产多实例 |

---

## Q13: EnableCachedEnforcer → 待办事项

收到，放入待办清单。

---

## Q14: 分离手机验证码与图片验证码开关

### 当前状态

`EnableCaptcha` 一个开关控制了全部验证码：

```csharp
// 图片验证码（登录）
// AccountService.cs:65
if (_rbacOptions.EnableCaptcha) { ... }

// 手机验证码（注册）
// AccountService.cs:322
if (_rbacOptions.EnableCaptcha) { ... }
```

### 修复方案

**Step 1 — `RbacOptions.cs`**：

```csharp
public class RbacOptions
{
    // 原字段，保留兼容
    [Obsolete("使用 EnableImageCaptcha 替代")]
    public bool EnableCaptcha { get; set; }

    /// <summary>是否开启图片验证码（登录）</summary>
    public bool EnableImageCaptcha { get; set; }

    /// <summary>是否开启手机短信验证码（注册/找回密码）</summary>
    public bool EnablePhoneCaptcha { get; set; }
    // ...
}
```

**Step 2 — `AccountService.cs`**：

```csharp
// 登录图片验证码 — L65
if (_rbacOptions.EnableImageCaptcha)  // 原: EnableCaptcha
{
    if (!_captcha.Validate(uuid, code))
        throw new UserFriendlyException("验证码错误");
}

// 注册手机验证码 — L322
if (_rbacOptions.EnablePhoneCaptcha)  // 原: EnableCaptcha
{
    await ValidationPhoneCaptchaAsync(PhoneValidationType.Register, input.Phone.Value, input.Code!);
}
```

**Step 3 — `appsettings.json`**：

```json
"RbacOptions": {
    "AdminPassword": "123456",
    "EnableImageCaptcha": false,   // ★ 拆分
    "EnablePhoneCaptcha": true,    // ★ 拆分
    "EnableRegister": true,
    "EnableDataBaseBackup": false
}
```

---

## Q15: OperLog 敏感信息脱敏——具体怎么做？

### 方案

**文件**: `OperLogGlobalAttribute.cs:98-100`

```csharp
if (operLogAttribute.IsSaveRequestData)
{
    // ★ 脱敏后再序列化
    var maskedArgs = MaskSensitiveData(context.ActionArguments);
    logEntity.RequestParam = JsonConvert.SerializeObject(maskedArgs);
}
```

**新增脱敏方法**：

```csharp
// 敏感字段关键词（不区分大小写）
private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
{
    "password", "pass", "pwd", "oldpassword", "newpassword",
    "token", "secret", "securitykey", "accesstoken", "refreshtoken",
    "code" // 验证码
};

private static Dictionary<string, object?> MaskSensitiveData(
    IDictionary<string, object?> arguments)
{
    var result = new Dictionary<string, object?>();
    foreach (var kv in arguments)
    {
        if (kv.Value is string strValue && SensitiveKeys.Contains(kv.Key))
        {
            result[kv.Key] = "***";  // 脱敏为 ***
        }
        else if (kv.Value is IDictionary<string, object?> nestedDict)
        {
            result[kv.Key] = MaskSensitiveData(nestedDict);  // 递归脱敏
        }
        else
        {
            result[kv.Key] = kv.Value;
        }
    }
    return result;
}
```

---

## Q16: Token 生成使用缓存

已在 V2/V3 中明确方案。简述：

```csharp
// AccountManager.cs:50
// 改动前：
UserRoleMenuDto userInfo = await _userManager.GetInfoAsync(userId);
// 改动后：
UserRoleMenuDto userInfo = await _userManager.GetInfoByCacheAsync(userId);
```

---

## Q17: 使用 Casbin 增量更新替代全量 LoadPolicy

### 当前问题

```csharp
// CasbinPolicyManager.cs:58
uow.OnCompleted(_enforcer.LoadPolicyAsync);
```

每次策略变更后全量重载。但当前代码**已经在做内存增量操作**：

```csharp
// 所有写方法中都已经有内存增量操作：
await _enforcer.AddGroupingPolicyAsync(sub, roleSub, domain);      // 内存增量
await _enforcer.RemoveGroupingPolicyAsync(sub, r, domain);         // 内存增量
await _enforcer.AddPoliciesAsync(policies);                        // 内存增量
await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);     // 内存增量
```

**问题是 `TriggerMemorySync` 在这些增量操作做完后，又触发全量 LoadPolicy，把刚才的增量操作覆盖掉（从数据库重新加载）**。如果 DB 写入和事务提交之间存在时序问题，LoadPolicy 甚至可能加载到旧数据。

### 修复方案

**移除 `TriggerMemorySync` 中的 LoadPolicy，仅依赖增量 API**：

```csharp
private void TriggerMemorySync()
{
    // ★ 不再触发全量重载。
    // 内存增量操作（AddPolicy/RemovePolicy）已经保证了实时性。
    // DB 的持久化已经在各方法中通过 Insertable/Deleteable 完成。
    // 仅在以下场景需要 LoadPolicy：
    //   1. 应用启动时（已在 SqlSugarCoreModule 中执行）
    //   2. Redis Watcher 通知时（已在 Watcher 回调中执行）
    //   3. 手动触发（保留为 public 方法供运维使用）
    
    // 原代码（注释保留）：
    // IUnitOfWork? uow = _unitOfWorkManager.Current;
    // if (uow != null)
    // {
    //     const string syncKey = "CasbinMemorySyncTriggered";
    //     if (!uow.Items.ContainsKey(syncKey))
    //     {
    //         uow.Items[syncKey] = true;
    //         uow.OnCompleted(_enforcer.LoadPolicyAsync);
    //     }
    // }
    // else
    // {
    //     _enforcer.LoadPolicy();
    // }
}
```

**保留 LoadPolicy 作为运维入口**：

```csharp
/// <summary>手动触发全量重载（运维用）</summary>
public async Task ReloadPoliciesAsync()
{
    await _enforcer.LoadPolicyAsync();
}
```

### 全局防抖 → 待办

待 P-02 修完后评估是否仍需要。若增量 API 稳定运行，全局防抖非必需。

---

## Q18: 并发 LoadPolicy 竞态条件——详细方案

### 分析

当前问题：10 个并发 UOW = `uow.OnCompleted` 注册 10 个回调 → 事务提交时几乎同时触发 10 次 `LoadPolicyAsync` → Enforcer 非线程安全 → 内存策略树可能损坏。

### 方案：全局防抖 + 读写锁

**修改文件**: `CasbinPolicyManager.cs`

```csharp
public class CasbinPolicyManager(...) : DomainService, ICasbinPolicyManager
{
    // ★ 全局防抖状态
    private static int _reloadPending = 0;  // 0=空闲, 1=重载已调度
    private static readonly object _reloadLock = new();

    /// <summary>
    /// 防抖重载：在 300ms 内多次调用只触发一次 LoadPolicy
    /// </summary>
    private void DebouncedReload()
    {
        // 如果已调度了重载，跳过
        if (Interlocked.CompareExchange(ref _reloadPending, 1, 0) == 0)
        {
            IUnitOfWork? uow = _unitOfWorkManager.Current;
            if (uow != null)
            {
                const string syncKey = "CasbinMemorySyncTriggered";
                if (!uow.Items.ContainsKey(syncKey))
                {
                    uow.Items[syncKey] = true;
                    uow.OnCompleted(async () =>
                    {
                        // 等待 300ms，合并窗口内的其他变更
                        await Task.Delay(300);
                        await _enforcer.LoadPolicyAsync();
                        // 重置状态
                        Interlocked.Exchange(ref _reloadPending, 0);
                    });
                }
            }
        }
    }

    // 如果 Q17 方案已移除 LoadPolicy，此方案也不需要
    // 直接在 TriggerMemorySync 中返回空即可
}
```

### 如果 Q17 增量方案已实施

**则 P-03 自动消失**——因为没有 LoadPolicy 调用，就不存在竞态。

**推荐路径**：先实施 Q17（移除 LoadPolicy），P-03 自然解决。

---

## Q19: IgnoreUrls HashSet 优化——详细方案

### 方案

**文件**: `CasbinAuthorizationMiddleware.cs`

```csharp
public class CasbinAuthorizationMiddleware(...) : IMiddleware, ITransientDependency
{
    private readonly HashSet<string> _exactIgnoreUrls;   // 精确匹配：O(1)
    private readonly List<string> _prefixIgnoreUrls;       // 前缀匹配：O(n) 但 n 极小

    public CasbinAuthorizationMiddleware(
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IEnforcer enforcer,
        IOptions<CasbinOptions> options)
    {
        _currentUser = currentUser;
        _currentTenant = currentTenant;
        _enforcer = enforcer;
        _options = options.Value;

        // ★ 预处理 IgnoreUrls
        _exactIgnoreUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _prefixIgnoreUrls = [];

        if (_options.IgnoreUrls != null)
        {
            foreach (string url in _options.IgnoreUrls)
            {
                if (url.StartsWith("exact:", StringComparison.OrdinalIgnoreCase))
                {
                    _exactIgnoreUrls.Add(url[6..]);  // 去掉 "exact:" 前缀
                }
                else
                {
                    _prefixIgnoreUrls.Add(url);
                }
            }
        }
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        string? path = context.Request.Path.Value;

        // 0. IgnoreUrls 检查（★ O(1) 精确 + O(m) 前缀，m ≪ n）
        if (!string.IsNullOrEmpty(path) && _exactIgnoreUrls.Count + _prefixIgnoreUrls.Count > 0)
        {
            // O(1) 精确匹配
            if (_exactIgnoreUrls.Contains(path))
            {
                await next(context);
                return;
            }
            // O(m) 前缀匹配，m 通常 ≤ 3
            foreach (string prefix in _prefixIgnoreUrls)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    await next(context);
                    return;
                }
            }
        }
        // ... 其余逻辑不变
    }
}
```

**效果**：
- 精确匹配：O(n) → O(1)
- 前缀匹配：O(n) → O(m)，其中 m ≤ 3（/swagger, /hangfire）

---

## Q20: MenuService.UpdateAsync 性能——详细修复方案

### 问题回顾

```csharp
// MenuService.cs:81-93
if (isApiChanged)
{
    List<Guid> roleIds = await ...ToListAsync();
    foreach (Role role in roles)                          // N 个角色
    {
        List<Guid> menuIds = await ...ToListAsync();      // DB 查询
        List<Menu> menus = await _repository.GetListAsync(...); // DB 查询
        await _casbinPolicyManager.SetRolePermissionsAsync(role, menus); // Casbin 全量重载
    }
}
```

**循环内**：每个角色 2 次 DB 查询 + 1 次 Casbin SetRolePermissions → 内部触发 TriggerMemorySync。N 个角色 = N×3 个 I/O 操作。

### 修复方案（在 P-02 修复基础上）

修改 `MenuService.cs:81-93`，批量收集再统一处理：

```csharp
if (isApiChanged)
{
    // ★ Step 1: 一次性获取所有受影响的角色ID
    List<Guid> roleIds = await _roleMenuRepository._DbQueryable
        .Where(x => x.MenuId == id)
        .Select(x => x.RoleId)
        .Distinct()
        .ToListAsync();

    if (roleIds.Count > 0)
    {
        // ★ Step 2: 批量获取所有角色
        List<Role> roles = await _roleRepository.GetListAsync(x => roleIds.Contains(x.Id));

        // ★ Step 3: 批量获取所有角色的菜单ID映射（一次查询）
        List<(Guid RoleId, Guid MenuId)> roleMenuMappings = await _roleMenuRepository._DbQueryable
            .Where(x => roleIds.Contains(x.RoleId))
            .Select(x => new { x.RoleId, x.MenuId })
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(x => (x.RoleId, x.MenuId)).ToList());

        // ★ Step 4: 获取所有涉及菜单（去重，一次查询）
        List<Guid> allMenuIds = roleMenuMappings.Select(x => x.MenuId).Distinct().ToList();
        List<Menu> allMenus = await _repository.GetListAsync(x => allMenuIds.Contains(x.Id));

        // ★ Step 5: 按角色分组，批量更新 Casbin
        Dictionary<Guid, List<Menu>> roleMenusMap = roleMenuMappings
            .GroupBy(x => x.RoleId)
            .ToDictionary(g => g.Key, g => g.Select(x => allMenus.First(m => m.Id == x.MenuId)).ToList());

        foreach (Role role in roles)
        {
            if (roleMenusMap.TryGetValue(role.Id, out List<Menu>? menus))
            {
                await _casbinPolicyManager.SetRolePermissionsAsync(role, menus);
            }
        }
    }
}
```

**效果**：N 个角色从 N×3 次 I/O 降为 4 次 I/O（1 次查角色 + 1 次查映射 + 1 次查菜单 + N 次内存操作）。

**注**：如果 Q17 已实施（移除 LoadPolicy），则 `SetRolePermissionsAsync` 不再触发 DB I/O，性能进一步大幅提升。

---

## Q21: DataPermission LIKE 全表扫描——PostgreSQL 方案

### 当前代码

```csharp
// SfCasbinRbacDbContext.cs:94
d.Ancestors!.Contains(currentDeptIdStr)
// → SQL: WHERE ancestors LIKE '%abc-123%'  → 全表扫描
```

### PostgreSQL 三选一

| 方案 | 需要 DBA | 代码改动 | 性能 |
|------|---------|---------|------|
| A: Ltree 扩展 | ✅ 需要 `CREATE EXTENSION ltree` | 改表结构 + 改代码 | 最优（GiST 索引） |
| B: 数组重叠 | ❌ 不需要扩展 | 改 SQL 表达式 | 优（GIN 索引） |
| C: 精确 LIKE | ❌ | 改 SQL 表达式 | 中（BTREE 索引） |

### 推荐方案 C（纯代码优化，无需装扩展）

**原理**：`Ancestors` 字段存储格式为 `,rootId,parentId,selfId,`（逗号包裹）。用 `LIKE '%,xxx,%'` 精确匹配，配合 BTREE 索引。

但 **PostgreSQL 的 BTREE 对 LIKE 的前缀匹配才能用索引，`LIKE '%,xxx,%'` 不以常量开头，无法用 BTREE**。

### 推荐方案 B（数组重叠，无需扩展）

**Step 1** — 修改 `Ancestors` 的存储方式或查询时转换：

```csharp
// SfCasbinRbacDbContext.cs:92-95
// ★ PostgreSQL 数组重叠方案
expUser.Or(u => SqlFunc.Subqueryable<Department>()
    .Where(d => d.Id == u.DepartmentId &&
               (d.Id == currentDeptId ||
                SqlFunc.ToArray<string>(d.Ancestors!.Trim(',').Split(','))
                    .Contains(currentDeptIdStr)))  // ★ 使用 PostgreSQL && 操作符
    .Any());
```

这翻译为 PostgreSQL SQL：
```sql
WHERE string_to_array(trim(',' from ancestors), ',') && ARRAY['abc-123']
-- 或配合 GIN 索引：
CREATE INDEX idx_dept_ancestors ON casbin_sys_dept USING GIN(string_to_array(trim(',' from ancestors), ','));
```

**Step 2** — 如果不想改代码，直接加索引也能缓解（针对方案 C）：

```sql
-- PostgreSQL pg_trgm 扩展（需要 DBA 安装一次）：
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX idx_dept_ancestors_trgm ON casbin_sys_dept USING GIN(ancestors gin_trgm_ops);
```

`pg_trgm` 的 GIN 索引可以加速 `LIKE '%xxx%'` 查询。

### 推荐路径

**短期（零 DBA）**：加 `pg_trgm` 索引（一条 SQL，效果立竿见影）
**长期（最优）**：迁移到方案 B（数组重叠 + GIN 索引）或方案 A（Ltree）》》》》我选择此方案

---

## Q22: CasbinSeedService Raw SQL + Task.Delay(100)——详细方案

### 问题

```csharp
// 1. 创建专用 SqlSugarClient（new ConnectionConfig）→ 绕过 DI
// 2. 使用 Raw SQL 查询 → 硬编码表名和字段名
// 3. Task.Delay(100) → hack 等待连接释放
// 4. domain 硬编码 "default"
```

### 修复方案：用 DI + SqlSugar Queryable 替代

```csharp
[UnitOfWork(IsDisabled = true)]
public async Task MigrateAllAsync()
{
    // ★ 不再创建专用 Client，使用 DI 注入的 Repository
    // ISqlSugarRepository<Role> 已由 DI 管理，连接池自动处理

    // ========== PHASE 1: 读取数据 ==========
    // ★ 使用 SqlSugar Queryable 替代 Raw SQL

    // 读取所有角色（含 tenant_id）
    var roles = await _roleRepo._Db.Queryable<Role>()
        .Where(r => !r.IsDeleted)
        .Select(r => new
        {
            r.Id, r.RoleCode, r.RoleName, r.State, r.TenantId  // ★ 含 tenant_id
        })
        .ToListAsync();

    // 读取所有菜单（含 tenant_id）
    var menus = await _roleRepo._Db.Queryable<Menu>()
        .Where(m => !m.IsDeleted)
        .Select(m => new
        {
            m.Id, m.MenuName, m.ApiUrl, m.ApiMethod, m.State, m.TenantId  // ★ 含 tenant_id
        })
        .ToListAsync();

    // 读取角色-菜单关联
    var roleMenus = await _roleRepo._Db.Queryable<RoleMenu>()
        .Select(rm => new { rm.RoleId, rm.MenuId })
        .ToListAsync();

    // 读取用户-角色关联
    var userRoles = await _roleRepo._Db.Queryable<UserRole>()
        .Select(ur => new { ur.UserId, ur.RoleId })
        .ToListAsync();

    // 不再需要 Task.Delay(100)

    // ========== PHASE 2: 构建规则（按租户隔离） ==========
    var rulesToInsert = new List<CasbinRule>();

    // 构建 role → tenantId 映射
    var roleTenantMap = roles.ToDictionary(r => r.Id, r => r.TenantId);

    // 构建 p 规则
    foreach (var rm in roleMenus)
    {
        var role = roles.FirstOrDefault(r => r.Id == rm.RoleId);
        var menu = menus.FirstOrDefault(m => m.Id == rm.MenuId);
        if (role == null || menu == null || string.IsNullOrEmpty(menu.ApiUrl))
            continue;

        // ★ 按租户构建 domain
        string domain = role.TenantId?.ToString() ?? "default";

        rulesToInsert.Add(new CasbinRule
        {
            PType = "p",
            V0 = role.RoleCode,
            V1 = domain,        // ★ 租户隔离
            V2 = menu.ApiUrl,
            V3 = string.IsNullOrEmpty(menu.ApiMethod) ? "*" : menu.ApiMethod.ToUpperInvariant()
        });
    }

    // 构建 g 规则
    foreach (var ur in userRoles)
    {
        var role = roles.FirstOrDefault(r => r.Id == ur.RoleId);
        if (role == null) continue;

        string domain = role.TenantId?.ToString() ?? "default";

        rulesToInsert.Add(new CasbinRule
        {
            PType = "g",
            V0 = ur.UserId.ToString(),
            V1 = role.RoleCode,
            V2 = domain         // ★ 租户隔离
        });
    }

    // ========== PHASE 3: 写入 ==========
    if (rulesToInsert.Count > 0)
    {
        await _roleRepo._Db.Deleteable<CasbinRule>().ExecuteCommandAsync();
        // 分批插入（保持原来的批量逻辑）
        int batchSize = 500;
        for (int i = 0; i < rulesToInsert.Count; i += batchSize)
        {
            var batch = rulesToInsert.Skip(i).Take(batchSize).ToList();
            await _roleRepo._Db.Insertable(batch).ExecuteCommandAsync();
        }
    }

    // ========== PHASE 4: 重载 ==========
    await _enforcer.LoadPolicyAsync();
}
```

### 改进总结

| 方面 | 原方案 | 新方案 |
|------|--------|--------|
| SQL 方式 | Raw SQL 字符串 | SqlSugar Queryable（类型安全） |
| 连接管理 | `new SqlSugarClient(...)` + `Task.Delay(100)` | DI 管理，连接池自动回收 |
| 多租户 | `domain = "default"` | `domain = tenantId?.ToString() ?? "default"` |
| tenant_id | 未查询 | Select 中包含 `TenantId` |
| 可维护性 | 硬编码表名/字段名 | 类型安全，重构友好 |

---

## 附：第二批问题状态总览

| Q | 状态 | 行动 |
|---|------|------|
| Q9 | 需验证后定 | 先加日志验证事务共享，大概率无需修复 |
| Q10 | 方案就绪 | 环境变量 + 启动校验 |
| Q11 | 确认废弃 | 标记 [Obsolete] + 清理配置 |
| Q12 | 方案就绪 | 内存黑名单 + 中间件 JTI 校验 |
| Q13 | 待办 | 记录到待办清单 |
| Q14 | 方案就绪 | 拆分 EnableImageCaptcha / EnablePhoneCaptcha |
| Q15 | 方案就绪 | 脱敏方法 + 敏感字段列表 |
| Q16 | 认可 | 改 `GetInfoAsync` → `GetInfoByCacheAsync` |
| Q17 | 方案就绪 | 移除 LoadPolicy，仅用增量 API |
| Q18 | 被 Q17 覆盖 | 如果移除 LoadPolicy，P-03 自动消失 |
| Q19 | 方案就绪 | HashSet 预处理 |
| Q20 | 方案就绪 | 批量查询替代循环 |
| Q21 | 方案就绪 | 推荐 pg_trgm GIN 索引（短期）+ 数组重叠（长期） |
| Q22 | 方案就绪 | SqlSugar Queryable 替代 Raw SQL |
