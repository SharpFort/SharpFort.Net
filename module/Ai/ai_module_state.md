# SharpFort.Ai 模块现状报告

> 日期：2026-05-23 | 仅描述现状，不含增强方案

---

## 1. 代码分层与引用关系

### 1.1 项目层级

模块包含 5 个项目，遵循 ABP DDD 分层架构：

```
SharpFort.Ai.Domain.Shared        ← 最底层，无项目内依赖
    ↑
SharpFort.Ai.Domain               ← 依赖 Domain.Shared
    ↑
SharpFort.Ai.Application.Contracts ← 依赖 Domain.Shared
    ↑
SharpFort.Ai.Application          ← 依赖 Application.Contracts + Domain
    ↑
SharpFort.Ai.SqlSugarCore         ← 依赖 Domain
```

### 1.2 各层外部依赖

| 项目 | 外部 NuGet / 项目引用 |
|------|----------------------|
| **Domain.Shared** | `Volo.Abp.Ddd.Domain.Shared` (10.2.0), `Volo.Abp.SettingManagement.Domain.Shared` (10.3.0) |
| **Domain** | `Volo.Abp.Ddd.Domain` (10.3.0), `Volo.Abp.Caching` (10.3.0), `Azure.AI.OpenAI` (2.2.0-beta.4), `Microsoft.Agents.AI.OpenAI` (1.3.0), `ModelContextProtocol.Core` (1.2.0), `AlipayEasySDK` (2.1.3), `Volo.Abp.DistributedLocking` (10.2.0)。项目引用：`SharpFort.Mapster`, `SharpFort.SqlSugarCore.Abstractions`, `SharpFort.Ai.Domain.Shared`, `SharpFort.Ai.Application.Contracts` |
| **Application.Contracts** | `Volo.Abp.SettingManagement.Application.Contracts` (10.3.0)。项目引用：`SharpFort.Ddd.Application.Contracts`, `SharpFort.Ai.Domain.Shared` |
| **Application** | `Volo.Abp.BackgroundJobs` (10.2.0)。项目引用：`SharpFort.Ddd.Application`, `SharpFort.Ai.Application.Contracts`, `SharpFort.Ai.Domain`, `SharpFort.CasbinRbac.Application.Contracts` |
| **SqlSugarCore** | 项目引用：`SharpFort.Mapster`, `SharpFort.SqlSugarCore`, `SharpFort.Ai.Domain` |

### 1.3 跨层引用规则

- **Domain.Shared**：被所有层引用，不引用任何其他项目
- **Domain**：引用 Domain.Shared，被 Application 和 SqlSugarCore 引用。Manager 类通过 DI 获取 `ISqlSugarRepository<T>`。Gateway 实现通过 Keyed DI 注册，路由键为 `AiModel.HandlerName`
- **Application.Contracts**：引用 Domain.Shared，被 Application 引用。定义服务接口和 DTO
- **Application**：引用 Domain 和 Application.Contracts。服务类编排 Domain Manager 和 Gateway
- **SqlSugarCore**：引用 Domain。`AiModuleDbContext` 类体为空，实体映射完全由实体上的 `[SugarTable]` 特性驱动

---

## 2. 各层文件清单与职责

### 2.1 Domain.Shared 层（62 个源文件）

**位置**：`SharpFort.Ai.Domain.Shared/`

