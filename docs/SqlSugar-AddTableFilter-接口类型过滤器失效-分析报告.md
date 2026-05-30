# SqlSugar AddTableFilter 接口类型过滤器失效 — 完整分析报告

## 一、问题概述

| 项 | 内容 |
|---|---|
| **现象** | 菜单管理中删除记录后，前端仍能看到已删除的数据 |
| **数据库** | `is_deleted` 字段已正确更新为 `TRUE`，软删除写入正常 |
| **根因定位** | `QueryFilter.AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted)` 接口类型过滤器在 SQL 生成时不生效，SQL 中缺少 `AND (is_deleted = false)` |
| **环境** | .NET 10 (net10.0.0) + SqlSugarCore 5.1.4.214 + ABP Framework 10.x + PostgreSQL |
| **修复状态** | 已用反射方案绕过（对每个具体实体类型单独注册），但**根因未明** |

---

## 二、架构分析（完整调用链）

### 2.1 DI 注册结构

```
ISqlSugarDbContext (Scoped)
  └── SqlSugarDbContextFactory         ← SharpFort.SqlSugarCore 模块

IEnumerable<ISqlSugarDbContextDependencies> (Transient)
  ├── DefaultSqlSugarDbContext         ← SharpFort.SqlSugarCore 模块
  │     └── CustomDataFilter: AddTableFilter<ISoftDelete> + AddTableFilter<IMultiTenant>
  ├── SfCasbinRbacDbContext            ← SharpFort.CasbinRbac.SqlSugarCore 模块
  │     └── CustomDataFilter: AddTableFilter(Expression<Func<User, bool>>) + AddTableFilter(Expression<Func<Role, bool>>)
  ├── FileManagementDbContext
  ├── AiModuleDbContext
  ├── FluidSequenceDbContext
  └── SfDbContext
```

### 2.2 过滤器注册流程

```
SqlSugarDbContextFactory 构造函数 (每次 Scope 创建)
  └── new SqlSugarScope(connectionConfig)
  └── ConfigureDbAop(sqlSugarClient)
        ├── sqlSugarClient.QueryFilter.AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted)   ← DefaultSqlSugarDbContext
        ├── sqlSugarClient.QueryFilter.AddTableFilter<IMultiTenant>(entity => entity.TenantId == ...) ← DefaultSqlSugarDbContext
        └── sqlSugarClient.QueryFilter.AddTableFilter(expUser.ToExpression())                    ← SfCasbinRbacDbContext
            sqlSugarClient.QueryFilter.AddTableFilter(expRole.ToExpression())                    ← SfCasbinRbacDbContext
```

### 2.3 仓储查询流程

```
SqlSugarRepository<TEntity>._DbQueryable
  └── _dbContextProvider.GetDbContextAsync()
        └── ISqlSugarDbContext → SqlSugarDbContextFactory
              └── SqlSugarScope (已注册所有过滤器)
                    └── Queryable<Menu>()  ← 应自动附加全局过滤器
```

### 2.4 类继承关系

```
SqlSugarDbContext (基类，实现 ISqlSugarDbContextDependencies)
  ├── DefaultSqlSugarDbContext   ← 软删除 + 多租户 + 审计字段 + 实体事件
  └── SfCasbinRbacDbContext      ← 数据权限过滤（未继承 DefaultSqlSugarDbContext）
```

---

## 三、诊断过程

### 3.1 第一阶段：打点确认过滤器是否被注册

在三个关键位置添加 `[DIAG]` Warning 级别日志：

**位置1：`SqlSugarDbContextFactory.ConfigureDbAop`**
```csharp
logger.LogWarning("[DIAG] ConfigureDbAop: Found {Count} ISqlSugarDbContextDependencies", dependencies.Count);
// ... 遍历每个 dependency 并记录类型名
logger.LogWarning("[DIAG] ConfigureDbAop: Invoking onClientConfig chain...");
```

**位置2：`DefaultSqlSugarDbContext.CustomDataFilter`**
```csharp
logger.LogWarning("[DIAG] DefaultSqlSugarDbContext.CustomDataFilter: IsSoftDeleteFilterEnabled={SoftDelete}", softDeleteEnabled);
logger.LogWarning("[DIAG] SoftDelete filter ADDED via AddTableFilter<ISoftDelete>");
```

**位置3：`SfCasbinRbacDbContext.CustomDataFilter`**
```csharp
logger.LogWarning("[DIAG] SfCasbinRbacDbContext.CustomDataFilter: IDataPermission.IsEnabled={DataPermission}");
```

### 3.2 第一次日志输出（未编译到 SharpFort.SqlSugarCore）

首次运行只看到 `SfCasbinRbacDbContext` 的日志，**没有**看到 `ConfigureDbAop` 和 `DefaultSqlSugarDbContext` 的日志。

