# Casbin-RBAC 模块代码审核报告 V3（第二轮复核深化）

> 审核日期：2026-05-19
> 版本链：V1（初稿）→ V2（专家复核修正）→ **V3（第二轮复核深化，代码级颗粒度）**
> 审核范围：`module/casbin-rbac/` 全部代码 + `src/Sf.Abp.Web/appsettings.json` + `SfAbpWebModule.cs` 管道
> 审核维度：性能、安全、业务逻辑

---

> **与 V2 的关系**：V3 在 V2 基础上深化了 4 个发现，未改动 V2 中已有的结论和定级。建议对照 V2 阅读，V3 新增/深化内容以 `★V3` 标记。

---

## V3 变更总览

| 编号 | 变更类型 | 简述 |
|------|---------|------|
| S-01 | **深化** | 修复方案从"移除 [AllowAnonymous]"细化为精确的 `[Authorize(Roles = "admin")]` |
| S-02 | **扩展** | 双写 Bug 范围从 CreateAsync 扩展到 UpdateAsync，给出精确行号 |
| B-02 | **深化** | 暴露 Phase 1 Raw SQL 缺少 `tenant_id` 字段的具体行号 |
| B-06 | **深化** | 新增 `keyMatch2` 的 `{id}` vs `:id` 格式不兼容子问题 |

---

## 一、关于您提出的两个问题

> V3 维持 V2 结论不变。详见 `CODE_AUDIT_REPORT.md`（V2）。

---

## 二、★V3 深化/扩展的安全问题

### 【P0】S-01: 迁移接口无权限控制 ★V3 深化

**V2 建议**：移除 `[AllowAnonymous]`，仅允许超级管理员调用。

**V3 深化分析**：仅移除 `[AllowAnonymous]` 不够精确。在 ABP 框架下，ApplicationService 方法默认需要认证，移除后**任何已登录用户均可调用**。但迁移端点应**仅限超级管理员**。

**验证依据**：JWT Claims 中已存储角色信息（`AccountManager.UserInfoToClaim:198` 将 RoleCodes 写入 `AbpClaimTypes.Role`），管道末尾的 `UseAuthorization()`（`SfAbpWebModule.cs:397`）能校验 `[Authorize(Roles = ...)]`。

**V3 精确修复代码**：
```csharp
// CasbinMigrationService.cs:26
// 替换：
// [Microsoft.AspNetCore.Authorization.AllowAnonymous] // 删除此行
// 为：
[Authorize(Roles = "admin")]  // 仅超级管理员可调用
public async Task<object> MigrateAllAsync()
```
> 注：使用字符串常量 `"admin"` 而非 `UserConst.AdminRolesCode`，因 Attribute 参数必须是编译期常量。

---

### 【P0】S-02: UserService 中 Casbin 策略双写 Bug ★V3 扩展

**V2 仅覆盖 CreateAsync，V3 发现 UpdateAsync 同样受影响。**

#### 位置 1：CreateAsync（V2 已发现）

**文件**: `UserService.cs:119,137`
```csharp
await _userManager.GiveUserSetRoleAsync([entitiy.Id], input.RoleIds ?? []);  // 行119：已处理 DB+内存
await SyncCasbinUserRoles(entitiy.Id, input.RoleIds ?? []);                   // 行137：★重复写入
```

#### 位置 2：UpdateAsync（★V3 新发现）

**文件**: `UserService.cs:218,223-225`
```csharp
await _userManager.GiveUserSetRoleAsync([id], input.RoleIds ?? []);            // 行218：已处理 DB+内存

// ★以下三行全部冗余且危险：
await _enforcer.RemoveFilteredGroupingPolicyAsync(0, id.ToString());            // 行223：仅内存操作
await SyncCasbinUserRoles(id, input.RoleIds ?? []);                            // 行225：内存 + DB SavePolicy
```

#### ★V3 调用链完整追踪

`GiveUserSetRoleAsync` → `CasbinPolicyManager.SetUserRolesAsync` 已完整处理：
1. **DB**: 物理删除旧 g-rules（`DELETE ... WHERE ptype='g' AND v0=sub AND v2=domain`）+ 批量插入新 g-rules ✓
2. **内存**: 移除旧角色（`RemoveGroupingPolicyAsync`）+ 添加新角色（`AddGroupingPoliciesAsync`）✓
3. **最终一致性**: 触发 `TriggerMemorySync`（UOW 完成后 `LoadPolicyAsync`）✓