| 类别 | 文件 | 说明 |
|------|------|------|
| 模块入口 | `SharpFortAiDomainSharedModule.cs` | ABP 模块注册 |
| 枚举 | `Enums/MessageType.cs` | Web / Api |
| 枚举 | `Enums/ModelType.cs` | Chat / Image / Embedding |
| 枚举 | `Enums/SessionType.cs` | Chat / Agent |
| 枚举 | `Enums/TaskStatusEnum.cs` | Processing / Success / Fail |
| 枚举 | `Enums/ModelApiType.cs` | Completions / Messages / Responses / GenerateContent |
| 常量 | `Consts/AiManagementConsts.cs` | 数据库表前缀 `"Ai_"` |
| 常量 | `Consts/AiHubConst.cs` | VIP 角色名 |
| 常量 | `Consts/ModelConst.cs` | 模型 ID 前缀剥离 |
| 属性 | `Attributes/SfAgentToolAttribute.cs` | Agent 工具标记 |
| 扩展 | `Extensions/EnumExtensions.cs` | 枚举 Description 读取 |
| 扩展 | `Extensions/JsonElementExtensions.cs` | JsonElement 安全遍历（30+ 方法） |
| DTO | `Dtos/TokenUsage.cs` | 简单 Token 统计 |
| DTO | `Dtos/MessageInputDto.cs` | 传入消息 DTO |
| DTO | `Dtos/AiModelDescribe.cs` | 模型路由描述符（核心） |
| DTO-OpenAI | `Dtos/OpenAi/` 下 29 个文件 | 完整 OpenAI 兼容协议请求/响应模型 |
| DTO-Anthropic | `Dtos/Anthropic/` 下 6 个文件 | Anthropic Messages 协议请求/响应模型 |
| DTO-Gemini | `Dtos/Gemini/GeminiGenerateContentAcquirer.cs` | Gemini 响应解析工具 |
| DTO-Image | `Dtos/OpenAi/Images/` 下 6 个文件 | DALL-E 图片生成请求/响应 |
| DTO-Embedding | `Dtos/OpenAi/Embeddings/` 下 3 个文件 | Embedding 请求/响应 |
| DTO-Responses | `Dtos/OpenAi/Responses/` 下 2 个文件 | OpenAI Responses API 请求/响应 |

### 2.2 Domain 层（~40 个源文件）

**位置**：`SharpFort.Ai.Domain/`

#### 实体（11 个）

| 实体 | 表名 | 基类 | 核心字段 |
|------|------|------|---------|
| `AiProvider` | `Ai_Provider` | `FullAuditedAggregateRoot<Guid>` | Name, Endpoint, ApiKey, ExtraUrl, OrderNum |
| `AiModel` | `Ai_Model` | `Entity<Guid>`, `ISoftDelete` | HandlerName, ModelId, Name, ModelType, ModelApiType, Multiplier, IsEnabled, AiProviderId |
| `Token` | `Ai_Token` | `FullAuditedAggregateRoot<Guid>` | TokenKey, UserId, ExpireTime, IsDisabled, IsEnableLog |
| `ChatSession` | `Ai_Session` | `FullAuditedAggregateRoot<Guid>` | UserId, SessionTitle, SessionContent, SessionType |
| `ChatMessage` | `Ai_Message` | `FullAuditedAggregateRoot<Guid>` | UserId, SessionId, TokenId, Content, Role, ModelId, TokenUsageValueObject, MessageType, IsHidden |
| `AiUsage` | `Ai_Usage` | `FullAuditedAggregateRoot<Guid>` | UserId, ModelId, TokenId, UsageTotalNumber, UsageInputTokenCount, UsageOutputTokenCount |
| `AiPrompt` | `Ai_Prompt` | `FullAuditedAggregateRoot<Guid>` | Code, Content, Description, DefaultModelId |
| `AiBlacklist` | `Ai_Blacklist` | `FullAuditedAggregateRoot<Guid>` | UserId, StartTime, EndTime |
| `AgentStore` | `Ai_AgentStore` | `FullAuditedAggregateRoot<Guid>` | SessionId, Store |
| `ImageStoreTaskAggregateRoot` | `Ai_ImageStoreTask` | `FullAuditedAggregateRoot<Guid>` | Prompt, ReferenceImagesPrefixBase64, StoreUrl, TaskStatus, UserId, ModelId, ErrorInfo |
| `TokenUsageValueObject` | （值对象，嵌入 ChatMessage） | — | OutputTokenCount, InputTokenCount, TotalTokenCount |

#### Gateway 接口（6 个）

