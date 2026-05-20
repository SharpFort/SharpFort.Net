# Casbin-RBAC 模块代码审核报告（v2 — 经专家复核修正）

> 审核日期：2026-05-19（初稿）→ 2026-05-19（复核修正）
> 审核范围：`module/casbin-rbac/` 全部代码 + `src/Sf.Abp.Web/appsettings.json` 配置 + `SfAbpWebModule.cs` 管道
> 审核维度：性能、安全、业务逻辑

---

## 零、复核修正说明

初稿经另一位专家独立审查后，发现 **2 处误判**和 **4 项遗漏**，本版已全面修正。修正点以 `[已修正]` / `[新增]` 标注。

---

## 一、关于您提出的两个问题（优先回复）

### 1. IgnoreUrls 配置过多的分析与建议

#### 当前配置
```json
"IgnoreUrls": [
    "exact:/api/app/account",
    "exact:/api/app/account/logout",
    "exact:/api/app/account/update-password",
    "exact:/api/app/account/update-icon",
    "/api/app/account/vue3router",
    "/api/app/account/login",
    "/api/app/account/register",
    "/api/app/captcha",
    "/swagger",
    "/hangfire",
    "/api/app/menu"
]
```

#### 管道全貌（关键！）

```
UseRefreshToken → UseAuthentication → CasbinAuthorizationMiddleware
    → ...静态文件... → UseUnitOfWork → UseAuthorization → UseAuditing → Endpoints
```

`UseAuthorization()` 位于 Casbin 中间件**之后**（`SfAbpWebModule.cs:397`）。因此：

- **`[Authorize]` 仍然生效**：即使 Casbin 中间件因 IgnoreUrls 放行，下游的 `UseAuthorization()` 仍会校验 JWT。
- **`[AllowAnonymous]` 也仍然生效**：Casbin 步骤 1 和 `UseAuthorization()` 都会识别并放行。

#### 修正后的分析

**[已修正]** 初稿认为 `IgnoreUrls` 会使 `[Authorize]` 形同虚设、`/api/app/menu` 可被未登录用户访问——**这是误判**。`UseAuthorization()` 在管道末尾仍会拦截未认证请求。

**真实问题**：

| 条目 | 问题等级 | 说明 |
|------|---------|------|
| `login`, `register`, `captcha` | 冗余（非 Bug） | 已有 `[AllowAnonymous]`，在 IgnoreUrls 中完全多余，但不会造成安全漏洞 |
| `/api/app/menu` | 低风险 | 绕过 Casbin 策略检查，但 `[Authorize]` 仍在。**任何已登录用户都能访问所有菜单端点**，这个到底是"特性"还是"漏洞"取决于业务语义 |
| `/swagger`, `/hangfire` | 合理 | 基础设施端点，不归 Casbin 管 |
| `exact:/api/app/account` 等 | 合理 | 通用端点，所有登录用户都应能访问 |

#### 建议方案（修正版）

**保守方案（推荐）**：保留当前 IgnoreUrls 结构，仅清理冗余项。

| 操作 | 理由 |
|------|------|
| 移除 `login`, `register`, `captcha` | 已有 `[AllowAnonymous]`，冗余配置 |
| 保留 `/api/app/menu` | `UseAuthorization()` 已确保认证，菜单是所有用户的基础需求 |
| 保留 `exact:/api/app/account` 等 | 通用端点，放行合理 |
| 保留 `/swagger`, `/hangfire` | 基础设施 |

**进阶方案**（如果要极致权限一致性）：创建 `common` 基础角色分配给所有用户，将通用端点从 IgnoreUrls 移到 Casbin 策略中管理。但这会增加策略数量和运行时开销，**不建议仅为"一致性"而做**。

### 2. 超级管理员跳过所有权限检查的分析

#### 当前实现（四层防御）

| 层级 | 位置 | 机制 |
|------|------|------|
| Casbin 模型层 | `rbac_with_domains_model.conf:18` | `g(r.sub, "sys-admin", r.dom)` 硬编码 |
| Casbin 策略层 | `CasbinPolicyManager.InitAdminPermissionAsync:232` | 给 admin 角色分配 `*, *` 通配符策略 |
| JWT Claims 层 | `AccountManager.UserInfoToClaim:190-194` | admin 用户获得 `*:*:*` 和 `admin` 角色 |
| 数据权限层 | `SfCasbinRbacDbContext.DataPermissionFilter:45-47` | admin 用户跳过所有数据过滤 |

#### 建议

**保留超级管理员机制**，优化点：

