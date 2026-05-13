using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Services;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Guids;
using Volo.Abp.Users;
using SharpFort.Ai.Application.Contracts.Dtos.Chat;
using SharpFort.Ai.Application.Contracts.Dtos;
using SharpFort.Ai.Application.Jobs;
using SharpFort.Ai.Domain.Entities;
using SharpFort.Ai.Domain.Managers;
using SharpFort.Ai.Domain.Shared.Enums;
using SharpFort.SqlSugarCore.Abstractions;
using System.Globalization;

namespace SharpFort.Ai.Application.Services;


/// <summary>
/// AI图片生成服务
/// </summary>
[Authorize]
public class AiImageService(
    ISqlSugarRepository<ImageStoreTaskAggregateRoot> imageTaskRepository,
    IBackgroundJobManager backgroundJobManager,
    AiBlacklistManager aiBlacklistManager,
    ModelManager modelManager,
    IGuidGenerator guidGenerator,
    IWebHostEnvironment webHostEnvironment, TokenManager tokenManager,
    ISqlSugarRepository<AiModel> aiModelRepository) : ApplicationService
{
    private readonly ISqlSugarRepository<ImageStoreTaskAggregateRoot> _imageTaskRepository = imageTaskRepository;
    private readonly IBackgroundJobManager _backgroundJobManager = backgroundJobManager;
    private readonly AiBlacklistManager _aiBlacklistManager = aiBlacklistManager;
    private readonly ModelManager _modelManager = modelManager;
    private readonly IGuidGenerator _guidGenerator = guidGenerator;
    private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
    private readonly TokenManager _tokenManager = tokenManager;
    private readonly ISqlSugarRepository<AiModel> _aiModelRepository = aiModelRepository;

    /// <summary>
    /// 生成图片（异步任务）
    /// </summary>
    /// <param name="input">图片生成输入参数</param>
    /// <returns>任务ID</returns>
    [HttpPost("ai-image/generate")]
    [Authorize]
    public async Task<Guid> GenerateAsync([FromBody] ImageGenerationInput input)
    {
        Guid userId = CurrentUser.GetId();

        // 黑名单校验
        await _aiBlacklistManager.VerifiyAiBlacklist(userId);

        //校验token
        if (input.TokenId is not null)
        {
            await _tokenManager.ValidateTokenAsync(input.TokenId);
        }




        // 创建任务实体
        ImageStoreTaskAggregateRoot task = new()
        {
            Prompt = input.Prompt,
            ReferenceImagesPrefixBase64 = input.ReferenceImagesPrefixBase64 ?? [],
            ReferenceImagesUrl = [],
            TaskStatus = TaskStatusEnum.Processing,
            UserId = userId,
            UserName = CurrentUser.UserName,
            TokenId = input.TokenId,
            ModelId = input.ModelId
        };

        await _imageTaskRepository.InsertAsync(task);
        // 入队后台任务
        await _backgroundJobManager.EnqueueAsync(new ImageGenerationJobArgs
        {
            TaskId = task.Id,
        });

        return task.Id;
    }

    /// <summary>
    /// 查询任务状态
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>任务详情</returns>
    [HttpGet("ai-image/task/{taskId}")]
    public async Task<ImageTaskOutput> GetTaskAsync([FromRoute] Guid taskId)
    {
        Guid userId = CurrentUser.GetId();

        ImageStoreTaskAggregateRoot task = await _imageTaskRepository.GetFirstAsync(x => x.Id == taskId && x.UserId == userId);
        return task == null
            ? throw new UserFriendlyException("任务不存在或无权访问")
            : new ImageTaskOutput
            {
                Id = task.Id,
                Prompt = task.Prompt,
                // ReferenceImagesBase64 = task.ReferenceImagesBase64,
                // ReferenceImagesUrl = task.ReferenceImagesUrl,
                // StoreBase64 = task.StoreBase64,
                StoreUrl = task.StoreUrl,
                TaskStatusEnum = task.TaskStatus,
                CreationTime = task.CreationTime,
                ErrorInfo = task.ErrorInfo,
            };
    }

    /// <summary>
    /// 上传Base64图片转换为URL
    /// </summary>
    /// <param name="base64Data">Base64图片数据（包含前缀如 data:image/png;base64,）</param>
    /// <returns>图片访问URL</returns>
    [HttpPost("ai-image/upload-base64")]
    [AllowAnonymous]
    public async Task<string> UploadBase64ToUrlAsync([FromBody] string base64Data)
    {
        if (string.IsNullOrWhiteSpace(base64Data))
        {
            throw new UserFriendlyException("Base64数据不能为空");
        }

        // 解析Base64数据
        string mimeType = "image/png";
        string base64Content = base64Data;

        if (base64Data.Contains(','))
        {
            string[] parts = base64Data.Split(',');
            if (parts.Length == 2)
            {
                // 提取MIME类型
                string header = parts[0];
                if (header.Contains(':') && header.Contains(';'))
                {
                    mimeType = header.Split(':')[1].Split(';')[0];
                }

                base64Content = parts[1];
            }
        }

        // 获取文件扩展名
        string extension = mimeType switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => ".png"
        };

        // 解码Base64
        byte[] imageBytes;
        try
        {
            imageBytes = Convert.FromBase64String(base64Content);
        }
        catch (FormatException)
        {
            throw new UserFriendlyException("Base64格式无效");
        }

        // ==============================
        // ✅ 按日期创建目录（yyyyMMdd）
        // ==============================
        string dateFolder = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string uploadPath = Path.Combine(
            _webHostEnvironment.ContentRootPath,
            "wwwroot",
            "ai-images",
            dateFolder
        );

        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        // 保存文件
        Guid fileId = _guidGenerator.Create();
        string fileName = $"{fileId}{extension}";
        string filePath = Path.Combine(uploadPath, fileName);

        await File.WriteAllBytesAsync(filePath, imageBytes);

        // 返回包含日期目录的访问URL
        return $"/wwwroot/ai-images/{dateFolder}/{fileName}";
    }

    /// <summary>
    /// 分页查询我的任务列表
    /// </summary>
    [HttpGet("ai-image/my-tasks")]
    public async Task<PagedResult<ImageTaskOutput>> GetMyTaskPageAsync([FromQuery] ImageMyTaskPageInput input)
    {
        Guid userId = CurrentUser.GetId();

        RefAsync<int> total = 0;
        List<ImageTaskOutput> output = await _imageTaskRepository._DbQueryable
            .Where(x => x.UserId == userId)
            .WhereIF(input.TaskStatusEnum is not null, x => x.TaskStatus == input.TaskStatusEnum)
            .WhereIF(!string.IsNullOrWhiteSpace(input.Prompt), x => x.Prompt.Contains(input.Prompt!))
            .WhereIF(input.StartTime is not null && input.EndTime is not null,
                x => x.CreationTime >= input.StartTime && x.CreationTime <= input.EndTime)
            .OrderByDescending(x => x.CreationTime)
            .Select(x => new ImageTaskOutput
            {
                Id = x.Id,
                Prompt = x.Prompt,
                StoreUrl = x.StoreUrl,
                TaskStatusEnum = x.TaskStatus,
                CreationTime = x.CreationTime,
                ErrorInfo = x.ErrorInfo,
                UserName = x.UserName,
                UserId = x.UserId
            })
            .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);


        return new PagedResult<ImageTaskOutput>(total, output);
    }

    /// <summary>
    /// 删除个人图片
    /// </summary>
    /// <param name="ids"></param>
    [HttpDelete("ai-image/my-tasks")]
    public async Task DeleteMyTaskAsync([FromQuery] List<Guid> ids)
    {
        Guid userId = CurrentUser.GetId();
        await _imageTaskRepository.DeleteAsync(x => ids.Contains(x.Id) && x.UserId == userId);
    }



    /// <summary>
    /// 获取图片模型列表
    /// </summary>
    /// <returns></returns>
    [HttpPost("ai-image/model")]
    [AllowAnonymous]
    public async Task<List<ModelGetListOutput>> GetModelAsync()
    {
        List<ModelGetListOutput> output = await _aiModelRepository._DbQueryable
            .Where(x => x.IsEnabled)
            .Where(x => x.ModelType == ModelType.Image)
            .Where(x => x.ModelApiType == ModelApiType.GenerateContent)
            .OrderByDescending(x => x.OrderNum)
            .Select(x => new ModelGetListOutput
            {
                Id = x.Id,
                ModelId = x.ModelId!,
                ModelName = x.Name!,
                ModelDescribe = x.Description,
                Remark = x.Description,
            }).ToListAsync();
        return output;
    }
}

/// <summary>
/// 分页结果
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class PagedResult<T>(long total, List<T> items)
{
    /// <summary>
    /// 总数
    /// </summary>
    public long Total { get; set; } = total;

    /// <summary>
    /// 数据列表
    /// </summary>
    public List<T> Items { get; set; } = items;
}