| 接口 | 协议 | 主要方法 |
|------|------|---------|
| `IChatCompletionService` | OpenAI Completions | `CompleteChatStreamAsync()`, `CompleteChatAsync()` |
| `IAnthropicChatCompletionService` | Anthropic Messages | `StreamChatCompletionsAsync()`, `ChatCompletionsAsync()` |
| `IOpenAiResponseService` | OpenAI Responses | `ResponsesStreamAsync()`, `ResponsesAsync()` |
| `IGeminiGenerateContentService` | Gemini | `GenerateContentStreamAsync()`, `GenerateContentAsync()` |
| `IImageService` | DALL-E / Image | `CreateImage()`, `CreateImageEdit()`, `CreateImageVariation()` |
| `ITextEmbeddingService` | Embedding | `EmbeddingAsync()` |
| `ISpecialCompatible` | 请求预处理 | `Compatible()`, `AnthropicCompatible()` |

#### Gateway 实现（7 个）

| 实现类 | 服务商 | 实现的接口 |
|--------|--------|-----------|
| `OpenAiChatCompletionsService` | 通用 OpenAI 兼容端点 | `IChatCompletionService` |
| `AzureOpenAiChatCompletionCompletionsService` | Azure OpenAI | `IChatCompletionService` |
| `DeepSeekChatCompletionsService` | DeepSeek | `IChatCompletionService` |
| `OpenAiResponseService` | OpenAI Responses API | `IOpenAiResponseService` |
| `GeminiGenerateContentService` | Google Gemini | `IGeminiGenerateContentService` |
| `AzureOpenAIServiceImageService` | Azure DALL-E | `IImageService` |
| `SiliconFlowTextEmbeddingService` | SiliconFlow | `ITextEmbeddingService` |

#### Manager（8 个）

| Manager | 职责 |
|---------|------|
| `AiGateWayManager` | 中央调度器。路由请求到 Gateway 实现，管理 SSE 流输出（75ms 缓冲），存储消息，统计用量。约 1575 行 |
| `ChatManager` | Agent 会话管理。**所有方法被注释，未实现** |
| `ModelManager` | AiModel 查询（GetAsync / GetListAsync / GetEnabledModelsAsync） |
| `TokenManager` | Token 验证与解析（ValidateTokenAsync） |
| `AiBlacklistManager` | 黑名单检查（VerifyAiBlacklist） |
| `AiMessageManager` | 消息创建工厂（CreateSystemMessageAsync / CreateUserMessageAsync） |
| `UsageStatisticsManager` | Token 用量聚合（SetUsageAsync，分布式锁保护） |
| `MessageLogManager` | 旧版 API 原始请求审计日志。**`[Obsolete]`，已删除** |

#### 辅助类

| 文件 | 说明 |
|------|------|
| `AiGateWay/HttpClientExtensions.cs` | HttpClient 扩展方法（PostJsonAsync, HttpRequestRaw 等） |
| `AiGateWay/ThorJsonSerializer.cs` | 共享 JSON 序列化选项 |
| `AiGateWay/SupplementalMultiplierHelper.cs` | Token 倍率调整 |
| `AiGateWay/SpecialCompatible.cs` | 请求预处理管道执行器 |
| `AiGateWay/SpecialCompatibleOptions.cs` | 预处理管道配置 |
| `AiGateWay/Impl/ThorAzureOpenAI/AzureOpenAIFactory.cs` | Azure OpenAI URL 构建 + 客户端缓存 |
| `Extensions/TimeExtensions.cs` | Unix 时间戳转换 |
| `Extensions/ChatMessageExtensions.cs` | OpenAI SDK ChatMessage 角色提取（反射） |
| `AiGateWay/Exceptions/ThorRateLimitException.cs` | 上游 429 异常 |
| `AiGateWay/Exceptions/PaymentRequiredException.cs` | 上游 402 异常 |

### 2.3 Application.Contracts 层（~55 个源文件）

**位置**：`SharpFort.Ai.Application.Contracts/`

#### 服务接口（8 个）

