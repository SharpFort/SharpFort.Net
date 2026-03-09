# Casbin 鉴权模块 Bug 分析报告

## 1. 问题现象
*   前端配置 `IgnoreUrls` 时正常访问。
*   注释 `IgnoreUrls` 后，接口返回 403 Forbidden。
*   数据库 `casbin_rule` 表中有数据，结构为 `p_type, v0, v1, v2, v3`。

## 2. 原因分析 (Root Cause)

经过对 `SharpFort.CasbinRbac` 模块源码的深度分析，发现 **中间件 (Middleware)** 与 **策略管理器 (PolicyManager)** 在生成用户主体 (Subject) 标识时存在不一致，这是导致鉴权失败的根本原因。

### 2.1 主体标识不匹配 (Subject Mismatch)

*   **策略写入端**: `CasbinPolicyManager.cs`
    ```csharp
    private string GetUserSubject(Guid userId) => $"u_{userId}";
    ```
    策略管理器在写入 `g` 策略（用户-角色关联）时，明确使用了 `u_` 前缀加上用户 ID (例如: `u_3fa85f64-...`)。数据库中的 `v0` 列存储的就是这种格式。

*   **鉴权执行端**: `CasbinAuthorizationMiddleware.cs`
    ```csharp
    var sub = _currentUser.Id?.ToString();
    ```
    鉴权中间件在构建请求上下文时，直接使用了原始的用户 ID UUID 字符串 (例如: `3fa85f64-...`)，**缺少了 `u_` 前缀**。

**结论**: 当请求到达 Casbin Enforcer 时，它试图查找主体为 `3fa85f64-...` 的权限策略，但数据库中存储的主体是 `u_3fa85f64-...`。由于标识不匹配，Casbin 无法找到该用户的角色关联，导致默认拒绝 (403)。

### 2.2 潜在的数据格式问题
*   **API 方法 (Action)**: 中间件使用 `context.Request.Method.ToUpper()` (如 `GET`)。如果数据库中 `ApiMethod` (对应 `casbin_rule` 的 `v3`) 存的是小写 `get`，会导致匹配失败。
*   **API 路径 (Object)**: 中间件使用 `path` (如 `/api/user/list`)。如果数据库中 `ApiUrl` (对应 `casbin_rule` 的 `v2`) 存的是 `api/user/list` (无前导斜杠)，可能会影响匹配准确性。

## 3. 关于数据结构的解答

**问题**: *是否在此表 (`casbin_rule`) 中还要存放 PermissionCode/Component 等信息么?*

**回答**: **不需要**。
*   `casbin_rule` 表是 Casbin 引擎专用的策略存储表，设计原则是**极简**，仅关注 **谁 (Subject)** 在 **哪 (Domain)** 对 **什么 (Object)** 能 **做什么 (Action)**。目前的 `v0/v1/v2/v3` 结构完全符合标准。
*   `PermissionCode` (权限标识)、`Component` (前端组件路径)、`Icon` (图标)、`Router` (前端路由) 等属于 **业务展示元数据**，应当且必须存储在 `casbin_sys_menu` (菜单表) 中。前端菜单渲染和按钮控制读取 `casbin_sys_menu`，后端接口鉴权读取 `casbin_rule`，两者职责分离。

## 4. 解决方案 (Solution)

### 步骤 1: 修正中间件代码 (Code Fix)
修改 `module/casbin-rbac/SharpFort.CasbinRbac.Domain/Authorization/CasbinAuthorizationMiddleware.cs`，统一加上 `u_` 前缀以匹配数据库策略。

```csharp
// 修改前
var sub = _currentUser.Id?.ToString();

// 修改后
var sub = $"u_{_currentUser.Id}";
```

### 步骤 2: 数据一致性检查 (Data Review)
建议检查 `casbin_sys_menu` 表中的数据，确保：
1.  `ApiUrl`: 建议以 `/` 开头 (例如 `/api/system/user`)，以匹配 `Request.Path`。
2.  `ApiMethod`: 建议全大写 (例如 `GET`, `POST`, `PUT`, `DELETE`, `*`)，以匹配 `Request.Method`。

## 5. 待确认
请审查以上分析。确认无误后，我将执行 **步骤 1** 的代码修改。
