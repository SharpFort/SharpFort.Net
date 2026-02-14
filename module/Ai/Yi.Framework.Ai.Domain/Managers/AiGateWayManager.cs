using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Volo.Abp.Domain.Services;
using Yi.Framework.Ai.Domain.AiGateWay;
using Yi.Framework.Ai.Domain.AiGateWay.Exceptions;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.Ai.Domain.Shared.Consts;
using ModelConst = Yi.Framework.Ai.Domain.Shared.Consts.ModelConst;
using Yi.Framework.Ai.Domain.Shared.Dtos;
using Yi.Framework.Ai.Domain.Shared.Dtos.Anthropic;
using Yi.Framework.Ai.Domain.Shared.Dtos.Gemini;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi.Embeddings;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi.Images;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi.Responses;
using Yi.Framework.Ai.Domain.Shared.Enums;
using Yi.Framework.Ai.Application.Contracts.Dtos.Chat;
using Yi.Framework.Ai.Application.Contracts.Dtos.ChatMessage;
using Yi.Framework.Ai.Application.Contracts.Dtos.Chat;
using Yi.Framework.Ai.Domain.Shared.Extensions;
using Yi.Framework.Core.Extensions;
using Yi.Framework.SqlSugarCore.Abstractions;
using JsonSerializer = System.Text.Json.JsonSerializer;
using ThorJsonSerializer = Yi.Framework.Ai.Domain.AiGateWay.ThorJsonSerializer;

namespace Yi.Framework.Ai.Domain.Managers;

public class AiGateWayManager : DomainService
{
    private readonly ISqlSugarRepository<AiProvider> _aiAppRepository;
    private readonly ISqlSugarRepository<AiModel> _aiModelRepository;
    private readonly ILogger<AiGateWayManager> _logger;
    private readonly AiMessageManager _aiMessageManager;
    private readonly UsageStatisticsManager _usageStatisticsManager;
    private readonly ISpecialCompatible _specialCompatible;
    // private PremiumPackageManager? _premiumPackageManager;
    private readonly ISqlSugarRepository<ImageStoreTaskAggregateRoot> _imageStoreTaskRepository;

    public AiGateWayManager(ISqlSugarRepository<AiProvider> aiAppRepository, ILogger<AiGateWayManager> logger,
        AiMessageManager aiMessageManager, UsageStatisticsManager usageStatisticsManager,
        ISpecialCompatible specialCompatible, ISqlSugarRepository<AiModel> aiModelRepository,
        ISqlSugarRepository<ImageStoreTaskAggregateRoot> imageStoreTaskRepository
        )
    {
        _aiAppRepository = aiAppRepository;
        _logger = logger;
        _aiMessageManager = aiMessageManager;
        _usageStatisticsManager = usageStatisticsManager;
        _specialCompatible = specialCompatible;
        _aiModelRepository = aiModelRepository;
        _aiModelRepository = aiModelRepository;
        _imageStoreTaskRepository = imageStoreTaskRepository;
    }

    // private PremiumPackageManager PremiumPackageManager =>
    //    _premiumPackageManager ??= LazyServiceProvider.LazyGetRequiredService<PremiumPackageManager>();

    /// <summary>
    /// 获取模型
    /// </summary>
    /// <param name="modelApiType"></param>
    /// <param name="modelId"></param>
    /// <returns></returns>
    public async Task<AiModelDescribe> GetModelAsync(ModelApiTypeEnum modelApiType, string modelId)
    {
        var aiModelDescribe = await _aiModelRepository._DbQueryable
            .LeftJoin<AiProvider>((model, app) => model.AiProviderId == app.Id)
            .Where((model, app) => model.ModelId == modelId)
            .Where((model, app) => model.ModelApiType == modelApiType)
            .Where((model, app) => model.IsEnabled)
            .Select((model, app) =>
                new AiModelDescribe
                {
                    AppId = app.Id,
                    AppName = app.Name,
                    Endpoint = app.Endpoint,
                    ApiKey = app.ApiKey,
                    OrderNum = model.OrderNum,
                    HandlerName = model.HandlerName,
                    ModelId = model.ModelId,
                    ModelName = model.Name,
                    Description = model.Description,
                    AppExtraUrl = app.ExtraUrl,
                    ModelExtraInfo = model.ExtraInfo,
                    Multiplier = model.Multiplier,
                    IsPremium = model.IsPremium,
                    ModelType = model.ModelType
                })
            .FirstAsync();
        if (aiModelDescribe is null)
        {
            throw new UserFriendlyException($"【{modelId}】模型当前版本【{modelApiType}】格式不支持");
        }

        // ✅ 统一处理模型前缀（网关层模型规范化）
        aiModelDescribe.ModelId = ModelConst.RemoveModelPrefix(aiModelDescribe.ModelId);

        return aiModelDescribe;
    }


