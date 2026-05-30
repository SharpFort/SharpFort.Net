# SharpFort.Ai 模块架构完整指南

> 编写日期：2026-05-23 | 基于当前代码库实际状态

---

## 目录

1. [模块概览](#1-模块概览)
2. [整体架构](#2-整体架构)
3. [Domain.Shared 层详解](#3-domainshared-层详解)
4. [Domain 层详解](#4-domain-层详解)
5. [Application.Contracts 层详解](#5-applicationcontracts-层详解)
6. [Application 层详解](#6-application-层详解)
7. [SqlSugarCore 层详解](#7-sqlsugarcore-层详解)
8. [核心数据流](#8-核心数据流)
9. [Gateway 适配器架构](#9-gateway-适配器架构)
10. [当前问题与增强方向](#10-当前问题与增强方向)

---

## 1. 模块概览

**模块定位**：SharpFort 平台的多协议 AI 网关模块。提供统一的 Chat / Image / Embedding / Agent 接口，屏蔽底层不同 AI 服务商（OpenAI、Anthropic、Gemini、DeepSeek、SiliconFlow、Azure OpenAI）的协议差异。

**核心能力**（当前已实现）：

| 能力 | 状态 | 说明 |
|------|------|------|
| 多协议 Chat 网关 | ✅ 完成 | OpenAI Completions / Responses、Anthropic Messages、Gemini GenerateContent |
| 流式 & 非流式 | ✅ 完成 | SSE 流式输出，75ms 缓冲合并，错误事件注入 |
| Token 用量统计 | ✅ 完成 | 精确到 user/model/token 维度的聚合统计 |
| Image 生成 | ⚠️ 部分 | Gemini 图片生成可用；DALL-E 变体生成未实现 |
| Text Embedding | ⚠️ 部分 | SiliconFlow 可用；缺少 pipeline |
| Agent 会话 | ❌ 未实现 | `ChatManager` 方法全部被注释，`SessionType.Agent` 无逻辑 |
| AI 工具（翻译/摘要/搜索） | ❌ Stub | `AiToolService` 全部抛 `NotImplementedException` |
| 用户配额系统 | ❌ 未实现 | 旧计划中有设计但未动工 |
| Prompt 模板管理 | ✅ 完成 | 完整 CRUD |
| Token 密钥管理 | ✅ 完成 | 完整生命周期（创建/启用/禁用/过期） |

---

## 2. 整体架构

遵循 **ABP 框架 DDD 分层架构**，共 6 个项目：

```
┌─────────────────────────────────────────────────────┐
│                  Application Layer                   │
│  SharpFort.Ai.Application.Contracts   (接口 + DTO)   │
│  SharpFort.Ai.Application             (服务实现)      │
├─────────────────────────────────────────────────────┤
│                   Domain Layer                       │
│  SharpFort.Ai.Domain.Shared  (枚举/常量/跨层DTO)      │
│  SharpFort.Ai.Domain          (实体/Manager/Gateway)  │
├─────────────────────────────────────────────────────┤
│                Infrastructure Layer                  │
│  SharpFort.Ai.SqlSugarCore   (ORM/数据库上下文)       │
└─────────────────────────────────────────────────────┘
```

**项目依赖关系**（箭头 = 依赖方向）：

```
Application ──────► Application.Contracts ──────► Domain.Shared
     │                      │
     ▼                      ▼
   Domain ─────────► Domain.Shared
     │
     ▼
 SqlSugarCore ────► Domain
```

**外部依赖**：

| 依赖包 | 用途 |
|--------|------|
| `Volo.Abp.Ddd.*` (10.2~10.3) | ABP DDD 框架基础 |
| `Azure.AI.OpenAI` (2.2.0-beta.4) | Azure OpenAI SDK（仅图片生成使用） |
| `Microsoft.Agents.AI.OpenAI` (1.3.0) | OpenAI SDK ChatMessage 类型引用 |
| `ModelContextProtocol.Core` (1.2.0) | MCP 协议支持（当前仅用于两个 [Obsolete] 工具） |
| `AlipayEasySDK` (2.1.3) | 支付宝 SDK（推测与付费/配额相关） |

---

## 3. Domain.Shared 层详解

**项目**：`SharpFort.Ai.Domain.Shared`
**定位**：跨层共享的类型定义，不包含业务逻辑。所有 DTO 和枚举都是框架无关的纯 C# 类型。

### 3.1 模块入口

| 文件 | 类型 | 说明 |
|------|------|------|
| `SharpFortAiDomainSharedModule.cs` | `AbpModule` | ABP 模块注册，依赖 `AbpDddDomainSharedModule` 和 `AbpSettingManagementDomainSharedModule` |

### 3.2 枚举（5 个）

| 文件 | 枚举 | 值 | 用途 |
|------|------|-----|------|
| `Enums/MessageType.cs` | `MessageType` | `Web=1, Api=2` | 区分消息来源 |
| `Enums/ModelType.cs` | `ModelType` | `Chat=0, Image=1, Embedding=2` | 模型能力分类 |
| `Enums/SessionType.cs` | `SessionType` | `Chat=0, Agent=1` | 会话类型 |
| `Enums/TaskStatusEnum.cs` | `TaskStatusEnum` | `Processing, Success, Fail` | 异步任务状态 |
| `Enums/ModelApiType.cs` | `ModelApiType` | `Completions, Messages, Responses, GenerateContent` | API 协议类型 |

### 3.3 常量（3 个）

| 文件 | 关键成员 | 用途 |
|------|---------|------|
| `Consts/AiManagementConsts.cs` | `DbTablePrefix = "Ai_"` | 数据库表前缀 |
| `Consts/AiHubConst.cs` | `VipRole = "SfXinAi-Vip"` | VIP 角色名 |
| `Consts/ModelConst.cs` | `RemoveModelPrefix()`, `GetModelPrefix()` | 剥离模型 ID 前缀（`yi-`, `ma-`） |

### 3.4 属性

| 文件 | 类型 | 用途 |
|------|------|------|
| `Attributes/SfAgentToolAttribute.cs` | `[AttributeUsage(Class\|Method)]` | 标记 Agent 工具，可选 `Name` 属性 |

### 3.5 扩展方法

| 文件 | 关键方法 | 用途 |
|------|---------|------|
| `Extensions/EnumExtensions.cs` | `GetDescription()` | 读取枚举 `[Description]` 值 |
| `Extensions/JsonElementExtensions.cs` | `GetPath()`, `GetString()`, `GetInt()`, `GetArray()`, `Deserialize<T>()`, `HasProperty()`, `IsNull()` 等 30+ | 安全的 `JsonElement` 深度遍历和类型转换 |

### 3.6 顶层 DTO

| 文件 | 关键属性 | 用途 |
|------|---------|------|
| `Dtos/TokenUsage.cs` | `OutputTokenCount`, `InputTokenCount`, `TotalTokenCount` | 简单 Token 统计 DTO |
| `Dtos/MessageInputDto.cs` | `Content`, `Role`, `ModelId`, `TokenUsage` | 传入消息的 DTO |
| `Dtos/AiModelDescribe.cs` | `AppId`, `Endpoint`, `ApiKey`, `ModelId`, `HandlerName`, `Multiplier`, `ModelType` | **核心路由描述符**——将 Provider + Model 的配置信息打包传递给 Gateway |

### 3.7 OpenAI 协议 DTO（`Dtos/OpenAi/`）

这是最大的一组 DTO，完整建模了 OpenAI 兼容 API 的请求/响应。所有类以 `Thor` 为前缀（项目代号）。

#### 响应基础

| 文件 | 类型 | 说明 |
|------|------|------|
| `ThorBaseResponse.cs` | `record ThorBaseResponse` | 响应基类：`ObjectTypeName`, `Error`, `Successful` |
| `ThorError.cs` | `class ThorError` | 错误模型：`Code`, `Message`, `Type`。支持单字符串和字符串数组两种错误格式 |
| `ThorUsageResponse.cs` | `record ThorUsageResponse` | Token 用量统计，含 3 个嵌套 detail 类（`InputTokensDetails`, `PromptTokensDetails`, `CompletionTokensDetails`） |

#### 消息模型

| 文件 | 类型 | 说明 |
|------|------|------|
| `ThorChatMessage.cs` | `class ThorChatMessage` | **核心消息模型**。双模式：`Content`（纯文本）或 `Contents`（多模态数组），通过 `ContentCalculated` 属性桥接 JSON 序列化。含 `ToolCalls`、`ReasoningContent`。提供静态工厂方法 `CreateSystemMessage()` 等 |
| `ThorChatMessageContent.cs` | `class ThorChatMessageContent` | 多模态内容块：`Type`（text/image_url/input_audio），`Text`，`ImageUrl`，`InputAudio` |
| `ThorVisionImageUrl.cs` | `class ThorVisionImageUrl` | 图片 URL 输入：`Url`（支持 data: URI），`Detail`（auto/low/high） |
| `ThorChatMessageAudioContent.cs` | `sealed class` | 音频输入：`Data`（base64），`Format` |
| `ThorChatMessageRoleConst.cs` | `static class` | 角色常量：`System`, `User`, `Assistant`, `Tool` |
| `ThorMessageContentTypeConst.cs` | `static class` | 内容类型常量：`Text`, `ImageUrl`, `Image` |

#### 请求模型

| 文件 | 类型 | 说明 |
|------|------|------|
| `ThorChatCompletionsRequest.cs` | `class ThorChatCompletionsRequest` | **完整 Chat Completions 请求**。含 `Messages`、`Model`、`Temperature`、`Stream`、`Tools`、`ResponseFormat`、`StopCalculated`（桥接 string/string[]）、`ToolChoiceCalculated`（桥接 string/object）、`Thinking`、`WebSearchOptions` 等。约 30+ 属性 |
| `ThorChatCompletionsResponse.cs` | `record` | 完整响应：`Id`、`Model`、`Choices`、`Usage`、`Error`。含 `SupplementalMultiplier()` 方法用于成本倍率调整 |
| `ThorChatChoiceResponse.cs` | `record` | 单个 choice：`Delta`（别名指向 `Message`）、`Message`、`FinishReason`、`FinishDetails` |

#### Function Calling / Tool 体系

| 文件 | 类型 | 说明 |
|------|------|------|
| `ThorChatMessageFunction.cs` | `class` | 函数调用：`Name`，`Arguments`（JSON string）。`ParseArguments()` 反序列化为 `Dictionary<string, object>` |
| `ThorToolCall.cs` | `class` | 工具调用：`Index`，`Id`（自动生成 Guid），`Function` |
| `ThorToolDefinition.cs` | `class` | 工具定义：`Type`，`Function`。`CreateFunctionTool()` 工厂方法 |
| `ThorToolFunctionDefinition.cs` | `class` | 函数定义：`Name`，`Description`，`Parameters`（JSON Schema） |
| `ThorToolFunctionPropertyDefinition.cs` | `class` + 嵌套 enum `FunctionObjectTypes` | JSON Schema 属性定义，支持 `object Type` 的 type-erased 模式（单类型 vs 多类型数组）。`DefineString()` 等链式工厂方法 |
| `ThorToolChoice.cs` | `class` | 工具选择控制：`Type`（none/auto/required），`Function`。`GetNone()` 等工厂方法 |
| `ThorToolChoiceFunctionTool.cs` | `class` | 强制指定函数：`Name` |
| `ThorToolTypeConst.cs` | `static class` | 常量：`Function` |
| `ThorToolChoiceTypeConst.cs` | `static class` | 常量：`Function`, `Auto`, `None`, `Required`（**注意**：`Required` 值有尾部空格 bug `"required "`） |

#### 高级特性

| 文件 | 类型 | 说明 |
|------|------|------|
| `ThorResponseFormat.cs` | `class` | JSON 模式 / Structured Output 配置 |
| `ThorResponseJsonSchema.cs` | `class` | JSON Schema 定义（`Name`, `Strict`, `Schema`） |
| `ThorChatAudioRequest.cs` | `sealed class` | 音频输出配置（`Voice`, `Format`） |
| `ThorChatClaudeThinking.cs` | `class` | Claude 式扩展思考配置（`Type`, `BudgetToken`） |
| `ThorChatWebSearchOptions.cs` | `class` + 嵌套 `ThorUserLocation` + `ThorUserLocationApproximate` | Web 搜索位置配置 |
| `ThorStreamOptions.cs` | `class` | 流选项（`IncludeUsage`） |
| `OpenAIConstant.cs` | `static class` | 协议常量：`Done = "[DONE]"`, `Data = "data:"`, `ThinkStart = "<think>"`, `ThinkEnd = "</think>"` |
| `ModelsListDto.cs` | `ModelsListDto` + `ModelsDataDto` | `/v1/models` 响应 |

#### Image DTO

| 文件 | 类型 | 说明 |
|------|------|------|
| `SharedImageRequestBaseModel.cs` | `record` | 图片请求基类：`N`, `Size`, `ResponseFormat`, `Model` |
| `ImageCreateRequest.cs` | `record` | 文生图请求：`Prompt`, `Quality`, `Style` |
| `ImageEditCreateRequest.cs` | `record` | 图编辑请求：`Image`（bytes）, `Mask`, `Prompt` |
| `ImageVariationCreateRequest.cs` | `record` | 图变体请求：`Image`（bytes） |
| `ImageCreateResponse.cs` | `record` + `ImageDataResult` | 图片响应：`Results`（Url/B64），`Usage` |

#### Embedding DTO

| 文件 | 类型 | 说明 |
|------|------|------|
| `ThorEmbeddingInput.cs` | `sealed class` | Embedding 代理输入 |
| `EmbeddingCreateRequest.cs` | `record` | Embedding 请求：`InputCalculated`（桥接 string/string[]），`Model`，`Dimensions`。`Validate()` 未实现 |
| `EmbeddingCreateResponse.cs` | `record` + `EmbeddingResponse` | Embedding 响应：`Data`（float[] 或 base64 string），含 `ConvertEmbeddingData()` 格式转换 |

#### Responses API DTO

| 文件 | 类型 | 说明 |
|------|------|------|
| `OpenAiResponsesInput.cs` | `class` | OpenAI Responses API 请求（新协议），使用 `JsonElement` 处理动态字段 |
| `OpenAiResponsesOutput.cs` | `class` + 3 嵌套类 | Responses API 响应，含 `SupplementalMultiplier()` |

### 3.8 Anthropic 协议 DTO（`Dtos/Anthropic/`）

| 文件 | 类型 | 说明 |
|------|------|------|
| `ThorJsonSerializer.cs` | `static class` | Anthropic 专用 JSON 序列化选项（`CamelCase` + `WriteNull` 忽略 + `UnsafeRelaxedJsonEscaping`） |
| `AnthropicCacheControl.cs` | `sealed class` | Prompt Cache 控制标记（`Type = "ephemeral"`） |
| `AnthropicInput.cs` | `sealed class` + 3 嵌套类 | **完整 Anthropic Messages API 请求**：`Model`、`Messages`、`Tools`、`SystemCalculated`（桥接 string/Content[]）、`Thinking`、`Temperature` |
| `AnthropicMessageInput.cs` | `class` | 单轮消息：`Role`，`ContentCalculated`（桥接 string/Content[]） |
| `AnthropicMessageContent.cs` | `class` + 嵌套 `AnthropicMessageContentSource` | 内容块：支持 `text`、`tool_use`、`thinking`、`image`（base64 source）。含 `CacheControl` |
| `AnthropicChatCompletionDto.cs` | 8 个类 | **完整 Anthropic 响应体系**：`AnthropicStreamDto`（SSE 事件包装）、`AnthropicChatCompletionDtoDelta`（增量更新）、`AnthropicChatCompletionDtoContentBlock`（内容块）、`AnthropicChatCompletionDto`（非流式完整响应）、`AnthropicCompletionDtoUsage`（用量统计）、`AnthropicServerToolUse`（服务端工具用量） |

### 3.9 Gemini 协议 DTO（`Dtos/Gemini/`）

| 文件 | 类型 | 说明 |
|------|------|------|
| `GeminiGenerateContentAcquirer.cs` | `static class` | Gemini JSON 提取工具集：`GetLastUserContent()`（提取最后一条用户消息）、`GetTextContent()`（提取非思考文本）、`GetUsage()`（构建 `ThorUsageResponse`）、`GetImagePrefixBase64()`（递归查找 base64 图片）。使用递归 JSON 遍历和启发式 base64 检测 |

---

## 4. Domain 层详解

**项目**：`SharpFort.Ai.Domain`
**定位**：核心业务逻辑层。包含实体定义、Gateway 接口与实现、Domain Manager、MCP 工具。

### 4.1 模块入口

| 文件 | 类型 | 说明 |
|------|------|------|
| `SharpFortAiDomainModule.cs` | `AbpModule` | 依赖 `SharpFortAiDomainSharedModule`、`SharpFortMapsterModule`、`AbpDddDomainModule`、`AbpCachingModule` |

### 4.2 实体（11 个）

#### 核心业务实体

| 文件 | 表名 | 基类 | 关键属性 | 说明 |
|------|------|------|---------|------|
| `Entities/AiProvider.cs` | `Ai_Provider` | `FullAuditedAggregateRoot<Guid>`, `IOrderNum` | `Name`（供应商名）、`Endpoint`（API 地址）、`ApiKey`、`ExtraUrl`、`OrderNum`。`[Navigate] List<AiModel>` | AI 服务供应商配置 |
| `Entities/AiModel.cs` | `Ai_Model` | `Entity<Guid>`, `IOrderNum`, `ISoftDelete` | `HandlerName`（路由到哪个 Gateway 实现）、`ModelId`（如 gpt-4）、`Name`、`Multiplier`（成本倍率）、`ModelType`、`ModelApiType`、`IsEnabled`、`AiProviderId`（FK） | AI 模型定义，含成本倍率和路由信息 |
| `Entities/Token.cs` | `Ai_Token` | `FullAuditedAggregateRoot<Guid>` | `TokenKey`（"yi-" 前缀 36 位）、`UserId`、`ExpireTime`（null=永不过期）、`IsDisabled`、`IsEnableLog`。`IsAvailable()` 方法 | 用户 API 密钥 |
| `Entities/ChatSession.cs` | `Ai_Session` | `FullAuditedAggregateRoot<Guid>` | `UserId`（索引）、`SessionTitle`、`SessionContent`、`SessionType`（Chat/Agent） | 聊天会话容器 |
| `Entities/ChatMessage.cs` | `Ai_Message` | `FullAuditedAggregateRoot<Guid>` | `UserId`、`SessionId`（复合索引）、`TokenId`、`Content`、`Role`、`ModelId`、`TokenUsageValueObject`、`MessageType`、`IsHidden` | 单条聊天消息，含 Token 用量 |
| `Entities/AiUsage.cs` | `Ai_Usage` | `FullAuditedAggregateRoot<Guid>` | `UserId`、`ModelId`、`TokenId`（复合索引）、`UsageTotalNumber`、`UsageOutputTokenCount`、`UsageInputTokenCount`。`AddOnceChat()` | 用户维度的 Token 聚合统计 |
| `Entities/AiPrompt.cs` | `Ai_Prompt` | `FullAuditedAggregateRoot<Guid>` | `Code`（唯一标识）、`Content`（模板文本）、`Description`、`DefaultModelId` | Prompt 模板 |
| `Entities/AiBlacklist.cs` | `Ai_Blacklist` | `FullAuditedAggregateRoot<Guid>` | `UserId`、`StartTime`、`EndTime` | 时间范围内的用户黑名单 |

#### 辅助实体

| 文件 | 表名 | 基类 | 关键属性 | 说明 |
|------|------|------|---------|------|
| `Entities/AgentStore.cs` | `Ai_AgentStore` | `FullAuditedAggregateRoot<Guid>` | `SessionId`（索引）、`Store`（big string -- 序列化的 Agent 状态） | Agent 会话状态持久化 |
| `Entities/ImageStoreTaskAggregateRoot.cs` | `Ai_ImageStoreTask` | `FullAuditedAggregateRoot<Guid>` | `Prompt`、`ReferenceImagesPrefixBase64`（JSON）、`StoreUrl`、`TaskStatus`、`UserId`、`ModelId`、`ErrorInfo`。`SetSuccess(url)` | 图片生成任务记录 |
| `Entities/MessageLogAggregateRoot.cs` | `Ai_Message_Log` | `Entity<Guid>` | ⚠️ **`[Obsolete]`**。`RequestBody`、`ApiKey`、`ModelId`、`ApiType` | 旧版 API 原始请求审计日志 |

#### 值对象

| 文件 | 类型 | 属性 |
|------|------|------|
| `Entities/ValueObjects/TokenUsageValueObject.cs` | `TokenUsageValueObject` | `OutputTokenCount`, `InputTokenCount`, `TotalTokenCount` |

### 4.3 Gateway 接口层（`AiGateWay/`）

6 个协议接口，定义了与各 AI 服务商通信的契约：

| 接口文件 | 主要方法 | 协议 |
|---------|---------|------|
| `IChatCompletionService.cs` | `CompleteChatStreamAsync()` → `IAsyncEnumerable<ThorChatCompletionsResponse>`, `CompleteChatAsync()` → `Task<ThorChatCompletionsResponse>` | OpenAI Completions |
| `IAnthropicChatCompletionService.cs` | `StreamChatCompletionsAsync()` → `IAsyncEnumerable<(string, AnthropicStreamDto?)>`, `ChatCompletionsAsync()` → `Task<AnthropicChatCompletionDto>` | Anthropic Messages |
| `IOpenAiResponseService.cs` | `ResponsesStreamAsync()` → `IAsyncEnumerable<(string, JsonElement?)>`, `ResponsesAsync()` → `Task<OpenAiResponsesOutput>` | OpenAI Responses（新版） |
| `IGeminiGenerateContentService.cs` | `GenerateContentStreamAsync()` → `IAsyncEnumerable<JsonElement?>`, `GenerateContentAsync()` → `Task<JsonElement>` | Gemini GenerateContent |
| `IImageService.cs` | `CreateImage()`, `CreateImageEdit()`, `CreateImageVariation()` → `Task<ImageCreateResponse>` | DALL-E / Image |
| `ITextEmbeddingService.cs` | `EmbeddingAsync()` → `Task<EmbeddingCreateResponse>` | Embedding |

**特殊兼容接口**：

| 文件 | 说明 |
|------|------|
| `ISpecialCompatible.cs` | 请求预处理管道接口：`Compatible(ThorChatCompletionsRequest)`, `AnthropicCompatible(AnthropicInput)` |
| `SpecialCompatible.cs` | 实现类，从 `IOptions<SpecialCompatibleOptions>` 读取 Actions 管道并依次执行 |
| `SpecialCompatibleOptions.cs` | 配置类：`Handles(List<Action>)`, `AnthropicHandles(List<Action>)` |

### 4.4 Gateway 实现层（`AiGateWay/Impl/`）

7 个具体实现，按服务商组织：

| 实现 | 目录 | 实现的接口 | 说明 |
|------|------|-----------|------|
| `OpenAiChatCompletionsService` | `ThorCustomOpenAI/Chats/` | `IChatCompletionService` | 通用 OpenAI 兼容端点。处理 SSE 流解析、`<think>` 标签识别（移动到 `ReasoningContent`）、429→`ThorRateLimitException`、401→`UnauthorizedAccessException` |
| `OpenAiResponseService` | `ThorCustomOpenAI/Chats/` | `IOpenAiResponseService` | OpenAI Responses API（新协议）。流式返回 `(eventType, JsonElement?)` 元组 |
| `AzureOpenAiChatCompletionCompletionsService` | `ThorAzureOpenAI/Chats/` | `IChatCompletionService` | Azure OpenAI。通过 `AzureOpenAIFactory` 构建 URL（`/openai/deployments/{model}/...?api-version=2025-03-01-preview`），SSE 流解析 |
| `AzureOpenAIServiceImageService` | `ThorAzureOpenAI/Images/` | `IImageService` | Azure DALL-E。`CreateImage()` 通过 SDK `ImageClient.GenerateImageAsync()`；`CreateImageEdit()` 通过 multipart form POST；`CreateImageVariation()` → `NotImplementedException` |
| `DeepSeekChatCompletionsService` | `ThorDeepSeek/Chats/` | `IChatCompletionService` | DeepSeek。与 OpenAI 兼容但默认端点为 `https://api.deepseek.com/v1`，402 → `PaymentRequiredException` |
| `GeminiGenerateContentService` | `ThorGemini/Chats/` | `IGeminiGenerateContentService` | Google Gemini。使用 `x-goog-api-key` header，端点格式 `{base}/v1beta/models/{model}:streamGenerateContent?alt=sse` |
| `SiliconFlowTextEmbeddingService` | `ThorSiliconFlow/Embeddings/` | `ITextEmbeddingService` | SiliconFlow Embedding。标准 `/v1/embeddings` 端点 |

#### 辅助类

| 文件 | 说明 |
|------|------|
| `HttpClientFactory.cs` | ⚠️ `[Obsolete]`。旧版 HTTP 客户端池（`ConcurrentDictionary` + `Lazy<List<HttpClient>>`），环境变量 `HttpClientPoolSize` 控制池大小 |
| `HttpClientExtensions.cs` | HttpClient 扩展方法：`PostJsonAsync<T>()`、`HttpRequestRaw()`、`PostFileAndReadAsAsync<T>()`、`DeleteAndReadAsAsync<T>()`。使用 `ThorJsonSerializer.DefaultOptions` |
| `ThorJsonSerializer.cs` | Domain 层共享的 JSON 序列化选项 |
| `SupplementalMultiplierHelper.cs` | `SetSupplementalMultiplier()` 扩展方法——将 `ThorUsageResponse` 所有 Token 字段乘以倍率 |
| `AzureOpenAIFactory.cs` | Azure OpenAI URL 构建 + `AzureOpenAIClient` 缓存 |

#### 异常

| 文件 | 说明 |
|------|------|
| `Exceptions/ThorRateLimitException.cs` | AI 服务返回 429 时抛出 |
| `Exceptions/PaymentRequiredException.cs` | DeepSeek 返回 402 时抛出 |

### 4.5 Domain Manager（8 个）

| 文件 | 基类 | 核心方法 | 说明 |
|------|------|---------|------|
| `Managers/AiGateWayManager.cs` | `DomainService` | **约 1575 行，核心调度器**。`GetModelAsync()`（解析 HandlerName → ModelDescribe）、`CompleteChatForStatisticsAsync()`、`CompleteChatStreamForStatisticsAsync()`（75ms 缓冲 SSE）、`CreateImageForStatisticsAsync()`、`EmbeddingForStatisticsAsync()`、`AnthropicCompleteChatStreamForStatisticsAsync()`、`GeminiGenerateContentImageForStatisticsAsync()`、**`UnifiedStreamForStatisticsAsync()`**（统一流式调度入口，按 ModelApiType 分发到 4 个私有处理器） | **模块中枢**——路由请求到正确的 Gateway 实现，管理流输出缓冲（`ConcurrentQueue<string>` + 75ms 定时器），存储聊天消息，统计用量 |
| `Managers/ModelManager.cs` | `DomainService` | 构造函数注入 `ISqlSugarRepository<AiModel>`，**目前无任何方法** | 空壳，待实现 |
| `Managers/AiBlacklistManager.cs` | `DomainService` | `VerifyAiBlacklist(Guid userId)` → 检查时间范围内的黑名单记录 | 黑名单访问控制 |
| `Managers/AiMessageManager.cs` | `DomainService` | `CreateSystemMessageAsync()`, `CreateUserMessageAsync()` | 消息创建工厂，确保角色正确 |
| `Managers/UsageStatisticsManager.cs` | `DomainService` | `SetUsageAsync(userId, modelId, tokenUsage, tokenId)` → 分布式锁保护的 upsert 聚合 | Token 用量统计聚合 |
| `Managers/ChatManager.cs` | `DomainService` | **所有方法全部被注释**（`AgentCompleteChatStreamAsync`, `GetTools`, `SendHttpStreamMessageAsync`）。注释说明："placeholder, waiting for Phase 3 to refactor with native OpenAI" | Agent 会话管理（未实现） |
| `Managers/MessageLogManager.cs` | `DomainService` | ⚠️ **`[Obsolete]`**。`CreateAsync(requestBody, apiKey, ...)` | 旧版日志记录 |
| `Managers/TokenManager.cs` | `DomainService` | `ValidateTokenAsync(object tokenOrId)` → `TokenValidationResult`（含 UserId, TokenId, Token, TokenName, IsEnableLog） | Token 验证和解析 |

### 4.6 MCP 工具（2 个）

| 文件 | 状态 | 说明 |
|------|------|------|
| `Mcp/HttpRequestTool.cs` | ⚠️ `[Obsolete]` | Agent HTTP 请求工具。`HttpRequest(url, method, body, headers)` |
| `Mcp/DateTimeTool.cs` | ⚠️ `[Obsolete]` | Agent 日期时间工具。`DateTime()` → `DateTime.Now` |

### 4.7 扩展

| 文件 | 说明 |
|------|------|
| `Extensions/TimeExtensions.cs` | Unix 时间戳转换：`ToUnixTimeSeconds()`, `ToUnixTimeMilliseconds()`, `FromUnixTimeSeconds()` |
| `Extensions/ChatMessageExtensions.cs` | OpenAI SDK `ChatMessage` 角色提取：通过反射读取私有 `Role` 属性 |

---

## 5. Application.Contracts 层详解

**项目**：`SharpFort.Ai.Application.Contracts`
**定位**：应用层接口和 DTO 定义。对外（前端/API 消费者）暴露的契约。

### 5.1 服务接口（8 个）

#### 管理类接口（继承 `IApplicationService`，需授权）

| 接口 | 方法 | 说明 |
|------|------|------|
| `IAiModelService` | CRUD：`GetListAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` | AI 模型配置管理 |
| `IAiProviderService` | CRUD：同上 | AI 供应商配置管理 |
| `IAiPromptService` | CRUD：同上 | Prompt 模板管理 |
| `IAiChatService` | `GetModelListAsync()` → `List<AiModelDto>`, `UnifiedSendAsync(ModelApiType, JsonElement, string, Guid?)` | 聊天核心——模型列表 + 统一流式发送 |
| `IAiToolService` | `TranslateAsync()`, `SummarizeAsync()`, `SearchAsync()` → `string` | AI 工具（翻译/摘要/AI 搜索）——⚠️ **全部未实现** |

#### 公共/匿名接口（不继承 `IApplicationService`）

| 接口 | 方法 | 说明 |
|------|------|------|
| `IModelService` | `GetListAsync()`, `GetProviderListAsync()`, `GetModelTypeOptionsAsync()`, `GetApiTypeOptionsAsync()` | 公共模型库浏览 + 下拉选项 |
| `IUsageStatisticsService` | `GetLast7DaysTokenUsageAsync()`, `GetModelTokenUsageAsync()`, `GetLast24HoursTokenUsageAsync()`, `GetTodayModelUsageAsync()` | 当前用户用量统计（4 个维度） |
| `ISystemUsageStatisticsService` | `GetTokenStatisticsAsync(TokenStatisticsInput)` → `TokenStatisticsOutput` | 系统级 Token 统计（按日/按模型） |

### 5.2 DTO 分类

#### 管理类 DTO（按实体组织在子目录中）

| 子目录 | Create DTO | Update DTO | List DTO | GetList Input |
|--------|-----------|-----------|----------|---------------|
| `Dtos/AiModel/` | `AiModelCreateInput`（11 字段） | `AiModelUpdateInput`（继承 Create） | `AiModelDto`（继承 `FullAuditedEntityDto<Guid>`） | `AiModelGetListInput`（SearchKey, AiProviderId） |
| `Dtos/AiProvider/` | `AiProviderCreateInput`（5 字段） | `AiProviderUpdateInput`（继承 Create） | `AiProviderDto` | `AiProviderGetListInput`（SearchKey） |
| `Dtos/AiPrompt/` | `AiPromptCreateInput`（3 字段） | `AiPromptUpdateInput`（继承 Create） | `AiPromptDto` | `AiPromptGetListInput`（SearchKey） |
| `Dtos/ChatSession/` | `ChatSessionCreateInput` | `ChatSessionUpdateInput` | `ChatSessionDto` | `ChatSessionGetListInput`（Title, Type） |
| `Dtos/ChatMessage/` | — | — | `ChatMessageDto` | `ChatMessageGetListInput`（SessionId 必填） |
| `Dtos/Token/` | `TokenCreateInput` | `TokenUpdateInput` | `TokenGetListOutputDto`、`TokenOutput`、`TokenSelectListOutputDto` | — |

#### Chat 相关 DTO

| 文件 | 关键类 | 用途 |
|------|--------|------|
| `Dtos/Chat/AgentSendInput.cs` | `AgentSendInput` | Agent 发送消息输入（含 `Tools` 工具列表） |
| `Dtos/Chat/AgentToolOutput.cs` | `AgentToolOutput` | 工具列表输出（Code + Name） |
| `Dtos/Chat/AgentResultOutput.cs` | `AgentResultOutput` + `AgentResultTypeEnum`（Text/ToolCalling/ToolCalled/Usage/ToolCallUsage） | Agent 流式结果输出，`Type` 驱动前端渲染 |
| `Dtos/Chat/ImageGenerationInput.cs` | `ImageGenerationInput` | 图片生成输入（Prompt + 参考图 base64） |
| `Dtos/Chat/ImageTaskOutput.cs` | `ImageTaskOutput` | 图片任务状态输出 |
| `Dtos/Chat/ImageMyTaskPageInput.cs` | `ImageMyTaskPageInput` | 我的图片任务分页查询 |
| `Dtos/Chat/MessageCreatedOutput.cs` | `MessageCreatedOutput` + `ChatMessageTypeEnum`（UserMessage/SystemMessage） | 消息创建成功事件输出 |

#### 用量统计 DTO

| 文件 | 关键类 | 用途 |
|------|--------|------|
| `Dtos/UsageStatistics/DailyTokenUsageDto.cs` | `DailyTokenUsageDto` | 7 天趋势图数据点 |
| `Dtos/UsageStatistics/HourlyTokenUsageDto.cs` | `HourlyTokenUsageDto` + `ModelTokenBreakdownDto` | 24 小时堆叠柱状图 |
| `Dtos/UsageStatistics/ModelTokenUsageDto.cs` | `ModelTokenUsageDto`（Model, Tokens, Percentage） | 模型用量饼图 |
| `Dtos/UsageStatistics/ModelTodayUsageDto.cs` | `ModelTodayUsageDto`（ModelId, UsageCount, TotalTokens, IconUrl） | 今日模型用量卡片 |
| `Dtos/UsageStatistics/UsageStatisticsGetInput.cs` | `UsageStatisticsGetInput`（TokenId 可选过滤） | 用量查询参数 |

#### 系统统计 DTO

| 文件 | 关键类 | 用途 |
|------|--------|------|
| `Dtos/SystemStatistics/ModelTokenStatisticsDto.cs` | `ModelTokenStatisticsDto` | 单模型 Token + 费用统计 |
| `Dtos/SystemStatistics/TokenStatisticsInput.cs` | `TokenStatisticsInput`（Date） | 按日查询输入 |
| `Dtos/SystemStatistics/TokenStatisticsOutput.cs` | `TokenStatisticsOutput`（Date + ModelStatistics 列表） | 系统统计输出 |

#### 模型库 DTO

| 文件 | 关键类 | 用途 |
|------|--------|------|
| `Dtos/Model/ModelLibraryDto.cs` | `ModelLibraryDto` + 嵌套 `ModelApiTypeOutput` | 公共模型库展示（含 ModelTypeName, ModelApiTypeName 计算属性） |
| `Dtos/Model/ModelLibraryGetListInput.cs` | `ModelLibraryGetListInput`（多条件筛选） | 模型库查询 |
| `Dtos/Model/ModelApiTypeOption.cs` | `ModelApiTypeOption` | 下拉选项 |
| `Dtos/Model/ModelTypeOption.cs` | `ModelTypeOption` | 下拉选项 |

#### FileMaster DTO

| 文件 | 关键类 | 用途 |
|------|--------|------|
| `Dtos/FileMaster/VerifyNextInput.cs` | `VerifyNextInput`（FileCount, DirectoryCount） | 文件组织工具配额验证 |

#### ⚠️ 顶层 DTO（与子目录版本并存的老版本）

| 文件 | 关键类 | 对应新版 | 说明 |
|------|--------|---------|------|
| `Dtos/MessageDto.cs` | `MessageDto` | `ChatMessageDto` | 旧版消息 DTO（无 `MessageType` 字段） |
| `Dtos/MessageGetListInput.cs` | `MessageGetListInput`, `MessageDeleteInput` | `ChatMessageGetListInput`, `ChatMessageDeleteInput` | 旧版列表/删除输入 |
| `Dtos/SendMessageInput.cs` | `SendMessageInput` + 嵌套 `Message` | — | 简单聊天完成请求 |
| `Dtos/SendMessageStreamOutputDto.cs` | `SendMessageStreamOutputDto` + 7 个嵌套类 | — | OpenAI 兼容流式响应 Schema（含内容过滤） |
| `Dtos/SessionDto.cs` | `SessionDto` | `ChatSessionDto` | 旧版会话 DTO |
| `Dtos/SessionGetListInput.cs` | `SessionGetListInput` | `ChatSessionGetListInput` | 旧版查询输入 |
| `Dtos/SessionCreateAndUpdateInput.cs` | `SessionCreateAndUpdateInput` | `ChatSessionCreateInput` | 旧版创建/更新 |
| `Dtos/ModelGetListOutput.cs` | `ModelGetListOutput` | — | 图片模型列表输出 |

---

## 6. Application 层详解

**项目**：`SharpFort.Ai.Application`
**定位**：应用服务实现。处理 HTTP 请求，进行授权验证，编排 Domain 层服务。

### 6.1 应用服务状态总览

| 服务 | 状态 | HTTP 端点 | 说明 |
|------|------|-----------|------|
| `AiChatService` | ✅ 实现 | —（内部服务） | 聊天核心——模型列表 + 黑名单检查 + 委托 `AiGateWayManager.UnifiedStreamForStatisticsAsync()` |
| `AiImageService` | ✅ 实现 | `POST ai-image/generate`, `GET ai-image/task/{id}`, `POST ai-image/upload-base64`, `GET ai-image/my-tasks`, `DELETE ai-image/my-tasks`, `POST ai-image/model` | 图片生成完整生命周期 |
| `AiModelService` | ✅ 实现 | `GET/POST/PUT/DELETE ai-model` | AI 模型 CRUD |
| `AiProviderService` | ✅ 实现 | `GET/POST/PUT/DELETE ai-provider` | 供应商 CRUD（删除时有子模型保护） |
| `AiPromptService` | ✅ 实现 | `GET/POST/PUT/DELETE ai-prompt` | Prompt 模板 CRUD（使用 Mapster 映射）。⚠️ **未实现 `IAiPromptService` 接口** |
| `TokenService` | ✅ 实现 | `GET token/list`, `GET token/select-list`, `POST token`, `PUT token`, `DELETE token/{id}`, `POST token/{id}/enable`, `POST token/{id}/disable` | Token 全生命周期管理 |
| `MessageService` | ✅ 实现 | — | 消息查询 + 软删除（标记 `IsHidden`；`IsDeleteSubsequent` 级联隐藏） |
| `SessionService` | ✅ 实现 | — | 会话 CRUD（继承 `CrudAppService`；删除时级联删除消息） |
| `FileMasterService` | ✅ 实现 | `POST FileMaster/VerifyNext`, `POST FileMaster/chat/completions` | 文件组织工具（用量限制 + Chat 代理） |
| `UsageStatisticsService` | ✅ 实现 | — | 用户用量统计（4 个维度，内存分组计算） |
| `SystemUsageStatisticsService` | ✅ 实现 | `POST system-statistics/token` | 系统级按日统计（费用字段硬编码为 0） |
| `AiToolService` | ❌ Stub | —（实现 `IAiToolService`） | 三个方法全部 `throw NotImplementedException` |
| `ChatManager` (Domain) | ❌ Stub | — | Agent 方法全部被注释 |
| `AiAccountService` | ⚠️ Obsolete | `GET account/ai` | 简单转发到 CasbinRbac `IAccountService` |
| `TestService` | 🧪 空壳 | — | 无任何方法，仅用于示例扩展 |

### 6.2 后台 Job

| 文件 | 说明 |
|------|------|
| `Jobs/ImageGenerationJobArgs.cs` | Job 参数：`TaskId` (Guid) |
| `Jobs/ImageGenerationJob.cs` | `AsyncBackgroundJob<ImageGenerationJobArgs>`：加载任务 → 构建 Gemini API 请求 → 调用 `AiGateWayManager.GeminiGenerateContentImageForStatisticsAsync()` → 异常时设置任务为 Fail |

---

## 7. SqlSugarCore 层详解

**项目**：`SharpFort.Ai.SqlSugarCore`
**定位**：数据访问层，极简。仅 3 个文件。

| 文件 | 说明 |
|------|------|
| `AiModuleDbContext.cs` | 继承 `SqlSugarDbContext`，特性 `[ConnectionStringName("Ai")]`。**类体为空**——实体映射完全由 `[SugarTable]` 特性驱动 |
| `SharpFortAiSqlSugarCoreModule.cs` | ABP 模块：`DependsOn` → `SharpFortAiDomainModule`、`SharpFortMapsterModule`、`SharpFortSqlSugarCoreModule`。`ConfigureServices` 中注册 `AiModuleDbContext` |
| `.csproj` | 项目引用 `SharpFort.Mapster`、`SharpFort.SqlSugarCore`、`SharpFort.Ai.Domain`。预留了 `DataSeeds/` 和 `Repositories/` 文件夹（当前为空） |

---

## 8. 核心数据流

### 8.1 Chat 流式请求完整路径

```
前端 POST /api/ai/chat
    │
    ▼
AiChatService.UnifiedSendAsync()
    │ 黑名单检查（非免费模型）
    │ 提取 modelId
    ▼
AiGateWayManager.UnifiedStreamForStatisticsAsync()
    │ 创建 ConcurrentQueue<string> + 75ms 定时器
    │ 根据 ModelApiType 路由到 4 个私有处理器之一
    │
    ├── Completions → ProcessCompletionsStreamAsync()
    │       └── IChatCompletionService (按 HandlerName 做 Keyed DI)
    │           ├── OpenAiChatCompletionsService (通用 OpenAI)
    │           ├── AzureOpenAiChatCompletionCompletionsService (Azure)
    │           └── DeepSeekChatCompletionsService (DeepSeek)
    │
    ├── Messages → ProcessAnthropicStreamAsync()
    │       └── IAnthropicChatCompletionService
    │           └── (通过 HttpClient 直接调用 Anthropic API)
    │
    ├── Responses → ProcessOpenAiResponsesStreamAsync()
    │       └── IOpenAiResponseService
    │           └── OpenAiResponseService
    │
    └── GenerateContent → ProcessGeminiStreamAsync()
            └── IGeminiGenerateContentService
                └── GeminiGenerateContentService

    每个处理器：
    1. SpecialCompatible 预处理请求
    2. 调用 Gateway Service 获取流
    3. 提取 system content 和 user content
    4. 累积 Token 用量
    5. 存储 ChatMessage（system + user）
    6. 调用 UsageStatisticsManager.SetUsageAsync()

    输出：
    ▼
SSE 流（data: {...}\n\n） → 前端
    ├── MessageCreatedOutput（消息已创建事件）
    ├── ThorChatCompletionsResponse（内容增量）
    └── 错误事件（异常信息包装为 SSE data）
```

### 8.2 Image 生成路径

```
前端 POST /api/ai-image/generate
    │
    ▼
AiImageService.GenerateAsync()
    │ 黑名单检查
    │ Token 验证
    │ 创建 ImageStoreTaskAggregateRoot（TaskStatus = Processing）
    │ 入队 ImageGenerationJob
    ▼
ImageGenerationJob.ExecuteAsync()   [后台]
    │ 加载 Task
    │ 构建 Gemini API 请求
    ▼
AiGateWayManager.GeminiGenerateContentImageForStatisticsAsync()
    │ 调用 GeminiGenerateContentService
    │ 提取 base64 图片
    │ POST → https://ccnetcore.com/prod-api/ai-image/upload-base64（远程上传）
    │ ImageStoreTaskAggregateRoot.SetSuccess(url)
    │
    └── 异常 → SetStatus(Fail) + ErrorInfo
```

### 8.3 Token 用量统计路径

```
每个 Chat 请求完成时
    │
    ▼
UsageStatisticsManager.SetUsageAsync(userId, modelId, usage, tokenId)
    │ 获取分布式锁（per user/token/model）
    │ SELECT 现有 AiUsage 记录
    │ Upsert → AddOnceChat(inputTokens, outputTokens)
    │ 释放锁
    ▼
AiUsage 表（user × model × token 维度聚合）
    ├── UsageTotalNumber（总调用次数）
    ├── UsageInputTokenCount（总输入 Token）
    ├── UsageOutputTokenCount（总输出 Token）
    └── TotalTokenCount（总计）
```

---

## 9. Gateway 适配器架构

### 9.1 注册与路由机制

Gateway 适配器通过 ABP **Keyed Service** 注册，路由键为 `AiModel.HandlerName`：

```
AiGateWayManager.GetModelAsync(apiType, modelId)
    │
    ▼
SELECT AiModel JOIN AiProvider → AiModelDescribe
    │ 含 HandlerName = "OpenAiChatCompletionsService" 等
    │
    ▼
ServiceProvider.GetRequiredKeyedService<IChatCompletionService>(modelDescribe.HandlerName)
    │
    ▼
具体 Gateway 实现
```

### 9.2 请求预处理管道（SpecialCompatible）

允许注册 Action 管道在请求发出前转换请求内容：

```
IOptions<SpecialCompatibleOptions>
    ├── Handles（List<Action<ThorChatCompletionsRequest>>）
    └── AnthropicHandles（List<Action<AnthropicInput>>）
```

用于处理不同供应商对 OpenAI 协议的"方言"差异（如某些字段名不同、格式差异等）。

### 9.3 JSON 序列化

所有 Gateway 实现共用 `ThorJsonSerializer.DefaultOptions`：
- `PropertyNamingPolicy = CamelCase`
- `DefaultIgnoreCondition = WhenWritingNull`
- `Encoder = UnsafeRelaxedJsonEscaping`

Domain.Shared 层还有一个独立的 `Anthropic\ThorJsonSerializer.cs`（同名不同命名空间），功能相同但用于 Anthropic DTO 的序列化。

---

## 10. 当前问题与增强方向

### 10.1 已识别问题

| 编号 | 严重程度 | 问题 | 位置 |
|------|---------|------|------|
| P1 | **高** | `AiToolService` 三个方法全部 Stub | `Application/Services/AiToolService.cs` |
| P2 | **高** | `ChatManager` Agent 功能全部被注释，Agent 会话类型无实际逻辑 | `Domain/Managers/ChatManager.cs` |
| P3 | **高** | 双套 DTO 体系并存（`ChatMessageDto` vs `MessageDto` 等），增加维护成本 | `Application.Contracts/Dtos/` |
| P4 | **中** | `AiGateWayManager` 1575 行单体类，职责过重（路由 + 流管理 + 消息存储 + 统计） | `Domain/Managers/AiGateWayManager.cs` |
| P5 | **中** | `AiPromptService` 未实现 `IAiPromptService` 接口 | `Application/Services/AiPromptService.cs` |
| P6 | **中** | `ModelManager` 空壳，构造函数注入了依赖但无任何方法 | `Domain/Managers/ModelManager.cs` |
| P7 | **中** | `ThorToolChoiceTypeConst.Required` 值为 `"required "`（尾部空格 bug） | `Domain.Shared/Dtos/OpenAi/ThorToolChoiceTypeConst.cs` |
| P8 | **中** | `SystemUsageStatisticsService` 费用字段硬编码为 0 | `Application/Services/SystemUsageStatisticsService.cs` |
| P9 | **低** | `EmbeddingCreateRequest.Validate()` 抛 `NotImplementedException` | `Domain.Shared/Dtos/OpenAi/Embeddings/EmbeddingCreateRequest.cs` |
| P10 | **低** | `AzureOpenAIServiceImageService.CreateImageVariation()` 未实现 | `Domain/AiGateWay/Impl/ThorAzureOpenAI/Images/` |
| P11 | **低** | 多个 `[Obsolete]` 标记的类待清理（`AiAccountService`, `MessageLogManager`, `HttpClientFactory`, MCP 工具, `MessageLogAggregateRoot`） | 多处 |
| P12 | **低** | `SqlSugarCore` 层预留的 `DataSeeds/` 和 `Repositories/` 文件夹为空 | `SqlSugarCore/` |
| P13 | **低** | 模块已有完整设计分析但从未开始实施 | 根目录旧文档（ai_module_analysis.md 等） |

### 10.2 建议的三阶段增强路线图

#### Phase 1 — 清理与修复（打基础）

1. 清理 `[Obsolete]` 代码：移除 `AiAccountService`、`MessageLogManager`/`MessageLogAggregateRoot`、`HttpClientFactory`、MCP 工具类
2. 统一 DTO 体系：合并双套 DTO（删除老的 `MessageDto`/`SessionDto` 系列，统一使用 `ChatMessageDto`/`ChatSessionDto`）
3. 修复已知 Bug：`ThorToolChoiceTypeConst.Required` 尾部空格、`AiPromptService` 未实现接口
4. 实现 `ModelManager` 基本方法

#### Phase 2 — 补齐核心功能

1. 实现 `AiToolService`：翻译、摘要、AI 搜索（通过调用现有 Gateway）
2. 重构 `AiGateWayManager`：拆分为多个职责单一的 Manager
3. 实现 Agent 会话流（`ChatManager` 方法）
4. 补齐 `EmbeddingCreateRequest.Validate()` 和 `CreateImageVariation()`
5. 实现 `SystemUsageStatisticsService` 费用计算

#### Phase 3 — 新增能力

1. **用户配额系统**：`UserAiQuota` 实体 + `AiQuotaService`（三级配额：匿名/免费/VIP）
2. **Prompt 增强**：变量替换、分类标签、多语言支持
3. **模型能力扩展**：Vision/JSON Mode/Context Window 配置
4. **向量与知识库**：PGVector 集成、Embedding Pipeline、语义搜索
5. **术语库**：翻译术语管理
6. **文章服务**：自动摘要、配图生成、关联推荐

---

> **下一步**：确认此架构分析是否准确，然后细化 Phase 1 的具体实施方案。
