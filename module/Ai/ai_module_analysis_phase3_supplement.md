# SharpFort.Ai 模块 — 补充需求分析（Phase 3 修正版）

> 生成日期：2026-05-11  
> 前置文档：`ai_module_analysis.md`  
> 触发条件：用户新增 3 项需求，需修正之前的精简/增强/补充清单

---

## 一、新增需求概述

| # | 需求 | 核心要点 |
|---|------|----------|
| 1 | 用户分级 AI 限额 | 未登录用户 → 登录用户 → VIP 用户，三级阶梯限额（次数+Token） |
| 2 | 限速防滥用 | 是否存在现有限速模块？AI 模块是否需要自建？ |
| 3 | 定时任务调用 AI API | 是否属于 AI 模块职责？ |

---

## 二、基础设施现状普查

### 2.1 限速模块

SharpFort **已有**完整的限速基础设施：

```csharp
// src/Sf.Abp.Web/SfAbpWebModule.cs (line 223-250)
service.AddRateLimiter(_ =>
{
    _.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    _.GlobalLimiter = PartitionedRateLimiter.CreateChained(
        PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            string userAgent = httpContext.Request.Headers.UserAgent.ToString();
            return RateLimitPartition.GetSlidingWindowLimiter(
                userAgent,
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 1000,   // 每窗口 1000 请求
                    Window = TimeSpan.FromSeconds(60),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        }),
        // ... 链式组合
    );
});

// 非开发环境启用
if (!env.IsDevelopment())
{
    app.UseRateLimiter();
}
```

**结论：** AI 模块**不需要自建限速模块**。SharpFort 已有基于 ASP.NET Core `System.Threading.RateLimiting` 的全局限速。AI 模块的职责是：
- 在 API 调用前**检查用户配额**（业务层限额，不是 HTTP 层限速）
- 复用全局限速中间件控制请求频率
- 如需更细粒度的 AI 端点限速（如 `/ai/chat` 单接口限制），可通过 `[EnableRateLimiting("policyName")]` 特性注册特定策略

### 2.2 AI 模块已有限流相关代码

```csharp
// ThorRateLimitException.cs — 处理上游 AI 供应商返回的 429
public class ThorRateLimitException : Exception { }

// 各 Gateway 实现中统一处理：
if (response.StatusCode == HttpStatusCode.TooManyRequests)
{
    throw new ThorRateLimitException();
}
```

这是**上游限流**（AI 供应商限我们），不是**下游限流**（我们限用户）。两者不冲突，都需要保留。

### 2.3 用户角色体系

SharpFort 使用 Casbin RBAC，通过 `UserRole` 管理角色。当前没有显式的"VIP"用户等级，但可以通过**角色**（如 `Free` / `Basic` / `Vip`）或**ABP Setting Management**来定义用户等级。

### 2.4 定时任务基础设施

SharpFort 已集成 **Hangfire**（SfAbpWebModule 中有 `app.UseAbpHangfireDashboard("/hangfire")`）。定时任务在其他模块中通过 Hangfire 的 `RecurringJob` 或 ABP 的 `IBackgroundJobManager` 调度。

---

## 三、对原分析结论的修正

### 3.1 精简清单修正

| 原结论 | 修正后 | 原因 |
|--------|--------|------|
| `Token` 实体 → **可选精简** | → **保留并增强** | 新增需求 #1 需要按用户维度统计配额消耗。`Token` 实体承载了 `UserId` → Token → `AiUsage` 的关联链。即使 Web 端不使用 Token，保留此实体可用于内部 API Key 管理 |
| `AiUsage` → 保留 | → 保留（优先级提升） | 配额系统的核心数据源，必须保留 |
| `UsageStatisticsService` → 保留 | → **保留并增强** | 需增加按时间窗口查询配额消耗的接口 |
| `AiBlacklist` → 保留 | → **重新评估** | 原用于封禁用户，可改造为"超额用户的临时限制"机制 |
| `UsageStatisticsManager` → 保留 | → **保留并增强** | 需在每次 API 调用前增加"配额检查"逻辑 |

### 3.2 增强清单修正

