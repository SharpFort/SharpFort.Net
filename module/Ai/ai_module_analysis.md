# SharpFort.Ai 模块 — 全量分析 & 精简重构清单

> 生成日期：2026-05-11  
> 分析范围：`E:\Projects\SharpFort.Net\module\Ai`  
> 分析目标：对照 SharpFort CRM/Blog 场景需求，输出精简、增强、补充三份清单

---

## 一、现有功能全量盘点

### 1.1 领域实体（13 张表）

| # | 实体 | 表名 | 用途 |
|---|------|------|------|
| 1 | `AiProvider` | Ai_Provider | AI 供应商/应用（Name, Endpoint, ApiKey, OrderNum） |
| 2 | `AiModel` | Ai_Model | 模型定义（ModelId, ModelType, ModelApiType, Multiplier, MultiplierShow, IconUrl, IsEnabled） |
| 3 | `AiPrompt` | Ai_Prompt | 提示词模板（Code, Content, Description, DefaultModelId） |
| 4 | `ChatSession` | Ai_Session | 聊天会话（UserId, SessionType: Chat/Agent, SessionContent） |
| 5 | `ChatMessage` | Ai_Message | 聊天消息（Role, Content, TokenUsage, MessageType: Web/Api, IsHidden） |
| 6 | `Token` | Ai_Token | API 密钥（TokenKey, UserId, Name, ExpireTime, IsDisabled, IsEnableLog） |
| 7 | `AiUsage` | Ai_Usage | 用量统计 — 按用户+模型+Token 三维度聚合 |
| 8 | `AiBlacklist` | Ai_Blacklist | 用户黑名单（UserId, StartTime, EndTime） |
| 9 | `AgentStore` | Ai_AgentStore | Agent 线程持久化 SessionId → Store |
| 10 | `AiAppShortcutAggregateRoot` | Ai_App_Shortcut | 渠道商快捷配置（Name, Endpoint, ApiKey） |
| 11 | `ImageStoreTaskAggregateRoot` | Ai_ImageStoreTask | 图片生成任务（含广场发布 PublishStatus/Categories/IsAnonymous） |
| 12 | `MessageLogAggregateRoot` | Ai_Message_Log | API 请求日志（RequestBody, ApiKey, ApiKeyName, ModelId, ApiType） |
| 13 | `AgentStoreAggregateRoot` | Ai_AgentStore | **与 #9 重复** — 同一张表两个实体类，需合并 |

### 1.2 枚举（9 个）

| 枚举 | 值 | 用途 |
|------|-----|------|
| `ModelType` | Chat / Image / Embedding | 模型能力分类 |
| `ModelApiType` | Completions / Messages / Responses / GenerateContent | API 协议类型 |
| `MessageType` | Web(1) / Api(2) | 消息来源渠道 |
| `SessionType` | Chat(0) / Agent(1) | 会话类型 |
| `TaskStatusEnum` | Processing / Success / Fail | 图片任务状态 |
| `PublishStatus` | Unpublished(0) / Published(1) | 图片广场发布状态 |
| `AnnouncementType` | Activity(1) / System(2) | 公告类型（前台功能） |
| `RankingType` | Model(0) / Tool(1) | 排行榜类型（中转站功能） |
| `TradeStatus` | WAIT_TRADE / WAIT_BUYER_PAY / TRADE_CLOSED / TRADE_SUCCESS / TRADE_FINISHED | 交易状态（商业支付） |

### 1.3 网关适配器（7 个实现，覆盖 4 种 API 协议）

| 协议 | 适配器 | 对应接口 |
|------|--------|----------|
| OpenAI Completions | ThorDeepSeek / ThorCustomOpenAI / ThorAzureOpenAI / ThorAzureDatabricks | `IChatCompletionService`（流式+非流式） |
| OpenAI Responses | 同上 | `IOpenAiResponseService`（流式+非流式） |
| Claude Messages | ThorClaude | `IAnthropicChatCompletionService`（流式+非流式） |
| Gemini GenerateContent | ThorGemini / ThorSiliconFlow | `IGeminiGenerateContentService`（流式+非流式） |
| Image (DALL-E) | ThorAzureOpenAI | `IImageService`（Create / Edit / Variation） |
| Embedding | ThorSiliconFlow | `ITextEmbeddingService` |