1. **Casbin 模型中去掉 `"sys-admin"` 硬编码**。既然 `InitAdminPermissionAsync` 已分配通配符策略 `*, *`，Matcher 中的 `g(r.sub, "sys-admin", r.dom)` 就是冗余的快速路径。可去掉它，仅依赖策略匹配。
2. **让 `SuperAdminRoleCode` 配置真正生效**：如果保留模型中的快速路径，应通过模型构建时注入配置值，而非硬编码。
3. **前端将超级管理员角色设为不可删除/不可编辑**。
4. **启动时检测多超管**，发出警告。

---

## 二、安全问题

### 【严重】S-01: 迁移接口无权限控制

**文件**: `CasbinMigrationService.cs:26`
```csharp
[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public async Task<object> MigrateAllAsync()
```
任何人可触发全量策略迁移，清空并重建 casbin_rule 表。

**建议**: 移除 `[AllowAnonymous]`，仅允许超级管理员调用。

### 【严重】S-02: UserService 中 Casbin 策略双写 Bug

**文件**: `UserService.cs:118-137` 和 `UserManager.cs:46-81`

调用链：
1. `CreateAsync` → `_userManager.GiveUserSetRoleAsync()` → `_casbinPolicyManager.SetUserRolesAsync()` → **第一次写入 g 策略**
2. `CreateAsync` → `SyncCasbinUserRoles()` → `_enforcer.AddGroupingPoliciesAsync()` → **第二次写入相同的 g 策略**

**后果**: casbin_rule 表产生重复 g 策略记录，策略数据膨胀。

**建议**: 删除 `UserService.SyncCasbinUserRoles()`，统一使用 `CasbinPolicyManager`。

### 【严重】S-03: 找回密码发送验证码接口缺少 [AllowAnonymous]

**文件**: `AccountService.cs:199-206`
```csharp
[HttpPost("account/captcha-phone/repassword")]
public async Task<object> PostCaptchaPhoneForRetrievePasswordAsync(...)
```
没有 `[AllowAnonymous]`。Casbin 中间件步骤 2 检查 `_currentUser.IsAuthenticated`，未登录用户直接返回 401。找回密码流程完全不可用。

**建议**: 添加 `[AllowAnonymous]`。

### 【严重】S-04: [新增] ABP UOW 与 SqlSugar Casbin 操作的事务隔离风险

**文件**: `CasbinPolicyManager.cs` 全部写方法

```csharp
// 所有 Casbin 写操作都通过 _roleRepository._Db (原始 SqlSugarClient)
await _roleRepository._Db.Insertable(rule).ExecuteCommandAsync();
await _roleRepository._Db.Deleteable<CasbinRule>()...ExecuteCommandAsync();
```

**问题**：`CasbinPolicyManager` 直接使用 `_roleRepository._Db`（底层 SqlSugarClient）。如果 ABP 的 UOW 和 SqlSugar 的 DbConnection 不是同一个实例，Casbin 表的写入将在**独立事务**中执行。一旦业务表操作失败回滚而 Casbin 表已提交，将导致**权限数据与业务数据永久不一致**。

**验证方法**：检查 `CasbinPolicyManager` 操作的 DB 连接是否与当前 UOW 共享同一个 `DbConnection` 和 `DbTransaction`。

**建议**: 
- 确认 SqlSugar 的 `SqlSugarScope` 配置了与 ABP UOW 共享连接
- 或使用 `IUnitOfWorkManager.Current` 获取当前事务的 DB 连接，传给 Casbin 操作

### 【高】S-05: JWT 密钥硬编码在配置文件中

**文件**: `appsettings.json:68-69`

对称密钥明文存储在版本控制中。

**建议**: 使用环境变量或 Secret Manager，appsettings.json 中仅保留占位符。

### 【高】S-06: 默认管理员密码过弱

**文件**: `appsettings.json:98` → `"AdminPassword": "123456"`

**建议**: 首次启动强制修改密码，或生成随机密码。

### 【高】S-07: JWT 无主动撤销/黑名单机制

**[新增]** 当前 `PostLogout` 仅清除缓存中的用户信息（`UserInfoCache`）。由于 JWT 是无状态的，**已被截获的 Token 在过期前仍然有效**。攻击者获取 Token 后可持续访问直到 Token 自然过期（默认 86400 分钟 = 60 天）。

**建议**: 引入 JWT 黑名单（Redis），登出时将 Token 的 JTI（JWT ID）存入 Redis 并设置过期时间 = Token 剩余有效期。Casbin 中间件增加 JTI 校验逻辑（或独立的中间件）。

### 【中】S-08: EnableCachedEnforcer 形同虚设

**文件**: `SharpFortCasbinRbacSqlSugarCoreModule.cs:60-66`
```csharp
if (casbinOptions.EnableCachedEnforcer)
{
    LogCachedEnforcerEnabled(logger);  // 仅打日志！
}
```
配置中 `EnableCachedEnforcer: true` 但实际上没启用缓存。

