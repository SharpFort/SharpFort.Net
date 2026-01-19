# 审计日志（Audit Logging）模块分析报告

## 1. 现状分析

基于对 `module/audit-logging` 模块及项目启动文件 (`src/Yi.Abp.Web/Program.cs`) 的代码分析，以下是关于当前日志系统的详细现状：

### 1.1 使用的日志库
*   **应用层日志 (Application Logging):** 项目使用了 **Serilog** 作为主要的日志记录库。在 `src/Yi.Abp.Web/Program.cs` 中进行了配置，支持控制台输出和文件滚动存储（`logs/all/` 和 `logs/error/`）。
*   **审计日志 (Audit Logging):** 审计日志模块依赖于 **ABP Framework** 的审计系统 (`Volo.Abp.Auditing`)。虽然它使用了 `Microsoft.Extensions.Logging` (ILogger) 来记录调试信息，但其核心职责是实现 `IAuditingStore` 接口，将审计数据持久化。

### 1.2 日志收集手段
审计日志主要通过 ABP 框架的 **拦截器 (Interceptors)** 和 **中间件 (Middleware)** 机制自动收集。
*   **Web 请求信息:** 包括 URL、HTTP 方法、客户端 IP、浏览器信息、执行时间、HTTP 状态码等。
*   **应用服务调用:** 记录了具体的 Service 和 Method 调用，以及参数（`AuditLogAction`）。
*   **实体变更追踪:** 记录了实体的创建、修改、删除操作，包含变更前后的属性值对比（`EntityChange` 和 `EntityPropertyChange`）。
*   **异常捕获:** 自动收集请求处理过程中的异常信息。

### 1.3 日志存储方式
*   **存储介质:** 关系型数据库 (RDBMS)。
*   **ORM 框架:** **SqlSugar** (`SqlSugarRepository`).
*   **表结构:**
    *   `Yi-AuditLog`: 存储请求维度的核心信息。
    *   `YiAuditLogAction`: 存储具体的服务方法调用。
    *   `YiEntityChange`: 存储实体级别的变更记录。
    *   `YiEntityPropertyChange`: 存储字段级别的变更详情。
*   **数据流向:** `AuditLogInfo` (ABP 对象) -> `AuditingStore` (实现类) -> `AuditLogInfoToAuditLogConverter` (转换器) -> `SqlSugarCoreAuditLogRepository` -> 数据库。

### 1.4 日志完整性
目前实现的日志收集功能相当完善：
*   ✅ **请求上下文:** 包含租户、用户、客户端、浏览器、IP 等元数据。
*   ✅ **执行性能:** 包含执行时长 (`ExecutionDuration`)。
*   ✅ **业务数据变更:** 能精确追踪到哪个属性从什么值变为什么值。
*   ✅ **入参记录:** 记录了方法调用的参数（JSON 格式）。

## 2. 功能评价

| 维度 | 评价 | 详情 |
| :--- | :--- | :--- |
| **完整性** | ⭐⭐⭐⭐⭐ | 涵盖了从 HTTP 请求到数据库字段变更的全链路信息。 |
| **查询能力** | ⭐⭐⭐ | 依赖 SQL 查询。虽然 SqlSugar 提供了查询支持，但在海量日志下，对 `Parameters` 或 `Exceptions` 等大文本字段的模糊搜索性能会急剧下降。 |
| **性能影响** | ⭐⭐ | **存在隐患**。日志直接写入业务数据库。在高并发场景下，频繁的日志写入（尤其是包含大量实体变更时）会占用大量数据库 I/O 和连接资源，影响业务性能。 |
| **维护性** | ⭐⭐⭐⭐ | 结构清晰，分层合理（Domain/SqlSugarCore），易于理解。 |
| **可视化** | ⭐⭐ | 目前似乎依赖开发人员查询数据库或简单的后台列表，缺乏直观的仪表盘（如 Kibana/Grafana）。 |

**需要补充的功能:**
1.  **日志清理/归档策略:** 目前未见自动清理旧日志的逻辑，数据库表会无限膨胀。
2.  **脱敏处理:** 虽然参数被记录，但未见明显的敏感数据（如密码）脱敏逻辑（除非在 DTO 层处理，但在审计层统一处理更安全）。
3.  **异步缓冲:** `AuditingStore` 虽然是异步保存，但仍在当前请求线程的上下文中（尽管 ABP 可能在请求结束时处理）。如果是高吞吐系统，建议使用消息队列缓冲日志写入。

