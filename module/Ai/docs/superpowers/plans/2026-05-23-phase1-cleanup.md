# Phase 1 - 清理与修复 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 删除所有 `[Obsolete]` 代码，统一双套 DTO 体系，修复 3 个已知 Bug，使模块处于可安全扩展的基线状态。

**Architecture:** 纯删除和修复操作，不引入新功能。先删代码后修 Bug，每项变更独立 commit。SessionService 因依赖 `CrudAppService<..., SessionDto, ...>` 泛型约束，暂时保留老 DTO，仅删除 MessageService 使用的老版本 DTO。

**Tech Stack:** C# / .NET 10 / ABP 10.3 / SqlSugar ORM

---

## 文件结构

| 操作 | 文件 | 说明 |
|------|------|------|
| 删除 | `Application/Services/AiAccountService.cs` | Obsolete 转发服务 |
| 删除 | `Domain/Managers/MessageLogManager.cs` | Obsolete 日志 Manager |
| 删除 | `Domain/Entities/MessageLogAggregateRoot.cs` | Obsolete 日志实体 |
| 删除 | `Domain/AiGateWay/HttpClientFactory.cs` | 无调用方的静态 HttpClientFactory |
| 删除 | `Domain/Mcp/HttpRequestTool.cs` | Obsolete MCP 工具 |
| 删除 | `Domain/Mcp/DateTimeTool.cs` | Obsolete MCP 工具 |
| 删除 | `Application.Contracts/Dtos/MessageDto.cs` | 老版本 DTO |
| 删除 | `Application.Contracts/Dtos/MessageGetListInput.cs` | 老版本 DTO（含 MessageDeleteInput） |
| 修改 | `Application/Services/MessageService.cs` | 切换引用到 ChatMessageDto 系列 |
| 修改 | `Domain.Shared/Dtos/OpenAi/ThorToolChoiceTypeConst.cs` | 修复尾部空格 Bug |
| 修改 | `Application/Services/AiPromptService.cs` | 添加 IAiPromptService 接口实现 |
| 修改 | `Domain/Managers/ModelManager.cs` | 添加基础查询方法 |

---

### Task 1: 删除 AiAccountService

**Files:**
- Delete: `SharpFort.Ai.Application/Services/AiAccountService.cs`

- [ ] **Step 1: 验证无外部引用**

```powershell
Select-String -Path "E:\Projects\SharpFort.Net\module\Ai\*.cs" -Pattern "AiAccountService" -Recurse
```

Expected: 仅匹配 `AiAccountService.cs` 自身（以及 `docs/` 中的分析文档）。

- [ ] **Step 2: 删除文件**

```powershell
Remove-Item "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Application\Services\AiAccountService.cs"
```

- [ ] **Step 3: 编译验证**

```bash
dotnet build E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Application\SharpFort.Ai.Application.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Application\Services\AiAccountService.cs"
git commit -m "chore(ai): remove obsolete AiAccountService forwarding wrapper"
```

---

### Task 2: 删除 MessageLogManager 和 MessageLogAggregateRoot

**Files:**
- Delete: `SharpFort.Ai.Domain/Managers/MessageLogManager.cs`
- Delete: `SharpFort.Ai.Domain/Entities/MessageLogAggregateRoot.cs`

- [ ] **Step 1: 验证无外部引用**

```powershell
Select-String -Path "E:\Projects\SharpFort.Net\module\Ai\*.cs" -Pattern "MessageLogManager|MessageLogAggregateRoot" -Recurse
```

Expected: 仅匹配这两个文件自身（以及分析文档）。

- [ ] **Step 2: 删除两个文件**

```powershell
Remove-Item "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\Managers\MessageLogManager.cs"
Remove-Item "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\Entities\MessageLogAggregateRoot.cs"
```

- [ ] **Step 3: 编译验证**

```bash
dotnet build E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\SharpFort.Ai.Domain.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\Managers\MessageLogManager.cs"
git add "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\Entities\MessageLogAggregateRoot.cs"
git commit -m "chore(ai): remove obsolete MessageLogManager and MessageLogAggregateRoot"
```

