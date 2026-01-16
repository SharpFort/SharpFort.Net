# 第一部分：ASP.NET Core RBAC 权限改造技术方案书 V1.0（基于 Casbin + SqlSugar）

## 1. 基础设施建设 (Infrastructure)

### 1.1 核心组件选型
*   **权限引擎**: Casbin.NET
*   **适配器**: `Casbin.NET.Adapter.SqlSugar` (SharpFort)
*   **ORM**: SqlSugar (Code First 模式)
*   **数据库策略**:
    *   启用 `InitKeyType.Attribute` 属性建表模式。
    *   系统自动维护 `CasbinRule` 表，无需手动编写 SQL 建表脚本。

### 1.2 依赖注入 (DI) 配置
*   **生命周期管理**:
    *   `ISqlSugarClient`: 注册为 **Scoped**。利用 `IsAutoCloseConnection` 避免 EF Core 常见的 `ObjectDisposedException`。
    *   `IEnforcer`: 注册为 Scoped 或 Singleton（需结合 `AutoSave` 策略决定，通常建议 Scoped 以复用 DbContext，或 Singleton 配合同步机制）。
*   **适配器注册**: 注册 `SqlSugarAdapter` 为 `IAdapter`。

## 2. 授权架构设计 (Authorization Architecture)

### 2.1 侵入性改造（去标点化）
*   **移除**: 所有业务 Controller 上的硬编码权限标签（如 `[Permission("user:add")]`）。
*   **保留**: `[Route]` 及 `[HttpGet/Post]` 等标准路由属性。
*   **目标**: 实现业务代码与权限逻辑的**物理层解耦**。

### 2.2 核心拦截机制
*   **拦截位置**: 全局中间件 (Middleware) 或 全局过滤器 (Global Filter)。
*   **鉴权逻辑**:
    *   **三元组映射**:
        *   `sub` (主体): 当前用户的角色标识 (Role Code)。
        *   `obj` (资源): 请求的 URL 路径 (Request Path)。
        *   `act` (动作): HTTP 方法 (Method)。
    *   **执行判断**: `await _enforcer.EnforceAsync(role, path, method)`。

## 3. 策略模型与匹配 (Model & Policy)
V1.2 生产落地版 2.2

### 3.2 动态路由处理
*   **匹配函数**: 使用 `keyMatch2`。
*   **规则示例**: 数据库存储 `/api/user/:id`，可匹配请求 `/api/user/1001`。

## 4. 数据同步与迁移 (Synchronization & Migration)

### 4.1 双写同步机制
*   **主权数据**: 现有的 RBAC 业务表（User/Role/Menu等路径:module/casbin-rbac/Yi.Framework.CasbinRbac.Domain/Entities）作为“写”的主入口，负责 UI 展示。
*   **策略同步**: 在业务层（Service）修改角色权限时，同步调用 Casbin API：
    *   `AddPolicyAsync()`
    *   `RemovePolicyAsync()`
    *   `AddGroupingPolicyAsync()` (处理角色继承或用户归属)

### 4.2 初始数据迁移
*   **方案**: 编写一次性迁移服务 (Migration Service)。
*   **逻辑**: 遍历现有业务表 -> 内存转换 -> 循环调用 `_enforcer.AddPolicyAsync` -> 自动持久化至 `CasbinRule`。

---


# ASP.NET Core RBAC 改造方案书（修订补充版 V1.1）

> **说明**：本部分是对 V1.0 方案的修正与增强，请以本版本中的架构决策为准。

## 1. 核心架构修正 (Architecture Corrections)

### 1.1 身份识别逻辑变更（至关重要）
*   **原方案**：`sub` 传递角色名（RoleName）。
*   **修正后**：`sub` 传递 **用户ID (UserId)**。
*   **理由**：解耦用户与角色。通过 Casbin 的 `g` 策略（`g, userId, roleId, domainId`）自动推导用户拥有的所有角色，无需在中间件中手动查询角色。
*   **中间件代码调整**：
    ```csharp
    // 传入 UserId字符串, 部门ID, 请求路径, 方法
    await _enforcer.EnforceAsync(userId.ToString(), deptId, path, method);
    ```

### 1.2 Model.conf 策略升级（解决超级管理员穿透）
*   **新增需求**：超级管理员（`super_admin`）不受部门隔离限制，可访问任意部门数据。
*   **配置修正**：
    ```ini
    [matchers]
    # 逻辑：(是超级管理员) OR (角色匹配 AND 部门匹配)
    # 注意：keyMatch2 放在最后以减少正则计算开销
    m = (g(r.sub, "super_admin", r.dom) || (g(r.sub, p.sub, r.dom) && r.dom == p.dom)) \
        && keyMatch2(r.obj, p.obj) && r.act == p.act
    ```

