# Casbin-RBAC 独立第三方复审报告

> 复审日期：2026-05-21
> 复审范围：CODE_AUDIT_REPORT.md(V2)、CODE_AUDIT_REPORT_V3.md(V3)、CODE_AUDIT_QA.md、CODE_AUDIT_QA_2.md、CODE_AUDIT_QA_3.md + 全部源码交叉验证
> 复审目标：仅列出方案欠妥、需要补充、以及新发现的 Bug

---

## 一、原审方案欠妥（需修正）

### 【严重】R-01: Q17 移除 LoadPolicy 方案会引入内存-DB 不一致

**原审方案（Q17）**：移除 `TriggerMemorySync` 中的 `LoadPolicyAsync`，仅依赖内存增量 API。

**问题**：当前代码执行顺序为：

```
1. DB 写入（_roleRepository._Db.Insertable/Deleteable）  ← 事务内，未提交
2. 内存增量更新（AddGroupingPolicyAsync 等）              ← 立即生效
3. uow.OnCompleted(LoadPolicyAsync)                       ← 事务提交后才执行
```

如果 ABP UOW **回滚**：
- 步骤 1 的 DB 写入被回滚 ✓
- 步骤 2 的内存增量更新**不会回滚** ✗
- 步骤 3 的 `OnCompleted` **不会触发**（仅成功才回调）✗

**后果**：Enforcer 内存中保留了已回滚的策略数据，且没有 LoadPolicy 来修正，直到下次重启或手动 LoadPolicy。

**修正建议**：
- **保留** `uow.OnCompleted(LoadPolicyAsync)` 作为最终一致性保障
- **新增** `uow.OnDisposed` 或 `uow.OnFailed` 回调中也触发 `LoadPolicyAsync`，回滚时修正内存
- 在此基础上再加**跨 UOW 全局防抖**（解决 P-03）

```csharp
private void TriggerMemorySync()
{
    IUnitOfWork? uow = _unitOfWorkManager.Current;
    if (uow != null)
    {
        const string syncKey = "CasbinMemorySyncTriggered";
        if (!uow.Items.ContainsKey(syncKey))
        {
            uow.Items[syncKey] = true;
            // 成功提交后重载（最终一致性）
            uow.OnCompleted(DebouncedLoadPolicyAsync);
            // ★ 新增：失败回滚时也重载（修正乐观更新）
            uow.OnFailed(() => { _ = DebouncedLoadPolicyAsync(); });
        }
    }
    else
    {
        _enforcer.LoadPolicy();
    }
}
```

---

### 【严重】R-02: Q17+Q18 的推导有逻辑错误 — P-03 不会因移除 LoadPolicy 而自动消失

**原审结论（Q18）**："如果 Q17 增量方案已实施，则 P-03 自动消失——因为没有 LoadPolicy 调用，就不存在竞态。"

**问题**：竞态不仅存在于 `LoadPolicyAsync`。当前代码中所有 `CasbinPolicyManager` 的写方法都在做**并发的内存增量操作**：

```csharp
// SetUserRolesAsync 中：
foreach (string r in oldRoles)
    await _enforcer.RemoveGroupingPolicyAsync(sub, r, domain);  // 并发写
await _enforcer.AddGroupingPoliciesAsync(policies);              // 并发写
```

10 个并发请求 = 10 个线程同时执行这些操作。即使移除 LoadPolicy，Enforcer 的内部策略模型仍被并发修改。

**修正建议**：无论是否移除 LoadPolicy，都应在 `CasbinPolicyManager` 层面加一把 `SemaphoreSlim` 保护所有写操作的原子性：

```csharp
private static readonly SemaphoreSlim _writeLock = new(1, 1);

public async Task SetUserRolesAsync(User user, List<Role> roles)
{
    await _writeLock.WaitAsync();
    try { /* 现有逻辑 */ }
    finally { _writeLock.Release(); }
}
```

---

### 【中】R-03: Q15 OperLog 脱敏方案无法生效

**原审方案（Q15）**：

```csharp
if (kv.Value is string strValue && SensitiveKeys.Contains(kv.Key))
{
    result[kv.Key] = "***";
}
```

**问题**：`context.ActionArguments` 的结构是 `{ "input": LoginInputVo对象 }`。Key 是参数名 `"input"`，Value 是 DTO 对象（不是 string），Password 是 DTO 内部属性。这段代码检查的是**参数名**而非**属性名**，永远不会匹配 `"password"`。

**修正建议**：改用 `JsonSerializer` 的自定义 Converter 在序列化时脱敏，或先将 DTO 序列化为 `JObject` 后递归遍历所有属性名：