---

### Task 3: 删除 HttpClientFactory（旧版静态类）

**Files:**
- Delete: `SharpFort.Ai.Domain/AiGateWay/HttpClientFactory.cs`

- [ ] **Step 1: 确认无调用方**

```powershell
Select-String -Path "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\*.cs" -Pattern "HttpClientFactory\." -Recurse
```

Expected: 无匹配。注意 `IHttpClientFactory`（接口）和 `HttpClientFactory`（静态类）是不同的，Gateway 实现全部通过 DI 注入 `IHttpClientFactory`。

- [ ] **Step 2: 删除文件**

```powershell
Remove-Item "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\AiGateWay\HttpClientFactory.cs"
```

- [ ] **Step 3: 编译验证**

```bash
dotnet build E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\SharpFort.Ai.Domain.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\AiGateWay\HttpClientFactory.cs"
git commit -m "chore(ai): remove obsolete static HttpClientFactory (replaced by IHttpClientFactory)"
```

---

### Task 4: 删除 MCP 工具类（HttpRequestTool + DateTimeTool）

**Files:**
- Delete: `SharpFort.Ai.Domain/Mcp/HttpRequestTool.cs`
- Delete: `SharpFort.Ai.Domain/Mcp/DateTimeTool.cs`

- [ ] **Step 1: 删除两个文件**

```powershell
Remove-Item "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\Mcp\HttpRequestTool.cs"
Remove-Item "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\Mcp\DateTimeTool.cs"
```

- [ ] **Step 2: 检查 Mcp 目录是否为空**

```powershell
Get-ChildItem "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\Mcp" -File
```

如果为空，删除目录：
```powershell
Remove-Item "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\Mcp"
```

- [ ] **Step 3: 编译验证**

```bash
dotnet build E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\SharpFort.Ai.Domain.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\Mcp\HttpRequestTool.cs"
git add "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\Mcp\DateTimeTool.cs"
git commit -m "chore(ai): remove obsolete MCP tool classes"
```

---

### Task 5: 统一 DTO —— 删除老版本 MessageDto 系列，修改 MessageService

**Files:**
- Delete: `SharpFort.Ai.Application.Contracts/Dtos/MessageDto.cs`
- Delete: `SharpFort.Ai.Application.Contracts/Dtos/MessageGetListInput.cs`
- Modify: `SharpFort.Ai.Application/Services/MessageService.cs`

- [ ] **Step 1: 删除老版本 DTO 文件**

```powershell
Remove-Item "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Application.Contracts\Dtos\MessageDto.cs"
Remove-Item "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Application.Contracts\Dtos\MessageGetListInput.cs"
```

- [ ] **Step 2: 修改 MessageService.cs —— 替换 using 和类型引用**

将 `using SharpFort.Ai.Application.Contracts.Dtos;` 替换为 `using SharpFort.Ai.Application.Contracts.Dtos.ChatMessage;`

将 `MessageDto` 替换为 `ChatMessageDto`
将 `MessageGetListInput` 替换为 `ChatMessageGetListInput`
将 `MessageDeleteInput` 替换为 `ChatMessageDeleteInput`

修改后的 `MessageService.cs` 完整内容：

