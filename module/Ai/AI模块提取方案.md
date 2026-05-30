# AI 模块提取方案

> 目标：将 NetCoreKevin-master 项目的完整 AI 模块移植到你的 DDD 分层项目中
> 策略：按依赖关系从底层到顶层，分6个阶段逐步提取

---

## 提取策略总览

```
阶段1: 独立框架模块 (零业务依赖，可直接复用)
  └─ kevin.AI.AgentFramework 项目整体

阶段2: 数据模型层 (Domain Entities + DTOs + Enums)
  └─ 10个实体 + 9个DTO + 2个枚举

阶段3: 领域接口层 (IServices + IRepositories)
  └─ 10个服务接口 + 10个仓储接口

阶段4: 业务服务层 (Application Services)
  └─ 11个服务实现（依赖你的项目基础设施）

阶段5: Web API 层 (Controllers)
  └─ 8个控制器

阶段6: 基础设施层 (EF Core Config + DI + 种子数据)
  └─ 2个EF配置 + DI注册 + 种子数据
```

---

## 阶段1：独立框架模块 `kevin.AI.AgentFramework`

### 移植方式：整体复制为独立 Class Library 项目

这是最核心、最独立的部分，几乎零业务依赖，可直接作为 NuGet 包或项目引用集成到你的项目中。

### 需要复制的文件清单

```
[你的项目]\src\[YourProject].AI.AgentFramework\
│
├── [YourProject].AI.AgentFramework.csproj    ← 从 kevin.AI.AgentFramework.csproj 重命名
├── AIAgentService.cs                         ← 核心：AI代理服务
├── AISetting.cs                              ← AI请求配置POCO
├── ServiceCollectionExtensions.cs            ← DI扩展方法
│
├── Const\
│   ├── SystemPrompt.cs                       ← 智能体统一规则提示词
│   └── SysTools.cs                           ← 系统内置工具注册表
│
├── Dto\
│   └── ChatHistoryItemDto.cs                 ← 聊天历史项DTO
│
├── Interfaces\
│   ├── IAIAgentService.cs                    ← AI代理服务接口
│   ├── IAIAgentToolSkillService.cs           ← 技能/工具装配接口
│   ├── IBaseAIToolService.cs                 ← 基础工具初始化接口
│   ├── IKevinAIChatMessageStore.cs           ← 消息存储接口
│   └── Tasks\
│       └── IKevinAITaskService.cs            ← 定时任务服务接口
│
├── Agent\
│   └── KevinChatMessageStore\
│       └── KevinChatMessageStore.cs          ← 聊天历史上下文提供者
│
├── Tools\
│   ├── CommonTools.cs                        ← 常用工具
│   ├── ShellTools.cs                         ← Shell执行工具
│   ├── PythonTools.cs                        ← Python执行工具
│   ├── AgentHttpClientTools.cs               ← HTTP客户端工具
│   └── HttpClientFunction.cs                 ← 搜索引擎工具
│
├── SkillClass\
│   ├── GetWeatherSkill.cs                    ← 天气查询技能示例
│   └── UnitConverterSkill.cs                 ← 单位转换技能示例
│
├── ScriptRunners\
│   └── PySubprocessScriptRunner.cs           ← Python子进程脚本运行器
│
├── Skills\                                   ← 文件型技能示例（可选）
│   ├── expense-report\
│   │   └── expense-report\
│   │       ├── SKILL.md
│   │       ├── assets\expense-report-template.md
│   │       └── references\POLICY_FAQ.md
│   └── system-ops\
│       └── system-ops\
│           ├── SKILL.md
│           ├── assets\template.md
│           ├── references\troubleshooting-guide.md
│           └── scripts\
│               ├── check-disk-usage.ps1
│               ├── check-system-info.ps1
│               └── check-top-processes.ps1
│
└── WorkFlows\
    └── WorkFlowsAndAIAgentsDemo.cs           ← 工作流示例（可选）
```

### NuGet 依赖（需在 .csproj 中添加）

```xml
<PackageReference Include="Microsoft.Agents.AI" Version="*" />
<PackageReference Include="Microsoft.Extensions.AI" Version="*" />
<PackageReference Include="OpenAI" Version="*" />
<PackageReference Include="ModelContextProtocol.Server" Version="*" />
<PackageReference Include="HttpMataki.NET.Auto" Version="*" />
```

### 需要适配的内容