`SyncCasbinUserRoles`（冗余路径）：
1. **内存**: `AddGroupingPoliciesAsync` — 重复添加（与 SetUserRolesAsync 中的添加重复）
2. **DB**: `SavePolicyAsync()` — ★V3 核实：当前 DI 注册的是 `SqlSugarAdapter`（NuGet 包），其 `SavePolicyAsync` 可正常持久化，**导致 DB 产生重复记录**

> ★V3 技术纠正：第二轮复核报告称 `SavePolicyAsync` 因 AutoSave 禁用而抛出异常——此说不准确。`ScopeFactoryCasbinAdapter`（会抛异常的那个）并未被 DI 注册（`SharpFortCasbinRbacSqlSugarCoreModule.cs` 注册的是 `new SqlSugarAdapter(...)`）。实际运行的 `SqlSugarAdapter` 的 `SavePolicyAsync` 正常执行持久化，所以 DB 重复确实会发生。

**V3 精确修复代码**：
```csharp
// ========== CreateAsync ==========
// 删除第 137 行：
// await SyncCasbinUserRoles(entitiy.Id, input.RoleIds ?? []);  // ← 删除整行

// ========== UpdateAsync ==========
// 删除第 223-225 行：
// await _enforcer.RemoveFilteredGroupingPolicyAsync(0, id.ToString());  // ← 删除
// await SyncCasbinUserRoles(id, input.RoleIds ?? []);                    // ← 删除

// ========== 同时删除整个私有方法 ==========
// 删除 UserService.cs 第 143-165 行的 SyncCasbinUserRoles 方法
```

---

### 【P0】S-03: 找回密码发送验证码接口缺少 [AllowAnonymous]

> V3 维持 V2 结论不变。

### 【P0】S-04: ABP UOW 与 SqlSugar Casbin 操作的事务隔离风险

> V3 维持 V2 结论不变。

---

## 三、性能问题

> V3 维持 V2 所有结论不变（P-01 ~ P-07）。详见 `CODE_AUDIT_REPORT.md`（V2）。

---

## 四、★V3 深化的业务逻辑问题

### 【P0】B-02: CasbinSeedService 忽略多租户 ★V3 深化

**V2 发现**：`CasbinSeedService.cs:43` 中 `string domain = "default"` 硬编码。

**★V3 深化**：问题不仅在第 43 行。Phase 1 的 Raw SQL 根本没有查询 `tenant_id` 字段。

#### 盲区 1：Role 查询缺少 tenant_id

**文件**: `CasbinSeedService.cs:69-72`
```sql
-- 当前 SQL（缺少 tenant_id）：
SELECT id, role_code, role_name, state
FROM casbin_sys_role
WHERE is_deleted = false
```
注释甚至写了 `no tenant_id needed`——这对多租户场景是错误的。

**需要改为**：
```sql
SELECT id, role_code, role_name, state, tenant_id
FROM casbin_sys_role
WHERE is_deleted = false
```

#### 盲区 2：Menu 查询同样缺少 tenant_id

**文件**: `CasbinSeedService.cs:88-91`
```sql
-- 当前 SQL：
SELECT id, menu_name, api_url, api_method, state
FROM casbin_sys_menu
WHERE is_deleted = false
```
**需要改为**：
```sql
SELECT id, menu_name, api_url, api_method, state, tenant_id
FROM casbin_sys_menu
WHERE is_deleted = false
```

#### 盲区 3：Phase 2 数据结构不含 tenant_id

**文件**: `CasbinSeedService.cs:50-52`
```csharp
// 当前 roleData 元组定义（无 tenant_id）：
List<(Guid Id, string RoleCode, string RoleName, bool State)> roleData;
```
**需要改为**：
```csharp
List<(Guid Id, string RoleCode, string RoleName, bool State, Guid? TenantId)> roleData;
```

#### 盲区 4：domain 构建逻辑

**文件**: `CasbinSeedService.cs:43`
```csharp
string domain = "default";  // 所有租户共用一个域
```
**需要改为**：按租户分别构建 domain：
```csharp
// 在构建每条规则时：
string domain = role.TenantId?.ToString() ?? "default";
```

---

### 【P1】B-06: Casbin 中间件大小写一致性 + ★V3 keyMatch2 格式陷阱

**V2 发现**：`keyMatch2` 大小写敏感。

**★V3 新增子问题**：ASP.NET `{id}` vs Casbin `:id` 格式不兼容。

#### 问题本质

Casbin 的 `keyMatch2` 支持两种参数通配：
- `/api/app/user/:id` → 匹配 `/api/app/user/123`（命名参数）
- `/api/app/user/*` → 匹配任意路径（全通配）

