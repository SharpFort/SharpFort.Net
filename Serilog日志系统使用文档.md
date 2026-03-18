# SharpFort.Net 项目 Serilog 日志系统使用文档

## 一、概述

本项目使用 **Serilog.AspNetCore** 作为日志收集和记录工具，结合 ABP vNext 框架的 `Volo.Abp.AspNetCore.Serilog` 模块，实现了完整的日志收集、过滤、存储和审计功能。

### 日志系统架构

```
┌─────────────────────────────────────────────────────────────┐
│                      应用程序层                               │
│  (Controllers, Services, Domain Events)                     │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│                   Serilog 日志收集层                          │
│  • ILogger<T> 注入                                           │
│  • Log.Information/Warning/Error/Debug                      │
│  • ABP Serilog Enrichers (增强器)                           │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│                   日志过滤与处理                              │
│  • 最小日志级别过滤 (Debug/Information/Warning/Error)        │
│  • 排除特定日志 (TaskCanceledException)                     │
│  • 按命名空间过滤 (Microsoft, Quartz)                        │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│                   日志输出 Sinks                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  文件输出     │  │  控制台输出   │  │  数据库输出   │      │
│  │  (异步)      │  │  (异步)      │  │  (审计日志)   │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

---

## 二、NuGet 包依赖

项目中使用的 Serilog 相关包（位于 `Sf.Abp.Web.csproj`）：

```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.0.1" />
<PackageReference Include="Serilog.Sinks.Async" Version="1.5.0" />
<PackageReference Include="Volo.Abp.AspNetCore.Serilog" Version="$(AbpVersion)" />
```

### 包说明

- **Serilog.AspNetCore**: Serilog 的 ASP.NET Core 集成包，提供日志记录核心功能
- **Serilog.Sinks.Async**: 异步日志输出，避免阻塞主线程，提升性能
- **Volo.Abp.AspNetCore.Serilog**: ABP 框架的 Serilog 集成模块，提供增强器和中间件

---

## 三、日志配置

### 3.1 Program.cs 中的日志初始化

**文件位置**: `src/Sf.Abp.Web/Program.cs`

```csharp
using Serilog;
using Serilog.Events;

// 创建全局静态日志记录器
Log.Logger = new LoggerConfiguration()
    // 过滤器：排除 TaskCanceledException 相关日志
    .Filter.ByExcluding(log =>
        log.Exception?.GetType() == typeof(TaskCanceledException) ||
        log.MessageTemplate.Text.Contains("\"message\": \"A task was canceled.\""))

    // 最小日志级别：Debug
    .MinimumLevel.Debug()

    // 覆盖特定命名空间的日志级别
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Error)
    .MinimumLevel.Override("Quartz", LogEventLevel.Warning)

    // 从日志上下文中丰富日志信息
    .Enrich.FromLogContext()

    // 异步写入文件 - 所有日志（Debug 及以上）
    .WriteTo.Async(c => c.File(
        "logs/all/log-.txt",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: LogEventLevel.Debug))

    // 异步写入文件 - 仅错误日志（Error 及以上）
    .WriteTo.Async(c => c.File(
        "logs/error/errorlog-.txt",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: LogEventLevel.Error))

    // 异步写入控制台
    .WriteTo.Async(c => c.Console())

    .CreateLogger();

try
{
    Log.Information("Sf框架-Abp.vNext，启动！");

    var builder = WebApplication.CreateBuilder(args);

    // 使用 Serilog 作为日志提供程序
    builder.Host.UseSerilog();

    await builder.Services.AddApplicationAsync<SfAbpWebModule>();
    var app = builder.Build();
    await app.InitializeApplicationAsync();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Sf框架-Abp.vNext，爆炸！");
}
finally
{
    // 确保所有日志被刷新到目标
    Log.CloseAndFlush();
}
```

### 3.2 日志级别说明

| 日志级别 | 说明 | 使用场景 |
|---------|------|---------|
| **Debug** | 调试信息 | 开发环境详细追踪，生产环境不建议开启 |
| **Information** | 一般信息 | 应用程序正常运行的关键节点 |
| **Warning** | 警告信息 | 潜在问题，但不影响运行 |
| **Error** | 错误信息 | 异常和错误，需要关注 |
| **Fatal** | 致命错误 | 导致应用程序崩溃的严重错误 |

### 3.3 日志文件输出

日志文件存储在 `src/Sf.Abp.Web/logs/` 目录下：

```
logs/
├── all/                    # 所有日志（Debug 及以上）
│   ├── log-20260201.txt
│   ├── log-20260202.txt
│   └── ...
└── error/                  # 错误日志（Error 及以上）
    ├── errorlog-20260201.txt
    ├── errorlog-20260202.txt
    └── ...