```csharp
if (operLogAttribute.IsSaveRequestData)
{
    string json = JsonConvert.SerializeObject(context.ActionArguments);
    JObject obj = JObject.Parse(json);
    MaskSensitiveProperties(obj);
    logEntity.RequestParam = obj.ToString(Formatting.None);
}

private static void MaskSensitiveProperties(JToken token)
{
    if (token is JObject obj)
    {
        foreach (JProperty prop in obj.Properties().ToList())
        {
            if (SensitiveKeys.Contains(prop.Name))
                prop.Value = "***";
            else
                MaskSensitiveProperties(prop.Value);
        }
    }
    else if (token is JArray arr)
    {
        foreach (JToken item in arr)
            MaskSensitiveProperties(item);
    }
}
```

---

### 【低】R-04: Q12 JWT 黑名单清理机制有竞态

**原审方案**：`if (_blacklist.Count % 100 == 0) CleanupExpired();`

**问题**：
1. `ConcurrentDictionary.Count` 是 O(n) 操作，性能开销大
2. `% 100 == 0` 判断不可靠——并发 Add/Remove 导致 Count 可能跳过 100
3. 高并发下多线程同时触发 CleanupExpired

**修正建议**：改用 `Timer` 定期清理（如每 5 分钟）：

```csharp
private readonly Timer _cleanupTimer;
public JwtBlacklist()
{
    _cleanupTimer = new Timer(_ => CleanupExpired(), null,
        TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
}
```

---

## 二、原审遗漏（需要补充的问题）

### 【严重】R-05: RoleCode 变更时用户角色 g-rules 丢失

**文件**: `RoleService.cs:128-131` + `CasbinPolicyManager.cs:258-272`

当 RoleCode 从 `editor` 改为 `content-editor` 时：

```
1. CleanRolePoliciesByRoleCodeAsync("editor", tenantId)
   → 删除所有 p-rules: p, editor, default, /api/xxx, GET   ← 正确
   → 删除所有 g-rules: g, userId, editor, default           ← ★ 用户绑定丢失！
2. GiveRoleSetMenuAsync([id], menuIds)
   → 创建新 p-rules: p, content-editor, default, /api/xxx, GET  ← 正确
   → 不会重建 g-rules（此方法只处理 p 规则）                      ← ★ 缺失！
```

**后果**：所有绑定该角色的用户在 Casbin 中**失去角色关联**。业务表 `casbin_sys_user_role` 仍保留 RoleId 映射（不受影响），但 Casbin 的 g-rules 被清空，这些用户的所有 API 请求都将收到 403。

**建议**：RoleCode 变更时，应将 g-rules 迁移而非删除：

```csharp
if (oldRoleCode != entity.RoleCode)
{
    // 迁移 g-rules：旧 RoleCode → 新 RoleCode
    await _casbinPolicyManager.MigrateRoleCodeAsync(
        oldRoleCode, entity.RoleCode!, entity.TenantId);
}
```

新增 `MigrateRoleCodeAsync`：
```csharp
public async Task MigrateRoleCodeAsync(string oldCode, string newCode, Guid? tenantId)
{
    string domain = GetTenantDomain(tenantId);

    // 1. DB: 更新 g-rules 的 V1 从旧 code 到新 code
    await _roleRepository._Db.Updateable<CasbinRule>()
        .SetColumns(x => x.V1 == newCode)
        .Where(x => x.PType == "g" && x.V1 == oldCode && x.V2 == domain)
        .ExecuteCommandAsync();

    // 2. DB: 更新 p-rules 的 V0 从旧 code 到新 code
    await _roleRepository._Db.Updateable<CasbinRule>()
        .SetColumns(x => x.V0 == newCode)
        .Where(x => x.PType == "p" && x.V0 == oldCode && x.V1 == domain)
        .ExecuteCommandAsync();

    // 3. 内存重载
    TriggerMemorySync();
}
```

---

### 【严重】R-06: 手机验证码验证后缓存移除使用了错误的 Key — 可重放攻击

**文件**: `AccountService.cs:269-274`

```csharp
// 设置缓存时用的 Key：
await _phoneCache.SetAsync(
    new CaptchaPhoneCacheKey(validationPhoneType, input.Phone),  // ★ Key = 手机号
    new CaptchaPhoneCacheItem(code), ...);

// 验证成功后移除缓存时：
await _phoneCache.RemoveAsync(
    new CaptchaPhoneCacheKey(validationPhoneType, code.ToString()));  // ★ Key = 验证码！
```

**问题**：移除缓存时将**验证码** `code` 当作了手机号传入 CacheKey，Key 不匹配，导致缓存条目**永远不会被成功移除**。

**后果**：
1. 同一验证码在 10 分钟滑动窗口内可**无限次重放**
2. 注册/找回密码流程的短信验证码验证形同虚设

**修复**：

```csharp
// AccountService.cs:273 — 修正 CacheKey
await _phoneCache.RemoveAsync(
    new CaptchaPhoneCacheKey(validationPhoneType,
        phone.ToString(CultureInfo.InvariantCulture)));  // ★ 用手机号，不是验证码
```

---

### 【中】R-07: 登录事件创建后未发布 — 登录审计日志完全失效

**文件**: `AccountService.cs:113-137`

