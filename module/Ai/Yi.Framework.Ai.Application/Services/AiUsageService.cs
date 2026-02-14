using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using Yi.Framework.Ai.Application.Contracts.Dtos.AiUsage;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.Ai.Domain.Managers;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Application.Services;

/// <summary>
/// 使用量统计服务
/// </summary>
[Authorize]
public class AiUsageService : ApplicationService
{
    private readonly ISqlSugarRepository<ChatMessage> _messageRepository;
    private readonly ISqlSugarRepository<AiUsage> _usageStatisticsRepository;
    private readonly ISqlSugarRepository<Token> _tokenRepository;
    private readonly ModelManager _modelManager;

    public AiUsageService(
        ISqlSugarRepository<ChatMessage> messageRepository,
        ISqlSugarRepository<AiUsage> usageStatisticsRepository,
        ISqlSugarRepository<Token> tokenRepository,
        ModelManager modelManager)
    {
        _messageRepository = messageRepository;
        _usageStatisticsRepository = usageStatisticsRepository;
        _tokenRepository = tokenRepository;
        _modelManager = modelManager;
    }

    /// <summary>
    /// 获取当前用户近7天的Token消耗统计
    /// </summary>
    /// <returns>每日Token使用量列表</returns>
    [HttpGet("ai-usage/last-7-days")]
    public async Task<List<DailyTokenUsageDto>> GetLast7DaysTokenUsageAsync([FromQuery] UsageStatisticsGetInput input)
    {
        var userId = CurrentUser.GetId();
        var endDate = DateTime.Today;
        var startDate = endDate.AddDays(-6); // 近7天

        // 从Message表统计近7天的token消耗
        var dailyUsage = await _messageRepository._DbQueryable
            .Where(x => x.UserId == userId)
            .Where(x => x.Role == "system")
            .Where(x => x.CreationTime >= startDate && x.CreationTime < endDate.AddDays(1))
            //.WhereIF(input.TokenId.HasValue, x => x.TokenId == input.TokenId) // ChatMessage doesn't have TokenId? It did in AiHub MessageAggregateRoot. Let's check ChatMessage
            .GroupBy(x => x.CreationTime.Date)
            .Select(g => new
            {
                Date = g.CreationTime.Date,
                Tokens = SqlFunc.AggregateSum(g.TokenUsage.TotalTokenCount)
            })
            .ToListAsync();

        // 生成完整的7天数据，包括没有使用记录的日期
        var result = new List<DailyTokenUsageDto>();
        for (int i = 0; i < 7; i++)
        {
            var date = startDate.AddDays(i);
            var usage = dailyUsage.FirstOrDefault(x => x.Date == date);

            result.Add(new DailyTokenUsageDto
            {
                Date = date,
                Tokens = usage?.Tokens ?? 0
            });
        }

        return result.OrderBy(x => x.Date).ToList();
    }

    /// <summary>
    /// 获取当前用户各个模型的Token消耗量及占比
    /// </summary>
    /// <returns>模型Token使用量列表</returns>
    [HttpGet("ai-usage/model-usage")]
    public async Task<List<ModelTokenUsageDto>> GetModelTokenUsageAsync([FromQuery] UsageStatisticsGetInput input)
    {
        var userId = CurrentUser.GetId();

        // 从UsageStatistics表获取各模型的token消耗统计
        var modelUsages = await _usageStatisticsRepository._DbQueryable
            .Where(x => x.UserId == userId)
            .WhereIF(input.TokenId.HasValue, x => x.TokenId == input.TokenId)
            .GroupBy(x => x.ModelId)
            .Select(x => new
            {
                ModelId = x.ModelId,
                TotalTokenCount = SqlFunc.AggregateSum(x.TotalTokenCount)
            })
            .ToListAsync();

        if (!modelUsages.Any())
        {
            return new List<ModelTokenUsageDto>();
        }

        // 计算总token数
        var totalTokens = modelUsages.Sum(x => x.TotalTokenCount);

        // 计算各模型占比
        var result = modelUsages.Select(x => new ModelTokenUsageDto
        {
            Model = x.ModelId,
            Tokens = x.TotalTokenCount,
            Percentage = totalTokens > 0 ? Math.Round((decimal)x.TotalTokenCount / totalTokens * 100, 2) : 0
        }).OrderByDescending(x => x.Tokens).ToList();

        return result;
    }
}