```csharp
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using SharpFort.Ai.Application.Contracts.Dtos.ChatMessage;
using SharpFort.Ai.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Application.Services;

public class MessageService(ISqlSugarRepository<ChatMessage> repository) : ApplicationService
{
    private readonly ISqlSugarRepository<ChatMessage> _repository = repository;

    [Authorize]
    public async Task<PagedResultDto<ChatMessageDto>> GetListAsync([FromQuery] ChatMessageGetListInput input)
    {
        RefAsync<int> total = 0;
        Guid userId = CurrentUser.GetId();
        List<ChatMessage> entities = await _repository._DbQueryable
            .Where(x => x.SessionId == input.SessionId)
            .Where(x => x.UserId == userId)
            .Where(x => !x.IsHidden)
            .OrderBy(x => x.Id)
            .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
        return new PagedResultDto<ChatMessageDto>(total, entities.Adapt<List<ChatMessageDto>>());
    }

    [Authorize]
    public async Task DeleteAsync([FromQuery] ChatMessageDeleteInput input)
    {
        Guid userId = CurrentUser.GetId();

        List<ChatMessage> messages = await _repository._DbQueryable
            .Where(x => input.Ids.Contains(x.Id))
            .Where(x => x.UserId == userId)
            .ToListAsync();

        if (messages.Count == 0)
        {
            return;
        }

        List<Guid> idsToHide = [.. messages.Select(x => x.Id)];

        if (input.IsDeleteSubsequent)
        {
            foreach (ChatMessage message in messages)
            {
                List<Guid> subsequentIds = await _repository._DbQueryable
                    .Where(x => x.SessionId == message.SessionId)
                    .Where(x => x.UserId == userId)
                    .Where(x => x.CreationTime > message.CreationTime)
                    .Where(x => !x.IsHidden)
                    .Select(x => x.Id)
                    .ToListAsync();

                idsToHide.AddRange(subsequentIds);
            }

            idsToHide = [.. idsToHide.Distinct()];
        }

        await _repository._Db.Updateable<ChatMessage>()
            .SetColumns(x => x.IsHidden)
            .Where(x => idsToHide.Contains(x.Id))
            .ExecuteCommandAsync();
    }
}
```

- [ ] **Step 3: 编译验证**

```bash
dotnet build E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Application\SharpFort.Ai.Application.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Application.Contracts\Dtos\MessageDto.cs"
git add "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Application.Contracts\Dtos\MessageGetListInput.cs"
git add "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Application\Services\MessageService.cs"
git commit -m "refactor(ai): unify DTOs - replace MessageDto/MessageGetListInput with ChatMessageDto series"
```

---

### Task 6: 修复 ThorToolChoiceTypeConst.Required 尾部空格

**Files:**
- Modify: `SharpFort.Ai.Domain.Shared/Dtos/OpenAi/ThorToolChoiceTypeConst.cs`

- [ ] **Step 1: 修改 Required 值**

将第 23 行：
```csharp
    public static string Required => "required ";
```
改为：
```csharp
    public static string Required => "required";
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain.Shared\SharpFort.Ai.Domain.Shared.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain.Shared\Dtos\OpenAi\ThorToolChoiceTypeConst.cs"
git commit -m "fix(ai): remove trailing space in ThorToolChoiceTypeConst.Required"
```

---

### Task 7: AiPromptService 实现 IAiPromptService 接口

**Files:**
- Modify: `SharpFort.Ai.Application/Services/AiPromptService.cs`

- [ ] **Step 1: 修改类签名**

将第 17 行：
```csharp
public class AiPromptService(ISqlSugarRepository<AiPrompt> repository) : ApplicationService
```
改为：
```csharp
public class AiPromptService(ISqlSugarRepository<AiPrompt> repository) : ApplicationService, IAiPromptService
```

需要添加 using：
```csharp
using SharpFort.Ai.Application.Contracts.IServices;
```

修改后的文件头（第 1-11 行保持不变，插入新 using）：
```csharp
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using SharpFort.Ai.Application.Contracts.Dtos.AiPrompt;
using SharpFort.Ai.Application.Contracts.IServices;
using SharpFort.Ai.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Application.Services;

[Authorize]
public class AiPromptService(ISqlSugarRepository<AiPrompt> repository) : ApplicationService, IAiPromptService
{
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Application\SharpFort.Ai.Application.csproj
```

Expected: Build succeeded, 0 errors。接口 `IAiPromptService` 的所有方法已被当前 `AiPromptService` 实现，仅签名不匹配（未声明接口）的问题。

- [ ] **Step 3: Commit**

```bash
git add "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Application\Services\AiPromptService.cs"
git commit -m "fix(ai): add missing IAiPromptService interface implementation to AiPromptService"
```

---