但 **不支持** ASP.NET Core 的 `{id}` 大括号格式。

#### 实际影响面

代码中存在大量 `{parameter}` 路由：

| 文件 | 路由 |
|------|------|
| `RoleService.cs:157` | `[Route("role/{id}/{state}")]` |
| `AccountService.cs:391` | `[Route("account/Vue3Router/{routerType?}")]` |
| `AuthService.cs:39` | `[HttpGet("auth/oauth/login/{scheme}")]` |

`ApiScanner.cs` 的 `ReplacePlaceholders`（行 100-108）仅处理 `[controller]` 和 `[action]`，不处理 `{parameter}`：
```csharp
path = path.Replace("[controller]", cName, StringComparison.OrdinalIgnoreCase);
path = path.Replace("[action]", actionName, StringComparison.OrdinalIgnoreCase);
// ★没有 {id} → :id 的转换！
```

#### 后果

如果菜单表的 `ApiUrl` 存储了 `{id}` 格式（无论是 ApiScanner 自动扫描还是管理员手动录入），实际请求路径 `/api/app/role/abc-123/true` 将**无法匹配**策略中存储的 `/api/app/role/{id}/{state}`。

当前模型 Matcher：`keyMatch2(r.obj, p.obj)` —— Casbin 不认识 `{id}` 语法，匹配直接失败 → 返回 403。

#### V3 精确修复建议

1. **ApiScanner 增加格式转换**（`ApiScanner.cs`）：
```csharp
// ReplacePlaceholders 方法中增加：
path = Regex.Replace(path, @"\{(\w+)\??\}", @":$1");
// 效果：{id} → :id, {routerType?} → :routerType
```

2. **MenuService 增加校验**（`MenuService.cs` CreateAsync/UpdateAsync）：
```csharp
// 在 CreateAsync/UpdateAsync 中增加：
if (!string.IsNullOrWhiteSpace(input.ApiUrl) && input.ApiUrl.Contains('{'))
{
    throw new UserFriendlyException("ApiUrl 不支持 {param} 格式，请使用 :param 或 * 通配符。示例：/api/app/user/:id");
}
```

3. **文档化约定**：管理员录入菜单 API 路径时使用 Casbin 兼容格式。

---

## 五、V3 完整问题清单

★ 标记表示 V3 新增或深化。未标记项维持 V2 原样。

| 编号 | 类别 | 严重度 | 问题简述 | V3 状态 |
|------|------|--------|---------|---------|
| S-01 | 安全 | P0 | 迁移接口无权限控制 | ★深化：精确 `[Authorize(Roles = "admin")]` |
| S-02 | 安全 | P0 | Casbin 策略双写（CreateAsync + UpdateAsync） | ★扩展：新增 UpdateAsync 路径 |
| S-03 | 安全 | P0 | 找回密码缺 [AllowAnonymous] | 维持 |
| S-04 | 安全 | P0 | UOW 事务隔离风险 | 维持 |
| S-05 | 安全 | P1 | JWT 密钥硬编码 | 维持 |
| S-06 | 安全 | P1 | 默认密码过弱 | 维持 |
| S-07 | 安全 | P1 | JWT 无黑名单 | 维持 |
| S-08 | 安全 | P2 | EnableCachedEnforcer 虚设 | 维持 |
| S-09 | 安全 | P2 | 手机验证码绕过 | 维持 |
| S-10 | 安全 | P3 | OperLog 敏感信息泄露 | 维持 |
| P-01 | 性能 | P0 | Token 生成未用缓存 | 维持 |
| P-02 | 性能 | P0 | 策略变更全量重载 | 维持 |
| P-03 | 性能 | P1 | 并发 LoadPolicy 竞态 | 维持 |
| P-04 | 性能 | P2 | IgnoreUrls O(n) 遍历 | 维持 |
| P-05 | 性能 | P2 | MenuService 循环刷新 | 维持 |
| P-06 | 性能 | P2 | DataPermission LIKE 全表扫描 | 维持 |
| P-07 | 性能 | P3 | Task.Delay(100) hack | 维持 |
| B-01 | 业务 | P0 | 登录拦截注释矛盾 | 维持 |
| B-02 | 业务 | P0 | 迁移忽略多租户 | ★深化：Raw SQL 缺 tenant_id |
| B-03 | 业务 | P1 | sys-admin 硬编码 | 维持 |
| B-04 | 业务 | P1 | 模型 Matcher 冗余快速路径 | 维持 |
| B-05 | 业务 | P1 | RoleCode 事务原子性 | 维持 |
| B-06 | 业务 | P1 | 大小写 + keyMatch2 `{id}` 格式陷阱 | ★深化：新增 `{id}` vs `:id` 子问题 |
| B-07 | 业务 | P2 | 跨租户唯一性缺 TenantId | 维持 |
| B-08 | 业务 | P2 | 用户删除策略清理不一致 | 维持 |
| B-09 | 业务 | P3 | ls_ 前缀限制位置不当 | 维持 |

