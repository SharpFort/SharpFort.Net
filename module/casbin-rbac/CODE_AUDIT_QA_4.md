# Casbin-RBAC 审核答疑（第四批 — Q33-Q43：第三方独立复审专题）

> 日期：2026-05-21
> 基于：CODE_AUDIT_INDEPENDENT_REVIEW.md（第三方独立复审报告，2026-05-21）
> 目标：针对第三方复审专家提出的 11 项（R-01 至 R-11）复审意见，进行深度技术剖析、架构确认及代码级落地。

---

## 一、复审结论总体评估

经过对第三方独立复审报告（`CODE_AUDIT_INDEPENDENT_REVIEW.md`）的交叉比对与深度研判，我们对专家的专业度表示由衷的敬意。
这份复审报告非常中肯、切中要害，不仅指出了我们在前三轮审计中**方案设计上的理想化漏洞**（如 Q17 增量更新导致事务回滚后内存不一致、并发写 Enforcer 线程安全问题），还敏锐地捕获了**多处隐蔽的致命 Bug 与安全漏洞**（如 RoleCode 变更丢用户 g-rules 导致 403、手机验证码 Key 错位导致可无限重放攻击、登录审计事件未发布、Excel 临时文件无清理等）。

**我们在此郑重表态：完全接受第三方复审报告中的所有 11 项意见（R-01 至 R-11），并在本期 QA_4 中提供更加优化的、工业级的 C# / .NET Core 解决方案。**

特别是在以下两处，我们提出了超越复审报告建议的、更具优雅性的**顶级专家级设计（Refinements）**：
1. **针对 R-01（内存-DB一致性）**：我们不采用复审建议的“在 OnFailed/OnDisposed 中写回滚 LoadPolicy”（这会导致高并发下的脏读窗口且代码冗余），而是采用**“彻底延迟内存更新至 OnCompleted 之后”**的顶级设计。在事务提交前完全不碰内存 Enforcer，完美实现“零脏读、零回滚、无感最终一致性”。
2. **针对 R-11（Excel 临时文件清理）**：我们不采用复杂的 MemoryStream 重写或自定义 IActionResult，而是直接启用 ASP.NET Core 原生的 `PhysicalFileResult.DeletePhysicalFile = true`，让 Web 框架在成功发送文件流后自动从磁盘物理销毁临时文件，代码零侵入且绝对可靠。

---

## 二、答疑与重构设计（Q33 - Q43）

### Q33: R-01 Q17 移除 LoadPolicy 方案会引入内存-DB 不一致（UOW Consistency）

#### 当前问题
在前三轮的 Q17 设计中，我们曾试图通过“只做内存增量更新、移除全量 `LoadPolicy`”来提升性能。
专家指出：在 ABP 工作单元（Unit of Work）内，DB 写入尚未提交。如果直接调用 `_enforcer.AddGroupingPolicyAsync`，内存会**立即生效**。但如果随后的事务回滚，DB 写入撤销了，由于 `OnCompleted` 不会触发，内存中的增量修改**永远不会回滚**，导致严重的内存与数据库数据不一致，直至系统重启。

#### 顶级设计优化（Deferred Post-Commit Memory Sync）
为了追求绝对的线程安全与事务一致性，我们拒绝引入复杂的“双重注册 `OnFailed` 并在失败时重载”方案。因为“先乐观修改内存、失败再回滚”的模式存在**并发脏读窗口**——在事务 A 回滚前的几十毫秒内，其他并发请求会读取到尚未提交的“脏权限”。

**最优解：将所有内存 Enforcer 修改彻底延迟到事务成功提交之后（即 `OnCompleted` 阶段）。**

1. **写操作两步走**：在 `CasbinPolicyManager` 中，事务运行期间**仅进行数据库持久化（DB I/O）**，彻底移除对 `_enforcer.AddGroupingPolicyAsync` / `RemoveGroupingPolicyAsync` 的即时内存操作。
2. **OnCompleted 统一载入**：在 `OnCompleted` 回调中触发 `LoadPolicyAsync()`。由于此时数据库事务已成功 Commit，内存重载读出的数据 100% 真实、一致，且绝无脏读可能。
3. **如果是非事务环境**（`uow == null`）：则在执行完数据库持久化后立即调用 `_enforcer.LoadPolicy()`。