额外基础设施：
- `ThorJsonSerializer` — 统一 JSON 序列化配置
- `ISpecialCompatible` / `SpecialCompatible` / `SpecialCompatibleOptions` — 请求参数兼容处理管道
- `SupplementalMultiplierHelper` — Token 用量倍率换算
- `HttpClientExtensions` / `HttpClientFactory` — HTTP 客户端扩展

### 1.4 Domain Managers（8 个）

| Manager | 职责 |
|---------|------|
| `AiGateWayManager` | 核心网关管理器：模型路由、协议分发、流式响应编排、用量记录 |
| `AiMessageManager` | 创建系统/用户消息 |
| `ChatManager` | Agent 流式对话编排 + 工具注册发现 |
| `TokenManager` | Token 验证、获取用户 Token |
| `ModelManager` | 模型查询 |
| `AiBlacklistManager` | 黑名单校验 |
| `MessageLogManager` | 请求日志写入 |
| `UsageStatisticsManager` | 用量统计聚合写入 |

### 1.5 Application Services（16 个）

| # | 服务 | 核心功能 |
|---|------|----------|
| 1 | `AiChatService` | 获取聊天模型列表、统一流式消息发送 |
| 2 | `AiImageService` | 图片生成任务、我的任务、**图片广场**、**发布广场** |
| 3 | `AiModelService` | 模型 CRUD |
| 4 | `AiProviderService` | 供应商 CRUD |
| 5 | `AiPromptService` | 提示词 CRUD |
| 6 | `AiToolService` | 翻译 / 总结 / 搜索 — **接口已定义，全部 throw NotImplementedException** |
| 7 | `AiAccountService` | 用户信息整合（转发到 CasbinRbac AccountService） |
| 8 | `ChannelService` | **渠道商管理**：应用/模型 CRUD + 快捷配置查询 |
| 9 | `MessageService` | 消息查询/删除（软隐藏） |
| 10 | `SessionService` | 会话 CRUD（含消息连删） |
| 11 | `TokenService` | Token CRUD + 启用/禁用 + 选择列表 |
| 12 | `OpenApiService` | **对外开放 API 端点**：Chat / Image / Embedding / Claude / Gemini |
| 13 | `UsageStatisticsService` | 用户用量统计：7天趋势 / 24小时柱状 / 今日卡片 / 模型占比 |
| 14 | `SystemUsageStatisticsService` | 系统级指定日期各模型 Token 统计 |
| 15 | `TestService` | 空占位 |
| 16 | `FileMasterService` | 文件管理服务 |

### 1.6 后台任务（1 个）

| Job | 功能 |
|-----|------|
| `ImageGenerationJob` | 通过 Gemini API 异步生成图片 |

### 1.7 MCP Agent 工具（5 个）

| 工具 | 用途 |
|------|------|
| `DateTimeTool` | 获取当前日期时间 |
| `DeepThinkTool` | 空占位 |
| `HttpRequestTool` | 发送 HTTP 请求（GET/POST/PUT/DELETE） |
| `OnlineSearchTool` | 百度千帆联网搜索 |
| `YxaiKnowledgeTool` | "意心Ai"平台知识库（硬编码 `ccnetcore.com` URL） |

---

## 二、需求对照映射

