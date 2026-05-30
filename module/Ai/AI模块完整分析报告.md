# AI 模块完整分析报告

> 项目：NetCoreKevin-master
> 分析范围：Kevin\Domain\Entities\AI、Kevin\Application\Services\AI、Kevin\Domain\Interfaces\IServices\AI 及所有关联模块
> 分析日期：2026-05-23

---

## 一、AI 模块完整文件路径清单

### 1. 领域实体层 (Domain Entities)
```
Kevin\Domain\Entities\AI\TAIApps.cs                    -- AI应用配置表实体
Kevin\Domain\Entities\AI\TAIChats.cs                   -- AI对话记录表实体
Kevin\Domain\Entities\AI\TAIChatHistorys.cs            -- AI聊天消息记录表实体
Kevin\Domain\Entities\AI\TAIChatMessageStore.cs        -- AI消息存储（框架消息持久化）表实体
Kevin\Domain\Entities\AI\TAIKmss.cs                    -- AI知识库表实体
Kevin\Domain\Entities\AI\TAIKmsDetails.cs              -- AI知识库文档详情表实体
Kevin\Domain\Entities\AI\TAIModels.cs                  -- AI模型配置表实体
Kevin\Domain\Entities\AI\TAIPrompts.cs                 -- AI提示词配置表实体
Kevin\Domain\Entities\AI\TAISkillToolManagement.cs     -- AI技能/工具注册管理表实体
Kevin\Domain\Entities\AI\TAISkillToolBindId.cs         -- AI技能/工具绑定关系表实体
```

### 2. 领域种子数据 (BaseDatas)
```
Kevin\Domain\BaseDatas\TAIPromptsBaseDatas.cs          -- 提示词种子数据
Kevin\Domain\BaseDatas\TAISkillToolManagementBaseDatas.cs -- 技能/工具种子数据
```

### 3. 服务接口层 (IServices)
```
Kevin\Domain\Interfaces\IServices\AI\IAIAppsService.cs
Kevin\Domain\Interfaces\IServices\AI\IAIChatsService.cs
Kevin\Domain\Interfaces\IServices\AI\IAIChatHistorysService.cs
Kevin\Domain\Interfaces\IServices\AI\IAIChatMessageStoreService.cs
Kevin\Domain\Interfaces\IServices\AI\IAIKmssService.cs
Kevin\Domain\Interfaces\IServices\AI\IAIKmsDetailsService.cs
Kevin\Domain\Interfaces\IServices\AI\IAIModelsService.cs
Kevin\Domain\Interfaces\IServices\AI\IAIPromptsService.cs
Kevin\Domain\Interfaces\IServices\AI\IAISkillToolManagementService.cs
Kevin\Domain\Interfaces\IServices\AI\IAISkillToolBindIdService.cs
```

### 4. 仓储接口层 (IRepositories)
```
Kevin\Domain\Interfaces\IRepositories\AI\IAIAppsRp.cs
Kevin\Domain\Interfaces\IRepositories\AI\IAIChatsRp.cs
Kevin\Domain\Interfaces\IRepositories\AI\IAIChatHistorysRp.cs
Kevin\Domain\Interfaces\IRepositories\AI\IAIChatMessageStoreRp.cs
Kevin\Domain\Interfaces\IRepositories\AI\IAIKmssRp.cs
Kevin\Domain\Interfaces\IRepositories\AI\IAIKmsDetailsRp.cs
Kevin\Domain\Interfaces\IRepositories\AI\IAIModelsRp.cs
Kevin\Domain\Interfaces\IRepositories\AI\IAIPromptsRp.cs
Kevin\Domain\Interfaces\IRepositories\AI\IAISkillToolManagementRp.cs
Kevin\Domain\Interfaces\IRepositories\AI\IAISkillToolBindIdRp.cs
```

### 5. 应用服务实现层 (Application Services)
```
Kevin\Application\Services\AI\AIAppsService.cs               -- AI应用管理服务
Kevin\Application\Services\AI\AIChatsService.cs              -- AI对话管理服务（核心：创建对话、加载模型/提示词/技能/工具）
Kevin\Application\Services\AI\AIChatHistorysService.cs       -- AI聊天记录管理服务
Kevin\Application\Services\AI\AIKmssService.cs               -- AI知识库管理服务
Kevin\Application\Services\AI\AIModelsService.cs             -- AI模型配置管理服务
Kevin\Application\Services\AI\AIPromptsService.cs            -- AI提示词配置管理服务
Kevin\Application\Services\AI\AISkillToolManagementService.cs -- AI技能/工具注册管理服务
Kevin\Application\Services\AI\AISkillToolBindIdService.cs    -- AI技能/工具绑定关系服务
Kevin\Application\Services\AI\AIAgentToolSkillService.cs     -- AI智能体技能工具装配服务（核心）
Kevin\Application\Services\AI\KevinAIChatMessageStore.cs     -- AI消息持久化存储服务
Kevin\Application\Services\AI\KevinAITasksService.cs         -- AI定时任务执行服务（核心）
```