**建议**: 升级 Casbin.NET 到支持 CachedEnforcer 的版本，或自行实现 `IEnforcer` 缓存装饰器。

### 【中】S-09: 手机验证码注册可绕过

**文件**: `AccountService.cs:322-326` — 当 `EnableCaptcha: false` 时跳过手机验证码校验。

**建议**: 手机验证码验证应与图片验证码开关分离。

### 【低】S-10: OperLog 记录请求参数可能泄露敏感信息

**文件**: `OperLogGlobalAttribute.cs:98-100` — 密码等敏感字段被记录到日志表。

**建议**: 脱敏处理后再序列化。

---

## 三、性能问题

### 【严重】P-01: Token 生成时每次都查询数据库

**文件**: `AccountManager.cs:50` → `UserManager.cs:163-168`

`GetTokenByUserIdAsync` 调用 `_userManager.GetInfoAsync(userId)` ——无缓存版本。每次颁发/刷新 Token 都触发完整连表查询。`GetInfoByCacheAsync` 已实现但只在 `GetInfoListAsync` 中使用。

**建议**: `GetTokenByUserIdAsync` 改为调用 `GetInfoByCacheAsync`。

### 【严重】P-02: 每次 Casbin 策略变更后触发全量重载

**文件**: `CasbinPolicyManager.cs:49-65`

```csharp
uow.OnCompleted(_enforcer.LoadPolicyAsync);  // 每次变更 → 全量 DB 加载
```

策略数量大时是严重的 I/O 和 CPU 毛刺来源。且当前内存中已通过 AddPolicy/RemovePolicy 做了增量更新，全量 LoadPolicy 只是"最终一致性兜底"，但这个兜底代价太大。

**建议**: 
1. **短期**：用内存增量操作替代 LoadPolicy，仅在应用启动或 Redis Watcher 通知时全量加载
2. **中期**：引入全局防抖机制，合并短时间内的多次重载请求

### 【严重】P-03: [新增] 并发 LoadPolicy 竞态条件

**文件**: `CasbinPolicyManager.cs:49-65`

```csharp
// 10 个并发请求同时修改权限 → 10 次 LoadPolicyAsync
uow.OnCompleted(_enforcer.LoadPolicyAsync);
```

`uow.Items` 防抖仅在同一 UOW 内有效。10 个并发 HTTP 请求 = 10 个独立 UOW = 10 次 LoadPolicy 同时触发。Enforcer 内部非线程安全，可能导致内存策略树损坏。

**建议**: 在 `TriggerMemorySync` 中引入**全局 Debounce**（如 Interlocked + Timer），将 100ms 内的多次触发合并为一次。

### 【中】P-04: IgnoreUrls 每请求 O(n) 遍历

**文件**: `CasbinAuthorizationMiddleware.cs:31-48`

每个请求遍历 IgnoreUrls 做 `StartsWith` + `Substring`。建议预处理为 HashSet。

### 【中】P-05: [已修正] MenuService.UpdateAsync 性能问题重新定级

**文件**: `MenuService.cs:81-93`

**初稿**将此归为【高】"N+1 问题"。**复核**指出：该逻辑仅在"菜单 API 路由发生变更"时触发，频次极低；真正的性能瓶颈是每次 `SetRolePermissionsAsync` → `TriggerMemorySync` → `LoadPolicyAsync`，即 N 个角色触发 N 次全量重载。

**修正定级**：从 P0 降为 P2。修复 P-02 后此问题自然缓解。

### 【中】P-06: [新增] DataPermission 树形路径 LIKE 查询

**文件**: `SfCasbinRbacDbContext.cs:94`

```csharp
d.Ancestors!.Contains(currentDeptIdStr)
```

`Contains` 翻译为 `LIKE '%xxx%'`，在大部门表上导致全表扫描。

**建议**: 
- PostgreSQL: 使用 Ltree 扩展
- SQL Server/MySQL: 改为 `LIKE '%,xxx,%'`（逗号包裹的精确匹配）确保索引可用

### 【低】P-07: CasbinSeedService 使用 raw SQL + Task.Delay(100)

迁移中 `Task.Delay(100)` 等待连接释放是 hack。

---

## 四、业务逻辑问题

### 【严重】B-01: 登录拦截逻辑与注释矛盾

**文件**: `AccountManager.cs:58-66`

```csharp
// 临时注释，允许无角色/无权限用户登录
if (userInfo.RoleCodes.Count == 0) { throw ... }
if (userInfo.PermissionCodes.Count == 0) { throw ... }
```

注释与代码矛盾。新注册用户在管理员分配角色前无法登录。

### 【严重】B-02: CasbinSeedService 忽略多租户

**文件**: `CasbinSeedService.cs:43` — `string domain = "default";`

迁移时所有租户数据合并到同一个域，跨租户权限串扰。

### 【高】B-03: Casbin 模型中 sys-admin 硬编码