```

**特点**：
- 按天滚动（`RollingInterval.Day`）
- 异步写入，不阻塞主线程
- 自动创建目录

---

## 四、ABP 模块集成

### 4.1 模块依赖

**文件位置**: `src/Sf.Abp.Web/SfAbpWebModule.cs`

```csharp
[DependsOn(
    typeof(AbpAspNetCoreSerilogModule),  // ABP Serilog 模块
    // ... 其他模块
)]
public class SfAbpWebModule : AbpModule
{
    // ...
}
```

### 4.2 中间件配置

在 `OnApplicationInitializationAsync` 方法中启用 Serilog 增强器：

```csharp
public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
{
    var app = context.GetApplicationBuilder();

    // ... 其他中间件

    // 审计日志
    app.UseAuditing();

    // Serilog 日志增强器（添加请求上下文信息）
    app.UseAbpSerilogEnrichers();

    // ... 其他中间件
}
```

**`UseAbpSerilogEnrichers()` 的作用**：
- 自动添加请求 ID、用户 ID、租户 ID 等上下文信息到日志
- 丰富日志内容，便于追踪和调试

---

## 五、日志类型与使用场景

### 5.1 应用程序日志（Serilog 文件日志）

#### 使用方式

通过依赖注入 `ILogger<T>` 使用：

```csharp
public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;

    public AuthService(ILogger<AuthService> logger)
    {
        _logger = logger;
    }

    public async Task LoginAsync(string username)
    {
        _logger.LogInformation("用户 {Username} 尝试登录", username);

        try
        {
            // 业务逻辑
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录失败: {Username}", username);
            throw;
        }
    }
}
```

#### 日志示例

```
2026-02-01 13:20:14.347 +08:00 [INF] Sf框架-Abp.vNext，启动！
2026-02-01 13:20:15.788 +08:00 [INF] 当前主机启动环境-【Development】
2026-02-01 13:20:15.788 +08:00 [INF] 当前主机启动地址-【http://*:19001】
```

### 5.2 审计日志（数据库日志）

#### 配置

**文件位置**: `src/Sf.Abp.Web/SfAbpWebModule.cs`

```csharp
public override Task ConfigureServicesAsync(ServiceConfigurationContext context)
{
    // 审计日志配置
    Configure<AbpAuditingOptions>(options =>
    {
        // 默认关闭，开启会有大量的审计日志
        options.IsEnabled = false;
    });

    // 忽略审计日志路径
    Configure<AbpAspNetCoreAuditingOptions>(options =>
    {
        options.IgnoredUrls.Add("/api/app/file/");
        options.IgnoredUrls.Add("/hangfire");
    });
}
```

#### 审计日志存储

**实现类**: `module/audit-logging/SharpFort.AuditLogging.Domain/AuditingStore.cs`

```csharp
public class AuditingStore : IAuditingStore, ITransientDependency
{
    protected IAuditLogRepository AuditLogRepository { get; }

    public virtual async Task SaveAsync(AuditLogInfo auditInfo)
    {
        // 记录调试日志
        Logger.LogDebug("Sf-请求追踪:" + JsonConvert.SerializeObject(auditInfo));

        // 保存到数据库
        using (var uow = UnitOfWorkManager.Begin())
        {
            await AuditLogRepository.InsertAsync(
                await Converter.ConvertAsync(auditInfo));
            await uow.CompleteAsync();
        }
    }
}
```

#### 审计日志实体

**文件位置**: `module/audit-logging/SharpFort.AuditLogging.Domain/Entities/AuditLog.cs`

**数据库表**: `Sf-AuditLog`

**字段包括**：
- 应用程序名称
- 租户信息
- 用户信息
- 执行时间和持续时间
- 客户端 IP、浏览器信息
- HTTP 方法、URL、状态码
- 请求参数和返回结果
- 异常信息

### 5.3 登录日志（数据库日志）

#### 实体定义

**文件位置**: `module/casbin-rbac/SharpFort.CasbinRbac.Domain/Entities/LoginLog.cs`

**数据库表**: `casbin_sys_login_log`

**字段**：
- `LoginUser`: 登录账号/用户名
- `LoginIp`: 登录 IP
- `LoginLocation`: 登录地点
- `Browser`: 浏览器
- `Os`: 操作系统
- `LogMsg`: 日志消息（如"登录成功"）
- `CreationTime`: 创建时间

#### 记录方式

通过领域事件自动记录：

**文件位置**: `module/casbin-rbac/SharpFort.CasbinRbac.Domain/EventHandlers/LoginEventHandler.cs`

```csharp
public class LoginEventHandler : ILocalEventHandler<LoginEventArgs>
{
    private readonly ILogger<LoginEventHandler> _logger;
    private readonly IRepository<LoginLog> _loginLogRepository;

