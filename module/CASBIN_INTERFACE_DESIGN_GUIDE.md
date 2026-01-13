# Casbin 模式接口设计与双写架构指南

## 1. 双写模式 (Dual-Write) 深度解析

### 1.1 定义与流程
“双写模式”是指在权限管理系统中，同时维护**业务配置数据（传统表）**和**鉴权规则数据（Casbin表）**，并保持两者单向同步的架构模式。

*   **Master (主数据)**: 传统 RBAC 表 (`SysMenu`, `SysRole`, `SysUserRole` 等)。
    *   **作用**: 存储 UI 元数据（菜单名、图标、排序）、层级结构、前端权限码 (`PermCode`)。
    *   **读写者**: 后台管理接口（Admin API）、前端菜单渲染组件。
*   **Slave (从数据/索引)**: Casbin 规则表 (`CasbinRule`)。
    *   **作用**: 存储扁平化的鉴权规则，专为高性能匹配设计。
    *   **读写者**: 鉴权中间件（Middleware）、`CasbinPolicyManager`（同步器）。

### 1.2 运作流程图解

1.  **配置阶段 (Configure)**:
    *   管理员在前端给“角色A”勾选菜单。
    *   后端更新 `SysRoleMenu` 表。
    *   **同步触发**: `CasbinPolicyManager` 读取 Menu 的 `ApiUrl` 和 `ApiMethod`，生成策略写入 `CasbinRule`。

2.  **运行阶段 (Runtime)**:
    *   **动作**: 用户发起请求 `GET /api/user`。
    *   **鉴权**: 中间件拦截 URL，查询 `CasbinRule` 表，不关心 `PermCode`。

---

## 2. 接口设计：RESTful vs Action Code

这是 Casbin 模式与传统模式最大的区别。

### 2.1 模式对比

| 特性 | **Casbin (RESTful 模式)** | **传统 RBAC (Action Code)** |
| :--- | :--- | :--- |
| **鉴权对象** | **Resource (URL)** | **Operation (String)** |
| **示例** | `GET /api/users/1` | `user:view` |
| **后端实现** | 全局中间件拦截 URL | Controller 方法上加 `[Permission("user:view")]` |
| **优点** | 解耦、通用、网关友好、无需侵入业务代码 | 语义明确、对前端友好 |
| **缺点** | URL 变更需同步策略 | 需要硬编码 Attribute |

### 2.2 最佳实践：混合模式

我们推荐 **后端纯 RESTful，前端保留 Action Code**。

*   **后端 (Casbin)**: 只认 `URL + Method`。
*   **前端 (Vue/React)**: 使用 `PermCode` 控制按钮显隐。
*   **连接点**: `SysMenu` 表。

### 2.3 接口定义示例 (SysMenu 配置)

| 业务场景 | Controller 定义 | 菜单表配置 (Casbin字段) | 菜单表配置 (前端字段) |
| :--- | :--- | :--- | :--- |
| **查看列表** | `[HttpGet("api/user")]` | `ApiUrl=/api/user`, `Method=GET` | `PermCode=system:user:list` |
| **新增** | `[HttpPost("api/user")]` | `ApiUrl=/api/user`, `Method=POST` | `PermCode=system:user:add` |
| **修改** | `[HttpPut("api/user/{id}")]` | `ApiUrl=/api/user/*`, `Method=PUT` | `PermCode=system:user:edit` |
| **删除** | `[HttpDelete("api/user/{id}")]` | `ApiUrl=/api/user/*`, `Method=DELETE` | `PermCode=system:user:delete` |
| **重置密码** | `[HttpPost("api/user/{id}/pwd")]` | `ApiUrl=/api/user/*/pwd`, `Method=POST` | `PermCode=system:user:reset` |

## 3. 关键策略详解

### 3.1 通配符的使用 (Wildcards)
Casbin 支持强大的 `KeyMatch` 函数，使得 URL 鉴权比传统字符串更灵活。

*   **场景**: 接口包含动态 ID，如 `/api/users/123/roles`。
*   **配置**:
    *   **推荐**: `/api/users/*/roles` (使用 `keyMatch2`)。
    *   **参数化**: `/api/users/:id/roles` (同样支持)。