```csharp
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
    // ★ loginEto 创建后没有任何 PublishAsync 调用！
    // 原代码 await LocalEventBus.PublishAsync(loginEto); 被重构时丢失了
}
```

**后果**：登录事件不会被记录，LoginLog 表为空，登录审计功能完全不工作。

**修复**：在构建 loginEto 后添加发布：

```csharp
await LocalEventBus.PublishAsync(loginEto);
```

---

### 【中】R-08: UserService.CreateAsync 硬编码默认密码 "123456"

**文件**: `UserService.cs:115`

```csharp
string password = string.IsNullOrEmpty(input.Password) ? "123456" : input.Password;
```

原审 S-06/Q11 讨论的是种子数据中的 `AdminPassword`（已废弃），但此处是**管理员创建用户**时的运行时逻辑。当管理员不填密码时，新用户获得硬编码的 "123456"。

**修复**：要么强制管理员输入密码，要么生成随机密码并返回/显示：

```csharp
if (string.IsNullOrEmpty(input.Password))
{
    throw new UserFriendlyException("创建用户时必须设置初始密码");
}
```

---

### 【中】R-09: MenuService.GetListAsync 返回 TotalCount 永远为 0

**文件**: `MenuService.cs:105-115`

```csharp
RefAsync<int> total = 0;
List<Menu> entities = await _repository._DbQueryable
    // ... filters ...
    .ToListAsync();  // ★ 用了 ToListAsync，不是 ToPageListAsync
return new PagedResultDto<MenuGetListOutputDto>(total, ...);  // total 始终为 0
```

`total` 声明后从未被赋值（`ToListAsync` 不会写入 `RefAsync<int>`）。前端分页组件如果依赖 `TotalCount` 将无法正确显示。

**修复**：如果菜单确实需要全量加载（树形结构），应直接设置 `total = entities.Count`。

---

### 【低】R-10: DataPermissionFilter 管理员判断使用默认字符串比较

**文件**: `SfCasbinRbacDbContext.cs:45`

```csharp
if (CurrentUser.UserName == UserConst.Admin || ...)
```

`==` 使用的是 `StringComparison.Ordinal`（因为 string 的 `==` operator）。但如果数据库大小写不敏感，一个名为 `"Admin"`（大写 A）的普通用户被 DB 存储为 `"admin"` 后读出来可能大小写不一致。建议统一使用 `StringComparison.OrdinalIgnoreCase` 或在代码中明确意图。

---

### 【低】R-11: Excel 导出临时文件无清理机制

**文件**: `UserService.cs:308-320`

```csharp
string filePath = Path.Combine(tempPath, fileName);
await MiniExcel.SaveAsAsync(filePath, exportData);
return new PhysicalFileResult(filePath, ...);
// ★ 文件在响应后不会被删除，长期运行会占满磁盘
```

**建议**：使用 `MemoryStream` + `FileStreamResult`，或在响应完成后删除临时文件（自定义 `IActionResult`）。

---

## 三、总结

### 按严重度汇总

| 编号 | 严重度 | 类型 | 问题 |
|------|--------|------|------|
| R-01 | 严重 | 方案欠妥 | Q17 移除 LoadPolicy 导致回滚后内存脏数据 |
| R-02 | 严重 | 方案欠妥 | Q18 推导错误，P-03 不因移除 LoadPolicy 而消失 |
| R-05 | 严重 | 遗漏 | RoleCode 变更丢失所有用户 g-rules |
| R-06 | 严重 | 新发现 | 验证码缓存移除用错 Key，可重放攻击 |
| R-03 | 中 | 方案欠妥 | OperLog 脱敏无法生效（检查参数名而非属性名） |
| R-07 | 中 | 新发现 | 登录事件未 Publish，审计日志失效 |
| R-08 | 中 | 遗漏 | CreateAsync 硬编码默认密码 "123456" |
| R-09 | 中 | 新发现 | Menu 列表 TotalCount 永远为 0 |
| R-04 | 低 | 方案欠妥 | JWT 黑名单清理竞态 |
| R-10 | 低 | 遗漏 | DataPermission 管理员判断字符串比较 |
| R-11 | 低 | 遗漏 | Excel 导出临时文件无清理 |

### 与原审报告的关系

- 原审 V2/V3 的 **26 项发现中，约 22 项结论正确**，质量较高
- 本复审新增 **4 项严重/中级问题**（R-05, R-06, R-07, R-09）
- 修正 **3 项方案设计**（R-01, R-02, R-03）
- 补充 **2 项低级遗漏**（R-10, R-11）

### 建议修复优先级

1. **立即修复**：R-06（验证码重放）、R-05（RoleCode 变更丢数据）、R-07（审计失效）
2. **阶段二与 P-02/P-03 一起修**：R-01、R-02（LoadPolicy 策略重新设计）
3. **阶段三**：R-03、R-08、R-09
4. **阶段四**：R-04、R-10、R-11
