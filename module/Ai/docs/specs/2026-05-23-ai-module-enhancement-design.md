# AI 模块增强设计规范

> 版本：v1.0 | 日期：2026-05-23 | 状态：待评审
> 基于 `ai_module_architecture_guide.md` 的架构分析

---

## 目录

1. [概述与目标](#1-概述与目标)
2. [Phase 1 - 清理与修复](#2-phase-1---清理与修复)
3. [Phase 2 - 补齐核心功能](#3-phase-2---补齐核心功能)
4. [Phase 3 - 新增能力](#4-phase-3---新增能力)
5. [实施顺序与依赖关系](#5-实施顺序与依赖关系)
6. [风险与注意事项](#6-风险与注意事项)

---

## 1. 概述与目标

### 1.1 背景

SharpFort.Ai 模块当前定位为"多协议 AI 网关"，已实现 4 种 API 协议（OpenAI Completions / Responses / Anthropic Messages / Gemini GenerateContent）的统一代理，包括流式和非流式两种模式。但作为平台级 AI 服务模块，仍存在三个核心不足：

1. **大量 Stub 和空壳**：`AiToolService` 的三个方法、`ChatManager` 的 Agent 能力、`ModelManager` 均为空实现
2. **无用量控制**：任何人可以无限量调用任何模型，缺少配额和计费基础
3. **缺乏平台级 AI 能力**：无向量存储、术语库、语义搜索、文章智能处理等附加值服务

### 1.2 设计目标

| 目标 | 衡量标准 |
|------|---------|
| 消除所有 Stub | 每个接口方法都有可工作的实现 |
| 架构可维护 | `AiGateWayManager` 拆分后每个类 ≤ 500 行 |
| 用量可控 | 三级配额（匿名/注册/VIP），超额即拦截 |
| 平台能力 | 提供向量存储、语义搜索、术语库、翻译增强 |
| 无破坏性变更 | 现有 API 端点保持不变 |

### 1.3 设计原则

- **委托而非重造**：新功能复用现有 Gateway 管道，不创建新的通信路径
- **渐进增强**：Phase 1 纯删除 → Phase 2 补齐 → Phase 3 新增，每阶段可独立交付
- **YAGNI**：只实现当前明确需要的功能，不在需求未确认前预留扩展点
- **接口先行**：所有新功能先定义接口（Application.Contracts），再写实现

---

## 2. Phase 1 - 清理与修复

### 2.1 [Obsolete] 代码清理

#### 2.1.1 删除清单

| 序号 | 文件 | 类型 | 删除理由 | 连带操作 |
|------|------|------|---------|---------|
| D1 | `Application/Services/AiAccountService.cs` | `AiAccountService` | 仅一行转发到 CasbinRbac `IAccountService` | 移除 HTTP 端点 `GET account/ai` |
| D2 | `Domain/Managers/MessageLogManager.cs` | `MessageLogManager` | 陈旧审计日志，已被配额系统思路替代 | 移除 DI 注册 |
| D3 | `Domain/Entities/MessageLogAggregateRoot.cs` | `MessageLogAggregateRoot` | 对应数据库表 `Ai_Message_Log` 不再需要 | 数据库迁移：DROP TABLE |
| D4 | `Domain/AiGateWay/HttpClientFactory.cs` | `HttpClientFactory` | 所有 Gateway 实现已改用 `IHttpClientFactory` | 无（无调用方） |
| D5 | `Domain/Mcp/HttpRequestTool.cs` | `HttpRequestTool` | MCP Agent 工具链已废弃 | 移除 `[SfAgentTool]` 注册 |
| D6 | `Domain/Mcp/DateTimeTool.cs` | `DateTimeTool` | MCP Agent 工具链已废弃 | 移除 `[SfAgentTool]` 注册 |

#### 2.1.2 删除流程

每个删除项按以下步骤执行：
1. 全局搜索引用方（`Grep` 类名/方法名）
2. 移除或替换引用
3. 删除源文件
4. 编译验证
5. 提交一个删除对应一个独立 commit

### 2.2 DTO 体系统一

#### 2.2.1 问题描述

当前模块有两套并行的消息/会话 DTO：

| 老版本（顶层 `Dtos/`） | 新版本（子目录 `Dtos/Chat*/`） | 差异 |
|--------------------------|-------------------------------|------|
| `MessageDto` | `ChatMessageDto` | 新版本多了 `MessageType` 字段 |
| `MessageGetListInput` | `ChatMessageGetListInput` | 结构相同 |
| `MessageDeleteInput` | `ChatMessageDeleteInput` | 结构相同 |
| `SessionDto` | `ChatSessionDto` | 结构相同 |
| `SessionGetListInput` | `ChatSessionGetListInput` | 结构相同 |
| `SessionCreateAndUpdateInput` | `ChatSessionCreateInput` + `ChatSessionUpdateInput` | 新版本拆分了 Create/Update |

#### 2.2.2 合并方案

**保留新版本（子目录系列），删除老版本（顶层系列）**：

| 操作 | 具体内容 |
|------|---------|
| 删除文件 | `Dtos/MessageDto.cs`, `Dtos/MessageGetListInput.cs`, `Dtos/SessionDto.cs`, `Dtos/SessionGetListInput.cs`, `Dtos/SessionCreateAndUpdateInput.cs` |
| 修改 `MessageService` | 改用 `ChatMessageDto`, `ChatMessageGetListInput`, `ChatMessageDeleteInput` |
| 修改 `SessionService` | 已继承 `CrudAppService<ChatSession, SessionDto, Guid, SessionGetListInput, SessionCreateAndUpdateInput>`，评估是否迁移到 `ChatSessionDto` 系列；如果 `SessionService` 仍需要保留，则将其参数改为新 DTO |
| 保留不动 | `SendMessageInput.cs`, `SendMessageStreamOutputDto.cs`（属于 API 兼容层，独立体系） |

### 2.3 Bug 修复

| 编号 | 文件 | 问题 | 修复 |
|------|------|------|------|
| B1 | `Domain.Shared/Dtos/OpenAi/ThorToolChoiceTypeConst.cs` | `Required = "required "` 尾部空格 | 改为 `"required"` |
| B2 | `Application/Services/AiPromptService.cs` | 类签名缺少 `IAiPromptService` | 改为 `ApplicationService, IAiPromptService` |
| B3 | `Domain/Managers/ModelManager.cs` | 空壳，注入依赖未使用 | 添加 `GetAsync(Guid id)` 和 `GetListAsync()` |

### 2.4 Phase 1 输出物

- [ ] 6 个 `[Obsolete]` 文件/类已删除
- [ ] 5 个老版本 DTO 文件已删除
- [ ] `MessageService` 切换到新 DTO
- [ ] B1/B2/B3 已修复
- [ ] 全量编译通过（`dotnet build` 0 errors 0 warnings）

---

## 3. Phase 2 - 补齐核心功能

### 3.1 AiToolService 实现

#### 3.1.1 接口契约（已有，不动）

```csharp
public interface IAiToolService : IApplicationService
{
    Task<string> TranslateAsync(string text, string targetLang, string? modelId = null);
    Task<string> SummarizeAsync(string content, string? modelId = null);
    Task<string> SearchAsync(string query, string? modelId = null);
}
```

#### 3.1.2 TranslateAsync 设计

```
流程：
1. 接收 text + targetLang + modelId?
2. 查询 AiPrompt 表：Code = "Translate"
3. 变量替换：{{text}} → text, {{target_lang}} → targetLang
4. （Phase 3 集成）从 AiGlossary 查询术语表 → 追加到 System Prompt
5. 构建 ThorChatCompletionsRequest（单条 user message）
6. modelId 为空时使用 AiPrompt.DefaultModelId，再空则使用系统默认翻译模型
7. 调用 AiGateWayManager.CompleteChatForStatisticsAsync()
8. 从响应提取 content → 返回纯文本
```

**Prompt 模板示例**（`AiPrompt` 表 `Code="Translate"`）：
```
You are a professional translator. Translate the following text to {{target_lang}}.
Output only the translated text, no explanations.

Text: {{text}}
```

#### 3.1.3 SummarizeAsync 设计

```
流程：
1. 接收 content + modelId?
2. 查询 AiPrompt 表：Code = "Summarize"
3. 变量替换：{{content}} → content
4. 请求构建 → Gateway 调用 → 提取文本
```

#### 3.1.4 SearchAsync 设计

```
流程：
1. 接收 query + modelId?
2. 查询 AiPrompt 表：Code = "Search"
3. 变量替换：{{query}} → query
4. 请求构建时启用 WebSearchOptions（透传搜索参数）
5. Gateway 调用 → 提取文本
```

#### 3.1.5 实现要点

- 依赖：`IAiPromptService`（查模板）、`AiGateWayManager`（调模型）
- 每个方法 **不超过 50 行**——直接通过 `AiPromptService.GetAsync()` 获取模板后做简单字符串替换
- Phase 3 引入 `AiPromptManager.ResolveAsync()` 后，升级为统一的变量解析
- Token 用量自动由 `CompleteChatForStatisticsAsync` 统计，无需额外处理

### 3.2 AiGateWayManager 拆分

#### 3.2.1 现状

`AiGateWayManager.cs` 约 1575 行，包含：
- 模型路由（`GetModelAsync`）
- 流式调度（`UnifiedStreamForStatisticsAsync` + 4 个私有 processor）
- 非流式包装（`CompleteChatForStatisticsAsync` 等 6 个方法）
- 消息存储（`ChatMessage` 创建逻辑混在 processor 中）
- 用量统计（`UsageStatisticsManager.SetUsageAsync` 调用）
- SSE 写入工具方法

#### 3.2.2 拆分方案

```
拆分前：
  AiGateWayManager (1575 行)
    ├── 路由
    ├── 流管理
    ├── 消息存储
    ├── 用量统计
    └── SSE 工具方法

拆分后：
  AiGateWayManager (~400 行)
    ├── GetModelAsync()
    ├── UnifiedStreamForStatisticsAsync()
    ├── 4 个私有 Process*StreamAsync()
    └── WriteAsEventStreamDataAsync()（工具方法）

  ChatMessageManager (~200 行，新建)
    ├── CreateChatMessagesAsync()  —— 创建 user + system 消息
    └── 从 4 个 Process*StreamAsync 中迁入

  UsageStatisticsManager (~250 行，增强现有）
    ├── SetUsageAsync()            —— 已有
    ├── CompleteChatForStatisticsAsync()           —— 迁入
    ├── AnthropicCompleteChatForStatisticsAsync()  —— 迁入
    ├── CreateImageForStatisticsAsync()            —— 迁入
    ├── EmbeddingForStatisticsAsync()              —— 迁入
    ├── OpenAiResponsesAsyncForStatisticsAsync()   —— 迁入
    └── GeminiGenerateContentForStatisticsAsync()  —— 迁入
```

#### 3.2.3 新文件清单

| 文件 | 位置 | 行数估算 | 职责 |
|------|------|---------|------|
| `ChatMessageManager.cs` | `Domain/Managers/` | ~200 | 聊天消息持久化 |
| `UsageStatisticsManager.cs` | `Domain/Managers/`（已有，增强） | ~250 | Token 统计包装 + 聚合 |

#### 3.2.4 迁移不变量

- 所有 public 方法签名保持不变
- 流式输出行为不变（75ms 缓冲、ConcurrentQueue、错误事件注入）
- 用量统计精度不变

### 3.3 ChatManager Agent 实现

#### 3.3.1 当前状态

`Domain/Managers/ChatManager.cs` 中三个方法全部被注释：
```csharp
// public async Task AgentCompleteChatStreamAsync(...) { /* placeholder */ }
// private List<ThorToolDefinition> GetTools() { /* placeholder */ }
// private async Task SendHttpStreamMessageAsync(...) { /* placeholder */ }
```

注释原文："placeholder, waiting for Phase 3 to refactor with native OpenAI"

#### 3.3.2 Agent 循环设计

```
AgentCompleteChatStreamAsync(sessionId, userId, content, modelId, tokenId, output)
  │
  ├── 1. 加载会话历史（ChatMessage 表，按 CreationTime 排序）
  ├── 2. 加载/创建 AgentStore（反序列化 Agent 状态）
  ├── 3. 构建 Messages 列表：
  │     System: Agent 系统 prompt + 工具定义 JSON
  │     History: 最近 N 轮对话
  │     User: content（当前用户输入）
  ├── 4. 构建 Tools 列表：
  │     扫描 [SfAgentTool] 标注的类 → 构建 ThorToolDefinition 数组
  ├── 5. 调用 AiGateWayManager 流式请求（tool_choice = "auto"）
  │
  │    循环（最多 N 轮，默认 10 轮）：
  │    ├── 发送请求
  │    ├── 流式输出 Delta 内容 → SSE（Type = Text）
  │    ├── 检测 FinishReason：
  │    │   ├── "stop" → 结束循环
  │    │   ├── "tool_calls" → 执行工具 → 结果追加到 Messages
  │    │   │     Type = ToolCalling（开始调用）
  │    │   │     Type = ToolCalled（调用完成）
  │    │   │     → 继续循环
  │    │   └── "length" / "content_filter" → 结束并报告
  │    └── Type = Usage（最终 Token 统计）
  │
  ├── 6. 保存 AgentStore（序列化最终状态）
  └── 7. 存储消息
```

#### 3.3.3 工具发现机制

```csharp
// 扫描所有标注了 [SfAgentTool] 的类（从 DI 容器获取）
// 反射获取 public 方法 → 解析参数名和类型 → 构建 JSON Schema
// 返回 List<ThorToolDefinition>

ThorToolDefinition CreateToolDefinitionFromMethod(MethodInfo method)
  → Function.Name = method.Name（或 SfAgentToolAttribute.Name）
  → Function.Description = 从 XML 注释提取（或默认描述）
  → Function.Parameters = 从方法参数构建 JSON Schema
```

#### 3.3.4 工具执行机制

```csharp
// 收到 model 的 tool_calls → 按 tool_call.Name 找到对应方法
// 解析 tool_call.Function.Arguments JSON → 参数列表
// MethodInfo.Invoke(toolInstance, args) → 获取结果
// 结果序列化为 JSON string → 追加为 role=tool 的 Message
```

### 3.4 补齐剩余 Stub

| 序号 | 位置 | 当前状态 | 实现 |
|------|------|---------|------|
| S1 | `EmbeddingCreateRequest.Validate()` | `throw NotImplementedException` | 验证 `Input` 非空、`Model` 非空或从配置取默认值 |
| S2 | `AzureOpenAIServiceImageService.CreateImageVariation()` | `throw NotImplementedException` | 评估：Azure 当前 API version 如支持即实现 HTTP 上传；不支持则保持现状并加注释说明 |
| S3 | `SystemUsageStatisticsService.GetTokenStatisticsAsync()` 费用计算 | `Cost = 0` 硬编码 | 从 `AiModel` 表读取定价信息（Phase 3 新增字段），乘以用量；定价字段为空时保持 0 |

### 3.5 Phase 2 输出物

- [ ] `AiToolService` 三个方法均可正常调用
- [ ] `AiGateWayManager` 拆分完成，新类各自 ≤500 行
- [ ] `ChatManager.AgentCompleteChatStreamAsync()` 可正常执行 Agent 循环
- [ ] S1/S2/S3 修复或明确标记
- [ ] 全量编译通过，现有 API 行为不变

---

## 4. Phase 3 - 新增能力

### 4.1 用户配额系统

#### 4.1.1 新增实体

```csharp
[SugarTable("Ai_UserQuota")]
public class UserAiQuota : FullAuditedAggregateRoot<Guid>
{
    public Guid UserId { get; set; }
    public UserTier Tier { get; set; } = UserTier.Anonymous;
    public QuotaPeriod Period { get; set; } = QuotaPeriod.Daily;
    public DateTime PeriodStart { get; set; }
    public int MaxCalls { get; set; }
    public int UsedCalls { get; set; }
    public long MaxTokens { get; set; }
    public long UsedTokens { get; set; }
    public bool IsEnabled { get; set; } = true;

    public bool IsExpired() => Period switch
    {
        QuotaPeriod.Daily => PeriodStart.Date < DateTime.UtcNow.Date,
        QuotaPeriod.Monthly => PeriodStart.Month != DateTime.UtcNow.Month,
        _ => false
    };

    public void ResetIfExpired()
    {
        if (IsExpired())
        {
            UsedCalls = 0;
            UsedTokens = 0;
            PeriodStart = DateTime.UtcNow;
        }
    }
}

public enum UserTier { Anonymous = 0, Registered = 1, Vip = 2 }
public enum QuotaPeriod { Daily, Monthly }
```

#### 4.1.2 默认配额配置

```csharp
public static class QuotaDefaults
{
    public static readonly Dictionary<UserTier, (int Calls, long Tokens)> Limits = new()
    {
        [UserTier.Anonymous]   = (5,      10_000),       // 5次/1万 token/天
        [UserTier.Registered]  = (50,     100_000),       // 50次/10万 token/天
        [UserTier.Vip]         = (500,    1_000_000),     // 500次/100万 token/天
    };
}
```

配额值放在 `appsettings.json` 中可配置，`QuotaDefaults` 仅作为回退默认值。

#### 4.1.3 新增接口与实现

```csharp
// Application.Contracts/IServices/IAiQuotaService.cs
public interface IAiQuotaService
{
    Task<QuotaCheckResult> CheckQuotaAsync(Guid userId, Guid? tokenId);
    Task RecordUsageAsync(Guid userId, Guid? tokenId, long inputTokens, long outputTokens);
    Task ResetExpiredQuotasAsync(); // Cron job 调用
    Task<UserQuotaRemaining> GetRemainingQuotaAsync(Guid userId);
}

public record QuotaCheckResult(bool Allowed, string? Message, long RemainingCalls, long RemainingTokens);
public record UserQuotaRemaining(UserTier Tier, long RemainingCalls, long RemainingTokens, DateTime PeriodEnd);
```

#### 4.1.4 拦截点

在 `AiGateWayManager.UnifiedStreamForStatisticsAsync()` **进入时**（向 AI 发送请求之前）插入：

```csharp
// 伪代码
var check = await _quotaService.CheckQuotaAsync(userId, tokenId);
if (!check.Allowed)
{
    // 输出 SSE 错误事件，告知用户配额不足及剩余量
    yield return WriteAsEventStreamDataAsync(new { error = check.Message, ... });
    yield break;
}

// ... 现有流式处理逻辑 ...

// 完成时记录用量
await _quotaService.RecordUsageAsync(userId, tokenId, inputTokens, outputTokens);
```

#### 4.1.5 配额检查逻辑

```
CheckQuotaAsync(userId, tokenId):
1. 确定用户 Tier：
   - 未登录 → Anonymous
   - 已登录 + 角色含 "SfXinAi-Vip" → Vip
   - 已登录 → Registered
2. 查询/创建 UserAiQuota（按 UserId 唯一）
3. 检查用户级黑名单（AiBlacklist）→ 黑名单用户直接拒绝
4. 检查周期是否过期 → 自动重置
5. 比较 UsedCalls vs MaxCalls, UsedTokens vs MaxTokens
6. 返回 QuotaCheckResult
```

### 4.2 AiPrompt 增强

#### 4.2.1 字段扩展

```csharp
// AiPrompt 实体新增字段
public class AiPrompt : FullAuditedAggregateRoot<Guid>
{
    // === 已有字段 ===
    public string Code { get; set; }
    public string Content { get; set; }
    public string? Description { get; set; }
    public Guid? DefaultModelId { get; set; }

    // === Phase 3 新增 ===
    public string? Category { get; set; }         // Translation | Summarization | Search | Custom
    public string? Variables { get; set; }        // JSON: ["var1", "var2"]
    public UserTier? MinTier { get; set; }        // 最低使用等级
    public int? MaxTokensPerCall { get; set; }    // 单次调用 Token 上限
}
```

#### 4.2.2 新增方法

```csharp
// Domain/Managers/AiPromptManager.cs（新建）
public class AiPromptManager : DomainService
{
    // 解析 Prompt：查 Code → 验证 Tier → 变量替换 → 返回文本
    public async Task<string> ResolveAsync(string code, Dictionary<string, string> variables, UserTier? userTier)
    {
        var prompt = await _repository.GetFirstAsync(p => p.Code == code);
        if (prompt == null) throw new UserFriendlyException($"Prompt '{code}' not found");

        if (prompt.MinTier.HasValue && userTier < prompt.MinTier)
            throw new UserFriendlyException("Your account tier does not have access to this feature");

        var result = prompt.Content;
        foreach (var (key, value) in variables)
            result = result.Replace($"{{{{{key}}}}}", value);

        return result;
    }
}
```

变量语法：`{{variable_name}}`，缺失变量抛异常。不提供默认值——调用方有责任传入完整变量。

#### 4.2.3 DTO 同步更新

- `AiPromptCreateInput`、`AiPromptUpdateInput`、`AiPromptDto` 新增 `Category`、`Variables`、`MinTier`、`MaxTokensPerCall`
- `AiPromptService` CRUD 方法自动适配（Mapster 映射）

### 4.3 AiModel 元数据扩展

#### 4.3.1 字段扩展

```csharp
// AiModel 实体新增字段
public bool SupportsVision { get; set; }           // Vision API
public bool SupportsJsonMode { get; set; }         // Structured Output
public int? MaxContextTokens { get; set; }         // 最大上下文窗口
public decimal? PricingInputPerK { get; set; }     // 每千 token 输入价（元）
public decimal? PricingOutputPerK { get; set; }    // 每千 token 输出价（元）
public decimal? PricingImagePerCall { get; set; }  // 每次图片生成价（元）
```

#### 4.3.2 DTO 同步更新

- `AiModelCreateInput`、`AiModelDto` 新增对应字段
- `ModelLibraryDto` 新增 `SupportsVision`、`SupportsJsonMode`、`MaxContextTokens`（供前端展示）

#### 4.3.3 费用计算使用

`SystemUsageStatisticsService.GetTokenStatisticsAsync()` 中：
```csharp
// 从 AiModel 查价格
var model = await _modelRepo.FindAsync(m => m.ModelId == stat.ModelId);
var cost = stat.Tokens / 1000m * (model?.PricingInputPerK ?? 0);
// PricingInputPerK 和 PricingOutputPerK 取均值简化，或分别存储输入/输出量
```

### 4.4 PGVector 向量存储

#### 4.4.1 新增实体

```csharp
[SugarTable("Ai_ArticleVector")]
public class ArticleVector : Entity<Guid>
{
    public string ArticleId { get; set; }            // 来源系统文章 ID
    public string ContentHash { get; set; }          // SHA256，增量检测
    public int ChunkIndex { get; set; }              // 分块序号
    public string ChunkText { get; set; }            // 分块文本（~512 tokens）
    public float[] Embedding { get; set; }           // pgvector vector(1536)
    public string ModelId { get; set; }              // 使用的 Embedding 模型
    public DateTime CreatedAt { get; set; }
}
```

**PGVector 表创建**（DBA 或迁移脚本执行）：
```sql
CREATE EXTENSION IF NOT EXISTS vector;
CREATE TABLE "Ai_ArticleVector" (
    -- ... 标准列 ...
    "Embedding" vector(1536)
);
CREATE INDEX ON "Ai_ArticleVector" USING ivfflat ("Embedding" vector_cosine_ops) WITH (lists = 100);
```

#### 4.4.2 新增接口

```csharp
// Application.Contracts/IServices/IEmbeddingPipelineService.cs
public interface IEmbeddingPipelineService
{
    Task EmbedArticleAsync(string articleId, string content, string? modelId = null);
    Task DeleteArticleAsync(string articleId);
    Task<List<ArticleSearchResult>> SearchSimilarAsync(string query, int topK = 5, float threshold = 0.7f);
}

public record ArticleSearchResult(string ArticleId, int ChunkIndex, string ChunkText, float Score);
```

#### 4.4.3 EmbedArticleAsync 流程

```
1. 计算 content 的 SHA256 → ContentHash
2. 查询该 ArticleId 是否已有向量
   - 如果 ContentHash 相同 → 跳过（未变更）
   - 如果不同 → 删除旧向量
3. 分块：按段落分割，每块 ~512 tokens，10% 重叠窗口
4. 每块调用 ITextEmbeddingService.EmbeddingAsync() → 获取 float[]
5. 批量写入 Ai_ArticleVector 表
```

**分块策略**：按 `\n\n` 分割段落，token 数估算 = charCount / 4，超过 512 的段落进一步按句号分割。

#### 4.4.4 SearchSimilarAsync 流程

```
1. 调用 ITextEmbeddingService.EmbeddingAsync() 将 query 转为向量
2. PGVector 余弦相似度查询：
   SELECT * FROM "Ai_ArticleVector"
   ORDER BY "Embedding" <=> query_vector
   LIMIT topK
3. 过滤 Score >= threshold 的结果
4. 返回 ArticleSearchResult 列表
```

#### 4.4.5 新增服务

```csharp
// Application.Contracts/IServices/IAiSearchService.cs
public interface IAiSearchService
{
    Task<List<SemanticSearchResult>> SemanticSearchAsync(string query, int topK = 5);
}

// Application/Services/AiSearchService.cs
// 调用 EmbeddingPipelineService.SearchSimilarAsync()
// 可选的 LLM 重排序（第二阶段）
```

### 4.5 术语库

#### 4.5.1 新增实体

```csharp
[SugarTable("Ai_Glossary")]
public class AiGlossary : FullAuditedAggregateRoot<Guid>
{
    public string SourceLanguage { get; set; }       // "zh", "en", "ja" ...
    public string TargetLanguage { get; set; }
    public string SourceTerm { get; set; }
    public string TargetTerm { get; set; }
    public string? Category { get; set; }
    public bool IsEnabled { get; set; } = true;
    // 唯一约束：(SourceLanguage, TargetLanguage, SourceTerm)
}
```

#### 4.5.2 新增接口

```csharp
// Application.Contracts/IServices/IAiGlossaryService.cs
public interface IAiGlossaryService : IApplicationService
{
    Task<PagedResultDto<AiGlossaryDto>> GetListAsync(AiGlossaryGetListInput input);
    Task<AiGlossaryDto> GetAsync(Guid id);
    Task<AiGlossaryDto> CreateAsync(AiGlossaryCreateInput input);
    Task<AiGlossaryDto> UpdateAsync(Guid id, AiGlossaryUpdateInput input);
    Task DeleteAsync(Guid id);
    Task<Dictionary<string, string>> GetGlossaryMapAsync(string sourceLang, string targetLang);
}
```

#### 4.5.3 与 TranslateAsync 集成

在 `AiToolService.TranslateAsync()` 中，Phase 3 增强：

```csharp
// 在现有流程之前插入
var glossary = await _glossaryService.GetGlossaryMapAsync("zh", targetLang); // 自动检测源语言或从参数获取
if (glossary.Any())
{
    var glossaryText = string.Join("\n", glossary.Select(kv => $"- {kv.Key} → {kv.Value}"));
    systemPrompt += $"\n\nUse the following glossary for terminology consistency:\n{glossaryText}";
}
```

### 4.6 文章智能服务

#### 4.6.1 接口定义

```csharp
// Application.Contracts/IServices/IAiArticleService.cs
public interface IAiArticleService
{
    Task<string> SummarizeAsync(string content, int? maxLength = null, string? style = null);
    Task<string> GenerateIllustrationAsync(string content, string? style = null);
    Task<List<ArticleRecommendation>> RecommendRelatedAsync(string articleId, int topK = 5);
}

public record ArticleRecommendation(string ArticleId, string Title, float Score);
```

#### 4.6.2 SummarizeAsync

委托 `AiToolService.SummarizeAsync()`，额外支持 `maxLength` 和 `style` 参数注入 Prompt 变量。

#### 4.6.3 GenerateIllustrationAsync

```
流程：
1. 提取文章关键场景：
   - 调用 LLM（使用 Summarize prompt 的变体）→ 生成图片描述 prompt
2. 调用 AiImageService.GenerateAsync() 生成图片
3. 返回图片 URL
```

#### 4.6.4 RecommendRelatedAsync

```
流程：
1. 查询该 ArticleId 的 ArticleVector（所有 chunk）
2. 取第一个 chunk 的 Embedding 作为查询向量（通常首段包含文章主旨）
3. 调用 EmbeddingPipelineService.SearchSimilarAsync()
4. 排除自身 ArticleId
5. 返回 topK 结果
```

### 4.7 Phase 3 输出物

- [ ] `UserAiQuota` 实体 + 数据库表
- [ ] `AiQuotaService` 完整实现（检查/记录/重置/查询）
- [ ] `AiPrompt` 字段扩展 + DTO 同步 + `AiPromptManager.ResolveAsync()`
- [ ] `AiModel` 字段扩展 + DTO 同步
- [ ] `ArticleVector` 实体 + PGVector 表 + 索引
- [ ] `EmbeddingPipelineService`（分块/向量化/搜索）
- [ ] `AiGlossary` 实体 + CRUD + 翻译集成
- [ ] `AiArticleService`（摘要/配图/推荐）
- [ ] `AiSearchService.SemanticSearchAsync()`
- [ ] Cron job：配额重置（`ResetExpiredQuotasAsync`，每日 00:05 执行）
- [ ] 全量编译通过，所有新接口有可用实现

---

## 5. 实施顺序与依赖关系

### 5.1 依赖图

```
Phase 1（无依赖）
  └── Phase 2（依赖 Phase 1 完成）
        ├── AiToolService（无内部依赖）
        ├── AiGateWayManager 拆分（无内部依赖）
        ├── ChatManager Agent（依赖 AiGateWayManager 拆分后的稳定接口）
        └── Stub 补齐（各自独立）
              │
              └── Phase 3（依赖 Phase 2 完成）
                    ├── 用户配额（依赖 AiGateWayManager 流式调度）
                    ├── AiPrompt 增强（依赖 Phase 2 AiPromptManager）
                    ├── AiModel 元数据（无内部依赖）
                    ├── PGVector（依赖 ITextEmbeddingService）
                    ├── 术语库（独立，但集成到 TranslateAsync）
                    ├── AiArticleService（依赖 PGVector + AiToolService）
                    └── AiSearchService（依赖 PGVector）
```

### 5.2 建议执行顺序

```
Phase 1:
  1.1 [Obsolete] 清理（D1→D6）
  1.2 DTO 体系统一
  1.3 Bug 修复（B1→B3）

Phase 2:
  2.1 AiGateWayManager 拆分（先重构再补功能，降低回归风险）
  2.2 AiToolService 实现
  2.3 Stub 补齐（S1→S3）
  2.4 ChatManager Agent

Phase 3:
  3.1 AiModel 元数据扩展（独立，先做）
  3.2 AiPrompt 增强 + AiPromptManager
  3.3 用户配额系统
  3.4 术语库 + CRUD
  3.5 PGVector + EmbeddingPipelineService
  3.6 AiSearchService
  3.7 AiArticleService
  3.8 翻译集成术语库（依赖 3.4）
  3.9 Cron job 配置
```

### 5.3 时间估算（参考）

| Phase | 内容 | 估算工作量 |
|-------|------|-----------|
| 1 | 清理 + 修复 | 1-2 天 |
| 2 | 补齐核心功能 | 3-5 天 |
| 3 | 新增能力 | 5-8 天 |
| **合计** | | **9-15 天** |

---

## 6. 风险与注意事项

### 6.1 风险矩阵

| 风险 | 级别 | 缓解措施 |
|------|------|---------|
| `AiGateWayManager` 拆分引入回归 | 中 | 拆分前先在 Chat 和 Image 两个核心路径上做手动回归测试；流式输出行为对比 |
| ChatManager Agent 循环死循环 | 中 | 硬限制最大循环次数（10 轮）；每次循环后检查 Token 用量防止无限消耗 |
| PGVector 扩展安装需 DBA 权限 | 中 | 提前与运维确认；提供 SQL 脚本而非代码迁移 |
| Phase 3 做完后 LOB 模块接入量超预期 | 低 | 配额系统天然限制；监控 `AiUsage` 表和 API 调用量 |
| 旧 DTO 删除后前端未同步更新 | 低 | Phase 1 前检查前端代码引用；Phase 1 不改变 API 端点路径 |

### 6.2 不变保障

以下内容在全部三个 Phase 中 **保持不变量**：

- 所有现有 HTTP 端点路径不变（Phase 1 仅删除 `GET account/ai`，该端点已标记 `[Obsolete]`）
- `AiGateWayManager.UnifiedStreamForStatisticsAsync()` 的 SSE 输出格式不变
- Gateway 适配器的接口契约不变（`IChatCompletionService` 等）
- 数据库表名不变（Phase 3 仅新增表，不修改现有表结构）

### 6.3 编码规范

以下规范适用于所有新增和修改代码：

- **不写 XML 文档注释**（除 public API 接口方法外）
- **不写行注释解释 WHAT**，仅在逻辑非直观时写 WHY 注释
- **文件聚焦单一职责**，每个新建文件 ≤ 500 行
- **使用 Mapster** 进行对象映射（与现有 AiPromptService 一致）
- **使用 ThorJsonSerializer.DefaultOptions** 进行 JSON 序列化
- **委托而非重造**：新服务通过调用现有 Gateway 和 Manager 完成任务

---

> **下一步**：评审通过后，按 Phase 1 → Phase 2 → Phase 3 顺序进入实施。
> 每个 Phase 开始前，使用 `writing-plans` skill 创建该 Phase 的详细实施计划。
