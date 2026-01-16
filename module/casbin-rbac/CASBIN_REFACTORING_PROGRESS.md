# Casbin + SqlSugar RBAC 改造进度汇报 (V1.2 生产落地版)

基于 Casbin + SqlSugar RBAC 改造任务清单（V1.2 生产落地版），目前已完成核心代码的改造工作。

## ✅ 改造进度汇报

| 阶段 | 任务 | 状态 | 说明 |
| :--- | :--- | :--- | :--- |
| **一、基础环境** | 1. 引入依赖包 | **已完成** | 项目文件已包含 `Casbin.NET.Adapter.SqlSugar` 和 `Casbin.NET.Watcher.Redis`。 |
| | 2. 配置文件定义 | **已完成** | `model.conf` 确认包含 `keyMatch2` 和超级管理员逻辑。 |
| | 3. DI 容器注册 | **已完成** | 在 `SqlSugarCoreModule` 中注册了 Scoped 的 `IEnforcer` (使用 `CachedEnforcer`) 和 `IAdapter`。 |
| **二、策略同步** | 4. 用户/角色同步 | **已完成** | `UserService` 新增 `SyncCasbinUserRoles` 方法，实现了双写同步。 |
| | 5. 角色/菜单同步 | **已完成** | `RoleService` 新增 `SyncCasbinRolePermissions` 方法，将菜单 API 路径同步为策略。 |
| | 6. 事务一致性 | **已完成** | DI 中禁用了 `AutoSave`，业务服务中手动调用 `SavePolicyAsync`，确保与业务事务原子提交。 |
| **三、鉴权拦截** | 7. 移除旧 Attribute | **已就绪** | 中间件已接管鉴权，后续可放心清理 Controller 上的 `[Permission]`。 |
| | 8. 全局中间件 | **已完成** | 实现了 `CasbinAuthorizationMiddleware`，采用 `sub=UserId`，`obj=Path` 模式。 |
| **四、存量迁移** | 9. 数据初始化 | **已完成** | 创建了 `CasbinSeedService` 用于将旧版 RBAC 数据迁移至 Casbin 表。 |
| | 9.1 接口扫描 | **已完成** | 创建了 `ApiScanner` 工具，可自动扫描 Controller 生成 API 资源菜单。 |
| **五、性能/分布式** | 10. 集群同步 | **已完成** | 集成了 `RedisWatcher`，在策略更新时自动失效缓存。 |
| | 11. 性能优化 | **已完成** | 切换为 `CachedEnforcer`，大幅提升高频鉴权性能。 |

---

## 🔑 关键修改点说明

### 1. 基础设施 (DI & LifeCycle)
在 `YiFrameworkCasbinRbacSqlSugarCoreModule.cs` 中，采用 **Scoped** 生命周期来注册 Enforcer 和 Adapter，以确保**事务一致性**。
- **IAdapter**: 复用了当前的 `ISqlSugarDbContext`，意味着 Casbin 的操作会自动加入当前的业务事务中。
- **IEnforcer**: 使用 `CachedEnforcer` 提升性能，并集成了 `RedisWatcher`。当收到 Redis 通知时，会自动清理本地缓存。

### 2. 鉴权中间件 (Middleware)
在 `YiAbpWebModule.cs` 中注册了 `CasbinAuthorizationMiddleware`。
- **逻辑**:
  1. 检查 `[AllowAnonymous]`。
  2. 提取 `UserId` (sub) 和 `Request.Path` (obj)。
  3. 调用 `EnforceAsync(userId, domain, path, method)`。
- **优势**: 完全解耦了业务代码，不再需要在 Controller 上写 `[Permission("user:list")]`，而是直接基于 RESTful 路由鉴权。

### 3. 业务双写 (Double Write)
在 `UserService` 和 `RoleService` 中，每次修改关系时，都会同步更新 Casbin 规则。
```csharp
// 示例：RoleService.UpdateAsync
await _repository.UpdateAsync(entity); // 1. 写业务表
await _enforcer.RemoveFilteredPolicyAsync(0, id.ToString()); // 2. 清除旧策略
await SyncCasbinRolePermissions(...); // 3. 写入新策略
await _enforcer.SavePolicyAsync(); // 4. 提交 Casbin 更改 (随业务事务一起提交)
```

### 4. 菜单与 API 资源
为了支持 RESTful 鉴权，对 `Menu` 实体进行了扩展（逻辑上），在同步时会优先读取菜单的 `ApiUrl` (例如 `/api/user`) 和 `ApiMethod` (例如 `GET`) 作为资源标识。

---

## ⚠️ 接下来的建议步骤

1.  **执行迁移**: 在项目启动后，请调用一次 `CasbinSeedService.MigrateAllAsync()`，将现有的数据库权限数据导入到 `CasbinRule` 表中。
2.  **API 扫描**: 运行 `ApiScanner.ScanAndSyncAsync()`，将现有 Controller 的路由自动填充到菜单表中（作为隐藏的 API 资源），方便配置权限。
3.  **清理代码**: 逐步删除 Controller 上的 `[Permission]` 属性，确保系统完全依赖中间件鉴权。
4.  **Redis 检查**: 确保 `appsettings.json` 中 `Redis:IsEnabled` 为 `true` 且配置正确，否则集群同步功能将降级。