### 6. 仓储实现层 (Repository Implementations)
```
Kevin\RepositorieRps\Repositories\AI\AIAppsRp.cs
Kevin\RepositorieRps\Repositories\AI\AIChatHistorysRp.cs
Kevin\RepositorieRps\Repositories\AI\AIChatMessageStoreRp.cs
Kevin\RepositorieRps\Repositories\AI\AIChatsRp.cs
Kevin\RepositorieRps\Repositories\AI\AIKmsDetailsRp.cs
Kevin\RepositorieRps\Repositories\AI\AIKmssRp.cs
Kevin\RepositorieRps\Repositories\AI\AIModelsRp.cs
Kevin\RepositorieRps\Repositories\AI\AIPromptsRp.cs
Kevin\RepositorieRps\Repositories\AI\AISkillToolBindIdRp.cs
Kevin\RepositorieRps\Repositories\AI\AISkillToolManagementRp.cs
```

### 7. 共享 DTO 层 (Shared DTOs)
```
Kevin\kevin.Share\Dtos\AI\AIAppsDto.cs
Kevin\kevin.Share\Dtos\AI\AIChatsDto.cs
Kevin\kevin.Share\Dtos\AI\AIChatHistorysDto.cs
Kevin\kevin.Share\Dtos\AI\AIKmsDetailsDto.cs
Kevin\kevin.Share\Dtos\AI\AIKmssDto.cs
Kevin\kevin.Share\Dtos\AI\AIModelsDto.cs
Kevin\kevin.Share\Dtos\AI\AIPromptsDto.cs
Kevin\kevin.Share\Dtos\AI\AISkillToolManagementDto.cs
Kevin\kevin.Share\Dtos\AI\AIAppsBindSkillToolsDto.cs
```

### 8. 枚举 (Enums)
```
Kevin\kevin.Share\Enums\AIType.cs                   -- AI平台类型 (OpenAI/Azure/智谱/Ollama/Bge)
Kevin\kevin.Share\Enums\AISkillToolTypeEnums.cs     -- 技能/工具类型枚举
```

### 9. AI Agent 框架核心模块 (`kevin.AI.AgentFramework`)
```
Kevin\kevin.Module\kevin.AI.AgentFramework\
├── kevin.AI.AgentFramework.csproj       -- 独立类库项目文件
├── AIAgentService.cs                  -- AI代理服务（核心：创建OpenAI客户端、发送消息、流式/非流式）
├── AISetting.cs                       -- AI请求配置POCO
├── ServiceCollectionExtensions.cs     -- DI注册扩展
├── Const\
│   ├── SystemPrompt.cs               -- 智能体统一规则提示词模板
│   └── SysTools.cs                   -- 系统内置工具注册表（HTTP/Shell/Python/CommonTools）
├── Dto\
│   └── ChatHistoryItemDto.cs         -- 聊天历史项DTO
├── Interfaces\
│   ├── IAIAgentService.cs            -- AI代理服务接口
│   ├── IAIAgentToolSkillService.cs   -- 技能/工具装配服务接口
│   ├── IBaseAIToolService.cs         -- 基础工具初始化接口
│   ├── IKevinAIChatMessageStore.cs   -- 消息存储接口
│   └── Tasks\
│       └── IKevinAITaskService.cs    -- 定时任务服务接口（Cron调度+任务执行）
├── Agent\KevinChatMessageStore\
│   └── KevinChatMessageStore.cs      -- 聊天历史上下文提供者
├── Tools\
│   ├── CommonTools.cs                -- 常用工具（系统平台/桌面路径/文件输出）
│   ├── ShellTools.cs                 -- Shell命令执行工具（跨平台+安全护栏）
│   ├── PythonTools.cs                -- Python脚本执行工具
│   ├── AgentHttpClientTools.cs       -- HTTP客户端工具（GET/POST/PUT/DELETE）
│   └── HttpClientFunction.cs         -- 搜索引擎工具（百度/必应/搜狗/360搜索）
├── SkillClass\
│   ├── GetWeatherSkill.cs            -- 天气查询技能示例
│   └── UnitConverterSkill.cs         -- 单位转换技能示例
├── ScriptRunners\
│   └── PySubprocessScriptRunner.cs   -- Python子进程脚本运行器
├── Skills\                           -- 文件型技能示例
│   ├── expense-report\               -- 费用报告技能
│   │   └── expense-report\
│   │       ├── SKILL.md              -- 技能指令文档
│   │       ├── assets\               -- 资源文件
│   │       │   └── expense-report-template.md
│   │       └── references\           -- 参考文档
│   │           └── POLICY_FAQ.md
│   └── system-ops\                   -- 系统运维技能
│       └── system-ops\
│           ├── SKILL.md              -- 技能指令文档
│           ├── assets\               -- 资源文件
│           │   └── template.md
│           ├── references\           -- 参考文档
│           │   └── troubleshooting-guide.md
│           └── scripts\              -- PowerShell 脚本
│               ├── check-disk-usage.ps1
│               ├── check-system-info.ps1
│               └── check-top-processes.ps1
└── WorkFlows\
    └── WorkFlowsAndAIAgentsDemo.cs   -- AI工作流编排示例（多Agent流水线，注释状态）
```