`rbac_with_domains_model.conf:18` 硬编码 `"sys-admin"`，配置项 `SuperAdminRoleCode` 未生效。

### 【高】B-04: [新增] 模型 Matcher 冗余快速路径

```
m = g(r.sub, "sys-admin", r.dom) || (正常路径...)
```

由于 `InitAdminPermissionAsync` 已分配 `*, *` 通配符策略，正常路径已经能匹配 admin 的所有请求。硬编码快速路径是冗余的，且引入了一个**无法通过配置控制的绕过点**。

**建议**: 移除模型中的 `g(r.sub, "sys-admin", r.dom) ||` 前缀，仅依赖通配符策略。

### 【高】B-05: RoleCode 修改时缺少事务原子性

**文件**: `RoleService.cs:128-135`

Casbin 清理（CleanRolePoliciesByRoleCodeAsync）与业务更新（UpdateAsync）不在同一个原子操作中。

### 【中】B-06: Casbin 中间件大小写一致性问题

`keyMatch2` 大小写敏感，路径 `/api/User` ≠ `/api/user`。

### 【中】B-07: RoleService 跨租户唯一性检查缺失 TenantId 过滤

### 【中】B-08: 用户删除时策略清理路径不一致

`UserService.DeleteAsync` 使用旧式 `_enforcer.RemoveFilteredGroupingPolicyAsync`（直接 Enforcer 调用），而非 `CasbinPolicyManager`。

### 【低】B-09: 注册用户名 ls_ 前缀限制应下沉到领域层

---

## 五、总结与分阶段修复计划

### 阶段一：安全漏洞封堵（P0 — 必须立即修复）

| 编号 | 问题 | 文件 | 修复方式 |
|------|------|------|---------|
| S-01 | 迁移接口无权限控制 | `CasbinMigrationService.cs:26` | 移除 `[AllowAnonymous]` |
| S-02 | Casbin 策略双写 | `UserService.cs:118-137` | 删除 `SyncCasbinUserRoles()` |
| S-03 | 找回密码缺 `[AllowAnonymous]` | `AccountService.cs:199` | 添加 `[AllowAnonymous]` |
| S-04 | UOW 与 Casbin 事务隔离 | `CasbinPolicyManager.cs` | 验证/修复事务共享 |
| B-02 | 迁移忽略多租户 | `CasbinSeedService.cs:43` | 按 TenantId 区分域 |

### 阶段二：性能与并发重构（P1 — 性能核心）

| 编号 | 问题 | 修复方式 |
|------|------|---------|
| P-01 | Token 生成未用缓存 | `GetTokenByUserIdAsync` → `GetInfoByCacheAsync` |
| P-02 | 策略变更全量重载 | 移除 `LoadPolicyAsync`，仅依赖内存增量 API |
| P-03 | 并发 LoadPolicy 竞态 | 全局 Debounce + Interlocked 防抖 |
| S-08 | EnableCachedEnforcer 虚设 | 升级 Casbin.NET 或实现缓存装饰器 |
| P-06 | DataPermission LIKE 查询 | 改用精确前缀匹配 |

### 阶段三：安全加固与体验优化（P2 — 纵深防御）

| 编号 | 问题 | 修复方式 |
|------|------|---------|
| S-05 | JWT 密钥硬编码 | 迁移到环境变量 |
| S-07 | JWT 无黑名单/撤销 | Redis 黑名单 + JTI 校验 |
| B-03/B-04 | sys-admin 硬编码 + 冗余路径 | 移除模型硬编码，仅依赖策略 |
| B-05 | RoleCode 事务原子性 | 统一事务管理 |
| B-06 | 大小写敏感 | 路径统一小写 |

### 阶段四：清理与规范化（P3 — 技术债务）

| 编号 | 问题 |
|------|------|
| S-06 | 默认密码过弱 |
| P-04 | IgnoreUrls O(n) 遍历 |
| B-01 | 登录拦截注释矛盾 |
| B-07~B-09 | 其他中低优问题 |

### 关于 IgnoreUrls 的最终建议

**不做大改**。当前 IgnoreUrls 配合 `UseAuthorization()` 的管道设计是合理且高性能的。仅移除 `login/register/captcha` 三个已有 `[AllowAnonymous]` 的冗余条目即可。不必为"一致性"将通用端点全部迁移到 Casbin 策略中——那会增加运行时开销且收益极小。

### 关于超级管理员的最终建议

**保留机制，去掉硬编码**。将 `rbac_with_domains_model.conf` 中 `g(r.sub, "sys-admin", r.dom) ||` 这层快速路径移除，仅依赖 `InitAdminPermissionAsync` 分配的通配符策略 `*, *`。这样超级管理员权限范围完全由策略数据控制，可通过配置灵活调整。
