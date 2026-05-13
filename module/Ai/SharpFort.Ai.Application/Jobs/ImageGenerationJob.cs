using System.Text.Json;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using SharpFort.Ai.Domain.Entities;
using SharpFort.Ai.Domain.Managers;
using SharpFort.Ai.Domain.Shared.Enums;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Application.Jobs;

/// <summary>
/// 图片生成后台任务
/// </summary>
public partial class ImageGenerationJob(
    ILogger<ImageGenerationJob> logger,
    AiGateWayManager aiGateWayManager,
    ISqlSugarRepository<ImageStoreTaskAggregateRoot> imageStoreTaskRepository) : AsyncBackgroundJob<ImageGenerationJobArgs>, ITransientDependency
{
    private readonly ILogger<ImageGenerationJob> _logger = logger;
    private readonly AiGateWayManager _aiGateWayManager = aiGateWayManager;
    private readonly ISqlSugarRepository<ImageStoreTaskAggregateRoot> _imageStoreTaskRepository = imageStoreTaskRepository;

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "开始执行图片生成任务，TaskId: {TaskId}, ModelId: {ModelId}, UserId: {UserId}")]
    private partial void LogTaskStart(Guid taskId, string modelId, Guid userId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "图片生成任务完成，TaskId: {TaskId}")]
    private partial void LogTaskComplete(Guid taskId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "图片任务失败，TaskId: {TaskId}，错误信息: {ErrorMessage}，错误堆栈：{StackTrace}")]
    private partial void LogTaskError(Exception ex, Guid taskId, string errorMessage, string? stackTrace);

    public override async Task ExecuteAsync(ImageGenerationJobArgs args)
    {
        ImageStoreTaskAggregateRoot task = await _imageStoreTaskRepository.GetFirstAsync(x => x.Id == args.TaskId) ?? throw new UserFriendlyException($"{args.TaskId} 图片生成任务不存在");
        LogTaskStart(task.Id, task.ModelId, task.UserId);
        try
        {
            // 构建 Gemini API 请求对象
            List<object> parts =
            [
                new { text = task.Prompt }
            ];

            // 添加参考图（如果有）
            foreach (string prefixBase64 in task.ReferenceImagesPrefixBase64)
            {
                (string? mimeType, string? base64Data) = ParsePrefixBase64(prefixBase64);
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = mimeType,
                        data = base64Data
                    }
                });
            }

            var requestObj = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user", parts = new List<object>
                        {
                            new { text = "我只要图片，直接生成图片，不要询问我" }
                        }
                    },
                    new { role = "user", parts }
                }
            };

            JsonElement request = JsonSerializer.Deserialize<JsonElement>(
                JsonSerializer.Serialize(requestObj));

            //里面生成成功已经包含扣款了
            await _aiGateWayManager.GeminiGenerateContentImageForStatisticsAsync(
                task.Id,
                task.ModelId,
                request,
                task.UserId,
                tokenId: task.TokenId);


            LogTaskComplete(args.TaskId);
        }
        catch (Exception ex)
        {
            LogTaskError(ex, args.TaskId, ex.Message, ex.StackTrace);

            task.TaskStatus = TaskStatusEnum.Fail;
            task.ErrorInfo = $"图片任务失败，TaskId: {args.TaskId}，错误信息: {ex.Message}，错误堆栈：{ex.StackTrace}";

            await _imageStoreTaskRepository.UpdateAsync(task);
        }
    }

    /// <summary>
    /// 解析带前缀的 Base64 字符串，提取 mimeType 和纯 base64 数据
    /// </summary>
    private static (string mimeType, string base64Data) ParsePrefixBase64(string prefixBase64)
    {
        // 默认值
        string mimeType = "image/png";
        string base64Data = prefixBase64;

        if (prefixBase64.Contains(','))
        {
            string[] parts = prefixBase64.Split(',');
            if (parts.Length == 2)
            {
                string header = parts[0];
                if (header.Contains(':') && header.Contains(';'))
                {
                    mimeType = header.Split(':')[1].Split(';')[0];
                }

                base64Data = parts[1];
            }
        }

        return (mimeType, base64Data);
    }
}