### 10. MCP Server 模块 (`Kevin.MCP.Server`)
```
Kevin\kevin.Module\Kevin.MCP.Server\
├── Kevin.AI.MCP.Server.csproj        -- 独立类库项目文件
├── ServiceCollectionExtensions.cs    -- MCP服务DI注册
├── 说明文档.txt                       -- 说明文档
├── Client\
│   ├── IMySSEToolClient.cs          -- MCP SSE客户端接口
│   └── MySSEToolClient.cs           -- MCP SSE客户端实现
├── Models\
│   └── MCPSSEClientSetting.cs       -- MCP SSE客户端配置
└── Tools\
    └── MyTool.cs                    -- MCP工具示例
```

### 11. Web API 控制器层 (Controllers)
```
Kevin\Kevin.Web.Basics\Controllers\AI\AIAppsController.cs              -- AI应用 CRUD API
Kevin\Kevin.Web.Basics\Controllers\AI\AIChatsController.cs             -- AI对话 API
Kevin\Kevin.Web.Basics\Controllers\AI\AIChatHistorysController.cs      -- AI聊天记录 API
Kevin\Kevin.Web.Basics\Controllers\AI\AIKmssController.cs              -- AI知识库 API
Kevin\Kevin.Web.Basics\Controllers\AI\AIModelsController.cs            -- AI模型配置 API
Kevin\Kevin.Web.Basics\Controllers\AI\AIPromptsController.cs           -- AI提示词配置 API
Kevin\Kevin.Web.Basics\Controllers\AI\AISkillToolManagementController.cs -- AI技能/工具管理 API
Kevin\Kevin.Web.Basics\Controllers\AI\AITasksController.cs             -- AI定时任务 API
```

### 12. EF Core 配置
```
Kevin\Kevin.EntityFrameworkCore\Configuration\TAIPromptsConfig.cs
Kevin\Kevin.EntityFrameworkCore\Configuration\TAISkillToolManagementConfig.cs
Kevin\Kevin.EntityFrameworkCore\Database\KevinDbContext.cs  (line 299-300 引用以上配置)
```

### 13. DI 注册
```
Kevin\Kevin.Web.Basics\Extensions\ServiceConfiguration.cs  (line 11-12, 221 #region AI相关注入)
Kevin\kevin.Module\kevin.AI.AgentFramework\ServiceCollectionExtensions.cs  (AddAIAgentClient)
Kevin\kevin.Module\Kevin.MCP.Server\ServiceCollectionExtensions.cs  (AddKevinMCPServer)
```

### 14. 任务模块配置
```
Kevin\Application\TaskModuleConfigs\AIKmssModuleConfigTasks.cs  -- 知识库模块任务配置
```

---

## 二、各文件功能简要描述

### 领域实体层