## 2. 基础设施增强 (Infrastructure Enhancements)

### 2.1 分布式同步 (Cluster Sync)
*   **组件**：引入 `Casbin.NET.Watcher.Redis`。
*   **机制**：
    *   **发布**：当任意节点执行 `SavePolicyAsync()` 后，Watcher 自动向 Redis Channel 推送消息。
    *   **订阅**：其他节点收到消息，自动触发 `LoadPolicyAsync()` 刷新内存。
*   **配置**：
    ```csharp
    // Startup.cs / Program.cs
    var watcher = new RedisWatcher("redis_connection_string");
    enforcer.SetWatcher(watcher);
    // 必须设置回调，否则收到消息不会有动作
    watcher.SetUpdateCallback(async () => await enforcer.LoadPolicyAsync());
    ```

### 2.2 性能优化 (Performance)
*   **组件**：使用 `CachedEnforcer` 替代标准 `Enforcer`。
*   **机制**：缓存 `(sub, dom, obj, act) -> result` 的计算结果。对于高频访问的接口（如 `/api/user/profile`），第二次请求直接读缓存，跳过 `keyMatch2` 正则匹配，性能提升 100倍+。
*   **注意**：配合 Watcher 使用时，需确保 Watcher 回调中包含**清空缓存**的逻辑（见下文新问题）。

## 3. 关键业务流程代码规范 (Implementation Standards)

### 3.1 事务一致性（双写保护）
**场景**：管理后台分配权限。
**规范**：必须禁用 `AutoSave`，手动控制保存时机，并共享数据库事务。

```csharp
public async Task AssignPermissionAsync(RolePermissionDto dto)
{
    // 1. 开启 SqlSugar 事务
    _db.BeginTran();
    try 
    {
        // 2. 写入业务表 (用于 UI 显示)
        await _db.Insertable(new SysRoleMenu { ... }).ExecuteCommandAsync();

        // 3. 写入 Casbin (用于鉴权)
        // 关键：临时关闭自动保存，防止 AddPolicyAsync 直接提交
        _enforcer.EnableAutoSave(false); 
        
        // 内存操作
        await _enforcer.AddPolicyAsync(dto.Role, dto.Domain, dto.Path, dto.Method);
        
        // 4. 手动触发持久化 (利用共享的 DbContext/Connection)
        // 注意：此处需确认 Adapter 是否支持利用外部事务，否则可能需要特殊处理
        await _enforcer.SavePolicyAsync(); 

        // 5. 提交事务
        _db.CommitTran();
        
        // 6. (可选) 如果使用了 CachedEnforcer，手动清理本地缓存
        _enforcer.ClearPolicyCache(); 
    }
    catch 
    {
        _db.RollbackTran();
        // 内存回滚：重新加载一次数据库策略，丢弃内存中的脏数据
        await _enforcer.LoadPolicyAsync();
        throw;
    }
    finally 
    {
        // 恢复自动保存状态 (视单例/Scoped生命周期而定)
        _enforcer.EnableAutoSave(true);
    }
}
```


# ASP.NET Core RBAC 权限改造技术方案书 (V1.2 生产落地版)

## 1. 基础设施与生命周期 (Infrastructure & DI)

### 1.1 核心组件
*   **权限引擎**: `Casbin.NET` (使用 `CachedEnforcer` 以提升性能)
*   **适配器**: `Casbin.NET.Adapter.SqlSugar`
*   **分布式组件**: `Casbin.NET.Watcher.Redis` (用于集群策略同步)

### 1.2 依赖注入 (DI) 配置规范
**核心原则**：确保数据库连接与权限引擎在同一请求周期内共享上下文，以支持事务。

*   **ISqlSugarClient**: 注册为 **Scoped**。
    *   配置 `IsAutoCloseConnection = true`。
    *   配置 `InitKeyType = InitKeyType.Attribute` (启用 Code First 属性建表)。
*   **IEnforcer**: 注册为 **Scoped**。
    *   原因：必须与 `ISqlSugarClient` 保持相同的生命周期，以便在事务中共享 Connection。
    *   注意：虽然 `CachedEnforcer` 内部有缓存，但在 Scoped 模式下，一级缓存是请求级的；二级缓存（静态/全局）需配合 Watcher 刷新。

## 2. 授权架构设计 (Authorization Architecture)

### 2.1 身份识别 (Identity)
*   **Subject (sub)**: 统一使用 **UserId** (字符串形式)，而非角色名。
*   **解耦逻辑**: 用户与角色的关系通过 Casbin 的 `g` 策略维护 (`g, userId, roleId`)，中间件无需关心具体角色。

### 2.2 策略模型 (Model.conf)
支持 **RBAC + 域(部门)隔离 + 角色继承 + 超级管理员穿透**。