**原因**：只编译了 `SharpFort.CasbinRbac.SqlSugarCore` 项目，`SharpFort.SqlSugarCore` 项目未被重新编译。

**解决**：加 `System.Console.WriteLine`（绕过日志系统确认编译状态），并完整 Rebuild Solution。

### 3.3 完整 Rebuild 后的日志（关键证据）

每次 HTTP 请求都完整执行了以下链路：

```
[DIAG-CONSOLE] ConfigureDbAop: START
[DIAG-CONSOLE] SqlSugarDbContext.OnSqlSugarClientConfig: DefaultSqlSugarDbContext
[DIAG] ConfigureDbAop: Found 6 ISqlSugarDbContextDependencies
[DIAG] ConfigureDbAop: Processing dependency DefaultSqlSugarDbContext (ExecutionOrder=0)
[DIAG] ConfigureDbAop: Processing dependency SfCasbinRbacDbContext (ExecutionOrder=0)
[DIAG] ConfigureDbAop: Processing dependency FileManagementDbContext (ExecutionOrder=0)
[DIAG] ConfigureDbAop: Processing dependency AiModuleDbContext (ExecutionOrder=0)
[DIAG] ConfigureDbAop: Processing dependency FluidSequenceDbContext (ExecutionOrder=0)
[DIAG] ConfigureDbAop: Processing dependency SfDbContext (ExecutionOrder=0)
[DIAG] ConfigureDbAop: Invoking onClientConfig chain...
[DIAG] DefaultSqlSugarDbContext.CustomDataFilter: IsSoftDeleteFilterEnabled=True, IsMultiTenantFilterEnabled=True
[DIAG] SoftDelete filter ADDED via AddTableFilter<ISoftDelete>
[DIAG] MultiTenant filter ADDED, TenantId=null
[DIAG] SfCasbinRbacDbContext.CustomDataFilter: IDataPermission.IsEnabled=True, CurrentUser.Id=6a033f96...
[DIAG] SfCasbinRbacDbContext: DataPermissionFilter applied
[DIAG] ConfigureDbAop: onClientConfig chain completed
```

### 3.4 结论

1. `ConfigureDbAop` — **被调用** ✓
2. `DefaultSqlSugarDbContext.CustomDataFilter` — **被调用** ✓
3. `IsSoftDeleteFilterEnabled` — **值为 True** ✓
4. `AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted)` — **被执行** ✓
5. 实际 SQL 中**没有** `AND (is_deleted = false)` — **过滤器未生效** ✗

---

## 四、关键对比证据

### 4.1 三种 AddTableFilter 调用方式的效果对比

| 调用方式 | 类型参数 | 调用位置 | SQL 中是否生效 |
|---------|---------|---------|-------------|
| `AddTableFilter<ISoftDelete>(e => !e.IsDeleted)` | **接口** `ISoftDelete` | `DefaultSqlSugarDbContext` | **不生效** ✗ |
| `AddTableFilter<IMultiTenant>(e => e.TenantId == ...)` | **接口** `IMultiTenant` | `DefaultSqlSugarDbContext` | **未验证**（推测同样不生效） |
| `AddTableFilter(Expression<Func<User, bool>>)` | **具体实体** `User` | `SfCasbinRbacDbContext` | **生效** ✓ |
| `AddTableFilter(Expression<Func<Role, bool>>)` | **具体实体** `Role` | `SfCasbinRbacDbContext` | **生效** ✓ |
| `AddTableFilter<Menu>(e => !e.IsDeleted)` | **具体实体** `Menu` | 修复后 | **生效** ✓ |

### 4.2 Menu 实体继承链

```
Menu : FullAuditedAggregateRoot<Guid>
  : AuditedAggregateRoot<Guid>
    : CreationAuditedAggregateRoot<Guid>
      : AuditedEntity<Guid>
        : Entity<Guid>
  , ISoftDelete          ← bool IsDeleted { get; set; }
  , IDeletionAuditedObject
  , IHasDeletionTime
  , IHasConcurrencyStamp
  , IOrderNum
  , IState
```

`typeof(ISoftDelete).IsAssignableFrom(typeof(Menu))` 返回 `true`。

### 4.3 同一版本的另一个已知 Bug

项目中之前发现并记录了 SqlSugar 5.1.4.214 + .NET 10 的 `Includes` 导航属性加载全局失效 Bug（详见 `module/casbin-rbac/SqlSugar-Includes-Bug-分析报告.md`）。两个 Bug 都涉及 .NET 10 表达式树/类型反射的兼容性问题，推测同根同源。

---

## 五、修复方案（已实施）

### 5.1 修复原理

将接口类型过滤器改为对每个具体实体类型单独注册：

