# Casbin-RBAC 重构总结与交付文档

## 1. 项目背景与目标
本项目旨在对现有的 `Yi.Framework.Rbac` 模块进行现代化重构，引入 **Casbin** 作为核心鉴权引擎，以支持更细粒度、更灵活的权限控制（API 级鉴权、多租户支持），并优化数据权限逻辑。

## 2. 核心架构变更
采用了 **“绞杀者模式”**，创建了独立的 `casbin-rbac` 模块进行开发，确保现有业务不受影响。

| 维度 | 旧 RBAC 模块 | 新 Casbin-RBAC 模块 |
| :--- | :--- | :--- |
| **鉴权核心** | 自定义 Attribute (`[Permission]`) + 字符串匹配 | **Casbin.NET** (基于策略库 `model.conf`) |
| **拦截方式** | MVC Filter (`PermissionGlobalAttribute`) | **Middleware** (`CasbinAuthorizationMiddleware`) |
| **控制粒度** | Controller/Action 级 | **API URL + Method 级** |
| **多租户** | 部分支持 | **原生支持** (Casbin Domains) |
| **数据权限** | 内存递归 (ToChildList)，性能较低 | **Ancestors 路径优化**，纯 SQL 过滤 |

## 3. 已完成工作 (Done)

### 3.1 基础设施层
- [x] **模块克隆**: 成功创建 `Yi.Framework.CasbinRbac` 模块，完成命名空间迁移。
- [x] **依赖集成**: 集成了 `Casbin.NET` 和 `Casbin.NET.Adapter.SqlSugar`。
- [x] **策略模型**: 配置了 RBAC with Domains 模型 (`rbac_with_domains_model.conf`)。

### 3.2 领域实体层 (Domain)
- [x] **多租户改造**: `User`, `Role`, `Department`, `Position` 实体实现了 `IMultiTenant`。
- [x] **组织架构优化**: `Department` 增加了 `Ancestors` 字段，支持高效树形查询。
- [x] **菜单增强**: `Menu` 实体增加了 `ApiUrl` 和 `ApiMethod` 字段。
- [x] **新表设计**: 创建了 `RoleField` (字段权限) 和 `TableConfig` (元数据) 实体定义。

### 3.3 核心逻辑层 (Managers)
- [x] **策略同步**: 实现了 `CasbinPolicyManager`，封装了 User-Role (`g`) 和 Role-Menu (`p`) 的策略同步逻辑。
- [x] **业务集成**: 改造了 `UserManager` 和 `RoleManager`，在分配角色/菜单时自动同步 Casbin。
- [x] **种子数据**: 创建了 `CasbinSeedService`，用于系统初始化时生成 Admin 权限。

### 3.4 运行时拦截层 (Runtime)
- [x] **中间件**: 实现了 `CasbinAuthorizationMiddleware`，支持 URL 鉴权和白名单 (`[AllowAnonymous]`)。
- [x] **扩展方法**: 提供了 `app.UseCasbinRbac()` 便捷注册方法。

### 3.5 数据权限层 (SqlSugarCore)
- [x] **过滤器重构**: 重写了 `YiCasbinRbacDbContext.DataPermissionFilter`，利用 `Ancestors` 字段实现了高效的 `DEPT_FOLLOW` (本部门及以下) 查询，并完善了 `CUSTOM`, `SELF` 等范围逻辑。

### 3.6 旧代码清理
- [x] **彻底清理**: 移除了所有旧的 `PermissionAttribute`, `PermissionGlobalAttribute` 及其在 Service 中的引用。
- [x] **Salt 移除**: 移除了 `UserDto` 和相关逻辑中的 `Salt` 字段（配合 BCrypt 升级）。

## 4. 交付物清单
1.  **新模块代码**: `module/casbin-rbac/` 下的所有源码。
2.  **配置文件**: `rbac_with_domains_model.conf`。
3.  **开发文档**: `CASBIN_RBAC_REFACTORING_PLAN.md`, `LEGACY_CODE_CLEANUP_PLAN.md`。

## 5. 后续待办与接入指南 (Action Required)

### ⚠️ 接入步骤 (必读)
由于本次重构涉及底层鉴权，Web 层需要手动接入中间件：