### Task 8: ModelManager 添加基础查询方法

**Files:**
- Modify: `SharpFort.Ai.Domain/Managers/ModelManager.cs`

- [ ] **Step 1: 替换 ModelManager.cs 完整内容**

将当前空壳替换为：

```csharp
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Services;
using SharpFort.Ai.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Domain.Managers;

public class ModelManager(
    ISqlSugarRepository<AiModel> aiModelRepository,
    ILogger<ModelManager> logger) : DomainService
{
    private readonly ISqlSugarRepository<AiModel> _aiModelRepository = aiModelRepository;
    private readonly ILogger<ModelManager> _logger = logger;

    public async Task<AiModel?> GetAsync(Guid id)
    {
        return await _aiModelRepository.FindAsync(x => x.Id == id);
    }

    public async Task<List<AiModel>> GetListAsync()
    {
        return await _aiModelRepository.GetListAsync();
    }

    public async Task<List<AiModel>> GetEnabledModelsAsync()
    {
        return await _aiModelRepository.GetListAsync(x => x.IsEnabled);
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\SharpFort.Ai.Domain.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add "E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\Managers\ModelManager.cs"
git commit -m "feat(ai): add basic query methods to ModelManager"
```

---

### Task 9: 关于 SessionService 的说明

**不修改 `SessionService`**。原因：`SessionService` 继承自 ABP 的 `CrudAppService<ChatSession, SessionDto, Guid, SessionGetListInput, SessionCreateAndUpdateInput>`，泛型参数直接绑定了老版本 DTO（`SessionDto`、`SessionGetListInput`、`SessionCreateAndUpdateInput`）。

修改这些 DTO 需要：
1. 让 `SessionDto` / `ChatSessionDto` 有完全一致的字段结构
2. 令 `SessionService` 改为继承 `CrudAppService<ChatSession, ChatSessionDto, Guid, ChatSessionGetListInput, ChatSessionCreateAndUpdateInput>`

当前两个版本 DTO 结构完全相同，合并风险极低但改动面广（影响 `SessionService` 所有 override 方法签名）。为保持 Phase 1 最小变更原则，Session DTO 合并推迟到 Phase 2，与 `AiGateWayManager` 拆分一起做。

老版本 Session DTO 保留不动，任务记录为后续待办。

---

### Task 10: 全量编译验证

- [ ] **Step 1: 编译全部 5 个项目**

```bash
dotnet build E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain.Shared\SharpFort.Ai.Domain.Shared.csproj
dotnet build E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Domain\SharpFort.Ai.Domain.csproj
dotnet build E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Application.Contracts\SharpFort.Ai.Application.Contracts.csproj
dotnet build E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.Application\SharpFort.Ai.Application.csproj
dotnet build E:\Projects\SharpFort.Net\module\Ai\SharpFort.Ai.SqlSugarCore\SharpFort.Ai.SqlSugarCore.csproj
```

Expected: All 5 projects build with 0 errors.

- [ ] **Step 2: 验证无残留引用**

```powershell
Select-String -Path "E:\Projects\SharpFort.Net\module\Ai\*.cs" -Pattern "AiAccountService|MessageLogManager|MessageLogAggregateRoot|HttpRequestTool" -Recurse
```

Expected: 仅 `docs/` 目录中的分析文档匹配。

---

### Phase 1 完成后状态

| 指标 | 变更前 | 变更后 |
|------|--------|--------|
| `[Obsolete]` 文件 | 6 个 | 0 个 |
| 老版本 DTO 文件 | 5 个（保留 Session 3 个） | 3 个（仅保留 Session 系列） |
| 已知 Bug | 3 个 | 0 个 |
| AiPromptService | 未实现接口 | 实现 `IAiPromptService` |
| ModelManager | 空壳 | 3 个基础方法 |
| 编译 | 0 errors | 0 errors |

---

> **下一步**：Phase 1 实施完成后，进入 Phase 2 计划编写（AiGateWayManager 拆分、AiToolService 实现、Agent 功能）。