| # | 对象 | 原改造内容 | 新增改造内容 |
|---|------|-----------|-------------|
| 2 | `AiPrompt` | 增加分类/变量/语言字段 | 增加 `MinTier`（最低用户等级）、`MaxTokensPerCall`（单次调用上限） |
| 7 | `ChatMessage` | 增加 `Metadata` JSON 字段 | 增加 `IsCounted`（是否计入配额），便于区分系统消息和用户消息 |
| — | `AiUsage`（新） | — | 增加 `PeriodStart`（配额周期起始时间），支持按日/周/月重置配额 |
| — | `AiProvider`（新） | — | 增加 `TierConfig`（JSON，定义该供应商下各等级用户的限额） |

### 3.3 补充清单修正

| # | 原新增功能 | 修正 |
|---|-----------|------|
| — | 无 | **新增** `UserAiQuota` 配置系统 |
| — | 无 | **新增** AI 调用前置拦截器（配额检查 + 限速检查） |
| — | 无 | **新增** 用户等级解析服务 |
| — | 定时任务？ | **明确**：定时任务调用 AI API 由**业务模块**配置（Blog 模块配总结任务，CRM 模块配分析任务）。AI 模块只提供 `IAiChatService` / `IAiToolService` 等调用接口 |

---

## 四、新增功能详细设计

### 4.1 用户分级配额系统

#### 4.1.1 新增实体：`UserAiQuota`

```csharp
[SugarTable("Ai_UserQuota")]
public class UserAiQuota : FullAuditedAggregateRoot<Guid>
{
    /// <summary>用户ID（null = 匿名用户默认配额）</summary>
    public Guid? UserId { get; set; }

    /// <summary>用户等级（Free / Basic / Vip）</summary>
    public string Tier { get; set; }  // 或枚举 UserTier

    /// <summary>配额周期类型（Daily / Weekly / Monthly）</summary>
    public QuotaPeriod Period { get; set; }

    /// <summary>当前周期开始时间</summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>允许的最大调用次数</summary>
    public int MaxCalls { get; set; }

    /// <summary>允许的最大 Token 消耗</summary>
    public long MaxTokens { get; set; }

    /// <summary>已使用调用次数</summary>
    public int UsedCalls { get; set; }

    /// <summary>已使用 Token 数</summary>
    public long UsedTokens { get; set; }

    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;
}

public enum QuotaPeriod
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2
}
```

**设计说明：**
- 匿名用户：通过 IP 或 SessionId 标识，`UserId = null`
- 登录用户：`UserId` 绑定到具体用户
- VIP 用户：通过角色判断，配额从对应 `Tier` 的配置读取
- `UsedCalls` / `UsedTokens` 每次 API 调用后递增
- 周期到期自动重置（通过定时任务或懒检查）

#### 4.1.2 默认配额配置（可通过 ABP Setting Management 动态调整）

| 用户等级 | 日调用次数 | 日 Token 上限 | 适用场景 |
|----------|-----------|-------------|----------|
| Anonymous | 5 | 10,000 | 游客试玩 |
| Free | 50 | 100,000 | 注册用户 |
| Vip | 500 | 1,000,000 | 付费用户 |

#### 4.1.3 新增服务：`AiQuotaService`

```csharp
public class AiQuotaService : DomainService
{
    /// <summary>检查用户是否超出配额（调用前校验）</summary>
    Task<QuotaCheckResult> CheckQuotaAsync(Guid? userId, string modelId);

    /// <summary>记录一次 API 调用消耗</summary>
    Task RecordUsageAsync(Guid? userId, string modelId, ThorUsageResponse usage, Guid? tokenId);

    /// <summary>重置过期周期配额</summary>
    Task ResetExpiredQuotasAsync();

    /// <summary>获取用户当前配额剩余</summary>
    Task<UserQuotaRemaining> GetRemainingQuotaAsync(Guid? userId);
}

public class QuotaCheckResult
{
    public bool Allowed { get; set; }
    public string? RejectReason { get; set; }  // "日调用次数已达上限" / "日Token消耗已达上限"
    public int RemainingCalls { get; set; }
    public long RemainingTokens { get; set; }
}
```

#### 4.1.4 配额检查时机

配额检查应在 **AiGateWayManager 的所有 API 调用入口**处统一拦截，而非分散在各 Application Service 中。具体拦截点：

