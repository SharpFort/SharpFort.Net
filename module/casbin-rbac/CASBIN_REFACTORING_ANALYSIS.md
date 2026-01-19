# Casbin RBAC 项目文件清理分析报告

本文档旨在分析 `module/casbin-rbac` 模块中因迁移到 Casbin 鉴权体系而废弃或需要调整的文件，并提供详细的清理建议。

## 1. 项目背景概述
`casbin-rbac` 模块是将原有基于 Attribute 的 RBAC 项目改造为基于 Casbin 的 RBAC 项目。改造的核心变化如下：
*   **鉴权引擎**: 从自定义的 Attribute + Handler 变更为 `Casbin.NET`。
*   **鉴权策略**: 从代码硬编码 `[Permission("user:list")]` 变更为基于 URL 的动态策略 (RESTful API)。
*   **数据流**: 权限数据写入 `casbin_rule` 表，并加载到内存中进行毫秒级判定。
*   **身份识别**: 鉴权主体 (Subject) 从 `RoleCode` 变更为 `UserId`，通过 Casbin 的 `g` 策略 (RBAC 角色继承) 关联用户与角色。

## 2. 待清理/调整文件清单

### 2.1 Authorization 目录 (鉴权核心)
路径: `module/casbin-rbac/Yi.Framework.CasbinRbac.Domain/Authorization/`

| 文件名 | 现状 | 分析 | 建议操作 |
| :--- | :--- | :--- | :--- |
| **CasbinAuthorizationMiddleware.cs** | ✅ **核心** | 新的鉴权中间件，替代了原有的过滤器。 | **保留** |
| **DataPermissionExtensions.cs** | ✅ **核心** | 数据权限扩展方法（行级权限），Casbin 暂时只接管了功能权限。 | **保留** |
| **IDataPermission.cs** | ✅ **核心** | 数据权限接口。 | **保留** |
| **RefreshTokenMiddleware.cs** | ✅ **认证** | 处理 JWT 刷新，属于认证层，与鉴权方式无关。 | **保留** |
| **PermissionAttribute.cs** (已移除) | 🗑️ **废弃** | 原有基于 Attribute 的鉴权标记。 | 确认已物理删除 |
| **PermissionGlobalAttribute.cs** (已移除) | 🗑️ **废弃** | 原有全局权限拦截器。 | 确认已物理删除 |
| **IPermissionHandler.cs** (已移除) | 🗑️ **废弃** | 原有鉴权处理器接口。 | 确认已物理删除 |
| **DefaultPermissionHandler.cs** (已移除) | 🗑️ **废弃** | 原有鉴权处理器实现。 | 确认已物理删除 |

### 2.2 Domain.Shared 目录 (共享定义)
路径: `module/casbin-rbac/Yi.Framework.CasbinRbac.Domain.Shared/`

| 文件名 | 现状 | 分析 | 建议操作 |
| :--- | :--- | :--- | :--- |
| **Attributes/YiPermissionAttribute.cs** | ❓ **冗余** | 用于标记 Method 的权限码。在 Casbin RESTful 模式下，权限基于 URL，不再依赖代码中的 Code 标记。目前似乎未被 `CasbinAuthorizationMiddleware` 使用。 | **建议移除** (除非用于生成前端权限标识或文档) |
| **Attributes/SecureResourceAttribute.cs** | ✅ **保留** | 字段级权限控制 (Field Security) 的标记，Casbin 改造目前主要关注 API 层面，字段级可能仍需此特性或后续整合。 | **保留** |

### 2.3 Application 层 (应用服务)
路径: `module/casbin-rbac/Yi.Framework.CasbinRbac.Application/Services/`

*   **RoleService.cs**:
    *   目前已包含 Casbin 同步逻辑 (`SyncCasbinRolePermissions`, `AddGroupingPoliciesAsync` 等)。
    *   `UpdateAsync` 方法中的 `UpdateDataScopeAsync` 逻辑保留用于数据权限。
    *   **建议**: 代码中存在注释掉的旧逻辑或过度复杂的同步逻辑，建议进一步封装到 `CasbinPolicyManager` 中，使 Application 层代码更简洁。但文件本身必须保留。
*   **UserService.cs**:
    *   已清理旧的 Permission 检查。
    *   **建议**: 检查是否存在未使用的 `[Permission]` 引用。

### 2.4 基础设施与配置

*   **YiCasbinRbacDbContext.cs**: 
    *   **现状**: 已重命名。
    *   **建议**: 确认是否已移除旧的 RBAC 表（如果不再需要），或者确认 `casbin_rule` 表的映射是否正常。
*   **Yi.Framework.CasbinRbac.Domain/model.conf**:
    *   **现状**: 存在 `rbac_with_domains_model.conf`。
    *   **建议**: 确保文件名与 `CasbinOptions` 配置一致，通常建议统一管理。

## 3. 详细清理与迁移建议

### 3.1 移除 `YiPermissionAttribute`
如果项目中确实全面转向基于 URL 的 RESTful 鉴权，那么 `[YiPermission("user:list")]` 这种标记将不再起作用。
**风险**: 前端可能仍在使用这些 Code 字符串做按钮级别的显隐控制（`v-auth="user:list"`）。
**对策**:
*   如果前端直接使用 API URL 做权限标识（`v-auth="'POST:/api/user'"`），则可以安全移除后端 Attribute。
*   如果前端仍需 Code，建议保留 Attribute 仅作为“元数据”用于生成前端所需的权限字典，但不参与后端鉴权拦截。
*   **当前建议**: **暂时保留**，作为元数据使用，待前端完全适配 RESTful 风格后再彻底移除。

### 3.2 规范化 Casbin 同步代码
目前 `RoleService.cs` 中直接调用了 `_enforcer.AddPolicyAsync` 等原生 API。建议将这些操作封装到 `Domain/Managers/CasbinPolicyManager.cs` 中，原因如下：
1.  **统一事务处理**: `CasbinPolicyManager` 可以专门处理 `EnableAutoSave(false)` 和 `SavePolicyAsync` 的事务组合逻辑。
2.  **解耦**: Application 层不应过多关注 Casbin 的具体实现细节（如 `sub` 到底传 `UserId` 还是 `RoleId`）。
3.  **缓存一致性**: Manager 层可以统一处理 `CachedEnforcer` 的缓存清除。

### 3.3 检查 Controller 层的遗留特性
遍历 `module/casbin-rbac` 下的所有 Controller，确保没有残留的 `[Permission(...)]` 特性。如果存在，应替换为 `[Authorize]` (仅做登录检查) 或直接移除 (依赖中间件)。

## 4. 结论
目前 `module/casbin-rbac` 的核心文件结构已基本符合 Casbin 改造要求。主要的清理工作集中在：
1.  **确认 `YiPermissionAttribute` 的去留**。
2.  **RoleService 中 Casbin 逻辑的重构与封装**。
3.  **确保没有遗漏的旧 Authorization 文件夹下的文件** (已确认清理)。

请开发人员根据本报告进行后续的代码重构与优化。