| 文件 | 功能描述 |
|------|---------|
| `TAIApps.cs` | AI应用配置实体，定义智能体的核心参数：绑定模型、提示词、知识库、温度、Token限制、输出消息类型、是否启用工具/技能 |
| `TAIChats.cs` | AI对话实体，记录用户与AI的对话会话，关联应用(TAIApps)和用户(TUser)，维护最后一条消息摘要 |
| `TAIChatHistorys.cs` | AI聊天消息实体，记录对话中的每条消息(发送/接收)、文件名附件 |
| `TAIChatMessageStore.cs` | AI消息存储实体，框架级消息持久化，记录ThreadId、Role、时间戳、序列化消息内容，支持索引查询 |
| `TAIKmss.cs` | 知识库配置实体，定义文档分块参数(MaxTokensPerParagraph/Line/OverlappingTokens)和向量化模型 |
| `TAIKmsDetails.cs` | 知识库文档详情实体，记录导入的文档信息(文件ID/类型/内容/URL/数据量/导入状态/错误信息) |
| `TAIModels.cs` | AI模型配置实体，记录模型平台类型、地址、名称、密钥、矢量值大小 |
| `TAIPrompts.cs` | AI提示词配置实体，存储System Prompt的名称、内容和描述 |
| `TAISkillToolManagement.cs` | 技能/工具注册实体，存储名称、方法名、描述、是否系统内置、启用状态、类型(Skill/Tool) |
| `TAISkillToolBindId.cs` | 技能/工具绑定关系实体，将Skill/Tool绑定到Agent(应用)或User(用户)，支持多对多关联 |

### 应用服务层

| 文件 | 功能描述 |
|------|---------|
| `AIAppsService.cs` | AI应用CRUD + 初始化 + 关联技能/工具绑定，核心业务服务 |
| `AIChatsService.cs` | AI对话管理：创建对话(加载模型+提示词+技能+工具上下文)、获取列表、更新主题、删除 |
| `AIChatHistorysService.cs` | AI聊天记录CRUD，管理对话内的消息历史 |
| `AIKmssService.cs` | AI知识库管理：分块策略配置、文档导入、向量化处理协调 |
| `AIModelsService.cs` | AI模型配置CRUD，管理多平台多类型模型接入 |
| `AIPromptsService.cs` | AI提示词模板CRUD，管理系统提示词库 |
| `AISkillToolManagementService.cs` | 技能/工具注册管理，区分Skill和Tool两种类型，支持系统内置保护 |
| `AISkillToolBindIdService.cs` | 技能/工具的绑定关系管理，支持批量绑定和按ID查询 |
| `AIAgentToolSkillService.cs` | **核心装配服务**：根据Agent/User ID查询绑定的Tools和Skills，通过反射/AIFunctionFactory动态创建AI工具实例 |
| `KevinAIChatMessageStore.cs` | AI消息持久化：将框架的ChatHistoryItem转为TAIChatMessageStore实体存储，支持按ThreadId查询历史 |
| `KevinAITasksService.cs` | **核心定时任务服务**：基于Hangfire的Cron任务管理(增删改触查)，任务执行时自动装配完整AI上下文并调用Agent |

### AI Agent 框架核心模块

| 文件 | 功能描述 |
|------|---------|
| `AIAgentService.cs` | **AI代理核心**：创建OpenAI兼容客户端(OpenAIClient)，支持流式/非流式消息发送，可配置超时/重试/HTTP日志 |
| `AISetting.cs` | AI请求配置POCO：AI地址、Key、默认模型、是否流式、流式回调、重试次数、超时、工具/技能开关 |
| `SystemPrompt.cs` | 统一智能体系统提示词：定义工作流程(规划-执行-反馈-迭代)、重要规则(诚实透明、引用来源)、格式规范 |
| `SysTools.cs` | 系统内置工具注册表字典：HTTP工具4个 + Shell工具1个 + Python工具2个 + Common工具3个，共10个内置工具 |
| `IAIAgentService.cs` | AI代理服务接口 |
| `IAIAgentToolSkillService.cs` | 技能/工具装配接口：6个方法覆盖按Agent/User/全局获取工具和技能 |
| `IBaseAIToolService.cs` | 基础工具接口，定义InitData(data)方法用于AI调用前传递上下文数据 |
| `IKevinAIChatMessageStore.cs` | 消息存储接口：AddMessagesAsync / GetMessagesAsync |
| `IKevinAITaskService.cs` | 定时任务接口(继承IBaseAIToolService)：Cron任务增删触查 + RunTask执行 |
| `KevinChatMessageStore.cs` | 聊天历史上下文提供者，实现IChatHistoryProvider，连接框架消息管道与持久化存储 |
| `CommonTools.cs` | 通用工具静态类：GetRuntimePlatform / GetDesktopPath / WriteTextToDesktop / WriteBytesToDesktop / WriteStreamToDesktop / CopyFileToDesktop |
| `ShellTools.cs` | Shell执行工具：跨平台(cmd/bash)、危险命令黑名单(rm -rf /等)、输出截断(50KB)、超时(60秒) |
| `PythonTools.cs` | Python执行工具：RunPythonPy(执行.py文件) / RunPythonCode(直接执行代码) / SavePythonToFile(保存代码) |
| `AgentHttpClientTools.cs` | HTTP客户端工具：GetAsync / PostAsync / PutAsync / DeleteAsync，支持查询参数、自定义Header、超时、自动解压 |
| `HttpClientFunction.cs` | 搜索引擎聚合：百度/必应/搜狗/360搜索 → HTML清洗 → AI总结提炼 |
| `GetWeatherSkill.cs` | Class Skill示例：天气查询，继承AgentClassSkill |
| `UnitConverterSkill.cs` | Class Skill示例：单位转换，含转换表资源和convert脚本 |
| `PySubprocessScriptRunner.cs` | Python子进程脚本运行器，用于文件型Skill的脚本执行 |
| `WorkFlowsAndAIAgentsDemo.cs` | 多Agent工作流编排示例(注释状态)：翻译流水线(法语→繁体→英语) |