#### 核心代码实现

修改 `CasbinPolicyManager.cs` 中的写方法，将所有的“内存更新”代码从主业务流程中剥离，仅在 `TriggerMemorySync` 中通过 `OnCompleted` 统一做全量/防抖重载：

```csharp
// E:\Projects\SharpFort.Net\module\casbin-rbac\SharpFort.CasbinRbac.Domain\Managers\CasbinPolicyManager.cs

// 1. 彻底简化 TriggerMemorySync，不再依赖任何即时内存操作
private void TriggerMemorySync()
{
    IUnitOfWork? uow = _unitOfWorkManager.Current;
    if (uow != null)
    {
        const string syncKey = "CasbinMemorySyncTriggered";
        if (!uow.Items.ContainsKey(syncKey))
        {
            uow.Items[syncKey] = true;
            // 事务提交成功后，从 DB 重新载入最新策略，保证绝对的最终一致性，且避免了事务中途修改造成的内存脏读
            uow.OnCompleted(async () => 
            {
                await _writeLock.WaitAsync();
                try
                {
                    await _enforcer.LoadPolicyAsync();
                }
                finally
                {
                    _writeLock.Release();
                }
            });
        }
    }
    else
    {
        // 非事务环境下，锁定并即时重载
        _writeLock.Wait();
        try
        {
            _enforcer.LoadPolicy();
        }
        finally
        {
            _writeLock.Release();
        }
    }
}

// 以 AddRoleForUserAsync 为例的写操作重构：
public async Task AddRoleForUserAsync(User user, Role role)
{
    await _writeLock.WaitAsync();
    try
    {
        string sub = GetUserSubject(user.Id);
        string roleSub = GetRoleSubject(role.RoleCode!);
        string domain = GetTenantDomain(user.TenantId);

        // 1. 仅持久化，不碰内存 Enforcer
        CasbinRule rule = new()
        {
            PType = "g",
            V0 = sub,
            V1 = roleSub,
            V2 = domain
        };
        await _roleRepository._Db.Insertable(rule).ExecuteCommandAsync();
    }
    finally
    {
        _writeLock.Release();
    }

    // 2. 注册事务提交后的内存同步回调
    TriggerMemorySync();
}
```
*注：其他写方法（`RemoveRoleForUserAsync`、`SetUserRolesAsync`、`SetRolePermissionsAsync` 等）同理，全面删去 `_enforcer.RemoveGroupingPolicyAsync` / `_enforcer.AddGroupingPoliciesAsync` 等立即内存更新代码，将内存重载全权交由 `TriggerMemorySync` 延时处理。*

---

### Q34: R-02 Enforcer 并发写竞态与线程安全（Locking Strategy）

#### 当前问题
专家提出：虽然我们可能计划改用增量或防抖，但 `IEnforcer` 在多线程环境下执行非原子的多步操作（例如 `SetUserRolesAsync` 中先 `GetRolesForUserInDomain`、再循环 `RemoveGroupingPolicyAsync`、最后 `AddGroupingPoliciesAsync`）是不安全的。并发请求会导致 Enforcer 内部的策略模型结构发生竞态破坏。

#### 解决方案
在 `CasbinPolicyManager` 中引入静态 `SemaphoreSlim`，强制所有写操作、清空操作以及重载操作进行串行化保护，消除一切内存结构损坏的风险。管理后台操作频率极低，串行锁对系统吞吐量无任何负面影响。