```
修复前（失效）：
  sqlSugarClient.QueryFilter.AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted);

修复后（生效）：
  sqlSugarClient.QueryFilter.AddTableFilter<Menu>(e => !e.IsDeleted);
  sqlSugarClient.QueryFilter.AddTableFilter<Role>(e => !e.IsDeleted);
  sqlSugarClient.QueryFilter.AddTableFilter<User>(e => !e.IsDeleted);
  // ... 自动扫描所有 [SugarTable] ISoftDelete 实体
```

### 5.2 实现方式

在 `DefaultSqlSugarDbContext` 中：

1. `GetEntityTypes(Type interfaceType)` — 扫描 `AppDomain.CurrentDomain.GetAssemblies()` 中所有带 `[SugarTable]` 且实现了指定接口的非抽象实体类型（带 `ConcurrentDictionary` 缓存）
2. `AddEntityFilters(...)` — 对每个实体类型，通过反射调用 `QueryFilterProvider.AddTableFilter<T>` 的泛型方法
3. `CustomDataFilter` — 使用表达式树动态构建 `e => !e.IsDeleted` 和 `e => e.TenantId == currentTenantId`

### 5.3 修改的文件

| 文件 | 修改内容 |
|------|---------|
| `framework/SharpFort.SqlSugarCore/DefaultSqlSugarDbContext.cs` | 核心修复：新增 `GetEntityTypes`、`AddEntityFilters`，重写 `CustomDataFilter` |
| `framework/SharpFort.SqlSugarCore/SqlSugarDbContextFactory.cs` | 清理诊断日志 |
| `framework/SharpFort.SqlSugarCore/SqlSugarDbContext.cs` | 清理诊断日志 |
| `module/casbin-rbac/.../SfCasbinRbacDbContext.cs` | 清理诊断日志 |

---

## 六、未解之谜（需深入 SqlSugar 源码）

**核心问题**：`AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted)` 调用了但 SQL 中不生效。

### 需要在 SqlSugar 源码中分析的关键位置

**文件：`SqlSugar/QueryFilter/QueryFilterProvider.cs`**

#### 1. `AddTableFilter<T>` 方法 — 接口类型如何存储

```
GITHUB: DotNetNext/SqlSugar → Src/Asp.Net/SqlSugar/QueryFilter/QueryFilterProvider.cs
```

关注点：
- 当 `T` 是接口类型（如 `ISoftDelete`）时，过滤器以什么 Key 存储？
  - 如果是 `typeof(ISoftDelete)` 作为 Key，后续查询 `Menu` 时如何匹配？
  - .NET 10 下 `Type` 作为字典 Key 的行为是否有变化？
- 与具体实体类型（`User`、`Role`）的存储方式有何不同？

#### 2. `GetQueryFilter` 或查询构建时 — 如何匹配实体到过滤器

```
GITHUB: DotNetNext/SqlSugar → Src/Asp.Net/SqlSugar/Queryable/QueryableAccessory.cs
或相关查询构建文件
```

关注点：
- 为 `Menu` 实体构建查询时，如何查找已注册的全局过滤器？
  - 是遍历所有注册的过滤器并检查 `Type.IsAssignableFrom(entityType)` 吗？
  - 还是有一个反向索引（从实体类型到过滤器）？
- .NET 10 下 `Type.IsAssignableFrom` 的行为是否发生了变化？
  - 特别是跨程序集的接口继承关系解析
  - 表达式树参数类型 `ISoftDelete` 到具体实体 `Menu` 的类型替换逻辑

#### 3. 表达式树处理 — 接口类型 Lambda 的参数类型转换

```
GITHUB: DotNetNext/SqlSugar → Src/Asp.Net/SqlSugar/Expressions/...
```

关注点：
- `Expression<Func<ISoftDelete, bool>>` 参数类型是 `ISoftDelete`，应用到 `Menu` 实体时需要将 `parameter` 从 `ISoftDelete` 替换为 `Menu`
- 这个替换过程（`ExpressionParameterReplacer` 或类似）在 .NET 10 下是否正常工作？
- `entity.IsDeleted` — 属性访问表达式对接口类型的解析在 .NET 10 下是否有变化？

### 辅助分析建议

可以在上述 SqlSugar 源码位置添加以下诊断：
1. 记录 `AddTableFilter<T>` 接收到的 `T` 类型和存储 Key
2. 记录查询构建时匹配到的过滤器列表
3. 记录表达式树参数替换前后的类型

---

## 七、环境信息

| 项 | 值 |
|---|---|
| .NET 版本 | net10.0.0 |
| SqlSugarCore | 5.1.4.214 |
| ABP Framework | 10.x |
| 数据库 | PostgreSQL |
| OS | Windows 11 |

---

## 八、相关文件索引

| 文件 | 路径 |
|------|------|
| DIAG 日志添加位置 | 已清理，详见第六节建议的 SqlSugar 源码位置 |
| 相关 Bug 报告 | `module/casbin-rbac/SqlSugar-Includes-Bug-分析报告.md` |