### MCP Server 模块

| 文件 | 功能描述 |
|------|---------|
| `ServiceCollectionExtensions.cs` | MCP Server DI注册：HttpTransport + StdioTransport + 程序集扫描Tools |
| `MyTool.cs` | MCP工具示例：Echo方法 |
| `MySSEToolClient.cs` | MCP SSE客户端实现 |

### Web API 控制器层

| 文件 | 功能描述 |
|------|---------|
| `AIAppsController.cs` | AI应用API：分页列表/全部列表/新增编辑/详情/删除/智能体初始化 |
| `AIChatsController.cs` | AI对话API：我的对话列表/新增对话/删除 |
| `AIChatHistorysController.cs` | AI聊天记录API：获取聊天记录/发送消息/流式消息 |
| `AIKmssController.cs` | AI知识库API：分页列表/列表/详情/新增编辑/删除 |
| `AIModelsController.cs` | AI模型配置API：分页列表/全部列表/新增编辑/详情/删除 |
| `AIPromptsController.cs` | AI提示词API：分页列表/列表/新增编辑/详情/删除 |
| `AISkillToolManagementController.cs` | AI技能/工具管理API：分页列表/全部列表/新增编辑/详情/删除 |
| `AITasksController.cs` | AI定时任务API：任务列表/删除任务/执行任务 |

---

## 三、AI 模块功能详细描述

### 核心架构

这是一个**多智能体 AI 应用平台**，基于 `Microsoft.Agents.AI` 框架构建。整体架构采用 DDD 分层设计，通过 **应用(Agent) + 模型(Models) + 提示词(Prompts) + 知识库(KMS) + 技能(Skills) + 工具(Tools)** 六大核心概念实现灵活的 AI 智能体编排。

### 十大功能模块

#### 1. AI 应用管理 (AIApps)

定义 AI 智能体应用的完整配置：
- 绑定对话模型(ChatModelID)、重排模型(RerankModelID)、提示词(AIPromptID)、知识库(KmsId)
- 参数配置：温度(Temperature 0.1-2.0)、提问Token限制(MaxAskPromptSize)、回答Token限制(AnswerTokens)
- 知识库检索参数：相似度阈值(Relevance)、向量匹配数(MaxMatchesCount)、Rerank数量(RerankCount)
- 输出消息类型：非流式文本 / 流式文本 / 图片 / 音频 / 视频 / 文件 / 链接 / 卡片（共8种）
- 每个应用可独立开启/关闭 AI Tools 和 AI Skills
- 支持 API SecretKey 鉴权机制
- 支持 HTTP 请求日志开关
- 提供"智能体初始化"接口，一键生成预配置的应用模板（含可用Skills和Tools列表）

#### 2. AI 模型管理 (AIModels)

管理多平台 AI 模型的接入配置：
- **6种平台类型**：OpenAI / Azure OpenAI / 智谱AI(ZhiPuAI) / Bge Embedding / Bge Rerank / Ollama
- **3种模型类型**：Chat(对话模型) / Embedding(矢量模型) / Rerank(重排模型)
- 每个模型配置项：平台类型、请求地址(EndPoint)、模型名称(ModelName)、模型密钥(ModelKey)、部署名(Azure专用)、矢量值大小(EmbeddingValueSize)
- 模型配置与 AI 应用(TAIApps)一对一关联，实现"换模型不换应用"