---

## 六、分阶段修复计划（V3 修订版）

### 阶段一：安全漏洞封堵（5 项，P0）

| 编号 | 文件 | 精确修复操作 |
|------|------|-------------|
| **S-01** | `CasbinMigrationService.cs:26` | ★替换 `[AllowAnonymous]` 为 `[Authorize(Roles = "admin")]` |
| **S-02** | `UserService.cs:137,143-165,223-225` | ★删除 `SyncCasbinUserRoles` 方法及 CreateAsync/UpdateAsync 中所有调用 |
| **S-03** | `AccountService.cs:200` | 添加 `[AllowAnonymous]` |
| **S-04** | `CasbinPolicyManager.cs` | 验证 `_roleRepository._Db` 与 ABP UOW 共享连接 |
| **B-02** | `CasbinSeedService.cs:50,69,88,43` | ★Phase 1 SQL 增加 `tenant_id`；Phase 2 按租户构建 domain |

### 阶段二：性能与并发重构（5 项，P0/P1）

| 编号 | 修复操作 |
|------|---------|
| **P-01** | `AccountManager.cs:50`：`GetInfoAsync` → `GetInfoByCacheAsync` |
| **P-02** | `CasbinPolicyManager.cs:58`：移除 `uow.OnCompleted(LoadPolicyAsync)`，仅依赖内存增量 API |
| **P-03** | `CasbinPolicyManager.cs`：新增全局 Debounce（`Interlocked` + `Timer`），合并 200ms 内多次触发 |
| **S-08** | 升级 Casbin.NET 或实现 `IEnforcer` 缓存装饰器 |
| **P-06** | `SfCasbinRbacDbContext.cs:94`：`Contains` → 精确前缀匹配或 Ltree |

### 阶段三：安全加固与业务修复（7 项，P1）

| 编号 | 修复操作 |
|------|---------|
| **S-05** | JWT 密钥迁移到环境变量 |
| **S-07** | Redis JWT 黑名单 + JTI 校验 |
| **B-01** | 更新注释或放开登录限制 |
| **B-03** | 模型去掉 `g(r.sub, "sys-admin", r.dom) \|\|` |
| **B-04** | 同上，仅依赖通配符策略 |
| **B-05** | 将 Casbin 操作纳入同一 UOW |
| **B-06** | ★路径统一小写 + `ApiScanner` 增加 `{param}`→`:param` 转换 + `MenuService` 格式校验 |

### 阶段四：清理与规范化（7 项，P2/P3）

| 编号 | 修复操作 |
|------|---------|
| S-06 | 首次启动强制改密码 |
| S-09 | 分离手机验证码开关 |
| S-10 | OperLog 脱敏 |
| P-04 | IgnoreUrls 预处理为 HashSet |
| B-07 | 增加 TenantId 过滤 |
| B-08 | DeleteAsync 统一使用 CasbinPolicyManager |
| B-09 | ls_ 前缀校验移到 UserManager |

---

## 附录 A：V2 → V3 技术纠正记录

| 复核报告主张 | V3 独立判断 | 理由 |
|-------------|-----------|------|
| S-02: `SavePolicyAsync` 因 AutoSave 禁用会抛出异常 | **认可结论，纠正原因** | 实际 DI 注册的是 `SqlSugarAdapter`（NuGet 包），`SavePolicyAsync` 正常执行；抛异常的是未被注册的 `ScopeFactoryCasbinAdapter` |
| S-01: 使用 `[Authorize(Roles = UserConst.AdminRolesCode)]` | **认可方向，纠正语法** | Attribute 参数必须是编译期常量，`UserConst.AdminRolesCode` 不满足；改为字符串字面量 `"admin"` |
| B-02: Raw SQL 缺少 tenant_id | **完全认可** | 代码注释甚至写了 "no tenant_id needed"，需要纠正 |
| B-06: keyMatch2 与 {id} 不兼容 | **完全认可** | 验证了模型 Matcher、ApiScanner 实现、实际控制器路由，三重确认 |

## 附录 B：V2 快照

V2 完整内容见 `CODE_AUDIT_REPORT.md`。V3 仅包含变更项和完整的问题清单/修复计划，所有未变更内容请参阅 V2。