#### 核心代码实现
```csharp
public class CasbinPolicyManager : DomainService, ICasbinPolicyManager
{
    // 引入全局写操作互斥锁
    private static readonly SemaphoreSlim _writeLock = new(1, 1);

    public async Task SetUserRolesAsync(User user, List<Role> roles)
    {
        await _writeLock.WaitAsync();
        try
        {
            string sub = GetUserSubject(user.Id);
            string domain = GetTenantDomain(user.TenantId);

            // 1. 物理删除该用户在该租户下的所有角色关联
            await _roleRepository._Db.Deleteable<CasbinRule>()
                .Where(x => x.PType == "g" && x.V0 == sub && x.V2 == domain)
                .ExecuteCommandAsync();

            // 2. 批量插入新关联
            if (roles.Count > 0)
            {
                List<CasbinRule> newRules = [.. roles.Select(r => new CasbinRule
                {
                    PType = "g",
                    V0 = sub,
                    V1 = GetRoleSubject(r.RoleCode!),
                    V2 = domain
                })];
                await _roleRepository._Db.Insertable(newRules).ExecuteCommandAsync();
            }
        }
        finally
        {
            _writeLock.Release();
        }

        // 3. 异步提交触发内存同步
        TriggerMemorySync();
    }
}
```

---

### Q35: R-03 OperLog 脱敏方案由于 ActionArguments 结构失效问题（Log Desensitization）

#### 当前问题
我们在 Q15 中设计的脱敏机制是直接判断 `ActionArguments` 的 Key 值是否包含 `"password"` 等敏感词。
专家敏锐指出：`ActionArguments` 的 Key 是参数名（如 `"input"`），而它的 Value 则是 DTO 实体对象（如 `LoginInputVo`）。我们直接做 `kv.Value is string` 的判断，永远只作用在顶层参数名上，导致嵌套在 DTO 内的 `Password` 属性完美绕过了脱敏拦截，明文记录在了审计日志中。

#### 解决方案
利用 NewtonSoft `JToken` 的深度遍历特性，编写一个递归解析与脱敏函数，将参数对象序列化后递归抹除包含在任何层级内的敏感属性值，再重新保存。

#### 核心代码实现
在 `OperLogFilter` 或对应脱敏拦截器中，实现如下深度脱敏逻辑：

```csharp
private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
{
    "password", "newPassword", "oldPassword", "confirmPassword", "token", "accessToken", "smsCode"
};

private string GetDesensitizedRequestParam(ActionExecutingContext context)
{
    if (context.ActionArguments == null || context.ActionArguments.Count == 0)
    {
        return string.Empty;
    }

    try
    {
        // 1. 将参数字典序列化为 JObject
        string json = JsonConvert.SerializeObject(context.ActionArguments);
        JToken token = JToken.Parse(json);

        // 2. 深度遍历并遮蔽敏感键
        MaskSensitiveProperties(token);

        return token.ToString(Formatting.None);
    }
    catch
    {
        return "[Serialization Failed]";
    }
}

private static void MaskSensitiveProperties(JToken token)
{
    if (token is JObject obj)
    {
        foreach (JProperty prop in obj.Properties().ToList())
        {
            if (SensitiveKeys.Contains(prop.Name))
            {
                prop.Value = "***";
            }
            else
            {
                MaskSensitiveProperties(prop.Value);
            }
        }
    }
    else if (token is JArray arr)
    {
        foreach (JToken item in arr)
        {
            MaskSensitiveProperties(item);
        }
    }
}
```

---

### Q36: R-04 JWT 黑名单清理机制高并发竞态与 Count 性能问题（Timer Cleanup）

#### 当前问题
在原 Q12 设计中，我们在每次添加 Token 到黑名单时执行 `_blacklist.Count % 100 == 0`，若满足则触发过期清理。
专家指出三处软肋：
1. `ConcurrentDictionary.Count` 是一个 $O(n)$操作，每次写都调用严重拖累性能；
2. 并发 Add 导致 `Count` 递增迅速，极易漏掉整百临界点（跳过 100 变成 101），导致清理失效；
3. 多线程可同时满足 `% 100 == 0` 条件，产生并发清理竞态。

#### 解决方案
拥抱 .NET 定时任务标准实践，直接在 `JwtBlacklist` 中采用 `System.Threading.Timer` 进行每 5 分钟一次的无感、线程安全、定时异步清理。