#### 3. AI 提示词管理 (AIPrompts)

集中管理 System Prompt 模板库：
- 名称 + 提示词内容(最长1500字符) + 描述
- 每个 AI 应用绑定一个提示词
- 配合全局 `SystemPrompt.SystemPromptText` 统一规则引擎，定义智能体行为规范：
  - 工作流程：分析与规划 → 工具执行 → 结果整合与推理 → 最终输出（循环迭代）
  - 核心规则：诚实透明、只基于文档/搜索回答、不编造不推测、引用来源
  - 安全限制：工具调用失败诚实告知、最多重试3次、Markdown格式输出
- 包含种子数据(`TAIPromptsBaseDatas.cs`)

#### 4. AI 知识库管理 (AIKmss) —— RAG 检索增强生成

自建知识库系统，实现文档向量化 + 语义检索：
- **文档分块策略**：可配置每段落最大Token数(默认299)、每行最大Token数(默认99)、重叠Token数(默认49)
- **支持多种文档格式**：Text / Markdown / PDF / Word / HTML
- **双来源导入**：本地文件上传 + 远程URL拉取
- **向量化处理**：绑定 Embedding 模型进行文档向量化
- **检索流程**：用户提问 → Embedding向量化 → 相似度匹配(Relevance阈值) → TopK召回(MaxMatchesCount) → Rerank重排 → 注入Prompt
- **导入状态跟踪**：ImportKmsStatus 枚举（Loading/Fail/Success），页面用颜色标记(橙/蓝/绿/红)
- **数据统计**：自动统计每个知识库的文档数量和数据处理量

#### 5. AI 对话管理 (AIChats)

多轮对话的完整生命周期管理：
- 每个对话绑定：AI应用(配置) + 用户(多租户隔离)
- **创建对话流程**：
  1. 获取AI应用配置（含模型、提示词、知识库、温度等）
  2. 获取模型详情（EndPoint、ModelKey、ModelName）
  3. 获取提示词（System Prompt）
  4. 查询该应用绑定的 Skill 列表（技能路径）
  5. 查询该应用绑定的 Tool 列表（工具方法名）
  6. 装配完整的 `ChatClientAgentOptions`（Tools + AIContextProviders）
  7. 创建 OpenAI 客户端并发送首条消息
- **流式输出**：通过 SignalR Hub 实时推送 AI 回答到前端（`signalRMsgService.SendIdentityIdMsg`）
- **消息持久化**：
  - 业务层：`TAIChatHistorys` 记录发送/接收的消息
  - 框架层：`TAIChatMessageStore` 通过 `KevinChatMessageStore`（实现 `IChatHistoryProvider`）持久化框架级消息上下文
- 对话名称自动从首条消息截取

#### 6. AI 技能/工具管理系统 (Skill/Tool Management) —— 最大亮点

一套完整的**双层可扩展能力体系**，通过数据库驱动的方式让 AI 智能体获得不同级别的能力：

##### 6a. Tool（工具层）—— 函数级能力，AI 可直接调用的 C# 静态方法

**系统内置10个工具**（`SysTools.cs` 注册表）：

| 工具名称 | 功能 | 安全机制 |
|---------|------|---------|
| `AgentHttpClientTools.GetAsync` | HTTP GET 请求 | 超时30s、自定义Header |
| `AgentHttpClientTools.PostAsync` | HTTP POST 请求 | 超时30s、ContentType |
| `AgentHttpClientTools.PutAsync` | HTTP PUT 请求 | 超时30s |
| `AgentHttpClientTools.DeleteAsync` | HTTP DELETE 请求 | 超时30s |
| `ShellTools.RunShell` | 跨平台Shell命令执行 | 危险命令黑名单(rm -rf /等)、输出截断50KB、超时60s |
| `PythonTools.RunPythonPy` | 执行.py脚本文件 | 进程隔离 |
| `PythonTools.RunPythonCode` | 直接执行Python代码字符串 | 自动保存为临时.py文件执行 |
| `CommonTools.GetRuntimePlatform` | 获取操作系统平台 | 无风险 |
| `CommonTools.GetDesktopPath` | 获取桌面路径 | 自动创建目录 |
| `CommonTools.WriteTextToDesktop` | 输出文本/HTML等文件到桌面 | 覆盖保护 |

**扩展工具**（不在静态注册表，由 `AIAgentToolSkillService` 动态创建）：