```ini
[request_definition]
r = sub, dom, obj, act

[policy_definition]
p = sub, dom, obj, act

[role_definition]
# g = 用户, 角色, 域
g = _, _, _

[policy_effect]
e = some(where (p.eft == allow))

[matchers]
# 逻辑：(是超级管理员) OR (角色匹配 AND 部门匹配)
# keyMatch2 用于处理 /api/user/:id 动态路由
m = (g(r.sub, "super_admin", r.dom) || (g(r.sub, p.sub, r.dom) && r.dom == p.dom)) \
    && keyMatch2(r.obj, p.obj) && r.act == p.act
```

## 3. 关键业务流程实现 (Implementation)

### 3.1 分布式同步与缓存刷新 (Cluster Sync)
在 `Startup.cs` 或初始化逻辑中配置 Redis Watcher。

```csharp
// 伪代码示例
var watcher = new RedisWatcher("redis_conn_str");
enforcer.SetWatcher(watcher);

// 关键回调：收到更新通知后，重载策略并清空缓存
watcher.SetUpdateCallback(async () => {
    // 1. 从数据库拉取最新策略到内存
    await enforcer.LoadPolicyAsync(); 
    
    // 2. 显式清空 CachedEnforcer 的缓存，防止旧结果残留
    if (enforcer is CachedEnforcer cached) {
        cached.ClearPolicyCache();
    }
});
```

### 3.2 事务一致性双写 (Atomic Transactions)
在管理后台（分配权限/角色）时，必须保证业务表与 Casbin 表的原子性。

```csharp
public async Task AssignPermissionAsync(RolePermissionDto dto)
{
    // 1. 开启业务事务 (利用 Scoped 注入的 SqlSugarClient)
    _db.BeginTran();
    try 
    {
        // 2. 写入原有业务表 (用于 UI 菜单显示)
        await _db.Insertable(new SysRoleMenu { ... }).ExecuteCommandAsync();

        // 3. 写入 Casbin 策略
        // 关键：禁用自动保存，防止 AddPolicyAsync 立即提交事务
        _enforcer.EnableAutoSave(false); 
        
        // 内存操作：添加策略
        await _enforcer.AddPolicyAsync(dto.Role, dto.Domain, dto.Path, dto.Method);
        
        // 4. 物理持久化
        // 因为共享了 Scoped 的 SqlSugarClient，此操作会自动加入当前的 _db 事务中
        await _enforcer.SavePolicyAsync(); 

        // 5. 提交事务 (两张表同时成功)
        _db.CommitTran();
        
        // 6. 触发 Watcher 通知其他节点 (SavePolicyAsync 内部通常会自动触发，若未触发需手动调用 watcher.Update())
    }
    catch (Exception ex)
    {
        _db.RollbackTran();
        // 内存回滚：重新加载数据库策略，丢弃内存中的脏数据
        await _enforcer.LoadPolicyAsync();
        throw;
    }
    finally 
    {
        // 恢复自动保存 (为后续操作重置状态)
        _enforcer.EnableAutoSave(true);
    }
}
```

### 3.3 全局拦截器 (Middleware)
```csharp
public async Task InvokeAsync(HttpContext context, IEnforcer enforcer)
{
    var path = context.Request.Path.Value;
    
    // 1. 白名单放行 (登录、静态资源等)
    if (IsWhitelisted(path)) { await _next(context); return; }

    // 2. 获取上下文信息
    var userId = context.User.FindFirst("sub")?.Value;
    var deptId = context.User.FindFirst("dept")?.Value;
    var method = context.Request.Method;

    // 3. 执行鉴权 (CachedEnforcer 会优先查缓存)
    // 注意：sub 传 userId，Casbin 会自动推导 g 关系中的角色
    if (await enforcer.EnforceAsync(userId, deptId, path, method))
    {
        await _next(context);
    }
    else
    {
        context.Response.StatusCode = 403;
    }
}
```

---

### ✅ 最终执行建议

1.  **开发顺序**：
    *   第一步：搭建 Casbin 基础结构，配置 `model.conf` 和 SqlSugar 适配器。
    *   第二步：编写“数据迁移工具”，将现有业务表数据导入 `CasbinRule` 表。
    *   第三步：实现 Middleware 拦截器，先打印日志不拦截，验证 `Enforce` 结果是否正确。
    *   第四步：实现“双写事务”逻辑，并接入 Redis Watcher。
    *   第五步：正式开启拦截。

2.  **避坑指南**：
    *   不要手动往 `CasbinRule` 表写 SQL，永远使用 `AddPolicy` API。
    *   不要在 `EnforceAsync` 之前忘记 `await`，否则会导致并发上下文问题。
    *   确保 Redis 服务高可用，否则 Watcher 断连可能导致节点策略不一致。