```
用户请求 → AI Controller (AiChatService/AiImageService/...) 
       → AiGateWayManager.UnifiedStreamForStatisticsAsync() 
       → [NEW] AiQuotaService.CheckQuotaAsync()  ← 在此检查
       → 通过 → 调用上游 AI API
       → 拒绝 → 返回 429 + 具体原因
```

### 4.2 限速策略

#### 4.2.1 分层限速设计

```
Layer 1（全局）: ASP.NET Core RateLimiter — 已有
  ├── 所有请求滑动窗口 1000/60s
  └── 在 SfAbpWebModule 中配置，非开发环境启用

Layer 2（AI 端点级）: 按用户等级的限速 — 需新增
  ├── 匿名用户：5 req/60s
  ├── Free 用户：20 req/60s
  └── Vip 用户：100 req/60s

Layer 3（业务配额级）: AiQuotaService — 新增
  ├── 日调用次数限制
  └── 日 Token 消耗限制
```

**实现建议：**

Layer 2 通过 ASP.NET Core 的 `AddRateLimiter` 添加 AI 专用策略：

```csharp
// 在 SfAbpWebModule 或 Ai 模块的 ConfigureServices 中
service.AddRateLimiter(options =>
{
    options.AddPolicy("ai-anonymous", context =>
        RateLimitPartition.GetSlidingWindowLimiter("anon",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(60),
                SegmentsPerWindow = 6
            }));

    options.AddPolicy("ai-free", context =>
        RateLimitPartition.GetSlidingWindowLimiter("free",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromSeconds(60),
                SegmentsPerWindow = 6
            }));

    options.AddPolicy("ai-vip", context =>
        RateLimitPartition.GetSlidingWindowLimiter("vip",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromSeconds(60),
                SegmentsPerWindow = 6
            }));
});
```

然后在 AI 的 Controller 上使用 `[EnableRateLimiting("ai-free")]` 或动态选择策略。

**注意：** Layer 2 和 Layer 3 是互补关系，不是替代。Layer 2 控制**请求频率**（太快了不行），Layer 3 控制**总量**（用多了不行）。

### 4.3 定时任务定位

| 定时任务 | 所属模块 | 说明 |
|----------|----------|------|
| 文章自动总结 | Blog/CMS 模块 | 业务逻辑：何时总结、总结哪些文章 |
| 文章自动翻译 | Blog/CMS 模块 | 业务逻辑：目标语言、触发条件 |
| 配额周期重置 | **AI 模块** | 这是 AI 基础设施职责，重置 `UserAiQuota.PeriodStart` |
| 文章向量化 | Blog/CMS 模块 | 何时向量化由内容管理决定 |
| 用量统计清理 | **AI 模块** | 清理过期 `AiUsage` 数据 |

**结论：** 与 AI 基础设施直接相关的定时任务（配额重置、数据清理）放在 AI 模块。与业务场景相关的定时任务（文章总结、翻译）由业务模块通过 Hangfire 调度，调用 AI 模块的 Service 接口。

---

## 五、修正后的三阶段路线图

### Phase 1 — 精简（不变）

```
1. 删除冗余实体/枚举/服务/DTO
2. 合并 AgentStoreAggregateRoot 与 AgentStore
3. 移除 ChannelService + 相关 DTO
4. 移除 OpenApiService 对外端点
5. 移除 MCP Tools 目录
6. 清理模块依赖注册
```

### Phase 2 — 增强（新增第10~13步）

```
7.  AiPrompt 增加分类/变量/语言字段 + MinTier/MaxTokensPerCall
8.  AiModel 增加能力标记字段
9.  AiProvider.ApiKey 加密存储
10. [NEW] AiUsage 增加 PeriodStart 字段，支持按周期统计
11. [NEW] ChatMessage 增加 IsCounted 字段
12. [NEW] 实现 AiToolService（SummarizeAsync / TranslateAsync / SearchAsync）
13. [NEW] AiProvider 增加 TierConfig JSON 字段
```

### Phase 3 — 补充（新增第14~21步，调整原计划）