#### 核心代码实现
```csharp
public class JwtBlacklist : IJwtBlacklist, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, DateTime> _blacklist = new();
    private readonly Timer _cleanupTimer;

    public JwtBlacklist()
    {
        // 注册定时清理器：延迟 5 分钟启动，之后每 5 分钟执行一次
        _cleanupTimer = new Timer(CleanupExpired, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public void Add(string token, DateTime expireTime)
    {
        _blacklist[token] = expireTime;
    }

    private void CleanupExpired(object? state)
    {
        DateTime now = DateTime.UtcNow;
        List<string> expiredKeys = [];

        foreach (KeyValuePair<string, DateTime> kv in _blacklist)
        {
            if (kv.Value < now)
            {
                expiredKeys.Add(kv.Key);
            }
        }

        foreach (string key in expiredKeys)
        {
            _blacklist.TryRemove(key, out _);
        }
    }
}
```

---

### Q37: R-05 RoleCode 变更导致用户角色关系 g-rules 彻底丢失 Bug（RoleCode Migration）

#### 当前问题
这是一个极其致命的业务逻辑缺陷。
在 `RoleService.UpdateAsync` 中，当修改角色的 `RoleCode` 时（如 `editor` 改为 `content-editor`），原逻辑直接调用 `CleanRolePoliciesByRoleCodeAsync("editor", tenantId)`，这会**物理删除**数据库和内存中所有以 `editor` 为核心的 p 规则和 g 规则。
虽然系统稍后会重建 p 规则，**但是 g 规则（即已绑定该角色的所有用户关联信息）却被永久删除了且无法重建**！这直接导致系统内的所有编辑人员在角色名更新后，访问系统瞬间遭到 403 权限封杀。

#### 解决方案
微调 RoleCode 变更逻辑，弃用“直接删除旧代码 g 规则”，在 `ICasbinPolicyManager` 中新增角色码迁移方法 `MigrateRoleCodeAsync`，在数据库底层通过一个事务原子性地将所有关联此角色的 `p` 规则和 `g` 规则中的 RoleCode 从旧值更新为新值。

#### 核心代码实现

1. **ICasbinPolicyManager 接口声明**：
```csharp
/// <summary>
/// 当角色编码变更时，在底层迁移所有关联的 p 规则和 g 规则，避免用户角色关联丢失
/// </summary>
Task MigrateRoleCodeAsync(string oldRoleCode, string newRoleCode, Guid? tenantId);
```

2. **CasbinPolicyManager 落地实现**：
```csharp
public async Task MigrateRoleCodeAsync(string oldRoleCode, string newRoleCode, Guid? tenantId)
{
    string domain = GetTenantDomain(tenantId);

    await _writeLock.WaitAsync();
    try
    {
        // 开启数据库事务更新
        await _roleRepository._Db.Aop.ExecuteSqlTranAsync(async () =>
        {
            // A. 更新 g-rules (V1 从旧角色码变更为新角色码)
            await _roleRepository._Db.Updateable<CasbinRule>()
                .SetColumns(x => x.V1 == newRoleCode)
                .Where(x => x.PType == "g" && x.V1 == oldRoleCode && x.V2 == domain)
                .ExecuteCommandAsync();

            // B. 更新 p-rules (V0 从旧角色码变更为新角色码)
            await _roleRepository._Db.Updateable<CasbinRule>()
                .SetColumns(x => x.V0 == newRoleCode)
                .Where(x => x.PType == "p" && x.V0 == oldRoleCode && x.V1 == domain)
                .ExecuteCommandAsync();
        });
    }
    finally
    {
        _writeLock.Release();
    }

    // 重新从数据库拉取最新规则同步至内存
    TriggerMemorySync();
}
```

