# 解决 SQLite "Database is Locked" 集成指南 (SqlSugar + ABP)

## 问题根因

当前的 `SQLite Error 5: 'database is locked'` 是由 **双 SqlSugar 客户端死锁** 引起的：

1. **Client A (业务层)**：您的业务代码（`RoleService`）使用的 `ISqlSugarClient` 实例，已经开启了事务并持有 SQLite 写锁。
2. **Client B (Casbin Adapter)**：`SqlSugarAdapter` 注入的是**另一个** `ISqlSugarClient` 实例，它有自己独立的数据库连接。
3. **死锁**：Client B 试图访问数据库时，发现已被 Client A 锁定，等待 30 秒后超时。

## 解决方案：使用同一个 SqlSugar 客户端实例

SqlSugar 项目中的正确做法是确保**整个请求生命周期内使用同一个 `ISqlSugarClient` 实例**。

### 方案一：使用 SqlSugarScope（推荐）

`SqlSugarScope` 是 SqlSugar 专门为依赖注入设计的线程安全容器，天然支持作用域隔离。

#### 1. 在 DI 配置中注册 SqlSugarScope

在您的 `Startup.cs` 或 ABP Module 的 `ConfigureServices` 方法中：

```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    var configuration = context.Services.GetConfiguration();
    
    // 替换原有的 ISqlSugarClient 注册
    context.Services.AddScoped<ISqlSugarClient>(sp =>
    {
        // 使用 SqlSugarScope 而不是 SqlSugarClient
        var scope = new SqlSugarScope(new ConnectionConfig
        {
            ConnectionString = configuration["ConnectionStrings:Default"],
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true,  // Scope 会自动管理连接生命周期
            InitKeyType = InitKeyType.Attribute
        });
        
        return scope;
    });
    
    // Casbin Adapter 会自动复用同一个 ISqlSugarClient 实例
    context.Services.AddScoped<SqlSugarAdapter>(sp =>
    {
        var client = sp.GetRequiredService<ISqlSugarClient>(); // 获取同一个实例！
        return new SqlSugarAdapter(client);
    });
}
```

**关键点**：
- ✅ `AddScoped` 确保同一个 HTTP 请求中，所有地方注入的 `ISqlSugarClient` 都是**同一个实例**
- ✅ 业务代码和 Casbin Adapter 共享同一个连接和事务

### 方案二：手动共享物理连接（备选）

如果您必须使用两个独立的 `SqlSugarClient` 实例，可以手动共享底层连接：

```csharp
// 在 RoleService 中
public async Task UpdateAsync(Guid id, RoleUpdateInputVo input)
{
    // 业务更新逻辑...
    
    // 在调用 Casbin 之前，手动同步连接和事务
    var businessClient = _sqlSugarRepository.Context; // 您的业务 SqlSugar 实例
    var casbinClient = _enforcer.GetAdapter<SqlSugarAdapter>().DbClient;
    
    // 共享物理连接
    casbinClient.Ado.Connection = businessClient.Ado.Connection;
    casbinClient.Ado.Transaction = businessClient.Ado.Transaction;
    
    // 现在调用 Casbin
    await _enforcer.SavePolicyAsync();
}
```

**缺点**：需要在每个调用点手动同步，容易遗漏。

### 方案三：启用 WAL 模式（强烈推荐作为辅助）

无论选择哪种方案，都**强烈建议**启用 SQLite 的 Write-Ahead Logging (WAL) 模式，大幅提升并发能力：

```sql
PRAGMA journal_mode = WAL;
```

或在代码中执行一次（应用启动时）：

```csharp
using (var connection = new SqliteConnection(connectionString))
{
    connection.Open();
    connection.Execute("PRAGMA journal_mode = WAL;");
}
```

## 验证方法

修改配置后，重新运行应用：

1. 在日志中应该看到 `SharesConnection=True`
2. Casbin 的 `DEBUG` 日志应该显示正常保存策略
3. 不再出现 "database is locked" 错误

## 总结

**推荐使用方案一**：将 `ISqlSugarClient` 改为 `SqlSugarScope`，并注册为 `Scoped` 生命周期。这是 SqlSugar 官方推荐的多租户/依赖注入场景最佳实践。

**辅助措施**：启用 SQLite WAL 模式。
