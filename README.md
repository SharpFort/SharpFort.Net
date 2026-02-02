# SharpFort.Net

<h4 align="center">极致安全 · 超高性能 · 功能强大</h4>
<h5 align="center">基于 .NET 10+ / ABP Core / Casbin / SqlSugar 的 DDD 领域驱动设计后端开源框架</h5>

---

## 🚀 简介

**SharpFort.Net** 是一套旨在打造极致安全与卓越性能的现代企业级后端开源框架。它深度融合了领域驱动设计（DDD）的思想，站在巨人的肩膀上（ABP Core & SqlSugar），通过精妙的架构设计，为开发者提供一个既“坚固如堡垒”又“灵活如利剑”的开发基石。

本项目致力于打破“高性能与易用性不可兼得”的魔咒，通过深度优化的底层架构和 AI 辅助的代码开发，让每一行代码都经得起推敲。

- **后端仓库**: `sharpfort.net`
- **前端仓库**: `sharpfort-net-vue`

---

## 🛠️ 开发者必读：环境配置与规范

为了保持代码库的整洁与可追溯性，本项目启用了严格的代码提交规范和工具。

### 1. 激活 Git 提交钩子 (Husky.Net)
项目使用 **Husky.Net** 来强制执行提交规范。在您首次拉取项目或更新依赖后，**必须**执行以下操作：

```bash
# 1. 还原 .NET 工具
dotnet tool restore

# 2. 安装并激活 Husky 钩子
dotnet husky install
```

### 2. Git 提交规范 (Commit Convention)
每次 `git commit` 时，提交信息必须包含以下标签之一，否则提交将被拦截：

| 标签 | 说明 | 示例 |
| :--- | :--- | :--- |
| **feat** | 新功能 (feature) | `feat: 增加用户权限校验逻辑` |
| **fix** | 修复 bug | `fix: 修复 Redis 缓存击穿问题` |
| **docs** | 文档、注释变更 | `docs: 更新系统架构图` |
| **style** | 代码格式 (不影响逻辑，如空格、缩进) | `style: 格式化 WebModule 代码` |
| **refactor** | 代码重构 (既非新功能也非修复 bug) | `refactor: 重构文件上传服务` |
| **perf** | 性能优化 | `perf: 优化 SQL 查询索引` |
| **test** | 增加或修改测试用例 | `test: 完善领域层单元测试` |
| **chore** | 构建过程或辅助工具变动 | `chore: 更新 NuGet 包引用` |
| **revert** | 代码回退 | `revert: 回退到上一个稳定版本` |
| **build** | 打包、发布相关变更 | `build: 发布 v1.1.0 镜像` |

> **提示**：建议使用 `git commit`（不带 `-m` 参数）以触发自动模板引导，或参考 [COMMIT_CONVENTION.md](./COMMIT_CONVENTION.md) 查看详细规范。

---

## ✨ 核心特性

- **极致安全**: 引入 **Casbin** 强大的访问控制模型，支持 RBAC, ABAC 等多种权限控制策略。
- **超高性能**: 选用 **SqlSugar** 作为底层 ORM，配合 .NET 10+ 原生性能优势。
- **DDD 实战**: 严格遵循领域驱动设计规范，提供清晰的限界上下文划分。
- **ABP Core 赋能**: 采用“去肥增瘦”策略，保留模块化、事件总线、DDD 基类等核心基础设施。
- **AI 深度融合**: 核心代码经过 AI 洗礼，集成 Semantic Kernel 提供智能助手与 RAG 能力。

---

## 🛠️ 技术栈 (当前阶段)

> **注**：本节保持当前核心组件，后续将根据规划逐步平滑升级。

### 后端 (SharpFort.Net)
- **核心框架**: .NET 10+ (当前为 .NET 8)
- **底层架构**: ABP Core (Modular, EventBus, UoW)
- **ORM 引擎**: SqlSugarCore
- **权限引擎**: Casbin (.NET 实现)
- **对象映射**: Mapster (高性能编译时映射)
- **任务调度**: Quartz.Net / Hangfire
- **日志系统**: Serilog
- **缓存体系**: ABP Cache (Local & Distributed Redis)

### 前端 (sharpfort-net-vue)
- **核心框架**: Vue 3 / Vite
- **UI 组件**: Element-Plus / Pure UI
- **状态管理**: Pinia

---

## 💡 核心选型深度解析 (The "Sharp" Architecture)

为了实现 **"坚如磐石，利如锋刃"** 的架构愿景，我们对每一项关键技术都进行了深度的对比与取舍，旨在构建一个**去肥增瘦**、**极致解耦**的下一代 .NET 后端系统。

### 1. 架构重组：ABP Core (Lite) + SqlSugar
- **决策**: 摒弃 ABP Framework 中臃肿的业务实现（如复杂的 Identity、AuditLogging 默认实现），仅保留其最核心的基础设施：**模块化 (Modularity)**、**依赖注入 (DI)**、**事件总线 (EventBus)** 和 **工作单元 (UoW)**。
- **价值**: 显著降低应用启动时间和内存占用，同时引入 **SqlSugar** 作为 ORM，利用其在多表查询、批量操作和语法糖上的优势，更符合国内开发者的极致性能需求。