| 工具名称 | 功能 |
|---------|------|
| `IKevinAITaskService.AddOrUpdateCronTask` | AI 创建/更新周期性定时任务 |
| `IKevinAITaskService.RemoveCronTask` | AI 删除定时任务 |
| `IKevinAITaskService.TriggerCronTask` | AI 立即触发定时任务 |
| `IKevinAITaskService.GetTaskList` | AI 查询自己的定时任务列表 |

**搜索引擎工具**（`HttpClientFunction.GetSeoAsync`）：
- 并发搜索：百度/必应/搜狗/360搜索
- HTML清洗：去script/style/comment/head + 标签属性 + 空白处理
- 编码智能识别：UTF-8/GBK自动检测
- AI总结提炼：搜索结果经AI提取总结为结构化Markdown

##### 6b. Skill（技能层）—— 文件型能力，含指令 + 脚本 + 资源

**Class Skill**（代码内嵌技能）：
- `GetWeatherSkill`：天气查询，继承 `AgentClassSkill<GetWeatherSkill>`，含 Frontmatter + Instructions + Script
- `UnitConverterSkill`：单位转换，含转换表资源(Resource) + 转换脚本(Script)

**File Skill**（文件型技能）：
- 目录结构：`SKILL.md`(技能指令) + `scripts/`(可执行脚本.py/.ps1/.sh) + `assets/`(模板资源) + `references/`(参考文档)
- 示例1：`expense-report` 费用报告生成技能
- 示例2：`system-ops` 系统运维技能（含3个PowerShell脚本：磁盘使用率、系统信息、进程监控）
- 运行器：`PySubprocessScriptRunner` 负责执行Python脚本

##### 6c. 绑定机制

通过 `TAISkillToolBindId` 表实现多对多绑定：
- 绑定目标：AI应用(TAIApps) / 用户(TUser) / 角色(TRole)
- 粒度控制：`GetAIAgentToolsAsync` 按Agent获取 / `GetUserAIAgentToolsAsync` 按Agent+User获取 / `GetAllAIAgentToolsAsync` 获取全局
- 技能同理：3个维度获取技能路径列表

#### 7. AI 定时任务 (Cron Tasks)

让 AI 获得**自主定时执行**能力，是该系统的重要特色：

- **底层引擎**：基于 Hangfire 的 `IRecurringJobManager` 实现 Cron 周期性任务调度
- **CRUD 操作**：
  - `AddOrUpdateCronTask`：创建或更新周期性任务（自动校验Cron表达式合法性）
  - `RemoveCronTask`：删除指定任务
  - `TriggerCronTask`：立即手动触发一次
  - `GetTaskList`：查询当前用户的所有定时任务及下次执行时间
- **任务执行流程**（`RunTask`）：
  1. 分布式锁防并发（`IDistributedLockProvider`）
  2. 解析任务上下文数据（从 `taskdata` 中提取 `ai_chats_id` 等）
  3. 查询数据库获取 AI 对话 → AI 应用 → AI 模型 → AI 提示词
  4. 装配完整的 `ChatClientAgentOptions`（加载 Skills + Tools）
  5. 调用 `AIAgentService.CreateOpenAIAgentAndSendMSG` 执行
  6. 结果通过站内消息系统推送给用户
- **多租户隔离**：任务ID前缀为用户ID

#### 8. MCP (Model Context Protocol) Server

独立的 MCP 协议服务模块：
- 支持两种传输方式：HTTP SSE + Stdio标准输入输出
- 自动扫描程序集注册 MCP Tools（`WithToolsFromAssembly()`）
- OpenTelemetry 链路追踪支持（已注释，可选开启）
- 独立 NuGet 包 `Kevin.AI.MCP.Server.csproj`

#### 9. AI 代理服务 (AIAgentService)

AI 交互的核心引擎：
- **多平台兼容**：基于 `OpenAI.OpenAIClient` SDK，兼容所有 OpenAI 协议的平台（阿里云DashScope、智谱AI、Azure OpenAI 等）
- **两种交互模式**：
  - 非流式：`aiAgent.RunAsync(msg)` → 返回完整结果
  - 流式：`aiAgent.RunStreamingAsync(msg)` → 逐Token回调 `StreameCallback`
- **灵活配置**（`AISetting`）：
  - AIUrl：API端点地址
  - AIKeySecret：认证密钥（本地模型可空）
  - AIDefaultModel：默认模型名称
  - IsStreame + StreameCallback：流式开关及回调
  - NetworkTimeout：请求超时（分钟）
  - MaxRetries：最大重试次数
  - IsHttpLog：HTTP请求/响应日志（可拦截调试）
  - IsAITools / IsAISkills：工具/技能开关