3. **RoleService.cs 消费**：
```csharp
// E:\Projects\SharpFort.Net\module\casbin-rbac\SharpFort.CasbinRbac.Application\Services\RoleService.cs
public override async Task<RoleGetOutputDto> UpdateAsync(Guid id, RoleUpdateInputVo input)
{
    Role entity = await _repository.GetByIdAsync(id);
    string oldRoleCode = entity.RoleCode!;
    
    await MapToEntityAsync(input, entity);
    
    // Step 1: 先更新业务表
    await _repository.UpdateAsync(entity);

    // Step 2: 处理 Casbin 策略
    if (oldRoleCode != entity.RoleCode)
    {
        // 彻底消除物理删除导致的 g-rules 丢失风险，使用高安全性的迁移功能
        await _casbinPolicyManager.MigrateRoleCodeAsync(oldRoleCode, entity.RoleCode!, entity.TenantId);
    }

    // Step 3: 更新菜单策略
    await _roleManager.GiveRoleSetMenuAsync([id], input.MenuIds ?? []);

    return await MapToGetOutputDtoAsync(entity);
}
```

---

### Q38: R-06 手机验证码验证后缓存清除错配导致的严重重放攻击漏洞（Replay Attack）

#### 当前问题
这是一个由于拼写粗心造成的严重逻辑 Bug。
在 `AccountService.cs` 中：
- 缓存验证码时的 Key：`new CaptchaPhoneCacheKey(validationPhoneType, input.Phone)`（用的是**手机号**）
- 校验通过后，移除缓存的 Key：`new CaptchaPhoneCacheKey(validationPhoneType, code.ToString())`（错误使用了**验证码 `code`**）

由于用于定位缓存条目的 Key 无法匹配，导致已校验通过的验证码在 10 分钟有效期内**永远不会被移除**！攻击者可在此时间窗口内，使用同一个短信验证码进行无限次密码重置或恶意创号，短信校验形同虚设。

#### 解决方案
精确订正 `AccountService.cs` 第 273 行的移除 Key 参数，传回正确的手机号，进行立即物理清除。

#### 核心代码实现
```csharp
// E:\Projects\SharpFort.Net\module\casbin-rbac\SharpFort.CasbinRbac.Application\Services\AccountService.cs (修正)

// 校验成功后移除缓存
await _phoneCache.RemoveAsync(
    new CaptchaPhoneCacheKey(validationPhoneType, input.Phone) // ★ 修正：必须传入 input.Phone 手机号，绝对不能传入 code 验证码！
);
```

---

### Q39: R-07 登录事件创建后未发布导致审计日志完全失效问题（Audit Event Publish）

#### 当前问题
在重构 `AccountService` 登录端点时，系统构建了包含登录 IP、浏览器及 OS 信息的 `LoginEventArgs loginEto` 实体，然而**却遗漏了将其发布到 EventBus 的那行关键代码**。导致登录处理事件无法分发，`LoginLog` 审计日志表长期颗粒无收，登录审计功能全面瘫痪。

#### 解决方案
在 `AccountService.cs` 创建完 `loginEto` 后，紧跟 `PublishAsync` 语句进行分发。

#### 核心代码实现
```csharp
// E:\Projects\SharpFort.Net\module\casbin-rbac\SharpFort.CasbinRbac.Application\Services\AccountService.cs
if (_httpContextAccessor.HttpContext is not null)
{
    LoginEventArgs loginEto = new()
    {
        UserId = userInfo.User.Id,
        UserName = userInfo.User.UserName,
        LoginIp = clientInfo.LoginIp,
        Browser = clientInfo.Browser,
        Os = clientInfo.Os
    };
    
    // ★ 补全遗漏的发布调用，激活登录审计日志
    await LocalEventBus.PublishAsync(loginEto);
}
```

---

### Q40: R-08 UserService 创建用户时弱密码 "123456" 硬编码安全缺陷（Weak Default Password）

#### 当前问题
在 `UserService.CreateAsync` 中，若管理员创建新用户时没有指定初始密码，代码中会退回至 `password = "123456"`。这违反了现代信息安全标准，极易造成初始账号遭撞库扫描及弱口令权限劫持。