| 原内容 | 适配方式 |
|--------|---------|
| `namespace kevin.AI.AgentFramework` | 改为你的命名空间如 `YourProject.AI.AgentFramework` |
| `using Kevin.AI.Dto` (AISetting引用) | AISetting.cs已在本模块内，改为内部引用 |
| `using Kevin.AI.Dto` (ChatHistoryItemDto引用) | ChatHistoryItemDto.cs已在本模块内 |
| `using kevin.Domain.Share.Dtos.AI` (IAIAgentToolSkillService的AITool引用) | 改为引用 `Microsoft.Extensions.AI.AITool` |

### 关键适配点说明

`IAIAgentToolSkillService` 和 `IAIAgentService` 接口中引用了 `Kevin.AI.Dto.AISetting`。
`AISetting` 类已在模块内的 `AISetting.cs` 中定义，需要将外部引用改为模块内部引用：

- `IAIAgentService.cs` 中的 `using Kevin.AI.Dto;` → `using YourProject.AI.AgentFramework;` (AISetting在本模块内)
- `AIAgentService.cs` 中的 `using Kevin.AI.Dto;` → 同上
- `HttpClientFunction.cs` 中的 `using Kevin.AI.Dto;` → 同上

---

## 阶段2：数据模型层

### 移植方式：按文件逐个复制，调整基类和命名空间

### 2a. 共享枚举（优先，无依赖）

```
[你的项目]\src\[YourProject].Share\Enums\
├── AIType.cs                ← AI平台类型枚举
└── AISkillToolTypeEnums.cs  ← 技能/工具类型枚举
```

**AIType.cs 枚举值：**
```csharp
public enum AIType
{
    OpenAI = 1,
    AzureOpenAI = 2,
    ZhiPuAI = 3,
    BgeEmbedding = 7,
    BgeRerank = 8,
    Ollama = 10,
    OllamaEmbedding = 11
}

public enum AIModelType
{
    Chat = 1,
    Embedding = 2,
    Rerank = 4
}
```

**适配**：命名空间从 `kevin.Domain.Share.Enums` → 你的枚举命名空间

### 2b. 领域实体（10个文件）

```
[你的项目]\src\[YourProject].Domain\Entities\AI\
├── TAIApps.cs
├── TAIChats.cs
├── TAIChatHistorys.cs
├── TAIChatMessageStore.cs
├── TAIKmss.cs
├── TAIKmsDetails.cs
├── TAIModels.cs
├── TAIPrompts.cs
├── TAISkillToolManagement.cs
└── TAISkillToolBindId.cs
```

**依赖基类替换（关键适配）：**

| 原基类 | 含义 | 适配方式 |
|--------|------|---------|
| `CUD_User` | CreateUserId + UpdateUserId + DeleteUserId + TenantId + 软删除 | 替换为你项目的审计基类 |
| `CD_User` | CreateUserId + DeleteUserId + TenantId + 软删除（无UpdateUserId） | 替换为你项目的审计基类 |
| `ITenant` | 多租户接口 | 替换为你项目的多租户接口 |

**实体间导航属性关系：**
- `TAIApps` → `TAIPrompts` (AIPromptID), `TAIKmss` (KmsId), `TAIModels` (ChatModelID/RerankModelID)
- `TAIChats` → `TAIApps` (AppId), `TUser` (UserId), `List<TAIChatHistorys>`
- `TAIChatHistorys` → `TAIChats` (AIChatsId)
- `TAIKmss` → `TAIModels` (aIModelsId)
- `TAIKmsDetails` → `TAIKmss` (KmsId), `TFile` (FileId)
- `TAISkillToolBindId` → `TAISkillToolManagement` (AISkillToolManagementId)

如果不需要完整的导航属性，可以先去掉 `virtual` 导航属性，保持外键Id即可。

### 2c. 共享 DTO（9个文件）

```
[你的项目]\src\[YourProject].Share\Dtos\AI\
├── AIAppsDto.cs
├── AIChatsDto.cs
├── AIChatHistorysDto.cs
├── AIKmsDetailsDto.cs
├── AIKmssDto.cs
├── AIModelsDto.cs
├── AIPromptsDto.cs
├── AISkillToolManagementDto.cs
└── AIAppsBindSkillToolsDto.cs
```

**依赖替换：**

| 原基类 | 适配方式 |
|--------|---------|
| `CUD_User_Dto` | 替换为你项目的DTO基类（含 Id/CreateTime/CreateUserId 等字段） |
| `dtoPagePar<T>` | 替换为你项目的分页请求基类 |
| `dtoPageData<T>` | 替换为你项目的分页结果基类 |
| `dtoPageList<T>` | 替换为你项目的列表结果基类 |
| `FileDto` | 替换为你项目的文件DTO |
| `FieldValidationException` | 替换为你项目的验证异常类 |