```
14. [NEW] 新建 AiQuota 实体 + AiQuotaService（用户分级配额）
15. [NEW] 实现 AiGateWayManager 中的配额检查拦截器
16. [NEW] 配置 AI 端点分层限速策略（Layer 2 + Layer 3）
17. [NEW] 配额周期重置定时任务（Hangfire RecurringJob）
18. [延续] 新建 AiGlossary 实体 + 词库管理服务
19. [延续] PGVector + ArticleVector + ArticleEmbeddingPipeline
20. [延续] AiSearchService（语义搜索）
21. [延续] TranslationPipelineService（翻译管道）
22. [延续] ArticleSummarizeService / ArticleIllustrationService / AiRecommendService
```

---

## 六、修正后的最终目标架构

### 实体（目标 10~12 个）

```
AiProvider          — 供应商管理            需求1
AiModel             — 模型管理              需求1
AiPrompt            — 提示词管理             需求3
ChatSession         — 会话                  需求4/6/8
ChatMessage         — 消息                  需求4/6/8
AiUsage             — 用量统计（增强）        需求1(新增)
UserAiQuota         — [NEW] 用户配额          需求1(新增)  
AiGlossary          — [NEW] 翻译词库          需求7/10
ArticleVector       — [NEW] 文章向量          需求9
ImageStoreTask      — 精简版图片任务          需求5
Token               — API密钥（保留）          需求1
UserTierConfig      — [NEW] 等级限额配置       需求1(新增)
```

### 应用服务（目标 13~15 个）

```
AiProviderService          — 供应商 CRUD
AiModelService             — 模型 CRUD
AiPromptService            — 提示词 CRUD
AiChatService              — 核心对话
AiImageService             — 绘图（精简）
AiQuotaService             — [NEW] 配额管理
AiGlossaryService          — [NEW] 词库 CRUD
AiSearchService            — [NEW] 语义搜索
AiRecommendService         — [NEW] 文章推荐
ArticleSummarizeService    — [NEW] 文章总结
ArticleIllustrationService — [NEW] 文章配图
TranslationPipelineService — [NEW] 翻译管道
UsageStatisticsService      — 用量统计（增强）
TokenService                — Token 管理（保留）
```

---

## 七、三问直接回答

### Q1: 用户分级限额是否需要 Token 统计？

**需要。** 但有两层：
1. **AiUsage**（已有）— 细粒度统计，按 用户+模型+Token 三维度记录每次调用的 Token 消耗
2. **UserAiQuota**（新建）— 粗粒度限额，按 用户+周期 维度聚合，是配额判断的数据源

两者关系：每次 API 调用 → 写入 AiUsage（细）→ 更新 UserAiQuota.UsedTokens（粗）→ 下次调用前检查 UserAiQuota 是否超限

### Q2: 限速需要 AI 模块自建吗？

**不需要。** SharpFort 已有 `System.Threading.RateLimiting` 全局限速。AI 模块需要做的是：
- 注册 AI 专用的限速策略（匿名/Free/VIP 不同速率）
- 在 Controller 或中间件中应用这些策略
- 在业务层实现**配额检查**（次数/Token 总量限制，区别于 HTTP 速率限制）

一句话区分：**限速 = 每秒最多几次请求（HTTP 层）；配额 = 每天最多用多少 Token（业务层）。**

### Q3: 定时任务放哪个模块？

| 放在 AI 模块 | 放在业务模块（Blog/CRM） |
|-------------|------------------------|
| 配额周期重置 | 文章自动总结 |
| 用量数据清理 | 文章自动翻译 |
| 过期 Token/Blacklist 清理 | 文章自动配图 |
| — | 文章定时向量化 |

AI 模块提供的是**被调用的能力**（Service 接口），业务模块通过 Hangfire 调度这些能力。

---

## 八、与原分析文档的关系

本文档是 `ai_module_analysis.md` 的补充和修正：

- `ai_module_analysis.md` 第三章"精简清单"中关于 `Token` 和 `AiUsage` 的结论以本文档 3.1 节为准
- `ai_module_analysis.md` 第四章"增强清单"已更新为本文档 3.2 节
- `ai_module_analysis.md` 第五章"补充清单"已更新为本文档 3.3 节
- `ai_module_analysis.md` 第六章"重构路线图"以本文档第五章为准

**建议:** 两个文档配合阅读。先看 `ai_module_analysis.md` 了解全貌和精简方针，再看本文档了解配额和限速的具体设计。
