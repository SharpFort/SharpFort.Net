# 旧 RBAC 代码清理与迁移计划

鉴于引入了 Casbin 作为核心鉴权引擎，我们需要移除与旧的基于 Attribute 的鉴权方式相关的代码，以避免逻辑冲突和冗余。同时，对领域层其他目录进行了审查，确保业务逻辑的连续性。

## 1. 待清理文件清单 (Authorization 目录)

路径: `module/casbin-rbac/Yi.Framework.CasbinRbac.Domain/Authorization/`

| 文件名 | 作用 | 现状分析 | 处理策略 | 替代方案 |
| :--- | :--- | :--- | :--- | :--- |
| **PermissionAttribute.cs** | 标记在 Controller 方法上，指定权限码 | Casbin 基于 URL 鉴权，不再依赖代码中的硬编码字符串。 | 🗑️ **移除** | Casbin `Enforce` API 或 URL 策略。 |
| **PermissionGlobalAttribute.cs** | MVC 过滤器，拦截请求并检查 PermissionAttribute。 | 与即将开发的 Casbin 中间件功能重叠且冲突。 | 🗑️ **移除** | `CasbinAuthorizationMiddleware`。 |
| **IPermissionHandler.cs** | 鉴权逻辑抽象接口。 | 旧逻辑接口。 | 🗑️ **移除** | `IEnforcer`。 |
| **DefaultPermissionHandler.cs** | 具体的鉴权实现。 | 旧逻辑实现。 | 🗑️ **移除** | Casbin 策略引擎。 |
| **RefreshTokenMiddleware.cs** | 刷新 Token 的中间件。 | 属于认证范畴。 | ✅ **保留** | - |
| **IDataPermission.cs** | 数据权限标记接口。 | 用于 SqlSugar 过滤器，属于数据权限范畴。 | ✅ **保留** | - |
| **DataPermissionExtensions.cs** | 数据权限扩展方法。 | 同上。 | ✅ **保留** | - |

## 2. 其他领域层目录审查

| 目录/文件 | 现状分析 | 决策 | 备注 |
| :--- | :--- | :--- | :--- |
| **EventHandlers/** | 日志和查询 Handler。 | ✅ **保留** | - |
| **Extensions/** | `CurrentUserExtensions`。 | ✅ **保留** | - |
| **Managers/AccountManager.cs** | Token 生成。 | ⚠️ **已修复** | 移除了 Salt 引用。 |
| **SqlSugarCore/DataSeeds/** | 种子数据。 | ✅ **保留** | 暂时注释状态，后续按需恢复或迁移到 CasbinSeedService。 |
| **SqlSugarCore/Repositories/** | 仓储实现。 | ✅ **保留** | 核心业务查询。 |
| **SqlSugarCore/DbContext** | `YiRbacDbContext`。 | ⚠️ **重命名** | 改为 `YiCasbinRbacDbContext.cs`。逻辑将在第五阶段重构。 |
| **SqlSugarCore/Module** | 模块入口。 | ⚠️ **修改** | 需注册 Casbin Adapter。 |

## 3. DTO 清理计划 (Domain.Shared)

| 文件 | 修改内容 | 原因 |
| :--- | :--- | :--- |
| **UserDto.cs** | 移除 `Salt` 属性。 | 密码升级为 BCrypt，不再需要单独存储/传输 Salt。 |
| **UserRoleMenuDto.cs** | 检查引用。 | 确保无 Salt 引用。 |

## 4. Application 层审查

| 目录/文件 | 现状分析 | 决策 | 备注 |
| :--- | :--- | :--- | :--- |
| **Services/** | 应用服务实现。 | ✅ **清理完成** | 仅 `UserService.cs` 包含旧特性，已清理。其他服务未发现。 |
| **YiFrameworkCasbinRbacApplicationModule.cs** | 模块定义。 | ✅ **保留** | 无特殊清理项。 |
| **SignalRHubs/** | 实时通知 Hub。 | ✅ **保留** | 业务逻辑。 |
| **Application.Contracts/** | 接口与 DTO。 | ✅ **保留** | 无需清理。 |

## 5. 执行计划

1.  **物理删除**: 删除 Authorization 目录下标记为 🗑️ 的文件。(已执行)
2.  **代码清理**:
    *   移除 `UserManager` 和 `AccountManager` 对 `Salt` 的引用。(已完成)
    *   移除 `UserDto` 的 `Salt` 属性。(已完成)
    *   移除 `Application` 层 `[Permission]` 特性。(已完成)
    *   移除 `Module` 类 `PermissionGlobalAttribute` 注册。(已完成)
3.  **SqlSugarCore 调整**:
    *   重命名 `YiRbacDbContext.cs` -> `YiCasbinRbacDbContext.cs`。(已执行)
    *   修改 `YiFrameworkCasbinRbacSqlSugarCoreModule.cs` 注册 Casbin Adapter。(已执行)
