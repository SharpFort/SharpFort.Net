using System.ComponentModel;
using System.Text.RegularExpressions;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;
using SharpFort.Ai.AgentFramework;
using SharpFort.Ai.AgentFramework.Const;
using SharpFort.Ai.AgentFramework.Interfaces;
using SharpFort.Ai.AgentFramework.Interfaces.Tasks;
using SharpFort.Ai.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;
using Volo.Abp.Application.Services;
using Volo.Abp.DistributedLocking;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace SharpFort.Ai.Application.Services;

public class KevinAITasksService : ApplicationService, IKevinAITaskService
{
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly JobStorage _jobStorage;
    private readonly IAIAgentService _aiAgentService;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly ISqlSugarRepository<AiApp, Guid> _appRepository;
    private readonly ISqlSugarRepository<AiModel, Guid> _modelRepository;
    private readonly ISqlSugarRepository<AiProvider, Guid> _providerRepository;
    private readonly ISqlSugarRepository<AiPrompt, Guid> _promptRepository;
    private readonly ISqlSugarRepository<ChatSession, Guid> _chatRepository;
    private object? _data;

    public KevinAITasksService(
        IRecurringJobManager recurringJobManager,
        JobStorage jobStorage,
        IAIAgentService aiAgentService,
        IAbpDistributedLock distributedLock,
        ISqlSugarRepository<AiApp, Guid> appRepository,
        ISqlSugarRepository<AiModel, Guid> modelRepository,
        ISqlSugarRepository<AiPrompt, Guid> promptRepository,
        ISqlSugarRepository<AiProvider, Guid> providerRepository,
        ISqlSugarRepository<ChatSession, Guid> chatRepository)
    {
        _recurringJobManager = recurringJobManager;
        _jobStorage = jobStorage;
        _aiAgentService = aiAgentService;
        _distributedLock = distributedLock;
        _appRepository = appRepository;
        _modelRepository = modelRepository;
        _promptRepository = promptRepository;
        _providerRepository = providerRepository;
        _chatRepository = chatRepository;
    }

    public void InitData(object data)
    {
        _data = data;
    }