*   **注意**: 这里的 `*` 匹配一级路径。如果需要匹配多级，可能需要 `keyMatch3` 或正则。

### 3.2 公共接口处理
对于下拉框字典、省市区数据等**非敏感、高频调用**的辅助接口：
1.  **方案 A (推荐)**: 在中间件中通过 `[AllowAnonymous]` 特性放行。
2.  **方案 B**: 配置一条针对所有登录用户 (`role_user`) 的宽泛 Casbin 策略，如 `p, role_user, *, /api/common/*, GET`。

### 3.3 调试技巧
如果遇到 403 错误，但你确定配置了策略：
1.  检查 **URL 大小写**：Casbin 默认是大小写敏感的。建议在中间件统一转小写。
2.  检查 **Method**：确保数据库存的是 `GET` 而不是 `get`。
3.  检查 **租户 ID**：确保当前用户的 TenantId 与策略中的 Domain 一致。



我查看了这个文档:https://www.casbin.org/zh/docs/rbac-with-domains-api ,貌似casbin的接口设计和传统的接口 权限授权模式 不一样呢?请你继续阅读casbin api相关的文档,给出最新最正确的接口及权限授权的设计,并和传统的接口及权限授权做对比.

Gemini
这是一个非常敏锐且关键的观察。Casbin 的官方文档确实提到了 RESTful API 的设计模式，这与传统的 Action Based（如 user:list, user:add）鉴权模式有本质的区别。

让我们来详细对比，并结合你提到的文档内容，给出“最新最正确”的设计方案。

特性	Casbin RESTful 模式 (推荐)	传统 RBAC (Action Code)
Object (资源)	URL 路径 (e.g., /api/users/123)	权限字符串 (e.g., user:edit)
Action (动作)	HTTP 方法 (e.g., GET, POST)	隐式包含 (e.g., user:list 中的 list)
匹配方式	KeyMatch / RegexMatch (路径匹配)	字符串完全相等匹配
灵活性	极高。无需修改代码，只改数据库即可拦截新接口。	较低。需要在 Controller 上加 Attribute [Permission("user:edit")]。
直观性	直观对应 HTTP 请求。	对应业务语义，对前端友好。
缺点	URL 变更会导致权限失效；粒度太细可能难以管理。	需要硬编码 Attribute；前后端通过字符串耦合。
解耦：鉴权逻辑完全从代码中剥离（Attributes），移入配置（Policy）。
通用性：这是微服务和网关层的标准鉴权方式。网关只知道 URL，不知道什么 user:list。
标准化：RESTful 本身就用 GET/POST/PUT/DELETE 表达了 查/增/改/删，Casbin 完美契合这一点。
在现代化的微服务/前后端分离架构中，我们推荐 混合模式，但以 RESTful 为主，Action Code 为辅。

后端只认 URL。这也是我在 CasbinAuthorizationMiddleware 中实现的方式。

Object: /api/system/user/*
Action: GET
中间件逻辑:
var obj = Request.Path.Value; // "/api/system/user/1"
var act = Request.Method;     // "GET"
Enforce(user, tenant, obj, act);


虽然后端只认 URL，但前端按钮（如“删除”按钮）直接绑定 URL 会很别扭。前端更习惯 v-auth="'user:delete'"。

解决方案：在 SysMenu 表中同时维护两套数据。
ApiUrl: /api/system/user/* (给后端 Casbin 用)
ApiMethod: DELETE (给后端 Casbin 用)
PermCode: system:user:delete (给前端 Vue 用)
不再使用 [Permission] 特性，而是依赖标准的路由。

示例代码:

[Route("api/system/user")]
public class UserController : Controller
{
    // 对应策略: (role, tenant, /api/system/user, GET)
    // 对应前端码: system:user:list
    [HttpGet]
    public IActionResult GetList() { ... }

    // 对应策略: (role, tenant, /api/system/user, POST)
    // 对应前端码: system:user:add
    [HttpPost]
    public IActionResult Create() { ... }

    // 对应策略: (role, tenant, /api/system/user/*, PUT)
    // 对应前端码: system:user:edit
    [HttpPut("{id}")]
    public IActionResult Update(string id) { ... }
}