| # | 你的需求 | 现有覆盖情况 |
|---|----------|-------------|
| 1 | 管理 AI 提供商、模型、密钥 | ✅ 已覆盖 — AiProvider + AiModel + Token，CRUD 完备 |
| 2 | 调用 AI API 对话/绘图等 | ✅ 已覆盖 — 7 个网关适配器，支持 OpenAI / Claude / Gemini / Image / Embedding |
| 3 | 管理提示词 | ✅ 已覆盖 — AiPrompt CRUD |
| 4 | 文章内容总结 | ⚠️ 骨架存在 — `AiToolService.SummarizeAsync` 未实现 |
| 5 | 根据内容绘图 | ⚠️ 图片生成有，但未对接「文章→绘图」工作流 |
| 6 | 文章翻译多国语言 | ⚠️ 骨架存在 — `AiToolService.TranslateAsync` 未实现 |
| 7 | 翻译前调用词库 | ❌ 不存在 — 无词库管理功能 |
| 8 | 对话推荐文章 | ❌ 不存在 — 无文章推荐功能，无 RAG 管道 |
| 9 | PGVector 向量存储 + AI 搜索 | ⚠️ Embedding 有 — `ITextEmbeddingService` 已实现，但缺少 PGVector 集成和 RAG 管道 |
| 10 | 自己维护翻译词库 | ❌ 不存在 — 无词库实体/服务 |

---

## 三、精简清单（应删除）

### 3.1 实体级删除

| 删除 | 原因 |
|------|------|
| `AiAppShortcutAggregateRoot` | 渠道商快捷配置，中转站商业功能 |
| `AgentStore` + `AgentStoreAggregateRoot` | Agent 智能体持久化，当前非必需；两个类映射同一张表应合并为一个 |
| `ImageStoreTaskAggregateRoot` 的广场/发布字段 | `PublishStatus`, `Categories`, `IsAnonymous` 属于图片广场功能 |
| `MessageLogAggregateRoot` | API 请求日志，非核心业务；如需审计可后续加回 |
| `PremiumPackageConst` | 高级套餐模型列表，商业功能 |

### 3.2 枚举级删除

| 删除 | 原因 |
|------|------|
| `AnnouncementType` | 公告类型，原项目前台功能 |
| `RankingType` | 排行榜（模型/工具），中转站功能 |
| `TradeStatus` | 交易状态，商业支付功能 |
| `PublishStatus` | 图片广场发布状态 |

### 3.3 服务级删除/合并

| 操作 | 对象 | 原因 |
|------|------|------|
| 删除 | `ChannelService` | 渠道商管理 — 中转站功能。其内的模型/应用 CRUD 已由 `AiModelService`/`AiProviderService` 覆盖 |
| 删除 | `AiAppShortcut` 相关 DTO | 对应实体删除 |
| 删除 | `Ranking` 全部 DTO + `IRankingService` | 排行榜功能 |
| 删除 | `ImagePlazaPageInput`, `PublishImageInput`, `ImageMyTaskPageInput` | 图片广场 DTO |
| 删除 | `SystemUsageStatisticsService` | 系统级统计不是当前需求 |
| 删除 | `TestService` | 空占位 |
| 删除 | `AiAccountService` | 简单转发，可内联到其他服务 |
| 可选精简 | `Token` 实体和 `TokenService` | 取决于 CRM/Blog 是否需要用户自管理 API Key。如仅管理员使用，可简化为配置项 |

### 3.4 Gateway / MCP 精简

| 操作 | 对象 | 原因 |
|------|------|------|
| 删除 | `YxaiKnowledgeTool` | 硬编码 `ccnetcore.com` URL，原项目特有 |
| 删除 | `DeepThinkTool` | 空占位 |
| 可选删除 | `OnlineSearchTool` | 百度搜索依赖外部 API Key，Blog/CRM 非必需 |
| 可选删除 | `HttpRequestTool` | Agent 工具，非必需 |
| 简化 | 网关适配器 | 保留 OpenAI Completions + Gemini + Image + Embedding；Claude Messages 和 Responses 如不使用可移除 |

### 3.5 端点精简

`OpenApiService` 完整实现了 OpenAI 兼容的对外中转 API：

```
POST /openApi/v1/chat/completions
POST /openApi/v1/images/generations
POST /openApi/v1/embeddings
POST /openApi/v1/messages
POST /openApi/v1beta/models/{modelId}:generateContent
POST /openApi/v1beta/models/{modelId}:streamGenerateContent
POST /openApi/v1/responses
```

如果你的 CRM/Blog **不需要对外提供 API 中转服务**，这些端点应全部移除。