## 3. 可配置性
*   **ABP 审计配置:** 是可配置的。可以通过 `AbpAuditingOptions` 全局开启/关闭审计、配置是否记录入参、配置忽略的控制器或方法。
*   **存储配置:** 目前直接依赖 DI 注入的 `ISqlSugarDbContext`，与业务数据库耦合。若要分离数据库，需要单独配置 SqlSugar 的连接实例。

## 4. 改造分析：更换为 Serilog + VictoriaLogs

**目标:** 将审计日志的存储后端从 SqlSugar (RDBMS) 迁移到 VictoriaLogs (高性能时序/日志数据库)，并利用 Serilog 作为传输通道。

### 4.1 改造难度与步骤

**难度评估: 高 (High)**
主要难点不在于写入，而在于**读取**和**业务逻辑的兼容性**。

| 步骤 | 动作 | 难度 | 说明 |
| :--- | :--- | :--- | :--- |
| 1. 写入层改造 | 修改 `AuditingStore` | 低 | 不再调用 `AuditLogRepository.InsertAsync`，而是直接调用 `Log.Information("AuditLog: {@AuditInfo}", auditInfo)`。或者编写一个 Serilog Sink 将结构化数据发送给 VictoriaLogs。 |
| 2. 存储层适配 | 配置 VictoriaLogs | 中 | 部署 VictoriaLogs，配置 Serilog 输出（可通过 HTTP 协议发送 JSON）。 |
| 3. 查询层重构 | **重写 Repository** | **高** | 现有的 `IAuditLogRepository` 提供了 `GetListAsync`, `GetAverageExecutionDurationPerDayAsync` 等方法，这些方法被业务层/UI 调用。**VictoriaLogs 不支持 SQL，也不支持 SqlSugar**。你需要重写这些接口的实现，改为调用 VictoriaLogs 的 HTTP API (LogQL) 来获取数据，并手动映射回 `AuditLog` 实体对象。 |
| 4. UI/API 适配 | 调整前端/API | 中 | 如果 VictoriaLogs 返回的数据结构与原 SQL 查询差异较大（例如分页、聚合方式），可能需要调整上层 API。 |

### 4.2 收益分析

| 收益点 | 说明 |
| :--- | :--- |
| **性能提升** | **极高**。将高频的写操作从业务数据库剥离，显著降低业务库负载。VictoriaLogs 写入性能远超 MySQL/PG。 |
| **存储成本** | **降低**。VictoriaLogs/VictoriaMetrics 对时序和日志数据有极高的压缩率（通常比 RDBMS 节省 10-50 倍空间）。 |
| **检索速度** | **提升**。全文检索和基于标签的检索速度在海量数据下远快于 SQL `LIKE` 查询。 |
| **架构解耦** | 日志系统与业务系统解耦，日志服务宕机不影响核心业务运行。 |
| **分析能力** | 配合 Grafana，可以轻松构建 QPS、平均耗时、错误率等可视化仪表盘。 |

### 4.3 建议方案

如果决定改造，建议分两步走：

1.  **双写阶段 (过渡期):**
    *   保留现有的 SqlSugar 存储，保证现有管理后台功能可用。
    *   同时在 `AuditingStore` 中通过 Serilog 将日志投递到 VictoriaLogs。
    *   在 Grafana 中搭建面板，验证数据的价值。

2.  **完全迁移 (长期):**
    *   当管理后台的日志查询需求可以通过 Grafana 跳转或新的基于 LogQL 的查询接口满足时，移除 SqlSugar 存储。
    *   注意：如果你的系统强依赖于“在一个事务中回滚业务操作同时也回滚日志（虽然审计日志通常不回滚）”或者“在业务界面强关联查询日志”，完全去数据库化需要谨慎评估。但通常审计日志独立存储是最佳实践。

---
**结论:** 目前的审计日志模块功能完善但架构较重（存数据库）。迁移到 VictoriaLogs 是架构升级的正确方向，能极大提升性能和可观测性，但需要重写查询逻辑（Repository 层），工作量主要集中在**读取侧的兼容**。