- **重试策略**：`ClientRetryPolicy` 指数退避重试
- **请求拦截**：`HttpClientAutoInterceptor` 可启停的HTTP日志拦截

#### 10. AI 工作流 (Workflows) [示例/规划级别]

多 Agent 流水线编排（当前为注释状态，说明是规划中的能力）：
- `WorkflowBuilder` 构建有向无环图(DAG)工作流
- 串联多个 AI Agent（如：法语翻译 → 繁体翻译 → 英语翻译）
- `TurnToken` 机制控制 Agent 执行顺序
- `InProcessExecution.RunStreamingAsync` 流式执行整个工作流

---

## 四、数据表关系

```
TAIModels (模型配置)
    ↓ ChatModelID
TAIApps (AI应用) ────────────────────────┐
    ↓ AIPromptID          ↓ KmsId        │
TAIPrompts (提示词)    TAIKmss (知识库)    │
                          ↓ aIModelsId    │
                       TAIModels          │
                          ↓               │
                       TAIKmsDetails (文档)│
                                          │
TAISkillToolManagement (技能/工具注册) ──┐ │
    ↓ (多对多)                          │ │
TAISkillToolBindId (绑定关系)            │ │
    ↓ BindId = TAIApps.Id ──────────────┘ │
                                          │
TAIChats (对话) ←── AppId ────────────────┘
    ↓
TAIChatHistorys (聊天消息)
    ↓ ThreadId
TAIChatMessageStore (框架消息存储)
```

---

## 五、架构亮点总结

| 特性 | 实现方式 |
|------|----------|
| **模型无关** | OpenAI SDK 兼容协议，支持 OpenAI/Azure/智谱/Ollama 等，换模型不改代码 |
| **双层扩展** | Tool(函数工具) + Skill(文件技能) 两层能力扩展体系，数据库驱动 + 静态注册混合 |
| **RAG知识库** | 自建知识库文档向量化 + 分块策略 + 相似度检索 + Rerank重排 |
| **自主定时** | AI 可自我创建Cron任务，定时自主执行（如新闻总结、报告生成），支持分布式锁防并发 |
| **安全护栏** | Shell危险命令黑名单、超时控制(60s)、输出截断(50KB)、最多重试3次 |
| **流式推送** | SignalR 实时推送 AI 回答 Token 到前端 |
| **多租户** | 所有实体支持 TenantId 隔离 + UserId 数据权限 |
| **双消息存储** | 业务聊天记录(TAIChatHistorys) + 框架级消息持久化(TAIChatMessageStore via IChatHistoryProvider) |
| **动态装配** | AIFunctionFactory 运行时动态创建 AIFunction，Skills 按路径动态加载 |
| **搜索引擎** | 多引擎聚合搜索(百度/必应/搜狗/360) + HTML清洗 + AI智能总结 |
| **MCP协议** | 独立MCP Server模块，支持SSE和Stdio传输，为Agent调用外部服务提供标准协议 |

---

## 六、NuGet 依赖关系

### kevin.AI.AgentFramework 项目依赖
- `Microsoft.Agents.AI` — AI Agent 框架
- `Microsoft.Extensions.AI` — AI 扩展抽象
- `OpenAI` — OpenAI SDK（兼容多平台）
- `ModelContextProtocol.Server` — MCP 服务端
- `HttpMataki.NET.Auto` — HTTP 拦截器

### 主项目（AI相关）
- `Hangfire` / `Hangfire.Core` — 定时任务调度
- `Cronos` — Cron 表达式解析
- `DistributedLock` (Medallion) — 分布式锁
- `Microsoft.AspNetCore.SignalR` — 实时通信
- `Qdrant.Client` — 向量数据库客户端（知识库向量存储）
- `Kevin.RAG.Ollama` — RAG 本地逻辑

---

## 七、统计汇总

| 类别 | 文件数 |
|------|--------|
| Domain Entities | 10 |
| Domain BaseDatas | 2 |
| Domain Interfaces (IServices) | 10 |
| Domain Interfaces (IRepositories) | 10 |
| Application Services | 11 |
| Repository Implementations | 10 |
| EF Core Configurations | 2 |
| Web Controllers | 8 |
| Shared DTOs | 9 |
| Shared Enums | 2 |
| AI Agent Framework Module | 20 |
| MCP Server Module | 5 |
| Task Module Configs | 1 |
| DI Registration + XML Docs | 2 |
| **总计** | **102** |
