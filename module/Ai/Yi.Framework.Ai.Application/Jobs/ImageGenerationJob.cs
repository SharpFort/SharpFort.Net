using System.Text.Json;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.Ai.Domain.Managers;
using Yi.Framework.Ai.Domain.Shared.Enums;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Application.Jobs;

/// <summary>
/// 图片生成后台任务
/// </summary>
public class ImageGenerationJob : AsyncBackgroundJob<ImageGenerationJobArgs>, ITransientDependency
{
    private readonly ILogger<ImageGenerationJob> _logger;
    private readonly AiGateWayManager _aiGateWayManager;
    private readonly ISqlSugarRepository<ImageStoreTaskAggregateRoot> _imageStoreTaskRepository;

    public ImageGenerationJob(
        ILogger<ImageGenerationJob> logger,
        AiGateWayManager aiGateWayManager,
        ISqlSugarRepository<ImageStoreTaskAggregateRoot> imageStoreTaskRepository)
    {
        _logger = logger;
        _aiGateWayManager = aiGateWayManager;
        _imageStoreTaskRepository = imageStoreTaskRepository;
    }

    public override async Task ExecuteAsync(ImageGenerationJobArgs args)
    {
        var task = await _imageStoreTaskRepository.GetFirstAsync(x => x.Id == args.TaskId);
        if (task is null)
        {
            throw new UserFriendlyException($"{args.TaskId} 图片生成任务不存在");
        }

        _logger.LogInformation("开始执行图片生成任务，TaskId: {TaskId}, ModelId: {ModelId}, UserId: {UserId}",
            task.Id, task.ModelId, task.UserId);
        try
        {
            // 构建 Gemini API 请求对象
            var parts = new List<object>
            {
                new { text = task.Prompt }
            };

            // 添加参考图（如果有）
            foreach (var prefixBase64 in task.ReferenceImagesPrefixBase64)
            {
                var (mimeType, base64Data) = ParsePrefixBase64(prefixBase64);
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

            var request = JsonSerializer.Deserialize<JsonElement>(
                JsonSerializer.Serialize(requestObj));

            //里面生成成功已经包含扣款了
            await _aiGateWayManager.GeminiGenerateContentImageForStatisticsAsync(
                task.Id,
                task.ModelId,
                request,
                task.UserId,
                tokenId: task.TokenId);


            _logger.LogInformation("图片生成任务完成，TaskId: {TaskId}", args.TaskId);
        }
        catch (Exception ex)
        {
            var error = $"图片任务失败，TaskId: {args.TaskId}，错误信息: {ex.Message}，错误堆栈：{ex.StackTrace}";
            _logger.LogError(ex, error);

            task.TaskStatus = TaskStatusEnum.Fail;
            task.ErrorInfo = error;

            await _imageStoreTaskRepository.UpdateAsync(task);
        }
    }

    /// <summary>
    /// 解析带前缀的 Base64 字符串，提取 mimeType 和纯 base64 数据
    /// </summary>
    private static (string mimeType, string base64Data) ParsePrefixBase64(string prefixBase64)
    {
        // 默认值
        var mimeType = "image/png";
        var base64Data = prefixBase64;

        if (prefixBase64.Contains(","))
        {
            var parts = prefixBase64.Split(',');
            if (parts.Length == 2)
            {
                var header = parts[0];
                if (header.Contains(":") && header.Contains(";"))
                {
                    mimeType = header.Split(':')[1].Split(';')[0];
                }

                base64Data = parts[1];
            }
        }

        return (mimeType, base64Data);
    }
}
