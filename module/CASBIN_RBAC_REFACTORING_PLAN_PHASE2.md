# Casbin RBAC 数据一致性与高性能重构计划

## 1. 目标
解决核心审计问题 "2.1 数据一致性与事务风险"，同时满足用户 "极致压榨单机性能" 的需求。

## 2. 核心架构变更

### 2.1 Enforcer 生命周期升级 (Scoped -> Singleton)
*   **现状**: `Scoped` 生命周期，每次请求创建一个 `Enforcer` 并执行 `LoadPolicy()`。
    *   *问题*: 严重性能隐患，每次请求都全量查库。
*   **改进**: `Singleton` 生命周期。
    *   *优势*: 真正的“机器缓存”，全量策略驻留内存，纳秒级鉴权。
    *   *实现*: 
        *   启动时创建单例 Enforcer。
        *   使用 `IServiceScopeFactory` 创建临时 Scope 进行首次 `LoadPolicy()`。
        *   之后的 `LoadPolicy` 或增量更新仅在写操作时触发。

### 2.2 读写分离架构 (Read/Write Splitting)
为了保证数据库事务的一致性，不能让 Singleton 的 Enforcer 直接管理数据库连接（因为它不参与当前的 Scoped Transaction）。

*   **写入 (Write) - 事务强一致性**: 
    *   不使用 `Enforcer.AddPolicy` (因为它是针对 Enforcer 自身 Adapter 的，难以融入业务事务)。
    *   **方案**: 直接使用 `ISqlSugarRepository<CasbinRule>` 操作数据库表。
    *   *优势*: `CasbinRule` 的增删改可以与 `SysUser`, `SysRole` 的增删改完全在一个 `UnitOfWork` 事务中。
*   **读取 (Read) - 内存极致性能**:
    *   `IEnforcer` 仅用于 `Enforce` 判断。
    *   内存数据同步通过 **显式同步 (Explicit Synchronization)** 触发。

### 2.3 流程设计：修改权限的原子操作
以 `SetUserRolesAsync` 为例：

1.  **开启事务 (UnitOfWork)**。
2.  **业务表更新**: 删除旧 UserRole，插入新 UserRole。
3.  **策略表更新**: 使用 Repository 删除旧 `CasbinRule` (g策略)，插入新 `CasbinRule`。
4.  **提交事务 (Commit)**。
5.  **内存同步 (OnSuccess)**: 调用 `SingletonEnforcer.LoadPolicy()` (全量) 或 `AddGroupingPolicy` (增量内存更新)。
    *   *注意*: 为了极致性能，建议尽量使用增量内存更新（避免 STW），但在复杂重构场景下，先做全量刷新保证正确性，后续优化为增量。考虑到单体应用内存操作极快，全量刷新（Reload）在 10w 条规则下通常也是毫秒级，可以接受。

---

## 3. 实施步骤

### 3.1 引入 CasbinRule 实体
已直接复用 `Casbin.Adapter.SqlSugar` 提供的 `CasbinRule` 实体，无需重复定义。

### 3.2 重构 CasbinPolicyManager
将原有的 `_enforcer.AddPolicy` 逻辑改为 `_repository.Insert`。

#### [COMPLETED] [CasbinPolicyManager.cs]
*   注入 `ISqlSugarRepository<CasbinRule>`。
*   实现 `AtomicDistribute` 模式：先写库，后通过 `Enforcer.LoadPolicy()` 刷新。
*   使用 `IUnitOfWorkManager.Current.OnCompleted` 确保事务提交后同步。

### 3.3 重构模块注册 (DI)
修改 `YiFrameworkCasbinRbacSqlSugarCoreModule.cs`。

#### [COMPLETED] [YiFrameworkCasbinRbacSqlSugarCoreModule.cs]
*   `AddScoped<IEnforcer>` -> `AddSingleton<IEnforcer>`。
*   配置 Enforcer 使用内存模式，并禁用 AutoSave。
*   引入 `ScopeFactoryCasbinAdapter` 解决 Singleton 调用 Scoped DbContext 问题。

### 3.4 验证计划
*   **单元测试**: 模拟事务回滚，验证 `CasbinRule` 表没有脏数据。
*   **集成测试**: 修改角色后，不重启服务，验证 API 权限是否实时生效（测试内存同步）。

## 4. 风险预案
*   **内存泄漏**: Singleton Enforcer 如果不恰当管理，策略无限增长。 -> Casbin 自身内存管理较好，只要不无限 Add 且能及时 Remove 即可。
*   **并发读写**: Casbin Enforcer 是线程安全的读，但在 Reload 瞬间可能有锁。 -> 对于高频读、低频写（后台改权限）的场景，Casbin 的 `ReadWriteLock` 是足够的。