### 2. 身份与权限的革新：Casdoor + Casbin
- **身份认证 (AuthN)**: 引入 **Casdoor**。
    - **理由**: 将用户体系从业务系统中剥离，实现真正的 **SSO (单点登录)**。支持高并发（Go语言编写），开箱即用的 UI，且避免了 IdentityServer/OpenIddict 的高昂学习成本。
    - **策略**: SharpFort 内部仅保留最基础的用户映射，核心认证逻辑外置，配合本地降级策略保障高可用。
- **授权引擎 (AuthZ)**: 引入 **Casbin.NET**。
    - **理由**: 替代 ABP 原生的 RBAC 静态权限。Casbin 支持 **RBAC**、**ABAC**、**RESTful** 等多种模型，通过 `model.conf` 即可动态调整权限策略，支持**行级**乃至**单元格级**的数据权限控制，灵活性极高。

### 3. 下一代可观测性：VictoriaStack
- **监控**: **VictoriaMetrics** 替代 Prometheus。
    - **理由**: 更高的写入性能、更好的压缩率（节省存储成本）、且原生支持 Grafana。
- **日志**: **VictoriaLogs** 替代 Elasticsearch (ELK)。
    - **理由**: 对于不需要复杂分词搜索的日志场景，VictoriaLogs 提供了极低的资源占用和极高的查询速度。通过 **Serilog Sink** 实现异步零 IO 阻塞写入，确保业务主线程不受日志影响。

### 4. 极限性能优化细节
- **对象映射**: 选用 **Mapster**。基于编译时代码生成，性能数倍于 AutoMapper，且内存占用更低。
- **序列化**: 坚持使用 **System.Text.Json** 并开启 **Source Generator**。避免运行时的反射开销，提供亚毫秒级的序列化速度。
- **数据处理**: 选用 **MiniExcel**。采用流式读写（Stream），避免将整个文件加载到内存，彻底解决百万级数据导入导出时的 **OOM (内存溢出)** 风险。
- **单据编码**: 基于 **Redis 原子计数器 (INCR)**。在高并发场景下保证单据号（如 PO-20251010-001）绝对唯一且连续。

### 5. SQLite 并发优化 (解决 "database is locked" 问题)

> [!IMPORTANT]
> **此章节仅针对 SQLite 数据库**。PostgreSQL、SQL Server、MySQL 等数据库拥有成熟的连接池和 MVCC 并发机制，**不存在此问题**，无需任何特殊配置。

在开发/测试阶段使用 SQLite 时，可能会遇到 `SQLite Error 5: 'database is locked'` 错误。这是因为 SQLite 使用**文件锁**机制，同一进程内多个连接会相互阻塞。

**本框架的解决方案**：

1. **使用 SqlSugarScope 替代 SqlSugarClient**（在 `SqlSugarDbContextFactory.cs` 中）
   - 确保同一请求作用域内 ABP 和 Casbin 共享同一个连接上下文
   - 从根本上避免多客户端争抢文件锁

2. **启用 SQLite PRAGMA 优化**（在 `YiFrameworkSqlSugarCoreModule.cs` 中，仅对 SQLite 生效）：

| PRAGMA 配置 | 作用 | 说明 |
| :--- | :--- | :--- |
| `journal_mode = WAL` | 启用 Write-Ahead Logging | 大幅提升读写并发能力，读操作不会阻塞写操作 |
| `synchronous = NORMAL` | 同步模式设为 NORMAL | 平衡性能与安全，减少不必要的磁盘同步操作 |
| `busy_timeout = 5000` | 遇锁等待 5 秒 | **关键配置**！遇到锁时等待 5 秒再重试，而非立即抛出异常 |

> **技术细节**：
> - **WAL 模式**：SQLite 默认使用 DELETE 日志模式，写操作会阻塞读操作。WAL 模式将写操作追加到单独的日志文件，允许读写并发。
> - **synchronous=NORMAL**：默认 FULL 模式每次事务都会 fsync，性能较慢；NORMAL 在 WAL 模式下仅在 checkpoint 时同步，足够安全且性能更好。
> - **busy_timeout**：SQLite 默认遇到锁会立即返回错误，设置超时后会自动重试，大大减少并发冲突。

此外，本框架使用 **SqlSugarScope** (而非 SqlSugarClient) 作为数据库连接容器，确保同一请求作用域内共享同一个连接上下文，从根本上避免多客户端争抢锁的问题。

**对比其他数据库**：

| 数据库 | 并发模型 | 是否需要此配置 |
| :--- | :--- | :--- |
| SQLite | 文件锁，单写多读 | ✅ **需要** |
| PostgreSQL | MVCC 多版本并发 | ❌ 不需要 |
| SQL Server | 行级锁 + MVCC | ❌ 不需要 |
| MySQL (InnoDB) | 行级锁 | ❌ 不需要 |

---

## 🗺️ 功能全景与技术实现规划 (Detailed Roadmap)