---

## 四、增强清单（保留但需改造）

| # | 对象 | 改造内容 |
|---|------|----------|
| 1 | `AiToolService` | **实现** `SummarizeAsync` / `TranslateAsync` / `SearchAsync`。这是需求 4-6-9 的核心 |
| 2 | `AiPrompt` | 增加字段：`Category`（分类：总结/翻译/绘图/推荐/对话）、`Variables`（JSON 数组，定义占位变量如 `{{title}}`, `{{content}}`）、`TargetLanguage`（目标语言 code） |
| 3 | `AiModel` | 增加能力标记：`SupportsVision`（是否视觉）、`SupportsJsonMode`、`MaxContextTokens`（最大上下文）、`PricingInputPer1K` / `PricingOutputPer1K`（定价） |
| 4 | `AiProvider.ApiKey` | 应改为加密存储（当前明文） |
| 5 | `ITextEmbeddingService` | 现有仅做了 Embedding 调用，需扩展为完整管道：文本切分 → 调用 Embedding → 存入 PGVector |
| 6 | `ChatSession` | 增加 `SourceType`（Direct / ArticleQA / Recommend）、`SourceId`（关联文章/实体 ID） |
| 7 | `ChatMessage` | 增加 `Metadata`（JSON 字段，存储来源文章 ID、引用片段、推荐评分等上下文） |

---

## 五、补充清单（全新开发）

| # | 功能 | 对应需求 | 建议实现 |
|---|------|----------|----------|
| 1 | **词库管理** | 需求 #7 #10 | 新实体 `AiGlossary`（SourceLanguage, TargetLanguage, SourceTerm, TargetTerm, DomainCategory, IsApproved），CRUD 服务，翻译前查询词库执行术语替换 |
| 2 | **文章向量化管道** | 需求 #9 | `ArticleEmbeddingPipeline` 领域服务：读取文章 → Markdown 清理 → 文本分块（chunking）→ 调用 `ITextEmbeddingService` → 写入 PGVector |
| 3 | **语义搜索服务** | 需求 #9 | `AiSearchService`：用户查询 → Embedding → PGVector 余弦相似搜索 → 返回相关文章列表 |
| 4 | **文章推荐服务** | 需求 #8 | `AiRecommendService`：基于用户对话上下文 / 浏览历史 → 向量检索 → LLM 生成推荐理由 |
| 5 | **翻译管道服务** | 需求 #6 #7 | `TranslationPipelineService`：原文检测 → 查询词库替换 → 加载翻译提示词 → 调用 LLM 翻译 → 回写结果 |
| 6 | **文章总结服务** | 需求 #4 | `ArticleSummarizeService`：加载文章内容 → 加载总结提示词 → 调用 LLM → 返回/存储总结 |
| 7 | **文章配图服务** | 需求 #5 | `ArticleIllustrationService`：提取文章关键描述 → 加载绘图提示词 → 调用 Gemini/Image API → 回写图片 URL |
| 8 | **对话式文章推荐** | 需求 #8 | 扩展 `ChatSession`：会话标记为 Recommend 模式 → 每轮触发语义搜索 → 注入 LLM 上下文 → 生成带推荐的自然语言回复 |
| 9 | **PGVector 集成** | 需求 #9 | 引入 `PgVector` NuGet 包 → `AiModuleDbContext` 配置向量扩展 → 新增 `ArticleVector` 实体 |

---

## 六、重构路线图

### Phase 1 — 精简（工作量最小，效果最大）

**目标：砍掉所有中转站商业逻辑，保留核心骨架**

1. 删除 3.1~3.5 列出的冗余实体/枚举/服务/DTO
2. 合并 `AgentStoreAggregateRoot` 与 `AgentStore`
3. 移除 `ChannelService` + 相关 DTO
4. 移除 `OpenApiService` 对外端点（如不需要）
5. 移除 MCP Tools 目录
6. 清理 `SharpFortAiDomainModule` / `SharpFortAiApplicationModule` 的依赖注册

**产出：** 干净的 Provider → Model → Prompt → Session → Message 核心链