| 接口 | 继承 | 方法 |
|------|------|------|
| `IAiModelService` | `IApplicationService` | CRUD：GetListAsync, GetAsync, CreateAsync, UpdateAsync, DeleteAsync |
| `IAiProviderService` | `IApplicationService` | CRUD |
| `IAiPromptService` | `IApplicationService` | CRUD |
| `IAiChatService` | `IApplicationService` | GetModelListAsync, UnifiedSendAsync |
| `IAiToolService` | `IApplicationService` | TranslateAsync, SummarizeAsync, SearchAsync |
| `IModelService` | 无（公共接口） | GetListAsync, GetProviderListAsync, GetModelTypeOptionsAsync, GetApiTypeOptionsAsync |
| `IUsageStatisticsService` | 无（公共接口） | GetLast7DaysTokenUsageAsync, GetModelTokenUsageAsync, GetLast24HoursTokenUsageAsync, GetTodayModelUsageAsync |
| `ISystemUsageStatisticsService` | 无（公共接口） | GetTokenStatisticsAsync |

#### DTO 目录

| 子目录 | Create / Update / List / GetList | 对应实体 |
|--------|-------------------------------|---------|
| `Dtos/AiModel/` | AiModelCreateInput, AiModelUpdateInput, AiModelDto, AiModelGetListInput | AiModel |
| `Dtos/AiProvider/` | AiProviderCreateInput, AiProviderUpdateInput, AiProviderDto, AiProviderGetListInput | AiProvider |
| `Dtos/AiPrompt/` | AiPromptCreateInput, AiPromptUpdateInput, AiPromptDto, AiPromptGetListInput | AiPrompt |
| `Dtos/ChatSession/` | ChatSessionCreateInput, ChatSessionUpdateInput, ChatSessionDto, ChatSessionGetListInput | ChatSession |
| `Dtos/ChatMessage/` | ChatMessageDto, ChatMessageGetListInput, ChatMessageDeleteInput | ChatMessage |
| `Dtos/Token/` | TokenCreateInput, TokenUpdateInput, TokenGetListOutputDto, TokenOutput, TokenSelectListOutputDto | Token |
| `Dtos/Chat/` | AgentSendInput, AgentToolOutput, AgentResultOutput, ImageGenerationInput, ImageTaskOutput, ImageMyTaskPageInput, MessageCreatedOutput | Chat / Agent / Image |
| `Dtos/UsageStatistics/` | UsageStatisticsGetInput, DailyTokenUsageDto, HourlyTokenUsageDto, ModelTokenUsageDto, ModelTokenBreakdownDto, ModelTodayUsageDto | 统计 |
| `Dtos/SystemStatistics/` | TokenStatisticsInput, TokenStatisticsOutput, ModelTokenStatisticsDto | 系统统计 |
| `Dtos/Model/` | ModelLibraryDto, ModelLibraryGetListInput, ModelApiTypeOption, ModelTypeOption | 模型库 |
| `Dtos/FileMaster/` | VerifyNextInput | 文件工具 |
| `Dtos/SendMessageInput.cs` | SendMessageInput + 嵌套 Message | 聊天请求（兼容层） |
| `Dtos/SendMessageStreamOutputDto.cs` | SendMessageStreamOutputDto + 7 个嵌套类 | 流式响应 Schema |
| `Dtos/ModelGetListOutput.cs` | ModelGetListOutput | 图片模型列表 |

### 2.4 Application 层（16 个源文件）

**位置**：`SharpFort.Ai.Application/`