我们将功能模块划分为四大象限，每个功能点都匹配了最佳的技术落地路径。

### 🛡️ 第一象限：核心基础 (Core Foundation)
*系统的骨架，继承 ABP Core 能力并进行轻量化改造。*

| 模块 | 功能细项 | 优先级 | 深度技术方案 |
| :--- | :--- | :--- | :--- |
| **身份认证** | **统一认证中心** | P0 | 集成 **Casdoor SDK**。后端仅验证 JWT 签名，剥离复杂登录逻辑。保留**超级管理员后门**以应对 SSO 宕机。 |
| | **多租户架构** | P0 | **SqlSugar 多租户方案** + Casdoor Organization 映射。支持**独立库**与**共享库**模式，利用 .NET 缓存优化租户解析。 |
| **RBAC权限** | **用户管理** | P0 | 仅存储核心映射数据，详细资料通过 API 实时/缓存从 Casdoor 拉取。 |
| | **角色与规则** | P0 | 基于 **CasbinRule** 表。抛弃传统 Role-Permission 关联表，使用 Casbin 的 P/G 策略实现动态授权。 |
| | **组织架构** | P0 | 实现 **Closure Table** (闭包表) 或 **Materialized Path** (物化路径) 算法，解决深层级部门递归查询的性能瓶颈。 |
| | **菜单/资源** | P0 | 前端路由与 API 资源的统一管理，支持生成 SPA 动态路由表。 |
| **通用配置** | **字典与参数** | P0 | **Abp.SettingManagement** + **Redis** 多级缓存（全局->租户->用户）。 |

### 🔧 第二象限：系统维护 (DevOps & Maintenance)
*体现“极致安全”与“高可观测性”的核心战场。*

| 模块 | 功能细项 | 优先级 | 深度技术方案 |
| :--- | :--- | :--- | :--- |
| **日志审计** | **操作/安全日志** | P0 | **Serilog** + **VictoriaLogs Agent**。实现全异步、零阻塞写入。记录 IP、耗时、旧值新值对比。 |
| | **异常追踪** | P0 | 全局异常过滤器捕获堆栈 + AI 智能分析（初步集成）。 |
| **系统监控** | **全栈监控** | P1 | **OpenTelemetry** 采集指标 -> **VictoriaMetrics** 存储 -> **Grafana** (嵌入式面板) 展示。监控 CPU/内存/GC/磁盘。 |
| | **在线用户** | P1 | **SignalR** (WebSocket) 实时保活 + Redis 存储会话状态。支持实时强退。 |
| **开发工具** | **代码生成器** | P1 | 基于 **Roslyn** 或 **Scriban** 模板引擎。支持 **DB First** (逆向工程)，一键生成后端 Entity/Service 及前端 Vue 代码。 |

### 🧩 第三象限：业务增强 (Business Expansion)
*解决企业级开发常见痛点，提升效率。*

| 模块 | 功能细项 | 优先级 | 深度技术方案 |
| :--- | :--- | :--- | :--- |
| **任务调度** | **分布式作业** | P1 | **Hangfire** (推荐，自带持久化与 UI) 或 Quartz.NET。支持 Cron 表达式在线管理。 |
| **消息中心** | **多渠道通知** | P1 | **Abp.Notification** + SignalR。封装统一 `ISmsSender`/`IEmailSender` 接口，适配阿里云/腾讯云。 |
| **文件服务** | **对象存储** | P1 | **Abp.BlobStoring**。支持 Local/MinIO/OSS/S3 无缝切换。实现分片上传与断点续传。 |
| **数据处理** | **导入导出** | P1 | **MiniExcel**。流式处理大数据量，内存占用极低。 |
| | **单据规则** | P2 | **Redis INCR** 原子递增。自定义规则引擎（前缀+日期+流水号）。 |

### 🔒 第四象限：高级特性 (Advanced & Security)
*SharpFort 的核心竞争力，区分于普通 Admin 项目。*

| 模块 | 功能细项 | 优先级 | 深度技术方案 |
| :--- | :--- | :--- | :--- |
| **深度安全** | **数据权限** | P0 | **Casbin ABAC** 模型 + **SqlSugar Global Filter**。实现行级过滤（如：销售只能看自己区域的数据）。 |
| | **精细化限流** | P1 | 基于 .NET 7+ **PartitionedRateLimiter**。实现 `User + Device + API` 维度的精细控制，结合 Redis Lua 脚本处理集群限流。 |
| | **敏感脱敏** | P0 | **AES-256** 加密存储。配合 EF Core/SqlSugar 的 **ValueConverter** 实现入库自动加密、出库自动脱敏。 |
| **AI 集成** | **智能助手** | P2 | **Semantic Kernel** + 本地知识库 (RAG)。实现自然语言查询数据库、生成报表。 |

---

## 📝 许可证
本项目遵循 [MIT](LICENSE) 开源协议。

---

## 🤝 贡献与反馈
欢迎提交 PR 参与建设。我们追求的不仅仅是代码，更是高性能的软件艺术。

> “坚如磐石，利如锋刃。”