### Phase 2 — 增强（改造现有）

**目标：让现有实体能支撑 CRM/Blog 场景**

7. `AiPrompt` 增加分类/变量/语言字段
8. `AiModel` 增加能力标记字段
9. `AiProvider.ApiKey` 加密存储
10. 实现 `AiToolService` 的 `SummarizeAsync` / `TranslateAsync` / `SearchAsync`
11. `ChatSession` 增加 `SourceType` / `SourceId`
12. `ChatMessage` 增加 `Metadata` JSON 字段

### Phase 3 — 补充（新建功能）

**目标：补齐所有缺失能力**

13. 新建 `AiGlossary` 实体 + 词库管理服务
14. 引入 PGVector，新建 `ArticleVector` 实体
15. 实现 `ArticleEmbeddingPipeline`（向量化管道）
16. 实现 `AiSearchService`（语义搜索）
17. 实现 `TranslationPipelineService`（翻译管道）
18. 实现 `ArticleSummarizeService`（文章总结）
19. 实现 `ArticleIllustrationService`（文章配图）
20. 实现 `AiRecommendService`（对话式推荐）

---

## 七、最终目标架构

### 实体（目标 8~9 个）

```
AiProvider          — 供应商管理（需求1）
AiModel             — 模型管理（需求1）
AiPrompt            — 提示词管理（需求3）
ChatSession         — 会话（需求4/6/8）
ChatMessage         — 消息（需求4/6/8）
AiUsage             — 用量统计（保留，成本管控）
AiGlossary          — [新] 翻译词库（需求7/10）
ArticleVector       — [新] 文章向量（需求9）
ImageStoreTask      — 精简版图片任务（仅保留 TaskStatus，删除广场/发布）（需求5）
```

### 应用服务（目标 10~11 个）

```
AiProviderService          — 供应商 CRUD
AiModelService             — 模型 CRUD
AiPromptService            — 提示词 CRUD
AiChatService              — 核心对话
AiImageService             — 绘图（精简）
AiGlossaryService          — [新] 词库 CRUD
AiSearchService            — [新] 语义搜索
AiRecommendService         — [新] 文章推荐
ArticleSummarizeService    — [新] 文章总结
ArticleIllustrationService — [新] 文章配图
TranslationPipelineService — [新] 翻译管道
```

### 网关层（目标 3~4 个协议）

```
IChatCompletionService      → OpenAI Chat Completions（保留）
IImageService               → Image Generation（保留）
ITextEmbeddingService       → Embedding（保留，扩展为管道）
IGeminiGenerateContentService → Gemini（保留，用于图片生成）
```

---

## 八、关于入手顺序的思考

你的直觉是"从 Domain Entities 开始"，这个方向是对的——Entity 是 DDD 的根基。但从效率角度，我建议：

**先砍再改后建**。理由：

1. **先砍**可以把代码量降 40%~50%，后续改动在"干净地基"上做，心智负担小得多
2. 砍的过程本身就会帮你理清实体间的真实依赖关系（比如删 `ChannelService` 时会发现 `AiModelService` 和 `AiProviderService` 已经覆盖了它的功能）
3. Phase 2 改 Entity 时，因为代码已经精简，你只需要改 5~7 个实体而不是 13 个
4. Phase 3 新建时，架构已经稳定，新增 Entity 只需要加文件，不涉及删除冲突

**具体执行顺序建议：**

```
Day 1-2: Phase 1 — 删除冗余（纯减法，风险低）
Day 3-5: Phase 2 — 增强实体 + 实现 AiToolService
Day 6+:  Phase 3 — 按优先级逐个建设新功能
```

优先建设顺序（按需求依赖关系排列）：
1. 词库管理（翻译的前置依赖）
2. 文章总结（最简单，验证 AiToolService 改造效果）
3. PGVector + 文章向量化（搜索和推荐的前置依赖）
4. 语义搜索
5. 翻译管道
6. 文章配图（依赖 Image API）
7. 对话式推荐（依赖搜索 + ChatSession 改造）