    private static bool IsValidCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression)) return false;
        string pattern = @"^(\S+\s){4,5}\S+$";
        if (!Regex.IsMatch(cronExpression.Trim(), pattern)) return false;
        var fields = cronExpression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return fields.Length is >= 5 and <= 7;
    }

    public Task<string> AddOrUpdateCronTask(
        [Description("name")] string name,
        [Description("content")] string content,
        [Description("cron表达式")] string cronExpression)
    {
        try
        {
            if (!IsValidCronExpression(cronExpression))
                return Task.FromResult($"添加或更新定时任务失败：cronExpression格式错误: {cronExpression}");

            var userId = CurrentUser?.Id?.ToString() ?? "system";
            _recurringJobManager.AddOrUpdate<IKevinAITaskService>(
                recurringJobId: userId + name,
                s => s.RunTask(userId, name, content, _data!),
                cronExpression,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

            return Task.FromResult($"添加或更新定时任务成功：{name}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"添加或更新定时任务失败：{name}，异常信息：{ex.Message}");
        }
    }

    public Task<List<string>> GetTaskList()
    {
        try
        {
            var storage = _jobStorage ?? JobStorage.Current;
            if (storage == null)
                return Task.FromResult(new List<string> { "Hangfire JobStorage 未初始化" });

            var userId = CurrentUser?.Id?.ToString() ?? "system";
            var connection = storage.GetConnection();
            var recurringJobs = connection.GetRecurringJobs();

            var result = recurringJobs
                .Where(t => t.Id.StartsWith(userId))
                .Select(r =>
                {
                    var next = r.NextExecution?.ToLocalTime().ToString("u") ?? "null";
                    var last = r.LastExecution?.ToLocalTime().ToString("u") ?? "null";
                    return $"name:{r.Id.Replace(userId, "")} | Cron:{r.Cron} | Next:{next} | Last:{last} | TimeZone:{r.TimeZoneId}";
                }).ToList();

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            return Task.FromResult(new List<string> { $"查询任务列表失败：{ex.Message}" });
        }
    }

    public Task<string> RemoveCronTask([Description("name")] string name)
    {
        try
        {
            var userId = CurrentUser?.Id?.ToString() ?? "system";
            _recurringJobManager.RemoveIfExists(userId + name);
            return Task.FromResult("移除定时任务成功：" + name);
        }
        catch (Exception ex)
        {
            return Task.FromResult($"移除定时任务失败：{name}，异常信息：{ex.Message}");
        }
    }

    public Task<string> TriggerCronTask([Description("name")] string name)
    {
        try
        {
            var userId = CurrentUser?.Id?.ToString() ?? "system";
            _recurringJobManager.Trigger(userId + name);
            return Task.FromResult("执行定时任务成功：" + name);
        }
        catch (Exception ex)
        {
            return Task.FromResult($"执行定时任务失败：{name}，异常信息：{ex.Message}");
        }
    }

    public async Task<string> RunTask(string userId, string taskName, string taskContent, object taskdata)
    {
        var lockKey = $"SharpFort.Ai.RunTask:{taskName}";
        await using var handle = await _distributedLock.TryAcquireAsync(lockKey);
        if (handle == null)
            return $"执行定时任务失败：{taskName}，任务正在执行中，请勿重复执行";

        try
        {
            var messageContent = $"AI:{userId} RunTask：执行任务 {taskName}";
            var dataJson = System.Text.Json.JsonSerializer.Serialize(taskdata);
            var aiChatsId = GetJsonValue(dataJson, "ai_chats_id");

            if (!string.IsNullOrEmpty(aiChatsId) && Guid.TryParse(aiChatsId, out var chatId))
            {
                var chat = await _chatRepository._DbQueryable
                    .Where(t => t.Id == chatId)
                    .FirstAsync();

                if (chat?.AppId == null) return messageContent;

                var app = await _appRepository._DbQueryable
                    .Where(t => t.Id == chat.AppId.Value)
                    .FirstAsync();
                if (app?.ChatModelId == null) return messageContent;

                var model = await _modelRepository._DbQueryable
                    .Where(t => t.Id == app.ChatModelId.Value)
                    .FirstAsync();
                if (model == null) return messageContent;

                // Get provider for endpoint and API key
                var provider = await _providerRepository._DbQueryable
                    .Where(t => t.Id == model.AiProviderId)
                    .FirstAsync();

                var prompt = app.AiPromptId != null
                    ? await _promptRepository._DbQueryable.Where(t => t.Id == app.AiPromptId.Value).FirstAsync()
                    : null;

                var systemPrompt = SystemPrompt.SystemPromptText;
                var chatOptions = new ChatClientAgentOptions
                {
                    Name = app.Name ?? "智能体",
                    Description = prompt?.Description ?? "你是一个智能体,请根据你的问题进行相关回答",
                    ChatOptions = new ChatOptions
                    {
                        MaxOutputTokens = app.MaxAskPromptSize,
                        Temperature = (float)(app.Temperature / 100),
                        ResponseFormat = ChatResponseFormat.Text,
                        Instructions = (prompt?.Content ?? "") + systemPrompt,
                    },
                };

                // Dynamic tool/skill loading
                var toolSkillService = LazyServiceProvider.LazyGetRequiredService<IAIAgentToolSkillService>();
                if (app.IsAiTools && toolSkillService != null)
                {
                    chatOptions.ChatOptions!.Tools ??= [];
                    var agentTools = await toolSkillService.GetUserAIAgentToolsAsync(taskdata, app.Id.ToString(), userId);
                    foreach (var tool in agentTools)
                        chatOptions.ChatOptions.Tools.Add(tool);
                }

                messageContent = (await _aiAgentService.CreateOpenAIAgentAndSendMSG(new AISetting
                {
                    AIUrl = provider?.Endpoint ?? "https://api.openai.com/v1/",
                    AIKeySecret = provider?.ApiKey ?? "",
                    AIDefaultModel = model.ModelId ?? "gpt-3.5-turbo",
                    IsStreame = false,
                    IsHttpLog = app.IsHttpLog,
                    MaxRetries = app.MaxRetries,
                    NetworkTimeout = app.NetworkTimeout,
                }, chatOptions, taskContent)).Item2;
            }

            return $"执行任务：{taskName}，内容：{taskContent}，返回结果：{messageContent}，执行成功";
        }
        catch (Exception ex)
        {
            return $"执行定时任务失败：{taskName}，异常信息：{ex.Message}";
        }
    }

    private static string? GetJsonValue(string json, string key)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(key, out var prop) ? prop.GetString() : null;
        }
        catch
        {
            return null;
        }
    }
}