---

## 阶段3：领域接口层

### 3a. 服务接口（IServices）

```
[你的项目]\src\[YourProject].Domain\Interfaces\IServices\AI\
├── IAIAppsService.cs
├── IAIChatsService.cs
├── IAIChatHistorysService.cs
├── IAIChatMessageStoreService.cs
├── IAIKmssService.cs
├── IAIKmsDetailsService.cs
├── IAIModelsService.cs
├── IAIPromptsService.cs
├── IAISkillToolManagementService.cs
└── IAISkillToolBindIdService.cs
```

**依赖替换：**
- `IBaseService` → 你的基础服务接口
- 返回值中的 `dtoPageData<T>` / `dtoPageList<T>` / `dtoPagePar<T>` → 你的分页类型

### 3b. 仓储接口（IRepositories）

```
[你的项目]\src\[YourProject].Domain\Interfaces\IRepositories\AI\
├── IAIAppsRp.cs
├── IAIChatsRp.cs
├── IAIChatHistorysRp.cs
├── IAIChatMessageStoreRp.cs
├── IAIKmssRp.cs
├── IAIKmsDetailsRp.cs
├── IAIModelsRp.cs
├── IAIPromptsRp.cs
├── IAISkillToolManagementRp.cs
└── IAISkillToolBindIdRp.cs
```

**依赖替换：** `IRepository<T>` / `IBaseRepository<T>` → 你的仓储基接口（需支持 Query()、Add()、AddRange()、SaveChangesAsync() 等基础方法）

---

## 阶段4：业务服务层（最复杂，依赖你的基础设施）

### 文件清单

```
[你的项目]\src\[YourProject].Application\Services\AI\
├── AIAppsService.cs
├── AIChatsService.cs
├── AIChatHistorysService.cs
├── AIKmssService.cs
├── AIModelsService.cs
├── AIPromptsService.cs
├── AISkillToolManagementService.cs
├── AISkillToolBindIdService.cs
├── AIAgentToolSkillService.cs          ← 核心：技能/工具装配
├── KevinAIChatMessageStore.cs          ← 核心：消息持久化
└── KevinAITasksService.cs              ← 核心：定时任务
```

### 基础设施依赖清单（需要适配）

| 原依赖 | 用途 | 适配建议 |
|--------|------|---------|
| `BaseService` | 基础服务（含 CurrentUser、HttpContext等） | 替换为你项目的BaseService |
| `IHttpContextAccessor` | 获取当前请求上下文 | ASP.NET Core 内置，直接可用 |
| `SnowflakeIdService.GetNextId()` | 分布式雪花ID生成 | 替换为你项目的ID生成策略 |
| `CurrentUser.UserId` | 当前登录用户ID | 替换为你项目的用户上下文获取方式 |
| `CurrentUser.TenantId` | 当前租户ID | 如果是单租户项目，可固定值或移除 |
| `MapTo<T>()` / `MapToList<T1, T2>()` | 对象映射（如AutoMapper） | 替换为你项目的映射方式 |
| `UserFriendlyException` | 用户友好异常 | 替换为你项目的异常类 |
| `IRecurringJobManager` (Hangfire) | 定时任务管理 | 仅 `KevinAITasksService` 需要。如不需要AI自主定时功能，此服务可略过 |
| `JobStorage` (Hangfire) | 任务存储 | 同上，仅定时任务需要 |
| `IDistributedLockProvider` | 分布式锁 | 仅定时任务防并发执行需要 |
| `IMessageService` | 站内消息推送 | 仅定时任务需要（结果推送）。可替换为你的通知服务 |
| `ISignalRMsgService` | SignalR实时推送 | 仅 `AIChatsService` 流式输出需要。可替换为你的实时通信方案 |

### 按功能模块的依赖复杂度排序（从简单到复杂）

1. **AIPromptsService** — 最简单，纯CRUD，只需 BaseService + IRepository
2. **AIModelsService** — 简单CRUD
3. **AISkillToolManagementService** — 简单CRUD + 技能/工具分类查询
4. **AISkillToolBindIdService** — 简单CRUD + 批量绑定
5. **AIAppsService** — CRUD + 技能/工具绑定关联
6. **AIChatHistorysService** — CRUD + 分页
7. **AIKmssService** — CRUD + 知识库文档处理协调
8. **AIAppsService(NewInitialization)** — 智能体初始化（聚合多个服务）
9. **AIAgentToolSkillService** — **核心装配**，依赖 IAISkillToolBindIdService + IAISkillToolManagementService + IKevinAITaskService
10. **KevinAIChatMessageStore** — 消息持久化，依赖 IAIChatMessageStoreRp
11. **AIChatsService** — **创建对话核心流程**，依赖最多：AIAppsService + AIModelsService + AIPromptsService + AIAgentToolSkillService + KevinAIChatMessageStore + SignalR
12. **KevinAITasksService** — **定时任务**，依赖最重：Hangfire + DistributedLock + MessageService + AIAgentService + 多个Repository

