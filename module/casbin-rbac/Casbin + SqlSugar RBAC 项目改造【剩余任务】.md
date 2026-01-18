# Casbin + SqlSugar RBAC 项目改造【待办】任务清单

本清单基于当前项目状态分析，列出了实现“全面终极改造”所需的剩余任务。

## 🚨 P0: 核心验证与切换 (Critical)

### 1. 执行全量数据迁移
*   **现状**: `CasbinSeedService` 代码已就绪，但尚未在生产/测试库执行。
*   **任务**:
    *   [x] 在程序启动或通过 API 触发 `CasbinSeedService.MigrateAllAsync()` (已创建 API `/api/casbin-migration/run`)。
    *   [ ] 核对 `CasbinRule` 表数据量，确认 User-Role (g) 和 Role-Permission (p) 记录准确无误。

### 2. 启用中间件拦截
*   **现状**: 中间件代码已存在，需确认是否已在 `Startup/Program.cs` 中注册并位于 `UseAuthentication` 之后。
*   **任务**:
    *   [ ] 确认中间件管道顺序：`UseAuthentication` -> **`UseMiddleware<CasbinAuthorizationMiddleware>()`** -> `UseAuthorization`。
    *   [ ] 进行冒烟测试：使用普通用户访问未授权接口，确认返回 403。

## 🚀 P1: 性能与架构升级 (Performance) ✅ 已完成

### 3. 启用 CachedEnforcer
*   **现状**: 已通过配置项 `Casbin:EnableCachedEnforcer` 支持（默认启用）。
*   ~~**任务**~~:
    *   [x] 修改 DI 注册，支持基于配置选择 Enforcer 类型。
    *   [x] 添加 `appsettings.json` 配置示例。

### 4. 启用 Redis Watcher (分布式同步)
*   **现状**: 已通过配置项 `Casbin:EnableRedisWatcher` 支持（默认禁用）。
*   ~~**任务**~~:
    *   [x] 取消注释并修复 Redis Watcher 初始化代码。
    *   [x] 添加配置开关，默认禁用（无需 Redis 也能正常启动）。
    *   [x] 在 Watcher 回调中添加策略重载逻辑。

## 🧹 P2: 代码清理 (Cleanup) ✅ 已完成

### 5. 移除旧版鉴权标签
*   **现状**: `PermissionAttribute` 和 `PermissionGlobalAttribute` 已标记为 `[Obsolete]`。
*   ~~**任务**~~:
    *   [x] 全局搜索 `[Permission]` 使用位置（主要在 `UserService.cs`、`ArticleService.cs` 等）。
    *   [x] 将 `PermissionAttribute` 标记为 `[Obsolete]`，提示开发者使用 Casbin 中间件。
    *   [x] 确保 Swagger 文档生成不依赖旧标签（已验证，Swagger 仅依赖路由和 XML 注释）。

## 📝 建议执行计划

1.  **立即执行**: 运行数据迁移，并在测试环境启用中间件。
2.  **下周计划**: 替换 CachedEnforcer 并调试 Redis Watcher，确保高性能。
3.  **最后收尾**: 清理旧代码，发布 v1.0 改造完成版。