    public async Task HandleEventAsync(LoginEventArgs eventData)
    {
        // 记录到 Serilog 文件日志
        _logger.LogInformation($"用户【{eventData.UserId}:{eventData.UserName}】登入系统");

        // 创建登录日志实体
        var loginLogEntity = new LoginLog(
            id: _guidGenerator.Create(),
            loginUser: eventData.UserName,
            logMsg: eventData.UserName + "登录系统",
            loginIp: eventData.LoginIp,
            loginLocation: eventData.LoginLocation,
            browser: eventData.Browser,
            os: eventData.Os,
            creatorId: eventData.UserId
        );

        // 保存到数据库
        await _loginLogRepository.InsertAsync(loginLogEntity);
    }
}
```

### 5.4 操作日志（数据库日志）

#### 实体定义

**文件位置**: `module/casbin-rbac/SharpFort.CasbinRbac.Domain/Entities/OperationLog.cs`

**数据库表**: `casbin_sys_operation_log`

**字段**：
- `Title`: 操作模块（如"用户管理"）
- `OperType`: 操作类型（枚举：增删改查等）
- `RequestMethod`: 请求方式（GET/POST/PUT/DELETE）
- `Method`: 方法名称（Controller.Action）
- `OperUser`: 操作人员账号
- `OperIp`: 操作 IP
- `OperLocation`: 操作地点
- `RequestParam`: 请求参数（JSON）
- `RequestResult`: 返回结果（JSON）
- `CreationTime`: 创建时间

---

## 六、日志收集流程

### 6.1 应用程序日志流程

```
┌─────────────────┐
│  应用程序代码    │
│  _logger.LogXXX │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Serilog 核心   │
│  日志级别过滤    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  日志增强器      │
│  添加上下文信息  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  异步 Sink      │
│  写入文件/控制台 │
└─────────────────┘
```

### 6.2 审计日志流程

```
┌─────────────────┐
│  HTTP 请求      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  ABP 审计中间件  │
│  UseAuditing()  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  AuditingStore  │
│  SaveAsync()    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  数据库存储      │
│  Sf-AuditLog表  │
└─────────────────┘
```

### 6.3 登录日志流程

```
┌─────────────────┐
│  用户登录请求    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  AuthService    │
│  登录验证        │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  发布领域事件    │
│  LoginEventArgs │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ LoginEventHandler│
│  事件处理器      │
└────────┬────────┘
         │
         ├─────────────────┐
         │                 │
         ▼                 ▼
┌─────────────────┐ ┌─────────────────┐
│  Serilog 文件   │ │  数据库存储      │
│  日志记录        │ │  LoginLog 表    │
└─────────────────┘ └─────────────────┘
```

---

## 七、配置文件说明

### 7.1 appsettings.json

**文件位置**: `src/Sf.Abp.Web/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**注意**：虽然 `appsettings.json` 中有 `Logging` 配置，但由于项目使用了 `builder.Host.UseSerilog()`，实际的日志配置由 `Program.cs` 中的 Serilog 配置控制。

### 7.2 数据库配置

审计日志、登录日志、操作日志都存储在数据库中，使用 SqlSugar ORM：

```json
{
  "DbConnOptions": {
    "Url": "Host=localhost;Port=5432;Database=sharpfort;Username=postgres;Password=***;",
    "DbType": "PostgreSQL",
    "EnabledSqlLog": true
  }
}
```

---

## 八、开发使用指南

### 8.1 在代码中记录日志

#### 1. 注入 ILogger

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }
}
```

#### 2. 记录不同级别的日志

```csharp
// 调试信息
_logger.LogDebug("调试信息: {Value}", someValue);

// 一般信息
_logger.LogInformation("用户 {UserId} 执行了操作 {Action}", userId, action);

// 警告
_logger.LogWarning("配置项 {ConfigKey} 未设置，使用默认值", configKey);

// 错误
_logger.LogError(exception, "处理请求时发生错误: {RequestId}", requestId);

// 致命错误
_logger.LogFatal(exception, "应用程序崩溃");
```

#### 3. 使用结构化日志

```csharp
// ✅ 推荐：使用占位符
_logger.LogInformation("用户 {Username} 从 {IpAddress} 登录", username, ipAddress);