    /// <summary>
    /// 聊天完成-非流式
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="request"></param>
    /// <param name="userId"></param>
    /// <param name="sessionId"></param>
    /// <param name="tokenId">Token Id（Web端传null或Guid.Empty）</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task CompleteChatForStatisticsAsync(HttpContext httpContext,
        ThorChatCompletionsRequest request,
        Guid? userId = null,
        Guid? sessionId = null,
        Guid? tokenId = null,
        CancellationToken cancellationToken = default)
    {
        _specialCompatible.Compatible(request);
        var response = httpContext.Response;
        // 设置响应头，声明是 json
        //response.ContentType = "application/json; charset=UTF-8";
        var modelDescribe = await GetModelAsync(ModelApiTypeEnum.Completions, request.Model);
        var chatService =
            LazyServiceProvider.GetRequiredKeyedService<IChatCompletionService>(modelDescribe.HandlerName);

        var sourceModelId = request.Model;
        request.Model = ModelConst.ProcessModelId(request.Model);

        var data = await chatService.CompleteChatAsync(modelDescribe, request, cancellationToken);
        data.SupplementalMultiplier(modelDescribe.Multiplier);
        if (userId is not null)
        {
            await _aiMessageManager.CreateUserMessageAsync(userId.Value, sessionId,
                new MessageInputDto
                {
                    Content = sessionId is null ? "不予存储" : request.Messages?.LastOrDefault().Content ?? string.Empty,
                    ModelId = sourceModelId,
                    TokenUsage = data.Usage,
                }, tokenId);

            await _aiMessageManager.CreateSystemMessageAsync(userId.Value, sessionId,
                new MessageInputDto
                {
                    Content =
                        sessionId is null ? "不予存储" : data.Choices?.FirstOrDefault()?.Delta.Content ?? string.Empty,
                    ModelId = sourceModelId,
                    TokenUsage = data.Usage
                }, tokenId);

            await _usageStatisticsManager.SetUsageAsync(userId.Value, sourceModelId, data.Usage, tokenId);

            /*
            if (modelDescribe.IsPremium)
            {
                var totalTokens = data.Usage?.TotalTokens ?? 0;
                if (totalTokens > 0)
                {
                    await PremiumPackageManager.TryConsumeTokensAsync(userId.Value, totalTokens);
                }
            }
            */
        }

        await response.WriteAsJsonAsync(data, cancellationToken);
    }


    /// <summary>
    /// 聊天完成-缓存处理
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="request"></param>
    /// <param name="userId"></param>
    /// <param name="sessionId"></param>
    /// <param name="tokenId">Token Id（Web端传null或Guid.Empty）</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task CompleteChatStreamForStatisticsAsync(
        HttpContext httpContext,
        ThorChatCompletionsRequest request,
        Guid? userId = null,
        Guid? sessionId = null,
        Guid? tokenId = null,
        CancellationToken cancellationToken = default)
    {
        var response = httpContext.Response;
        // 设置响应头，声明是 SSE 流
        response.ContentType = "text/event-stream;charset=utf-8;";
        response.Headers.TryAdd("Cache-Control", "no-cache");
        response.Headers.TryAdd("Connection", "keep-alive");


        _specialCompatible.Compatible(request);
        var modelDescribe = await GetModelAsync(ModelApiTypeEnum.Completions, request.Model);
        var chatService =
            LazyServiceProvider.GetRequiredKeyedService<IChatCompletionService>(modelDescribe.HandlerName);

        var sourceModelId = request.Model;
        request.Model = ModelConst.ProcessModelId(request.Model);

        var completeChatResponse = chatService.CompleteChatStreamAsync(modelDescribe, request, cancellationToken);
        var tokenUsage = new ThorUsageResponse();

        //缓存队列算法
        // 创建一个队列来缓存消息
        var messageQueue = new ConcurrentQueue<string>();

        StringBuilder backupSystemContent = new StringBuilder();
        // 设置输出速率（例如每50毫秒输出一次）
        var outputInterval = TimeSpan.FromMilliseconds(75);
        // 标记是否完成接收
        var isComplete = false;
        // 启动一个后台任务来消费队列
        var outputTask = Task.Run(async () =>
        {
            while (!(isComplete && messageQueue.IsEmpty))
            {
                if (messageQueue.TryDequeue(out var message))
                {
                    await response.WriteAsync(message, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
                    await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                if (!isComplete)
                {
                    // 如果没有完成，才等待，已完成，全部输出
                    await Task.Delay(outputInterval, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    //已经完成了，也等待，但是速度可以放快
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                }
            }
        }, cancellationToken);


        //IAsyncEnumerable 只能在最外层捕获异常（如果你有其他办法的话...）
        try
        {
            await foreach (var data in completeChatResponse)
            {
                data.SupplementalMultiplier(modelDescribe.Multiplier);
                if (data.Usage is not null && (data.Usage.CompletionTokens > 0 || data.Usage.OutputTokens > 0))
                {
                    tokenUsage = data.Usage;
                }

                var message = JsonSerializer.Serialize(data, ThorJsonSerializer.DefaultOptions);
                backupSystemContent.Append(data.Choices.FirstOrDefault()?.Delta.Content);
                // 将消息加入队列而不是直接写入
                messageQueue.Enqueue($"data: {message}\n\n");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Ai对话异常，用户ID：{userId}");
            var errorContent = $"对话Ai异常，异常信息：\n当前Ai模型：{request.Model}\n异常信息：{e.Message}\n异常堆栈：{e}";
            var model = new ThorChatCompletionsResponse()
            {
                Choices = new List<ThorChatChoiceResponse>()
                {
                    new ThorChatChoiceResponse()
                    {
                        Delta = new ThorChatMessage()
                        {
                            Content = errorContent
                        }
                    }
                }
            };
            var message = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            backupSystemContent.Append(errorContent);
            messageQueue.Enqueue($"data: {message}\n\n");
        }

        //断开连接
        messageQueue.Enqueue("data: [DONE]\n\n");
        // 标记完成并发送结束标记
        isComplete = true;

        await outputTask;


        await _aiMessageManager.CreateUserMessageAsync(userId, sessionId,
            new MessageInputDto
            {
                Content = sessionId is null ? "不予存储" : request.Messages?.LastOrDefault()?.MessagesStore ?? string.Empty,
                ModelId = sourceModelId,
                TokenUsage = tokenUsage,
            }, tokenId);

        await _aiMessageManager.CreateSystemMessageAsync(userId, sessionId,
            new MessageInputDto
            {
                Content = sessionId is null ? "不予存储" : backupSystemContent.ToString(),
                ModelId = sourceModelId,
                TokenUsage = tokenUsage
            }, tokenId);

        await _usageStatisticsManager.SetUsageAsync(userId, sourceModelId, tokenUsage, tokenId);

        // 扣减尊享token包用量
        if (userId is not null)
        {
            /*
            if (modelDescribe.IsPremium)
            {
                var totalTokens = tokenUsage.TotalTokens ?? 0;
                if (totalTokens > 0)
                {
                    await PremiumPackageManager.TryConsumeTokensAsync(userId.Value, totalTokens);
                }
            }
            */
        }
    }


    /// <summary>
    /// 图片生成
    /// </summary>
    /// <param name="context"></param>
    /// <param name="userId"></param>
    /// <param name="sessionId"></param>
    /// <param name="request"></param>
    /// <param name="tokenId">Token Id（Web端传null或Guid.Empty）</param>
    /// <exception cref="BusinessException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task CreateImageForStatisticsAsync(HttpContext context, Guid? userId, Guid? sessionId,
        ImageCreateRequest request, Guid? tokenId = null)
    {
        try
        {
            var model = request.Model;
            if (string.IsNullOrEmpty(model)) model = "dall-e-2";

            var modelDescribe = await GetModelAsync(ModelApiTypeEnum.Completions, model);

            // 获取渠道指定的实现类型的服务
            var imageService =
                LazyServiceProvider.GetRequiredKeyedService<IImageService>(modelDescribe.HandlerName);

            var response = await imageService.CreateImage(request, modelDescribe);

            if (response.Error != null || response.Results.Count == 0)
            {
                throw new BusinessException(response.Error?.Message ?? "图片生成失败", response.Error?.Code?.ToString());
            }

            await context.Response.WriteAsJsonAsync(response);

            await _aiMessageManager.CreateUserMessageAsync(userId, sessionId,
                new MessageInputDto
                {
                    Content = sessionId is null ? "不予存储" : request.Prompt,
                    ModelId = model,
                    TokenUsage = response.Usage,
                }, tokenId);

            await _aiMessageManager.CreateSystemMessageAsync(userId, sessionId,
                new MessageInputDto
                {
                    Content = sessionId is null ? "不予存储" : response.Results?.FirstOrDefault()?.Url,
                    ModelId = model,
                    TokenUsage = response.Usage
                }, tokenId);

            await _usageStatisticsManager.SetUsageAsync(userId, model, response.Usage, tokenId);

            /*
            // 直接扣减尊享token包用量
            if (userId is not null)
            {
                var totalTokens = response.Usage.TotalTokens ?? 0;
                if (totalTokens > 0)
                {
                    await PremiumPackageManager.TryConsumeTokensAsync(userId.Value, totalTokens);
                }
            }
            */
        }
        catch (Exception e)
        {
            var errorContent = $"图片生成Ai异常，异常信息：\n当前Ai模型：{request.Model}\n异常信息：{e.Message}\n异常堆栈：{e}";
            throw new UserFriendlyException(errorContent);
        }
    }


    /// <summary>
    /// 向量生成
    /// </summary>
    /// <param name="context"></param>
    /// <param name="userId"></param>
    /// <param name="sessionId"></param>
    /// <param name="input"></param>
    /// <param name="tokenId">Token Id（Web端传null或Guid.Empty）</param>
    /// <exception cref="Exception"></exception>
    /// <exception cref="BusinessException"></exception>
    public async Task EmbeddingForStatisticsAsync(HttpContext context, Guid? userId, Guid? sessionId,
        ThorEmbeddingInput input, Guid? tokenId = null)
    {
        try
        {
            if (input == null) throw new Exception("模型校验异常");

            using var embedding =
                Activity.Current?.Source.StartActivity("向量模型调用");

            var modelDescribe = await GetModelAsync(ModelApiTypeEnum.Completions, input.Model);

            // 获取渠道指定的实现类型的服务
            var embeddingService =
                LazyServiceProvider.GetRequiredKeyedService<ITextEmbeddingService>(modelDescribe.HandlerName);

            var embeddingCreateRequest = new EmbeddingCreateRequest
            {
                Model = input.Model,
                EncodingFormat = input.EncodingFormat
            };

            //dto进行转换，支持多种格式
            if (input.Input is JsonElement str)
            {
                if (str.ValueKind == JsonValueKind.String)
                {
                    embeddingCreateRequest.Input = str.ToString();
                }
                else if (str.ValueKind == JsonValueKind.Array)
                {
                    var inputString = str.EnumerateArray().Select(x => x.ToString()).ToArray();
                    embeddingCreateRequest.InputAsList = inputString.ToList();
                }
                else
                {
                    throw new Exception("Input，输入格式错误，非string或Array类型");
                }
            }
            else if (input.Input is string strInput)
            {
                embeddingCreateRequest.Input = strInput;
            }
            else
            {
                throw new Exception("Input，输入格式错误，未找到类型");
            }


            var stream =
                await embeddingService.EmbeddingAsync(embeddingCreateRequest, modelDescribe, context.RequestAborted);

            var usage = new ThorUsageResponse()
            {
                PromptTokens = stream.Usage?.PromptTokens ?? 0,
                InputTokens = stream.Usage?.InputTokens ?? 0,
                CompletionTokens = 0,
                TotalTokens = stream.Usage?.InputTokens ?? 0
            };
            await context.Response.WriteAsJsonAsync(new
            {
                input.Model,
                stream.Data,
                stream.Error,
                Object = stream.ObjectTypeName,
                Usage = usage
            });

            //知识库暂不使用message统计
            // await _aiMessageManager.CreateUserMessageAsync(userId, sessionId,
            //     new MessageInputDto
            //     {
            //         Content = string.Empty,
            //         ModelId = input.Model,
            //         TokenUsage = usage,
            //     });
            //
            // await _aiMessageManager.CreateSystemMessageAsync(userId, sessionId,
            //     new MessageInputDto
            //     {
            //         Content = string.Empty,
            //         ModelId = input.Model,
            //         TokenUsage = usage
            //     });

            await _usageStatisticsManager.SetUsageAsync(userId, input.Model, usage, tokenId);
        }
        catch (ThorRateLimitException)
        {
            context.Response.StatusCode = 429;
        }
        catch (UnauthorizedAccessException e)
        {
            context.Response.StatusCode = 401;
        }
        catch (Exception e)
        {
            var errorContent = $"嵌入Ai异常，异常信息：\n当前Ai模型：{input.Model}\n异常信息：{e.Message}\n异常堆栈：{e}";
            throw new UserFriendlyException(errorContent);
        }
    }


    /// <summary>
    /// Anthropic聊天完成-非流式
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="request"></param>
    /// <param name="userId"></param>
    /// <param name="sessionId"></param>
    /// <param name="tokenId">Token Id（Web端传null或Guid.Empty）</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task AnthropicCompleteChatForStatisticsAsync(HttpContext httpContext,
        AnthropicInput request,
        Guid? userId = null,
        Guid? sessionId = null,
        Guid? tokenId = null,
        CancellationToken cancellationToken = default)
    {
        _specialCompatible.AnthropicCompatible(request);
        var response = httpContext.Response;
        // 设置响应头，声明是 json
        //response.ContentType = "application/json; charset=UTF-8";
        var modelDescribe = await GetModelAsync(ModelApiTypeEnum.Messages, request.Model);

        var sourceModelId = request.Model;
        request.Model = ModelConst.ProcessModelId(request.Model);

        var chatService =
            LazyServiceProvider.GetRequiredKeyedService<IAnthropicChatCompletionService>(modelDescribe.HandlerName);
        var data = await chatService.ChatCompletionsAsync(modelDescribe, request, cancellationToken);

        var currentUsage = data.Usage;
        ThorUsageResponse tokenUsage = new ThorUsageResponse
        {
            InputTokens = (currentUsage?.InputTokens??0) + (currentUsage?.CacheCreationInputTokens??0)+ (currentUsage?.CacheReadInputTokens??0),
            OutputTokens = (currentUsage?.OutputTokens??0),
            TotalTokens = (currentUsage?.InputTokens??0) + (currentUsage?.CacheCreationInputTokens??0)+ (currentUsage?.CacheReadInputTokens??0)+(currentUsage?.OutputTokens??0)
        };
        
        
        tokenUsage.SetSupplementalMultiplier(modelDescribe.Multiplier);

        if (userId is not null)
        {
            await _aiMessageManager.CreateUserMessageAsync(userId.Value, sessionId,
                new MessageInputDto
                {
                    Content = "不予存储",
                    ModelId = sourceModelId,
                    TokenUsage = tokenUsage,
                }, tokenId);

            await _aiMessageManager.CreateSystemMessageAsync(userId.Value, sessionId,
                new MessageInputDto
                {
                    Content = "不予存储",
                    ModelId = sourceModelId,
                    TokenUsage = tokenUsage
                }, tokenId);

            await _usageStatisticsManager.SetUsageAsync(userId.Value, sourceModelId, tokenUsage, tokenId);

            /*
            // 直接扣减尊享token包用量
            var totalTokens = tokenUsage.TotalTokens ?? 0;
            if (totalTokens > 0)
            {
                await PremiumPackageManager.TryConsumeTokensAsync(userId.Value, totalTokens);
            }
            */
        }

        await response.WriteAsJsonAsync(data, cancellationToken);
    }


    /// <summary>
    /// Anthropic聊天完成-缓存处理
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="request"></param>
    /// <param name="userId"></param>
    /// <param name="sessionId"></param>
    /// <param name="tokenId">Token Id（Web端传null或Guid.Empty）</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task AnthropicCompleteChatStreamForStatisticsAsync(
        HttpContext httpContext,
        AnthropicInput request,
        Guid? userId = null,
        Guid? sessionId = null,
        Guid? tokenId = null,
        CancellationToken cancellationToken = default)
    {
        var response = httpContext.Response;
        // 注意：SSE响应头推迟到第一条消息成功获取后再设置

        _specialCompatible.AnthropicCompatible(request);
        var modelDescribe = await GetModelAsync(ModelApiTypeEnum.Messages, request.Model);
        var chatService =
            LazyServiceProvider.GetRequiredKeyedService<IAnthropicChatCompletionService>(modelDescribe.HandlerName);

        var sourceModelId = request.Model;
        request.Model = ModelConst.ProcessModelId(request.Model);

        var completeChatResponse = chatService.StreamChatCompletionsAsync(modelDescribe, request, cancellationToken);
        ThorUsageResponse? tokenUsage = new ThorUsageResponse();
        bool isFirst = true;
        try
        {
            await foreach (var responseResult in completeChatResponse)
            {
                // 第一条消息成功获取，才设置 SSE 响应头
                if (isFirst)
                {
                    response.ContentType = "text/event-stream;charset=utf-8;";
                    response.Headers.TryAdd("Cache-Control", "no-cache");
                    response.Headers.TryAdd("Connection", "keep-alive");
                    isFirst = false;
                }

                if (responseResult.Item1.Contains("exception"))
                {
                    //兼容部分ai工具问题
                    continue;
                }
                //部分供应商message_start放一部分
                if (responseResult.Item1.Contains("message_start"))
                {
                    var currentTokenUsage = responseResult.Item2.Message.Usage;
                    if ((currentTokenUsage.InputTokens ?? 0) != 0)
                    {
                        tokenUsage.InputTokens = (currentTokenUsage?.InputTokens??0) + (currentTokenUsage?.CacheCreationInputTokens??0)+ (currentTokenUsage?.CacheReadInputTokens??0);
                    }
                    if ((currentTokenUsage.OutputTokens ?? 0) != 0)
                    {
                        tokenUsage.OutputTokens = currentTokenUsage.OutputTokens;
                    }
                }

                //message_delta又放一部分
                if (responseResult.Item1.Contains("message_delta"))
                {
                    var currentTokenUsage = responseResult.Item2.Usage;

                    if ((currentTokenUsage.InputTokens ?? 0) != 0)
                    {
                        tokenUsage.InputTokens =  (currentTokenUsage?.InputTokens??0) + (currentTokenUsage?.CacheCreationInputTokens??0)+ (currentTokenUsage?.CacheReadInputTokens??0);;
                    }
                    if ((currentTokenUsage.OutputTokens ?? 0) != 0)
                    {
                        tokenUsage.OutputTokens = currentTokenUsage.OutputTokens;
                    }
                }
                
               
                await WriteAsEventStreamDataAsync(httpContext, responseResult.Item1, responseResult.Item2,
                    cancellationToken);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Ai对话异常，用户ID：{userId}");
            var errorContent = $"对话Ai异常，异常信息：\n当前Ai模型：{sourceModelId}\n异常信息：{e.Message}\n异常堆栈：{e}";
            throw new UserFriendlyException(errorContent);
        }
        tokenUsage.TotalTokens = (tokenUsage.InputTokens ?? 0) + (tokenUsage.OutputTokens ?? 0);
        tokenUsage.SetSupplementalMultiplier(modelDescribe.Multiplier);
        await _aiMessageManager.CreateUserMessageAsync(userId, sessionId,
            new MessageInputDto
            {
                Content = "不予存储",
                ModelId = sourceModelId,
                TokenUsage = tokenUsage,
            }, tokenId);

        await _aiMessageManager.CreateSystemMessageAsync(userId, sessionId,
            new MessageInputDto
            {
                Content = "不予存储",
                ModelId = sourceModelId,
                TokenUsage = tokenUsage
            }, tokenId);

        await _usageStatisticsManager.SetUsageAsync(userId, sourceModelId, tokenUsage, tokenId);

        /*
        // 直接扣减尊享token包用量
        if (userId.HasValue && tokenUsage is not null)
        {
            var totalTokens = tokenUsage.TotalTokens ?? 0;
            if (tokenUsage.TotalTokens > 0)
            {
                await PremiumPackageManager.TryConsumeTokensAsync(userId.Value, totalTokens);
            }
        }
        */
    }


    /// <summary>
    /// OpenAi 响应-非流式-缓存处理
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="request"></param>
    /// <param name="userId"></param>
    /// <param name="sessionId"></param>
    /// <param name="tokenId"></param>
    /// <param name="cancellationToken"></param>
    public async Task OpenAiResponsesAsyncForStatisticsAsync(HttpContext httpContext,
        OpenAiResponsesInput request,
        Guid? userId = null,
        Guid? sessionId = null,
        Guid? tokenId = null,
        CancellationToken cancellationToken = default)
    {
        // _specialCompatible.AnthropicCompatible(request);
        var response = httpContext.Response;
        // 设置响应头，声明是 json
        //response.ContentType = "application/json; charset=UTF-8";
        var modelDescribe = await GetModelAsync(ModelApiTypeEnum.Responses, request.Model);

        var chatService =
            LazyServiceProvider.GetRequiredKeyedService<IOpenAiResponseService>(modelDescribe.HandlerName);
        var sourceModelId = request.Model;
        request.Model = ModelConst.ProcessModelId(request.Model);

        var data = await chatService.ResponsesAsync(modelDescribe, request, cancellationToken);

        data.SupplementalMultiplier(modelDescribe.Multiplier);

        var tokenUsage = new ThorUsageResponse
        {
            InputTokens = data.Usage.InputTokens,
            OutputTokens = data.Usage.OutputTokens,
            TotalTokens = data.Usage.InputTokens + data.Usage.OutputTokens,
        };
        if (userId is not null)
        {
            await _aiMessageManager.CreateUserMessageAsync(userId.Value, sessionId,
                new MessageInputDto
                {
                    Content = "不予存储",
                    ModelId = sourceModelId,
                    TokenUsage = tokenUsage,
                }, tokenId);

            await _aiMessageManager.CreateSystemMessageAsync(userId.Value, sessionId,
                new MessageInputDto
                {
                    Content = "不予存储",
                    ModelId = sourceModelId,
                    TokenUsage = tokenUsage
                }, tokenId);

            await _usageStatisticsManager.SetUsageAsync(userId.Value, sourceModelId, tokenUsage, tokenId);

            /*
            // 直接扣减尊享token包用量
            var totalTokens = tokenUsage.TotalTokens ?? 0;
            if (totalTokens > 0)
            {
                await PremiumPackageManager.TryConsumeTokensAsync(userId.Value, totalTokens);
            }
            */
        }

        await response.WriteAsJsonAsync(data, cancellationToken);
    }


    /// <summary>
    /// OpenAi响应-流式-缓存处理
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="request"></param>
    /// <param name="userId"></param>
    /// <param name="sessionId"></param>
    /// <param name="tokenId">Token Id（Web端传null或Guid.Empty）</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task OpenAiResponsesStreamForStatisticsAsync(
        HttpContext httpContext,
        OpenAiResponsesInput request,
        Guid? userId = null,
        Guid? sessionId = null,
        Guid? tokenId = null,
        CancellationToken cancellationToken = default)
    {
        var response = httpContext.Response;
        // 设置响应头，声明是 SSE 流
        response.ContentType = "text/event-stream;charset=utf-8;";
        response.Headers.TryAdd("Cache-Control", "no-cache");
        response.Headers.TryAdd("Connection", "keep-alive");

        var modelDescribe = await GetModelAsync(ModelApiTypeEnum.Responses, request.Model);
        var chatService =
            LazyServiceProvider.GetRequiredKeyedService<IOpenAiResponseService>(modelDescribe.HandlerName);
        var sourceModelId = request.Model;
        request.Model = ModelConst.ProcessModelId(request.Model);

        var completeChatResponse = chatService.ResponsesStreamAsync(modelDescribe, request, cancellationToken);
        ThorUsageResponse? tokenUsage = null;
        try
        {
            await foreach (var responseResult in completeChatResponse)
            {
                //message_start是为了保底机制
                if (responseResult.Item1.Contains("response.completed"))
                {
                    var obj = responseResult.Item2!.Value;
                    int inputTokens = obj.GetPath("response", "usage", "input_tokens").GetInt();
                    int outputTokens = obj.GetPath("response", "usage", "output_tokens").GetInt();
                    inputTokens = Convert.ToInt32(inputTokens * modelDescribe.Multiplier);
                    outputTokens = Convert.ToInt32(outputTokens * modelDescribe.Multiplier);
                    tokenUsage = new ThorUsageResponse
                    {
                        PromptTokens = inputTokens,
                        InputTokens = inputTokens,
                        OutputTokens = outputTokens,
                        CompletionTokens = outputTokens,
                        TotalTokens = inputTokens + outputTokens,
                    };
                }

                await WriteAsEventStreamDataAsync(httpContext, responseResult.Item1, responseResult.Item2,
                    cancellationToken);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Ai响应异常，用户ID：{userId}");
            var errorContent = $"响应Ai异常，异常信息：\n当前Ai模型：{request.Model}\n异常信息：{e.Message}\n异常堆栈：{e}";
            throw new UserFriendlyException(errorContent);
        }

        await _aiMessageManager.CreateUserMessageAsync(userId, sessionId,
            new MessageInputDto
            {
                Content = "不予存储",
                ModelId = sourceModelId,
                TokenUsage = tokenUsage,
            }, tokenId);

        await _aiMessageManager.CreateSystemMessageAsync(userId, sessionId,
            new MessageInputDto
            {
                Content = "不予存储",
                ModelId = sourceModelId,
                TokenUsage = tokenUsage
            }, tokenId);

        await _usageStatisticsManager.SetUsageAsync(userId, sourceModelId, tokenUsage, tokenId);

        /*
        // 直接扣减尊享token包用量
        if (userId.HasValue && tokenUsage is not null)
        {
            var totalTokens = tokenUsage.TotalTokens ?? 0;
            if (tokenUsage.TotalTokens > 0)
            {
                await PremiumPackageManager.TryConsumeTokensAsync(userId.Value, totalTokens);
            }
        }
        */
    }


    /// <summary>
    /// Gemini 生成-非流式-缓存处理
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="modelId"></param>
    /// <param name="request"></param>
    /// <param name="userId"></param>
    /// <param name="sessionId"></param>
    /// <param name="tokenId"></param>
    /// <param name="cancellationToken"></param>
    public async Task GeminiGenerateContentForStatisticsAsync(HttpContext httpContext,
        string modelId,
        JsonElement request,
        Guid? userId = null,
        Guid? sessionId = null,
        Guid? tokenId = null,
        CancellationToken cancellationToken = default)
    {
        var response = httpContext.Response;
        var modelDescribe = await GetModelAsync(ModelApiTypeEnum.GenerateContent, modelId);

        var chatService =
            LazyServiceProvider.GetRequiredKeyedService<IGeminiGenerateContentService>(modelDescribe.HandlerName);
        var data = await chatService.GenerateContentAsync(modelDescribe, request, cancellationToken);

        var tokenUsage = GeminiGenerateContentAcquirer.GetUsage(data);
        //如果是图片模型，单独扣费
        if (modelDescribe.ModelType == ModelTypeEnum.Image)
        {
            tokenUsage = new ThorUsageResponse
            {
                InputTokens = (int)modelDescribe.Multiplier,
                OutputTokens = (int)modelDescribe.Multiplier,
                TotalTokens = (int)modelDescribe.Multiplier
            };
        }
        else
        {
            tokenUsage.SetSupplementalMultiplier(modelDescribe.Multiplier);
        }

        if (userId is not null)
        {
            await _aiMessageManager.CreateUserMessageAsync(userId.Value, sessionId,
                new MessageInputDto
                {
                    Content = "不予存储",
                    ModelId = modelId,
                    TokenUsage = tokenUsage,
                }, tokenId);

            await _aiMessageManager.CreateSystemMessageAsync(userId.Value, sessionId,
                new MessageInputDto
                {
                    Content = "不予存储",
                    ModelId = modelId,
                    TokenUsage = tokenUsage
                }, tokenId);

            await _usageStatisticsManager.SetUsageAsync(userId.Value, modelId, tokenUsage, tokenId);

            /*
            // 直接扣减尊享token包用量
            var totalTokens = tokenUsage.TotalTokens ?? 0;
            if (totalTokens > 0)
            {
                await PremiumPackageManager.TryConsumeTokensAsync(userId.Value, totalTokens);
            }
            */
        }

        await response.WriteAsJsonAsync(data, cancellationToken);
    }


    /// <summary>
    /// Gemini 生成-流式-缓存处理
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="modelId"></param>
    /// <param name="request"></param>
    /// <param name="userId"></param>
    /// <param name="sessionId"></param>
    /// <param name="tokenId">Token Id（Web端传null或Guid.Empty）</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task GeminiGenerateContentStreamForStatisticsAsync(
        HttpContext httpContext,
        string modelId,
        JsonElement request,
        Guid? userId = null,
        Guid? sessionId = null,
        Guid? tokenId = null,
        CancellationToken cancellationToken = default)
    {
        var response = httpContext.Response;
        // 设置响应头，声明是 SSE 流
        response.ContentType = "text/event-stream;charset=utf-8;";
        response.Headers.TryAdd("Cache-Control", "no-cache");
        response.Headers.TryAdd("Connection", "keep-alive");

        var modelDescribe = await GetModelAsync(ModelApiTypeEnum.GenerateContent, modelId);
        var chatService =
            LazyServiceProvider.GetRequiredKeyedService<IGeminiGenerateContentService>(modelDescribe.HandlerName);

        var completeChatResponse = chatService.GenerateContentStreamAsync(modelDescribe, request, cancellationToken);
        ThorUsageResponse? tokenUsage = null;
        try
        {
            await foreach (var responseResult in completeChatResponse)
            {
                if (responseResult!.Value.GetPath("candidates", 0, "finishReason").GetString() == "STOP")
                {
                    tokenUsage = GeminiGenerateContentAcquirer.GetUsage(responseResult!.Value);
                    //如果是图片模型，单独扣费
                    if (modelDescribe.ModelType == ModelTypeEnum.Image)
                    {
                        tokenUsage = new ThorUsageResponse
                        {
                            InputTokens = (int)modelDescribe.Multiplier,
                            OutputTokens = (int)modelDescribe.Multiplier,
                            TotalTokens = (int)modelDescribe.Multiplier
                        };
                    }
                    else
                    {
                        tokenUsage.SetSupplementalMultiplier(modelDescribe.Multiplier);
                    }
                }

                await response.WriteAsync($"data: {JsonSerializer.Serialize(responseResult)}\n\n", Encoding.UTF8,
                    cancellationToken).ConfigureAwait(false);
                await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Ai生成异常，用户ID：{userId}");
            var errorContent = $"生成Ai异常，异常信息：\n当前Ai模型：{modelId}\n异常信息：{e.Message}\n异常堆栈：{e}";
            throw new UserFriendlyException(errorContent);
        }

        await _aiMessageManager.CreateUserMessageAsync(userId, sessionId,
            new MessageInputDto
            {
                Content = "不予存储",
                ModelId = modelId,
                TokenUsage = tokenUsage,
            }, tokenId);

        await _aiMessageManager.CreateSystemMessageAsync(userId, sessionId,
            new MessageInputDto
            {
                Content = "不予存储",
                ModelId = modelId,
                TokenUsage = tokenUsage
            }, tokenId);

        await _usageStatisticsManager.SetUsageAsync(userId, modelId, tokenUsage, tokenId);

        /*
        // 直接扣减尊享token包用量
        if (userId.HasValue && tokenUsage is not null)
        {
            var totalTokens = tokenUsage.TotalTokens ?? 0;
            if (tokenUsage.TotalTokens > 0)
            {
                await PremiumPackageManager.TryConsumeTokensAsync(userId.Value, totalTokens);
            }
        }
        */
    }

    private const string ImageStoreHost = "https://ccnetcore.com/prod-api";

    /// <summary>
    /// Gemini 生成(Image)-非流式-缓存处理
    /// 返回图片绝对路径
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="modelId"></param>
    /// <param name="request"></param>
    /// <param name="userId"></param>
    /// <param name="sessionId"></param>
    /// <param name="tokenId"></param>
    /// <param name="cancellationToken"></param>
    public async Task GeminiGenerateContentImageForStatisticsAsync(
        Guid taskId,
        string modelId,
        JsonElement request,
        Guid userId,
        Guid? sessionId = null,
        Guid? tokenId = null,
        CancellationToken cancellationToken = default)
    {
        var imageStoreTask = await _imageStoreTaskRepository.GetFirstAsync(x => x.Id == taskId);
        var modelDescribe = await GetModelAsync(ModelApiTypeEnum.GenerateContent, modelId);

        var chatService =
            LazyServiceProvider.GetRequiredKeyedService<IGeminiGenerateContentService>(modelDescribe.HandlerName);
        var data = await chatService.GenerateContentAsync(modelDescribe, request, cancellationToken);

        // 检查是否被大模型内容安全策略拦截
        var rawResponse = data.GetRawText();
        if (rawResponse.Contains("policies.google.com/terms/generative-ai/use-policy"))
        {
            _logger.LogWarning($"图片生成被内容安全策略拦截，模型:【{modelId}】，请求信息：【{request}】");
            throw new UserFriendlyException("您的提示词涉及敏感信息，已被大模型拦截，请调整提示词后再试！");
        }

        //解析json，获取base64字符串
        var imagePrefixBase64 = GeminiGenerateContentAcquirer.GetImagePrefixBase64(data);
        if (string.IsNullOrWhiteSpace(imagePrefixBase64))
        {
            _logger.LogError($"图片生成解析失败，模型:【{modelId}】，请求信息：【{request}】，请求响应信息：【{data}】");
            throw new UserFriendlyException("大模型没有返回图片，请调整提示词或稍后再试");
        }

        //远程调用上传接口，将base64转换为URL
        var httpClient = LazyServiceProvider.LazyGetRequiredService<IHttpClientFactory>().CreateClient();
        // var uploadUrl = $"https://ccnetcore.com/prod-api/ai-hub/ai-image/upload-base64";
        var uploadUrl = $"{ImageStoreHost}/ai-image/upload-base64";
        var content = new StringContent(JsonSerializer.Serialize(imagePrefixBase64), Encoding.UTF8, "application/json");
        var uploadResponse = await httpClient.PostAsync(uploadUrl, content, cancellationToken);
        uploadResponse.EnsureSuccessStatusCode();
        var storeUrl = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);

        var tokenUsage = new ThorUsageResponse
        {
            InputTokens = (int)modelDescribe.Multiplier,
            OutputTokens = (int)modelDescribe.Multiplier,
            TotalTokens = (int)modelDescribe.Multiplier,
        };

        await _aiMessageManager.CreateSystemMessageAsync(userId, sessionId,
            new MessageInputDto
            {
                Content = "不予存储",
                ModelId = modelId,
                TokenUsage = tokenUsage
            }, tokenId);

        await _usageStatisticsManager.SetUsageAsync(userId, modelId, tokenUsage, tokenId);

        /*
        // 直接扣减尊享token包用量
        var totalTokens = tokenUsage.TotalTokens ?? 0;
        if (totalTokens > 0)
        {
            await PremiumPackageManager.TryConsumeTokensAsync(userId, totalTokens);
        }
        */

        //设置存储base64和url
        imageStoreTask.SetSuccess($"{ImageStoreHost}{storeUrl}");
        await _imageStoreTaskRepository.UpdateAsync(imageStoreTask);
    }

    /// <summary>
    /// 统一流式处理 - 支持4种API类型的原封不动转发
    /// </summary>
    public async Task UnifiedStreamForStatisticsAsync(
        HttpContext httpContext,
        ModelApiTypeEnum apiType,
        JsonElement requestBody,
        string modelId,
        Guid? userId = null,
        Guid? sessionId = null,
        Guid? tokenId = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var response = httpContext.Response;
        // 设置响应头，声明是 SSE 流
        response.ContentType = "text/event-stream;charset=utf-8;";
        response.Headers.TryAdd("Cache-Control", "no-cache");
        response.Headers.TryAdd("Connection", "keep-alive");

        var sourceModelId = modelId;
        // 处理模型前缀
        modelId = ModelConst.RemoveModelPrefix(modelId);

        var modelDescribe = await GetModelAsync(apiType, sourceModelId);

        // 公共缓存队列
        var messageQueue = new ConcurrentQueue<string>();
        var outputInterval = TimeSpan.FromMilliseconds(75);
        var isComplete = false;

        // 公共消费任务
        var outputTask = Task.Run(async () =>
        {
            while (!(isComplete && messageQueue.IsEmpty))
            {
                if (messageQueue.TryDequeue(out var message))
                {
                    await response.WriteAsync(message, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
                    await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                if (!isComplete)
                {
                    await Task.Delay(outputInterval, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                }
            }
        }, cancellationToken);
        
        StreamProcessResult? processResult = null;

        switch (apiType)
        {
            case ModelApiTypeEnum.Completions:
                processResult = await ProcessCompletionsStreamAsync(messageQueue, requestBody, modelDescribe, userId, cancellationToken);
                break;
            case ModelApiTypeEnum.Messages:
                processResult = await ProcessAnthropicStreamAsync(messageQueue, requestBody, modelDescribe, userId, cancellationToken);
                break;
            case ModelApiTypeEnum.Responses:
                processResult = await ProcessOpenAiResponsesStreamAsync(messageQueue, requestBody, modelDescribe, userId, cancellationToken);
                break;
            case ModelApiTypeEnum.GenerateContent:
                processResult = await ProcessGeminiStreamAsync(messageQueue, requestBody, modelDescribe, userId, cancellationToken);
                break;
            default:
                throw new UserFriendlyException($"不支持的API类型: {apiType}");
        }
        
        
        // 统一的统计处理
        var userMessageId = await _aiMessageManager.CreateUserMessageAsync(userId, sessionId,
            new MessageInputDto
            {
                Content = sessionId is null ? "不予存储" : processResult?.UserContent ?? string.Empty,
                ModelId = sourceModelId,
                TokenUsage = processResult?.TokenUsage,
            }, tokenId,createTime:startTime);

        var systemMessageId = await _aiMessageManager.CreateSystemMessageAsync(userId, sessionId,
            new MessageInputDto
            {
                Content = sessionId is null ? "不予存储" : processResult?.SystemContent ?? string.Empty,
                ModelId = sourceModelId,
                TokenUsage = processResult?.TokenUsage
            }, tokenId);

        // 流式返回消息ID
        var now = DateTime.Now;
        var userMessageOutput = new MessageCreatedOutput
        {
            TypeEnum = ChatMessageTypeEnum.UserMessage,
            MessageId = userMessageId,
            CreationTime = startTime
        };
        messageQueue.Enqueue($"data: {JsonSerializer.Serialize(userMessageOutput, ThorJsonSerializer.DefaultOptions)}\n\n");

        var systemMessageOutput = new MessageCreatedOutput
        {
            TypeEnum = ChatMessageTypeEnum.SystemMessage,
            MessageId = systemMessageId,
            CreationTime = now
        };
        messageQueue.Enqueue($"data: {JsonSerializer.Serialize(systemMessageOutput, ThorJsonSerializer.DefaultOptions)}\n\n");

        // 标记完成并等待消费任务结束
        messageQueue.Enqueue("data: [DONE]\n\n");
        isComplete = true;
        await outputTask;
        
        await _usageStatisticsManager.SetUsageAsync(userId, sourceModelId, processResult?.TokenUsage, tokenId);

        /*
        // 扣减尊享token包用量
        if (userId.HasValue && processResult?.TokenUsage is not null && modelDescribe.IsPremium)
        {
            var totalTokens = processResult?.TokenUsage.TotalTokens ?? 0;
            if (totalTokens > 0)
            {
                await PremiumPackageManager.TryConsumeTokensAsync(userId.Value, totalTokens);
            }
        }
        */
    }

    #region 统一流式处理 - 各API类型的具体实现

    /// <summary>
    /// 流式处理结果，包含用户输入、系统输出和 token 使用情况
    /// </summary>
    private class StreamProcessResult
    {
        public string UserContent { get; set; } = string.Empty;
        public string SystemContent { get; set; } = string.Empty;
        public ThorUsageResponse TokenUsage { get; set; } = new();
    }

    /// <summary>
    /// 处理 OpenAI Completions 格式流式响应
    /// </summary>
    private async Task<StreamProcessResult> ProcessCompletionsStreamAsync(
        ConcurrentQueue<string> messageQueue,
        JsonElement requestBody,
        AiModelDescribe modelDescribe,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var request = requestBody.Deserialize<ThorChatCompletionsRequest>(ThorJsonSerializer.DefaultOptions)!;
        _specialCompatible.Compatible(request);

        // 提取用户最后一条消息
        var userContent = request.Messages?.LastOrDefault()?.MessagesStore ?? string.Empty;

        // 处理模型前缀
        request.Model = ModelConst.ProcessModelId(request.Model);

        var chatService = LazyServiceProvider.GetRequiredKeyedService<IChatCompletionService>(modelDescribe.HandlerName);
        var completeChatResponse = chatService.CompleteChatStreamAsync(modelDescribe, request, cancellationToken);
        var tokenUsage = new ThorUsageResponse();
        var systemContentBuilder = new StringBuilder();

        try
        {
            await foreach (var data in completeChatResponse)
            {
                data.SupplementalMultiplier(modelDescribe.Multiplier);
                if (data.Usage is not null && (data.Usage.CompletionTokens > 0 || data.Usage.OutputTokens > 0))
                {
                    tokenUsage = data.Usage;
                }

                // 累加系统输出内容 (choices[].delta.content)
                var deltaContent = data.Choices?.FirstOrDefault()?.Delta?.Content;
                if (!string.IsNullOrEmpty(deltaContent))
                {
                    systemContentBuilder.Append(deltaContent);
                }

                var message = JsonSerializer.Serialize(data, ThorJsonSerializer.DefaultOptions);
                messageQueue.Enqueue($"data: {message}\n\n");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ai对话异常，用户ID：{UserId}", userId);
            var errorContent = $"对话Ai异常，异常信息：\n当前Ai模型：{request.Model}\n异常信息：{e.Message}\n异常堆栈：{e}";
            systemContentBuilder.Append(errorContent);
            var model = new ThorChatCompletionsResponse()
            {
                Choices = new List<ThorChatChoiceResponse>()
                {
                    new ThorChatChoiceResponse()
                    {
                        Delta = new ThorChatMessage()
                        {
                            Content = errorContent
                        }
                    }
                }
            };
            var errorMessage = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            messageQueue.Enqueue($"data: {errorMessage}\n\n");
        }
        
        return new StreamProcessResult
        {
            UserContent = userContent,
            SystemContent = systemContentBuilder.ToString(),
            TokenUsage = tokenUsage
        };
    }

    /// <summary>
    /// 处理 Anthropic Messages 格式流式响应
    /// </summary>
    private async Task<StreamProcessResult> ProcessAnthropicStreamAsync(
        ConcurrentQueue<string> messageQueue,
        JsonElement requestBody,
        AiModelDescribe modelDescribe,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var request = requestBody.Deserialize<AnthropicInput>(ThorJsonSerializer.DefaultOptions)!;
        _specialCompatible.AnthropicCompatible(request);

        // 提取用户最后一条消息
        var lastMessage = request.Messages?.LastOrDefault();
        var userContent = lastMessage?.Content ?? string.Empty;
        if (string.IsNullOrEmpty(userContent) && lastMessage?.Contents != null && lastMessage.Contents.Any())
        {
            // 如果是 Contents 数组，提取第一个 text 类型的内容
            var textContent = lastMessage.Contents.FirstOrDefault(c => c.Type == "text");
            userContent = textContent?.Text ?? System.Text.Json.JsonSerializer.Serialize(lastMessage.Contents);
        }

        // 处理模型前缀
        request.Model = ModelConst.ProcessModelId(request.Model);

        var chatService = LazyServiceProvider.GetRequiredKeyedService<IAnthropicChatCompletionService>(modelDescribe.HandlerName);
        var completeChatResponse = chatService.StreamChatCompletionsAsync(modelDescribe, request, cancellationToken);
        var tokenUsage = new ThorUsageResponse();
        var systemContentBuilder = new StringBuilder();

        try
        {
            await foreach (var responseResult in completeChatResponse)
            {
                // 部分供应商message_start放一部分
                if (responseResult.Item1.Contains("message_start"))
                {
                    var currentTokenUsage = responseResult.Item2?.Message?.Usage;
                    if (currentTokenUsage != null)
                    {
                        if ((currentTokenUsage.InputTokens ?? 0) != 0)
                        {
                            tokenUsage.InputTokens = (currentTokenUsage.InputTokens ?? 0) +
                                                     (currentTokenUsage.CacheCreationInputTokens ?? 0) +
                                                     (currentTokenUsage.CacheReadInputTokens ?? 0);
                        }
                        if ((currentTokenUsage.OutputTokens ?? 0) != 0)
                        {
                            tokenUsage.OutputTokens = currentTokenUsage.OutputTokens;
                        }
                    }
                }

                // message_delta又放一部分
                if (responseResult.Item1.Contains("message_delta"))
                {
                    var currentTokenUsage = responseResult.Item2?.Usage;
                    if (currentTokenUsage != null)
                    {
                        if ((currentTokenUsage.InputTokens ?? 0) != 0)
                        {
                            tokenUsage.InputTokens = (currentTokenUsage.InputTokens ?? 0) +
                                                     (currentTokenUsage.CacheCreationInputTokens ?? 0) +
                                                     (currentTokenUsage.CacheReadInputTokens ?? 0);
                        }
                        if ((currentTokenUsage.OutputTokens ?? 0) != 0)
                        {
                            tokenUsage.OutputTokens = currentTokenUsage.OutputTokens;
                        }
                    }
                }

                // 累加系统输出内容 (delta.text)
                var deltaText = responseResult.Item2?.Delta?.Text;
                if (!string.IsNullOrEmpty(deltaText))
                {
                    systemContentBuilder.Append(deltaText);
                }

                // 序列化为SSE格式字符串
                var data = JsonSerializer.Serialize(responseResult.Item2, ThorJsonSerializer.DefaultOptions);
                messageQueue.Enqueue($"{responseResult.Item1.Trim()}\ndata: {data}\n\n");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ai对话异常，用户ID：{UserId}", userId);
            var errorContent = $"对话Ai异常，异常信息：\n当前Ai模型：{request.Model}\n异常信息：{e.Message}\n异常堆栈：{e}";
            systemContentBuilder.Append(errorContent);
            throw new UserFriendlyException(errorContent);
        }

        tokenUsage.TotalTokens = (tokenUsage.InputTokens ?? 0) + (tokenUsage.OutputTokens ?? 0);
        tokenUsage.SetSupplementalMultiplier(modelDescribe.Multiplier);

        return new StreamProcessResult
        {
            UserContent = userContent,
            SystemContent = systemContentBuilder.ToString(),
            TokenUsage = tokenUsage
        };
    }

    /// <summary>
    /// 处理 OpenAI Responses 格式流式响应
    /// </summary>
    private async Task<StreamProcessResult> ProcessOpenAiResponsesStreamAsync(
        ConcurrentQueue<string> messageQueue,
        JsonElement requestBody,
        AiModelDescribe modelDescribe,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var request = requestBody.Deserialize<OpenAiResponsesInput>(ThorJsonSerializer.DefaultOptions)!;

        // 提取用户输入内容 (input 字段可能是字符串或数组)
        var userContent = string.Empty;
        if (request.Input.ValueKind == JsonValueKind.String)
        {
            userContent = request.Input.GetString() ?? string.Empty;
        }
        else if (request.Input.ValueKind == JsonValueKind.Array)
        {
            // 获取最后一个 user 角色的消息
            var inputArray = request.Input.EnumerateArray().ToList();
            var lastUserMessage = inputArray.LastOrDefault(x =>
                x.TryGetProperty("role", out var role) && role.GetString() == "user");
            if (lastUserMessage.ValueKind != JsonValueKind.Undefined)
            {
                if (lastUserMessage.TryGetProperty("content", out var content))
                {
                    userContent = content.GetString() ?? string.Empty;
                }
            }
        }

        // 处理模型前缀
        request.Model = ModelConst.ProcessModelId(request.Model);

        var chatService = LazyServiceProvider.GetRequiredKeyedService<IOpenAiResponseService>(modelDescribe.HandlerName);
        var completeChatResponse = chatService.ResponsesStreamAsync(modelDescribe, request, cancellationToken);
        ThorUsageResponse? tokenUsage = null;
        var systemContentBuilder = new StringBuilder();

        try
        {
            await foreach (var responseResult in completeChatResponse)
            {
                // 提取输出文本内容 (response.output_text.delta 事件)
                if (responseResult.Item1.Contains("response.output_text.delta"))
                {
                    var delta = responseResult.Item2?.GetPath("delta").GetString();
                    if (!string.IsNullOrEmpty(delta))
                    {
                        systemContentBuilder.Append(delta);
                    }
                }

                if (responseResult.Item1.Contains("response.completed"))
                {
                    var obj = responseResult.Item2!.Value;
                    int inputTokens = obj.GetPath("response", "usage", "input_tokens").GetInt();
                    int outputTokens = obj.GetPath("response", "usage", "output_tokens").GetInt();
                    inputTokens = Convert.ToInt32(inputTokens * modelDescribe.Multiplier);
                    outputTokens = Convert.ToInt32(outputTokens * modelDescribe.Multiplier);
                    tokenUsage = new ThorUsageResponse
                    {
                        PromptTokens = inputTokens,
                        InputTokens = inputTokens,
                        OutputTokens = outputTokens,
                        CompletionTokens = outputTokens,
                        TotalTokens = inputTokens + outputTokens,
                    };
                }

                // 序列化为SSE格式字符串
                var data = JsonSerializer.Serialize(responseResult.Item2, ThorJsonSerializer.DefaultOptions);
                messageQueue.Enqueue($"{responseResult.Item1.Trim()}\ndata: {data}\n\n");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ai响应异常，用户ID：{UserId}", userId);
            var errorContent = $"响应Ai异常，异常信息：\n当前Ai模型：{request.Model}\n异常信息：{e.Message}\n异常堆栈：{e}";
            systemContentBuilder.Append(errorContent);
            throw new UserFriendlyException(errorContent);
        }

        return new StreamProcessResult
        {
            UserContent = userContent,
            SystemContent = systemContentBuilder.ToString(),
            TokenUsage = tokenUsage ?? new ThorUsageResponse()
        };
    }

    /// <summary>
    /// 处理 Gemini GenerateContent 格式流式响应
    /// </summary>
    private async Task<StreamProcessResult> ProcessGeminiStreamAsync(
        ConcurrentQueue<string> messageQueue,
        JsonElement requestBody,
        AiModelDescribe modelDescribe,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        // 提取用户最后一条消息 (contents[last].parts[last].text)
        var userContent = GeminiGenerateContentAcquirer.GetLastUserContent(requestBody);

        var chatService = LazyServiceProvider.GetRequiredKeyedService<IGeminiGenerateContentService>(modelDescribe.HandlerName);
        var completeChatResponse = chatService.GenerateContentStreamAsync(modelDescribe, requestBody, cancellationToken);
        ThorUsageResponse? tokenUsage = null;
        var systemContentBuilder = new StringBuilder();

        try
        {
            await foreach (var responseResult in completeChatResponse)
            {
                // 累加系统输出内容 (candidates[0].content.parts[].text，排除 thought)
                var textContent = GeminiGenerateContentAcquirer.GetTextContent(responseResult!.Value);
                if (!string.IsNullOrEmpty(textContent))
                {
                    systemContentBuilder.Append(textContent);
                }

                if (responseResult!.Value.GetPath("candidates", 0, "finishReason").GetString() == "STOP")
                {
                    tokenUsage = GeminiGenerateContentAcquirer.GetUsage(responseResult!.Value);
                    // 如果是图片模型，单独扣费
                    if (modelDescribe.ModelType == ModelTypeEnum.Image)
                    {
                        tokenUsage = new ThorUsageResponse
                        {
                            InputTokens = (int)modelDescribe.Multiplier,
                            OutputTokens = (int)modelDescribe.Multiplier,
                            TotalTokens = (int)modelDescribe.Multiplier
                        };
                    }
                    else
                    {
                        tokenUsage.SetSupplementalMultiplier(modelDescribe.Multiplier);
                    }
                }

                messageQueue.Enqueue($"data: {JsonSerializer.Serialize(responseResult)}\n\n");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ai生成异常，用户ID：{UserId}", userId);
            var errorContent = $"生成Ai异常，异常信息：\n当前Ai模型：{modelDescribe.ModelId}\n异常信息：{e.Message}\n异常堆栈：{e}";
            systemContentBuilder.Append(errorContent);
            throw new UserFriendlyException(errorContent);
        }

        return new StreamProcessResult
        {
            UserContent = userContent,
            SystemContent = systemContentBuilder.ToString(),
            TokenUsage = tokenUsage ?? new ThorUsageResponse()
        };
    }

    #endregion

    #region 流式传输格式Http响应

    private static readonly byte[] EventPrefix = "event: "u8.ToArray();
    private static readonly byte[] DataPrefix = "data: "u8.ToArray();
    private static readonly byte[] NewLine = "\n"u8.ToArray();
    private static readonly byte[] DoubleNewLine = "\n\n"u8.ToArray();

    /// <summary>
    ///     使用 JsonSerializer.SerializeAsync 直接序列化到响应流
    /// </summary>
    private static async ValueTask WriteAsEventStreamDataAsync<T>(
        HttpContext context,
        string @event,
        T value,
        CancellationToken cancellationToken = default)
    {
        var response = context.Response;
        var bodyStream = response.Body;
        // 确保 SSE Header 已经设置好
        // e.g. Content-Type: text/event-stream; charset=utf-8
        await response.StartAsync(cancellationToken).ConfigureAwait(false);
        // 写事件类型
        //此处事件前缀重复了
        // await bodyStream.WriteAsync(EventPrefix, cancellationToken).ConfigureAwait(false);
        await WriteUtf8StringAsync(bodyStream, @event.Trim(), cancellationToken).ConfigureAwait(false);
        await bodyStream.WriteAsync(NewLine, cancellationToken).ConfigureAwait(false);
        // 写 data: + JSON
        await bodyStream.WriteAsync(DataPrefix, cancellationToken).ConfigureAwait(false);
        await JsonSerializer.SerializeAsync(
            bodyStream,
            value,
            ThorJsonSerializer.DefaultOptions,
            cancellationToken
        ).ConfigureAwait(false);
        // 事件结束 \n\n
        await bodyStream.WriteAsync(DoubleNewLine, cancellationToken).ConfigureAwait(false);
        // 及时把数据发送给客户端
        await bodyStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }


    private static async ValueTask WriteUtf8StringAsync(Stream stream, string value, CancellationToken token)
    {
        if (string.IsNullOrEmpty(value))
            return;
        var buffer = Encoding.UTF8.GetBytes(value);
        await stream.WriteAsync(buffer, token).ConfigureAwait(false);
    }

    #endregion
}