1.  **注册模块**: 在 `Yi.Abp.Web` 的模块依赖中，将 `YiFrameworkRbacWebModule` 替换为 `YiFrameworkCasbinRbacWebModule` (需创建或调整引用)。
2.  **配置中间件**: 在 `Yi.Abp.Web` 的 `Program.cs` 或 `Startup.cs` 中：
    ```csharp
    app.UseAuthentication();
    // 添加这一行 👇
    app.UseCasbinRbac(); 
    app.UseAuthorization();
    ```
3.  **执行初始化**: 系统首次启动时，需调用 `CasbinSeedService.SeedAsync()` 以初始化超级管理员权限，否则可能无法访问接口。

### 🚀 未来规划
1.  **字段级权限**: 目前仅定义了表结构，后续需在 DTO 映射层或 JSON 序列化层实现字段过滤逻辑。
2.  **性能调优**: 随着策略数量增长，考虑引入 Casbin 的 `Watcher` 机制实现多实例缓存同步。

## 6. 字段级权限开发指南 (Field Security Guide)

### 6.1 简介
本模块实现了高性能的字段级权限控制 (Field Level Security)，使用了 **JSON 序列化拦截** 技术。
当后端返回 DTO 给前端时，系统会自动检查当前用户的角色是否在 sys_role_field 表中有禁止访问的记录（黑名单）。如果有，该字段会在 JSON 中被彻底移除。

### 6.2 使用方法 (How to Use)
开发者只需在 Output DTO 类上添加 [SecureResource] 特性即可。

**示例代码:**

`csharp
using Yi.Framework.CasbinRbac.Domain.Shared.Attributes;

namespace Yi.Framework.Rbac.Application.Contracts.Dtos.User
{
    // 1. 标记资源名 (对应 RoleField 表中的 TableName)
    [SecureResource("sys_user")] 
    public class UserDto : EntityDto<Guid>
    {
        public string UserName { get; set; }

        public string Nick { get; set; }

        // 2. 如果管理员配置了 "sys_user" 表的 "Phone" 字段为禁止，
        //    则此属性在序列化时会被忽略，前端收不到该字段。
        public string Phone { get; set; } 
    }
}
`

### 6.3 注意事项
1.  **资源名一致性**: [SecureResource("xxx")] 中的字符串必须与 SysTableConfig 和 SysRoleField 表中的 TableName 严格一致（不区分大小写）。
2.  **性能**: 该机制经过优化（缓存了反射元数据和权限规则），性能损耗极低，可放心在大列表接口使用。
3.  **仅限 Output**: 该机制目前仅针对 JSON **输出** (Write) 生效。输入 (Read) 暂不拦截。


## 7. 安全增强开发指南 (Security Hardening)

### 7.1 简介
为了解决 URL 变更导致的权限失效问题，并防止大小写绕过攻击，系统引入了 **资源标识符 (Resource ID)** 和 **URL 归一化** 机制。

### 7.2 资源标识符 (YiPermissionAttribute)
强烈建议在 Controller 的 Action 上绑定固定的权限代码，而不是依赖 URL。

**示例代码:**

`csharp
using Yi.Framework.CasbinRbac.Domain.Shared.Attributes;

[Route("api/users")]
public class UserController : Controller
{
    // 绑定固定权限码 "user:list"
    // 即使路由改为 "api/v2/users/get-all"，原有 Casbin 策略 (obj="user:list") 依然有效！
    [HttpGet]
    [YiPermission("user:list")] 
    public async Task<List<UserDto>> GetListAsync() { ... }
}
`

### 7.3 默认 URL 策略 (Fallback)
如果未标记 [YiPermission]，系统将降级使用 URL 进行鉴权，但会自动进行 **归一化处理**：
*   转换为**小写** (Lowercase)
*   **去除尾部斜杠** (Trim trailing slash)

例如：请求 /API/User/List/ 会被转换为 /api/user/list 进行鉴权。请确保 Casbin 数据库中的策略也使用小写 URL。

### 7.4 调试模式 (Debug Mode)
开发人员在调试 403 问题时，可在请求头中添加 X-Casbin-Debug: true。
响应头将包含详细的鉴权参数：
*   X-Casbin-Result: True/False
*   X-Casbin-Sub: u_GUID
*   X-Casbin-Obj: user:list 或 /api/user/list