// ❌ 不推荐：字符串拼接
_logger.LogInformation($"用户 {username} 从 {ipAddress} 登录");
```

### 8.2 启用/禁用审计日志

修改 `SfAbpWebModule.cs`：

```csharp
Configure<AbpAuditingOptions>(options =>
{
    // 启用审计日志
    options.IsEnabled = true;  // 改为 true
});
```

**注意**：启用后会记录大量请求信息，建议仅在需要时开启。

### 8.3 自定义日志过滤

在 `Program.cs` 中添加过滤规则：

```csharp
Log.Logger = new LoggerConfiguration()
    .Filter.ByExcluding(log =>
        // 排除特定异常
        log.Exception?.GetType() == typeof(MyCustomException))
    .Filter.ByExcluding(log =>
        // 排除特定消息
        log.MessageTemplate.Text.Contains("HealthCheck"))
    // ...
    .CreateLogger();
```

### 8.4 添加自定义 Sink

如果需要将日志输出到其他目标（如 Elasticsearch、数据库等）：

```csharp
Log.Logger = new LoggerConfiguration()
    // ... 现有配置
    .WriteTo.Async(c => c.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))))
    .CreateLogger();
```

---

## 九、日志查询与分析

### 9.1 文件日志查询

使用文本编辑器或命令行工具查看：

```bash
# 查看最新的所有日志
tail -f logs/all/log-20260318.txt

# 查看错误日志
tail -f logs/error/errorlog-20260318.txt

# 搜索特定关键字
grep "用户登录" logs/all/log-20260318.txt
```

### 9.2 数据库日志查询

#### 审计日志查询

```sql
SELECT * FROM "Sf-AuditLog"
WHERE "ExecutionTime" >= '2026-03-01'
ORDER BY "ExecutionTime" DESC;
```

#### 登录日志查询

```sql
SELECT * FROM casbin_sys_login_log
WHERE "LoginUser" = 'admin'
ORDER BY "CreationTime" DESC;
```

#### 操作日志查询

```sql
SELECT * FROM casbin_sys_operation_log
WHERE "OperType" = 2  -- 删除操作
ORDER BY "CreationTime" DESC;
```

---

## 十、性能优化建议

### 10.1 使用异步 Sink

项目已使用 `WriteTo.Async()`，确保日志写入不阻塞主线程。

### 10.2 控制日志级别

生产环境建议：
- 最小级别设置为 `Information`
- 关闭 `Debug` 级别日志

```csharp
.MinimumLevel.Information()  // 生产环境
```

### 10.3 限制日志大小

添加文件大小限制：

```csharp
.WriteTo.Async(c => c.File(
    "logs/all/log-.txt",
    rollingInterval: RollingInterval.Day,
    fileSizeLimitBytes: 100_000_000,  // 100MB
    rollOnFileSizeLimit: true))
```

### 10.4 定期清理日志

设置日志保留天数：

```csharp
.WriteTo.Async(c => c.File(
    "logs/all/log-.txt",
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 30))  // 保留 30 天
```

---

## 十一、常见问题

### Q1: 日志文件没有生成？

**检查**：
- 确保应用程序有写入 `logs/` 目录的权限
- 检查 `Program.cs` 中的日志配置是否正确
- 查看控制台是否有错误信息

### Q2: 日志级别不生效？

**原因**：`appsettings.json` 中的 `Logging` 配置被 Serilog 覆盖。

**解决**：在 `Program.cs` 的 Serilog 配置中修改日志级别。

### Q3: 审计日志没有记录？

**检查**：
- `AbpAuditingOptions.IsEnabled` 是否为 `true`
- 请求路径是否在 `IgnoredUrls` 列表中
- 数据库连接是否正常

### Q4: 如何在生产环境中查看日志？

**方案**：
1. 使用日志聚合工具（如 ELK、Seq）
2. 配置远程日志收集
3. 使用云日志服务（如 Azure Application Insights）

---

## 十二、总结

### 日志系统特点

1. **多层次日志**：
   - 应用程序日志（Serilog 文件）
   - 审计日志（数据库）
   - 登录日志（数据库）
   - 操作日志（数据库）

2. **高性能**：
   - 异步写入
   - 日志级别过滤
   - 按需启用审计日志

3. **易于使用**：
   - 依赖注入 `ILogger<T>`
   - 结构化日志
   - 自动上下文信息

4. **灵活配置**：
   - 代码配置（Program.cs）
   - 模块化设计
   - 可扩展 Sink

### 最佳实践

1. 使用结构化日志（占位符而非字符串拼接）
2. 选择合适的日志级别
3. 避免记录敏感信息（密码、Token）
4. 定期清理历史日志
5. 生产环境关闭 Debug 日志
6. 使用日志聚合工具进行集中管理

---

**文档版本**: 1.0
**更新日期**: 2026-03-18
**适用项目**: SharpFort.Net (ABP vNext + Serilog)