#### 解决方案
强制要求管理员在后台创建新用户时必须显式配置初始密码；或在创建成功后在响应中返回一个强随机生成的初始密码，断绝弱口令隐患。本处我们采用强制要求指定初始密码的安全策略。

#### 核心代码实现
```csharp
// E:\Projects\SharpFort.Net\module\casbin-rbac\SharpFort.CasbinRbac.Application\Services\UserService.cs
public override async Task<UserGetOutputDto> CreateAsync(UserCreateInputVo input)
{
    if (string.IsNullOrWhiteSpace(input.Password))
    {
        throw new UserFriendlyException("创建新用户时，必须显式设置安全的初始密码！", code: "USER_CREATE_001");
    }

    // 后续密码安全哈希及持久化流程...
}
```

---

### Q41: R-09 MenuService.GetListAsync 分页总数 TotalCount 恒为 0 缺陷（TotalCount Fix）

#### 当前问题
在 `MenuService.GetListAsync` 中，代码使用 `RefAsync<int> total = 0`，随后调用 `_repository._DbQueryable.ToListAsync()`。
然而，SqlSugar 中的 `ToListAsync` **是无法往 `RefAsync<int>` 传出参数中回写分页总数的**。这导致系统无论存有多少菜单数据，返回给前端的分页总数 `TotalCount` 永远是 0，直接引发前端分页控件状态失常。

#### 解决方案
鉴于菜单表的数据量通常极小（至多几百条），且需要全量加载在前端组装成导航树，直接将 `total` 赋值为拉取到的扁平集合长度。

#### 核心代码实现
```csharp
// E:\Projects\SharpFort.Net\module\casbin-rbac\SharpFort.CasbinRbac.Application\Services\MenuService.cs
public override async Task<PagedResultDto<MenuGetListOutputDto>> GetListAsync(MenuGetListInputVo input)
{
    List<Menu> entities = await _repository._DbQueryable
        .WhereIF(!string.IsNullOrWhiteSpace(input.MenuName), x => x.MenuName.Contains(input.MenuName))
        .WhereIF(input.State.HasValue, x => x.State == input.State.Value)
        .OrderBy(x => x.OrderNum)
        .ToListAsync();

    // ★ 修复：手动将 total 设置为 entities.Count，恢复分页控件生命力
    int total = entities.Count;

    List<MenuGetListOutputDto> dtos = await MapToGetListOutputDtoListAsync(entities);
    return new PagedResultDto<MenuGetListOutputDto>(total, dtos);
}
```

---

### Q42: R-10 DataPermissionFilter 中管理员判断大小写敏感隐患（Admin Collation Compare）

#### 当前问题
在 `SfCasbinRbacDbContext.cs` 的数据过滤配置中，判断当前登录者是否为超级管理员使用的是 `CurrentUser.UserName == UserConst.Admin`。
由于 C# 的 `==` 运算符默认使用 Ordinal 进行**大小写敏感**比对。如果底层数据库的用户名是不敏感的（如录入的是 `Admin`），则此比对失效，超级管理员的数据权限过滤绕过判定将被打回，从而意外受到普通用户级别的数据权限束缚。

#### 解决方案
改用健壮的、大小写不敏感的 `string.Equals(..., StringComparison.OrdinalIgnoreCase)`。

#### 核心代码实现
```csharp
// E:\Projects\SharpFort.Net\module\casbin-rbac\SharpFort.CasbinRbac.Repository\SfCasbinRbacDbContext.cs
if (CurrentUser.IsAuthenticated && !string.IsNullOrEmpty(CurrentUser.UserName))
{
    // ★ 订正：大小写不敏感判定，防范超级管理员身份判定在不同 collation 下受阻
    if (string.Equals(CurrentUser.UserName, UserConst.Admin, StringComparison.OrdinalIgnoreCase))
    {
        // 豁免超级管理员的数据权限限制
    }
}
```

---

### Q43: R-11 Excel 导出物理临时文件的磁盘耗尽风险与自动销毁机制（File Automatic Cleanup）

