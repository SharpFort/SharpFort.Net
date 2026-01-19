# Casbin RBAC 模块优化与独立化分析报告

## 1. 现状评估

当前 `module/casbin-rbac` 模块正处于从传统 Attribute 鉴权向 Casbin 策略鉴权过渡的混合状态。虽然核心功能已初步实现，但代码组织、性能优化以及模块边界方面仍存在显著的改进空间。

### 1.1 关键问题
*   **模块耦合**: 传统的 RBAC 实体（`Role`, `Menu`, `UserRole` 等）与 Casbin 策略管理逻辑紧密耦合在同一个模块中。虽然业务上两者强相关，但技术实现上，Casbin 的策略存储（`CasbinRule` 表）与业务元数据表（如 `Sys_Menu`）是分离的，目前的 `RoleService` 等服务中同时混杂了业务逻辑和策略同步逻辑。
*   **性能瓶颈**: 
    *   **同步加载**: `CasbinPolicyManager` 中的 `TriggerMemorySync` 采用全量 `LoadPolicyAsync()`，这在策略条目达到万级以上时会造成显著的性能抖动。
    *   **正则匹配**: `keyMatch2` 在高并发场景下的性能开销不可忽视，缺乏二级缓存机制。
*   **事务一致性**: `CasbinPolicyManager` 中手动操作 `CasbinRule` 表和 `_enforcer` 内存的方法虽然尽力保证一致性，但缺乏统一的“双写事务”封装，容易在异常情况下导致内存与数据库不一致。

## 2. 优化建议

### 2.1 架构优化：模块解耦与分层 (建议采纳)
建议将 `casbin-rbac` 拆分为两个逻辑层（或物理模块），以实现关注点分离：

1.  **Core Authorization Module (核心鉴权层)**:
    *   **职责**: 仅负责 Casbin 的核心鉴权逻辑。
    *   **包含**: `CasbinAuthorizationMiddleware` (中间件), `ICasbinPolicyManager` (策略管理接口), `model.conf`, `YiCasbinDbContext` (仅包含 `CasbinRule` 表)。
    *   **特点**: **完全不依赖**具体的业务实体（如 User/Role/Menu）。它只认 `sub` (字符串), `obj` (URL), `act` (Method)。
    *   **收益**: 该模块可以被复用到任何需要 URL 鉴权的系统中，完全独立。

2.  **Business Sync Module (业务同步层 - 即当前的 casbin-rbac)**:
    *   **职责**: 负责监听或主动处理业务实体（User/Role/Menu）的变化，并将其翻译为 Casbin 策略，调用核心层的 Manager 进行同步。
    *   **包含**: `RoleService` (增强版), `UserService` (增强版), 领域事件处理器 (如 `RoleUpdatedEventHandler`)。
    *   **交互**: 业务层通过 Domain Event 或直接调用 `ICasbinPolicyManager` 来驱动权限变更。

### 2.2 代码组织与重构
*   **引入 Domain Event (领域事件) 驱动同步**:
    *   目前在 `RoleService` 的 `UpdateAsync` 中显式调用 `SyncCasbinRolePermissions`。
    *   **优化**: `RoleService` 只负责更新 `Role/Menu` 表，然后发布 `RolePermissionsUpdatedEvent`。
    *   **Handler**: 创建一个 `CasbinSyncEventHandler`，订阅上述事件，在 Handler 中调用 `CasbinPolicyManager` 进行策略同步。
    *   **优势**: 解耦业务逻辑与同步逻辑，`RoleService` 更加纯粹。

*   **重构 CasbinPolicyManager**:
    *   **当前**: 混合了 `SqlSugar` 直接操作 `CasbinRule` 表（为了事务）和 `_enforcer` 内存操作。
    *   **优化**: 封装 `CasbinUnitOfWork`，利用 Casbin Adapter 的原生能力（如果支持事务传递），或者规范化“双写”模式。确保 `Enforcer` 操作被包装在事务提交后的 `OnCompleted` 回调中（用于内存刷新），而数据库操作在事务中执行。

### 2.3 性能优化
*   **增量同步 (Incremental Sync)**:
    *   目前的 `TriggerMemorySync` 调用了 `LoadPolicyAsync()` (全量加载)。
    *   **优化**: 对于单条策略的增删（如分配一个用户角色），仅在内存中执行 `Add/RemoveGroupingPolicy`，**不触发全量 Load**。仅在多节点环境下，通过 Redis Pub/Sub 通知其他节点进行增量更新或（不得不）全量更新。
*   **引入 CachedEnforcer**:
    *   使用 `CachedEnforcer` 替代标准 `Enforcer`，缓存 `(sub, obj, act) -> result` 的计算结果，大幅减少 `keyMatch2` 的执行次数。
    *   **注意**: 策略变更时需及时清空缓存。

## 3. 是否独立模块开发的分析

**问题**: 是否将原来的 RBAC 表迁移功能以独立模块开发？

**分析**:
*   **现状**: 目前 RBAC 表（User, Role, Menu）承载了两个功能：1. 前端 UI 展示（菜单树、角色管理）；2. 作为 Casbin 策略的“源数据”。
*   **拆分利弊**:
    *   **利 (Pros)**: 如果将“同步逻辑”做成一个独立的 `Casbin.Sync.Rbac` 模块，那么 `casbin-rbac` 模块就可以变成一个纯粹的通用鉴权引擎，不再依赖具体的 RBAC 表结构。这意味着你可以随时更换底层的 RBAC 实现（例如换一套表结构），只需要重写 Sync 模块即可。
    *   **弊 (Cons)**: 增加了工程复杂度。对于当前项目而言，RBAC 表结构相对稳定，过度拆分可能带来不必要的开发成本。

**结论**:
**不需要物理拆分成两个完全独立的 NuGet 包/项目，但强烈建议在代码结构上进行逻辑分层（通过文件夹或命名空间）。**

**推荐方案**:
1.  保持在 `casbin-rbac` 模块中。
2.  建立 `Synchronization` 目录，专门存放 `SyncHandlers` (事件处理器)。
3.  将 `CasbinPolicyManager` 抽象得更纯粹，只处理 `string` 类型的 `sub, obj, act`，完全移除对 `User/Role` 实体类的依赖（方法签名改为 `AddRoleForUserAsync(string userSub, string roleSub)`）。
4.  由 `Synchronization` 层负责从 `User` 实体提取 `id` 转换为 `string`，然后调用 Manager。

## 4. 行动计划 (Action Plan)

1.  **重构 CasbinPolicyManager**: 移除所有 Entity 依赖，改为纯字符串参数。
2.  **实现领域事件同步**:
    *   定义 `UserRoleChangedEvent`, `RoleMenuChangedEvent`。
    *   在 `RoleService` 中发布这些事件，移除直接的同步代码。
    *   编写 `CasbinSyncEventHandler` 处理事件并调用 Manager。
3.  **性能升级**:
    *   配置 `CachedEnforcer`。
    *   优化 `TriggerMemorySync` 为增量模式 (仅在分布式场景下考虑全量或通知)。

通过上述优化，代码将更加清晰、解耦，且具备更好的扩展性和性能。