### 适配建议

如果你的项目使用不同的ORM（如 SqlSugar），需要重写 Repository 层。但 Entity 和 DTO 层的结构可以保持不变。

如果你的项目不需要以下功能，可以暂时跳过对应服务：
- 不需要**定时任务** → 跳过 `KevinAITasksService`，`IKevinAITaskService` 接口仅保留但不实现
- 不需要**知识库/RAG** → 跳过 `AIKmssService` + `AIKmsDetails` 相关
- 不需要**站内消息推送** → `KevinAITasksService` 中移除消息推送代码
- 不需要**SignalR实时推送** → `AIChatsService` 中移除 SignalR 相关代码

---

## 阶段5：Web API 控制器层

### 文件清单

```
[你的项目]\src\[YourProject].WebApi\Controllers\AI\
├── AIAppsController.cs
├── AIChatsController.cs
├── AIChatHistorysController.cs
├── AIKmssController.cs
├── AIModelsController.cs
├── AIPromptsController.cs
├── AISkillToolManagementController.cs
└── AITasksController.cs
```

### 依赖适配

| 原依赖 | 适配方式 |
|--------|---------|
| `[MyArea]` / `[MyModule]` | 替换为你项目的权限/模块注解 |
| `[ActionDescription]` | 可选权限描述，可移除 |
| `[HttpLog]` | 审计日志注解，可替换为你项目的审计方案 |
| `[SkipAuthority]` | 跳过权限验证，替换为你项目的匿名访问注解 |
| `[CacheDataFilter]` | 缓存过滤器，可替换或移除 |
| `[Transactional]` | 事务注解，替换为你项目的事务方案 |
| `[Authorize]` | ASP.NET Core 内置，直接可用 |
| `[ApiController]` | ASP.NET Core 内置，直接可用 |
| `ControllerBase` | ASP.NET Core 内置，直接可用 |

---

## 阶段6：基础设施层

### 6a. EF Core 实体配置

```
[你的项目]\src\[YourProject].EntityFrameworkCore\Configuration\
├── TAIPromptsConfig.cs
└── TAISkillToolManagementConfig.cs
```

其他8个实体使用 EF Core Convention 自动映射，无需额外配置。如需精细控制，按需创建。

### 6b. 数据库上下文注册

在你的 `DbContext` 中添加：

```csharp
public DbSet<TAIApps> TAIApps { get; set; }
public DbSet<TAIChats> TAIChats { get; set; }
public DbSet<TAIChatHistorys> TAIChatHistorys { get; set; }
public DbSet<TAIChatMessageStore> TAIChatMessageStore { get; set; }
public DbSet<TAIKmss> TAIKmss { get; set; }
public DbSet<TAIKmsDetails> TAIKmsDetails { get; set; }
public DbSet<TAIModels> TAIModels { get; set; }
public DbSet<TAIPrompts> TAIPrompts { get; set; }
public DbSet<TAISkillToolManagement> TAISkillToolManagement { get; set; }
public DbSet<TAISkillToolBindId> TAISkillToolBindId { get; set; }
```

### 6c. DI 注册

在你的 `ServiceConfiguration.cs` 或 `Program.cs` 中添加：

```csharp
// 1. Agent框架核心
services.AddAIAgentClient();     // 注册 IAIAgentService
// 可选：services.AddKevinMCPServer();  // MCP服务

// 2. 业务服务（按需注册）
services.AddScoped<IAIAppsService, AIAppsService>();
services.AddScoped<IAIChatsService, AIChatsService>();
services.AddScoped<IAIChatHistorysService, AIChatHistorysService>();
services.AddScoped<IAIKmssService, AIKmssService>();
services.AddScoped<IAIModelsService, AIModelsService>();
services.AddScoped<IAIPromptsService, AIPromptsService>();
services.AddScoped<IAISkillToolManagementService, AISkillToolManagementService>();
services.AddScoped<IAISkillToolBindIdService, AISkillToolBindIdService>();
services.AddScoped<IAIAgentToolSkillService, AIAgentToolSkillService>();
services.AddScoped<IKevinAIChatMessageStore, KevinAIChatMessageStore>();
// 可选：services.AddScoped<IKevinAITaskService, KevinAITasksService>();

// 3. 仓储
services.AddScoped<IAIAppsRp, AIAppsRp>();
// ... 其余9个仓储
```