| 服务 | 端点 | 状态 |
|------|------|------|
| `AiChatService` | 内部服务 | ✅ 正常。模型列表 + 黑名单检查 + 委托 AiGateWayManager |
| `AiImageService` | `POST ai-image/generate`, `GET ai-image/task/{id}`, `POST ai-image/upload-base64`, `GET/DELETE ai-image/my-tasks`, `POST ai-image/model` | ✅ 正常。图片生成全生命周期 |
| `AiModelService` | `GET/POST/PUT/DELETE ai-model` | ✅ 正常。AI 模型 CRUD |
| `AiProviderService` | `GET/POST/PUT/DELETE ai-provider` | ✅ 正常。供应商 CRUD，删除时有子模型保护 |
| `AiPromptService` | `GET/POST/PUT/DELETE ai-prompt` | ✅ 正常。Prompt 模板 CRUD，使用 Mapster |
| `TokenService` | `GET token/list`, `GET token/select-list`, `POST/PUT/DELETE token`, `POST token/{id}/enable`, `POST token/{id}/disable` | ✅ 正常。Token 全生命周期 |
| `MessageService` | 内部服务 | ✅ 正常。消息查询 + 软删除（级联隐藏） |
| `SessionService` | 内部服务（继承 `CrudAppService`） | ✅ 正常。会话 CRUD，删除时级联删除消息 |
| `FileMasterService` | `POST FileMaster/VerifyNext`, `POST FileMaster/chat/completions` | ✅ 正常。文件组织工具 |
| `UsageStatisticsService` | 内部服务 | ✅ 正常。用户用量 4 维度统计 |
| `SystemUsageStatisticsService` | `POST system-statistics/token` | ⚠️ 部分。统计正常，费用字段硬编码为 0 |
| `AiToolService` | 内部服务 | ❌ Stub。TranslateAsync / SummarizeAsync / SearchAsync 均抛 NotImplementedException |
| `TestService` | 无 | 🧪 空壳。无任何方法 |
| `ImageGenerationJob` | 后台 Job | ✅ 正常。Gemini 图片生成异步任务 |

### 2.5 SqlSugarCore 层（3 个源文件）

**位置**：`SharpFort.Ai.SqlSugarCore/`

| 文件 | 说明 |
|------|------|
| `AiModuleDbContext.cs` | `[ConnectionStringName("Ai")]`，类体为空，映射由实体 `[SugarTable]` 驱动 |
| `SharpFortAiSqlSugarCoreModule.cs` | ABP 模块注册，DependsOn Domain + Mapster + SqlSugarCore |
| 预留目录 | `DataSeeds/` 和 `Repositories/` 为空 |

---

## 3. 已实现功能清单

### 3.1 AI 网关

| 功能 | 完成度 | 说明 |
|------|--------|------|
| OpenAI Chat Completions（流式） | ✅ 100% | SSE 解析、`<think>` 标签处理、401/429 异常 |
| OpenAI Chat Completions（非流式） | ✅ 100% | 标准请求/响应 |
| OpenAI Responses API（流式） | ✅ 100% | 新协议，按 eventType 分发 |
| OpenAI Responses API（非流式） | ✅ 100% | — |
| Anthropic Messages（流式） | ✅ 100% | SSE 事件解析（message_start / content_block_delta / message_delta） |
| Anthropic Messages（非流式） | ✅ 100% | Token 用量含 cache_creation / cache_read |
| Gemini GenerateContent（流式） | ✅ 100% | `x-goog-api-key` 认证 |
| Gemini GenerateContent（非流式） | ✅ 100% | — |
| Azure OpenAI Chat | ✅ 100% | Azure SDK URL 格式，API version 2025-03-01-preview |
| DeepSeek Chat | ✅ 100% | 与 OpenAI 兼容，402 特殊处理 |
| SiliconFlow Embedding | ✅ 100% | `/v1/embeddings`，Bearer 认证 |
| 统一流式调度 | ✅ 100% | `UnifiedStreamForStatisticsAsync` 按 ModelApiType 路由到 4 个协议处理器 |
| 请求预处理管道 | ✅ 100% | ISpecialCompatible / SpecialCompatibleOptions |
| Token 用量倍率 | ✅ 100% | SupplementalMultiplier 应用于所有协议 |

### 3.2 图片生成

| 功能 | 完成度 | 说明 |
|------|--------|------|
| Gemini 图片生成 | ✅ 100% | 含参考图 base64 输入，远程上传 URL |
| 图片任务管理 | ✅ 100% | 创建 / 查询状态 / 我的任务列表 / 删除 |
| Azure DALL-E 文生图 | ✅ 100% | 通过 Azure SDK ImageClient |
| Azure DALL-E 图编辑 | ✅ 100% | multipart form POST |
| Azure DALL-E 图变体 | ❌ 0% | 方法体为 `throw NotImplementedException` |

