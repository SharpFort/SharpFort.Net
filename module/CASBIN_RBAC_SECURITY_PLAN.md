# Casbin RBAC 安全增强计划 (Security Hardening)

## 1. 目标
解决审计报告 "2.4 安全性与防御" 和 "2.5 可维护性与审计" 中提到的风险。
1.  **URL 脆弱性**: 消除对 URL 字符串完全匹配的依赖，转为更稳定的资源代码 (Resource ID)。
2.  **规避大小写绕过**: 强制 URL 归一化。
3.  **可观测性**: 提供鉴权诊断能力。

## 2. 核心架构设计

### 2.1 资源标识符 (YiPermissionAttribute)
*   **位置**: `Yi.Framework.CasbinRbac.Domain.Shared.Attributes`
*   **示例**:
    ```csharp
    [YiPermission("user:list")] 
    [HttpGet("list")]
    public async Task<List<UserDto>> GetListAsync() ...
    ```
*   **机制**: 
    *   在 Controller Action 上标记唯一的权限码。
    *   Casbin 策略存储 `p, role, tenant, user:list, GET` (不再存 `/api/user/list`)。

### 2.2 权限扫描器 (PermissionScanner)
*   **生命周期**: Singleton, Startup.
*   **逻辑**: 
    *   扫描所有 Controller。
    *   提取 `[YiPermission]` 和 `[HttpMethod]` + `[Route]`。
    *   构建映射表: `Dictionary<UrlPattern, PermissionCode>` (考虑正则或 AntPathMatcher)。
    *   *过渡方案*: 为了兼容旧有 URL 策略，我们采用 "混合模式" —— 中间件先尝试匹配 PermissionCode，匹配不到则降级使用 Raw URL。

### 2.3 中间件增强 (Middleware Enhancement)
*   **URL 归一化**: `path.ToLower().TrimEnd('/')`。
*   **双重鉴权逻辑**:
    1.  如果 Action 有 `YiPermission`，则 Enforce(sub, dom, **PermissionCode**, act)。
    2.  否则，Enforce(sub, dom, **NormalizedUrl**, act)。
*   **调试头 (Debug Header)**:
    *   如果请求头包含 `X-Casbin-Debug: true` 且用户是 Admin。
    *   Response Header 添加 `X-Casbin-Result: Allowed/Denied`, `X-Casbin-Sub: ...`。

## 3. 实施步骤

1.  创建 `YiPermissionAttribute`.
2.  创建 `PermissionScanner` (利用 ABP 的 EndpointDataSource 或反射).
3.  改造 `CasbinAuthorizationMiddleware`:
    *   注入 `PermissionScanner` (获取映射) 或 `IEndpointFeature` (直接从 HttpContext 获取 Endpoint Metadata).
    *   实现归一化和诊断逻辑.

### 特别优化: 利用 ASP.NET Core Endpoint Routing
不需要自己写正则匹配 URL！
ASP.NET Core 中间件执行时，路由已经匹配完毕。
我们可以直接从 `HttpContext.GetEndpoint()` 获取 `Endpoint`，然后读取上面的 Metadata (`YiPermissionAttribute`)。
**这是最完美、性能最高的方案。**

## 4. 影响范围
*   **Application/Web 层**: 需要修改中间件。
*   **Domain.Shared**: 新增 Attribute。
*   **现有数据**: 数据库里的 Casbin 规则目前是 URL 格式。**实施此改动后，需提供 SQL 脚本或 Seed方法将 URL 规则迁移为 Code 规则** (或者暂时保留 URL 兼容)。
    *   *策略*: 暂时保持 URL 兼容模式。鉴权时优先取 Code，取不到用 URL。后续逐步迁移数据。