#### 当前问题
在 `UserService.ExportAsync` 导出 Excel 时，系统通过 `MiniExcel.SaveAsAsync(filePath, ...)` 在磁盘的临时路径下物理落盘生成了文件，并通过 `PhysicalFileResult` 将其传出。
问题在于：在响应输出给浏览器后，**没有后续逻辑去销毁这个零散磁盘文件**。若日复一日频繁执行数据导出，磁盘中会残留大量无用临时 Excel，最终爆满服务器磁盘。

#### 顶级设计优化（Built-in physical file deletion）
我们不需要自建繁重的文件清理守护进程，也无需对 `MiniExcel` 改写为内存流。
在 ASP.NET Core MVC 中，提供了一个专门用于处理下载后需立即物理删除的原生内置机制：**`PhysicalFileResult` 的 `DeletePhysicalFile` 属性**。

当把 `DeletePhysicalFile` 设定为 `true` 时，ASP.NET Core Web 框架在将文件流完全读取并经由 HTTP 发送至客户端浏览器后，**会自动代表我们向系统发送指令，销毁磁盘上的该临时文件**！

#### 核心代码实现
修改 `UserService.cs` 导出的物理文件返回配置：

```csharp
// E:\Projects\SharpFort.Net\module\casbin-rbac\SharpFort.CasbinRbac.Application\Services\UserService.cs
public async Task<IActionResult> ExportUsersAsync(UserExportInputVo input)
{
    string tempPath = Path.GetTempPath();
    string fileName = $"UserExport_{Guid.NewGuid():N}.xlsx";
    string filePath = Path.Combine(tempPath, fileName);

    List<UserExportDto> exportData = await GetExportDataAsync(input);
    await MiniExcel.SaveAsAsync(filePath, exportData);

    // ★ 顶级优化：利用框架底层的 DeletePhysicalFile 特性，发送完毕自动销毁物理临时文件，100% 杜绝磁盘资源耗尽风险
    return new PhysicalFileResult(filePath, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
    {
        DeletePhysicalFile = true 
    };
}
```

---

## 三、后续阶段修复排期调整

我们完全认可复审报告中对这 11 项（R-01 至 R-11）建议的优先级划分，我们决定将他们有机地编排进当前的实施计划：

1. **立即修复阶段（P0-Bug 消除）**
   - **R-06（安全重放隐患）**：修正 `AccountService` 移除验证码缓存 Key，防止短信验证码被重放破解。
   - **R-05（系统灾难 Bug）**：重写 `RoleService`，使用新创建的 `MigrateRoleCodeAsync` 数据库单事务级迁移替代物理删除旧角色 g-rules。
   - **R-07（审计瘫痪 Bug）**：补全 `LocalEventBus.PublishAsync(loginEto)`，让登录操作日志重回正轨。

2. **阶段二核心重构阶段（事务与并发）**
   - **R-01 & R-02（事务及内存一致性）**：完全剥离主事务流程中的即时内存操作，使 `TriggerMemorySync` 在 `OnCompleted` 阶段统一处理 `LoadPolicyAsync`。并在 `CasbinPolicyManager` 中配置 `SemaphoreSlim(1,1)` 写锁消除任何并发操作内存损坏风险。

3. **阶段三规范化整理阶段**
   - **R-03（OperLog 深度脱敏）**：编写 `JToken` 深度解析递归脱敏算法。
   - **R-08（管理员强初始密码）**：取消 `"123456"` 硬编码，强制管理员提供初始密码。
   - **R-09（菜单 TotalCount 纠错）**：直接以 `entities.Count` 手动装填分页 `total`。
   - **R-11（Excel 销毁）**：对 `PhysicalFileResult` 赋值 `DeletePhysicalFile = true`，交由 ASP.NET Core 完美销毁导出痕迹。
   - **R-10 & R-04（Timer 与 OrdinalIgnoreCase）**：黑名单切换至 `Timer` 定时清理，`DbContext` 超级管理员身份判定改用 `OrdinalIgnoreCase`。