### 6d. 种子数据（可选）

```
[你的项目]\src\[YourProject].Domain\BaseDatas\
├── TAIPromptsBaseDatas.cs              ← 预置提示词
└── TAISkillToolManagementBaseDatas.cs  ← 预置技能/工具
```

在数据初始化时调用这些种子数据填充默认的提示词和系统内置工具列表。

---

## MCP Server 模块（可选独立模块）

如果需要在 AI 模块中支持 MCP 协议（为外部 AI 客户端提供标准化工具调用接口）：

```
[你的项目]\src\[YourProject].AI.MCP.Server\
├── [YourProject].AI.MCP.Server.csproj
├── ServiceCollectionExtensions.cs
├── Client\
│   ├── IMySSEToolClient.cs
│   └── MySSEToolClient.cs
├── Models\
│   └── MCPSSEClientSetting.cs
└── Tools\
    └── MyTool.cs
```

此模块独立于核心 AI 功能，可按需移植。

---

## 快速启动建议（最小可用子集）

如果希望快速看到效果，建议按以下最小子集先跑通核心流程：

### 第一轮（核心对话）

1. ✅ 阶段1全部：`kevin.AI.AgentFramework` 独立模块
2. ✅ 枚举：`AIType.cs` + `AISkillToolTypeEnums.cs`
3. ✅ 实体（最小集）：`TAIModels.cs` + `TAIPrompts.cs` + `TAIApps.cs` + `TAIChats.cs` + `TAIChatHistorys.cs`
4. ✅ DTO（最小集）：对应实体的5个DTO
5. ✅ 接口（最小集）：`IAIModelsService` + `IAIPromptsService` + `IAIAppsService` + `IAIChatsService` + `IAIChatHistorysService`
6. ✅ 仓储接口：对应5个Rp接口
7. ✅ 服务实现：`AIModelsService` + `AIPromptsService` + `AIAppsService` + `AIChatsService` + `AIChatHistorysService`
8. ✅ 仓储实现：对应5个Rp实现
9. ✅ 控制器：`AIModelsController` + `AIPromptsController` + `AIAppsController` + `AIChatsController` + `AIChatHistorysController`
10. ✅ DI注册 + DbContext配置

跑通后得到：**创建AI模型配置 → 创建提示词 → 创建AI应用 → 发起对话 → AI回复** 的完整链路。

### 第二轮（技能/工具系统）

11. ✅ 实体：`TAISkillToolManagement.cs` + `TAISkillToolBindId.cs`
12. ✅ 对应接口、服务、仓储、控制器
13. ✅ `AIAgentToolSkillService.cs`

### 第三轮（高级功能）

14. ✅ 知识库：`TAIKmss.cs` + `TAIKmsDetails.cs` + `AIKmssService.cs`（需要向量数据库）
15. ✅ 定时任务：`KevinAITasksService.cs`（需要 Hangfire）
16. ✅ 消息存储：`TAIChatMessageStore.cs` + `KevinAIChatMessageStore.cs`

---

## 关键注意事项

1. **`BaseService` 替换**：所有 Application Service 继承 `BaseService`，需要替换为你项目的基类，确保 `CurrentUser`、`_httpContextAccessor` 等可用。

2. **`SnowflakeIdService.GetNextId()`**：分布式ID生成器，可替换为 Guid、自增ID 或你自己的雪花ID实现。

3. **多租户**：如果你的项目是单租户，可以忽略所有 `TenantId` 相关的过滤和赋值逻辑。

4. **`MapTo<T>()` / `MapToList<T1, T2>()`**：实体与DTO之间的映射方法，如果是 AutoMapper 的扩展，直接安装 AutoMapper 并配置 Profile；如果是自定义扩展，需一并移植。

5. **文件型技能(Skills)路径**：`Skills\` 目录下的技能文件需要复制到你的项目的输出目录（设置 `Copy to Output Directory`）。

6. **Python依赖**：`PythonTools` 和 `PySubprocessScriptRunner` 需要运行环境安装 Python 3.x 并配置 PATH。

7. **向量数据库**：知识库 RAG 功能依赖 Qdrant 向量数据库（代码中引用了 `Qdrant.Client`），需要单独部署。