### 3.3 用户与权限

| 功能 | 完成度 | 说明 |
|------|--------|------|
| Token 密钥管理 | ✅ 100% | 创建 / 更新 / 删除 / 启用 / 禁用 / 选择列表 |
| Token 验证 | ✅ 100% | 验证 Token 有效性（非空 / 未禁用 / 未过期） |
| 黑名单控制 | ✅ 100% | 时间范围黑名单，ChatService 调用前检查 |
| 用户配额控制 | ❌ 0% | 未实现。任何人可无限量调用 |

### 3.4 聊天与消息

| 功能 | 完成度 | 说明 |
|------|--------|------|
| 会话管理（ChatSession） | ✅ 100% | CRUD + 用户隔离 + 删除级联 |
| 消息管理（ChatMessage） | ✅ 100% | 按会话查询 + 软删除（标记 IsHidden）+ 级联隐藏 |
| 消息持久化 | ✅ 100% | ChatMessage 含 TokenUsageValueObject |
| Agent 会话 | ❌ 0% | SessionType.Agent 无对应逻辑，ChatManager 方法全部被注释 |
| 消息角色标记 | ✅ 100% | MessageType（Web/Api）区分来源 |

### 3.5 配置管理

| 功能 | 完成度 | 说明 |
|------|--------|------|
| AI 供应商管理 | ✅ 100% | CRUD，删除时检查子模型 |
| AI 模型管理 | ✅ 100% | CRUD，含路由（HandlerName）、倍率、开关 |
| Prompt 模板管理 | ✅ 100% | CRUD，Code 唯一标识 |
| 模型库（公共浏览） | ✅ 100% | 多条件筛选，按供应商/类型/API 类型 |

### 3.6 统计

| 功能 | 完成度 | 说明 |
|------|--------|------|
| Token 用量聚合 | ✅ 100% | 按 UserId × ModelId × TokenId 维度，分布式锁保护 |
| 用户用量统计 | ✅ 100% | 7 天趋势 / 模型占比 / 24 小时堆叠 / 今日卡片 |
| 系统用量统计 | ⚠️ 80% | 按日按模型统计正常，费用计算未实现（硬编码 0） |

### 3.7 AI 工具

| 功能 | 完成度 | 说明 |
|------|--------|------|
| 翻译（TranslateAsync） | ❌ 0% | `throw NotImplementedException` |
| 摘要（SummarizeAsync） | ❌ 0% | `throw NotImplementedException` |
| AI 搜索（SearchAsync） | ❌ 0% | `throw NotImplementedException` |

### 3.8 扩展功能

| 功能 | 完成度 | 说明 |
|------|--------|------|
| PGVector 向量存储 | ❌ 0% | 未实现 |
| 术语库 | ❌ 0% | 未实现 |
| 语义搜索 | ❌ 0% | 未实现 |
| 文章智能服务 | ❌ 0% | 未实现 |
| Embedding Pipeline | ❌ 0% | 未实现 |

---

## 4. 功能完成度汇总

| 类别 | 功能数 | 完成 | 部分 | 未实现 |
|------|--------|------|------|--------|
| AI 网关 | 13 | 13 | 0 | 0 |
| 图片生成 | 5 | 4 | 0 | 1 |
| 用户与权限 | 4 | 3 | 0 | 1 |
| 聊天与消息 | 5 | 4 | 0 | 1 |
| 配置管理 | 4 | 4 | 0 | 0 |
| 统计 | 3 | 2 | 1 | 0 |
| AI 工具 | 3 | 0 | 0 | 3 |
| 扩展功能 | 5 | 0 | 0 | 5 |
| **合计** | **42** | **30** | **1** | **11** |

### 总体完成度：**30 / 42 ≈ 71%**

核心网关层已完整可用，主要缺口集中在 AI 工具和扩展能力两个领域。
