using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using SharpFort.Ai.Application.Contracts.Dtos.UsageStatistics;
using SharpFort.Ai.Application.Contracts.IServices;
using SharpFort.Ai.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Application.Services;

/// <summary>
/// 使用量统计服务
/// </summary>
[Authorize]
public class UsageStatisticsService(
    ISqlSugarRepository<ChatMessage> messageRepository,
    ISqlSugarRepository<AiUsage> usageStatisticsRepository,
    ISqlSugarRepository<Token> tokenRepository,
    ISqlSugarRepository<AiModel> aiModelRepository) : ApplicationService, IUsageStatisticsService
{
    private readonly ISqlSugarRepository<ChatMessage> _messageRepository = messageRepository;
    private readonly ISqlSugarRepository<AiUsage> _usageStatisticsRepository = usageStatisticsRepository;
    // private readonly ISqlSugarRepository<PremiumPackageAggregateRoot> _premiumPackageRepository;
    private readonly ISqlSugarRepository<Token> _tokenRepository = tokenRepository;
    private readonly ISqlSugarRepository<AiModel> _aiModelRepository = aiModelRepository;

    /// <summary>
    /// 获取当前用户近7天的Token消耗统计
    /// </summary>
    /// <returns>每日Token使用量列表</returns>
    public async Task<List<DailyTokenUsageDto>> GetLast7DaysTokenUsageAsync([FromQuery] UsageStatisticsGetInput input)
    {
        Guid userId = CurrentUser.GetId();
        DateTime endDate = DateTime.Today;
        DateTime startDate = endDate.AddDays(-6); // 近7天

        // 从Message表统计近7天的token消耗
        var dailyUsage = await _messageRepository._DbQueryable
            .Where(x => x.UserId == userId)
            .Where(x => x.Role == "system")
            .Where(x => x.CreationTime >= startDate && x.CreationTime < endDate.AddDays(1))
            .WhereIF(input.TokenId.HasValue, x => x.TokenId == input.TokenId)
            .GroupBy(x => x.CreationTime.Date)
            .Select(g => new
            {
                g.CreationTime.Date,
                Tokens = SqlFunc.AggregateSum(g.TokenUsage.TotalTokenCount)
            })
            .ToListAsync();

        // 生成完整的7天数据，包括没有使用记录的日期
        List<DailyTokenUsageDto> result = [];
        for (int i = 0; i < 7; i++)
        {
            DateTime date = startDate.AddDays(i);
            var usage = dailyUsage.FirstOrDefault(x => x.Date == date);

            result.Add(new DailyTokenUsageDto
            {
                Date = date,
                Tokens = usage?.Tokens ?? 0
            });
        }

        return [.. result.OrderBy(x => x.Date)];
    }

    /// <summary>
    /// 获取当前用户各个模型的Token消耗量及占比
    /// </summary>
    /// <returns>模型Token使用量列表</returns>
    public async Task<List<ModelTokenUsageDto>> GetModelTokenUsageAsync([FromQuery] UsageStatisticsGetInput input)
    {
        Guid userId = CurrentUser.GetId();

        // 从UsageStatistics表获取各模型的token消耗统计（按ModelId聚合，因为同一模型可能有多个TokenId的记录）
        // AiUsage: ModelId, TotalTokenCount
        var modelUsages = await _usageStatisticsRepository._DbQueryable
            .Where(x => x.UserId == userId)
            .WhereIF(input.TokenId.HasValue, x => x.TokenId == input.TokenId)
            .GroupBy(x => x.ModelId)
            .Select(x => new
            {
                x.ModelId,
                TotalTokenCount = SqlFunc.AggregateSum(x.TotalTokenCount)
            })
            .ToListAsync();

        if (modelUsages.Count == 0)
        {
            return [];
        }

        // 计算总token数
        long totalTokens = modelUsages.Sum(x => x.TotalTokenCount);

        // 计算各模型占比
        List<ModelTokenUsageDto> result = [.. modelUsages.Select(x => new ModelTokenUsageDto
        {
            Model = x.ModelId!,
            Tokens = x.TotalTokenCount,
            Percentage = totalTokens > 0 ? Math.Round((decimal)x.TotalTokenCount / totalTokens * 100, 2) : 0
        }).OrderByDescending(x => x.Tokens)];

        return result;
    }



    /// <summary>
    /// 获取当前用户近24小时每小时Token消耗统计（柱状图）
    /// </summary>
    /// <returns>每小时Token使用量列表，包含各模型堆叠数据</returns>
    public async Task<List<HourlyTokenUsageDto>> GetLast24HoursTokenUsageAsync(
        [FromQuery] UsageStatisticsGetInput input)
    {
        Guid userId = CurrentUser.GetId();
        DateTime now = DateTime.Now;
        DateTime startTime = now.AddHours(-23); // 滚动24小时，从23小时前到现在
        DateTime startHour = new(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, 0, 0);

        // 从Message表查询近24小时的数据，只选择需要的字段
        var messages = await _messageRepository._DbQueryable
            .Where(x => x.UserId == userId)
            .Where(x => x.Role == "system")
            .Where(x => x.CreationTime >= startHour)
            .WhereIF(input.TokenId.HasValue, x => x.TokenId == input.TokenId)
            .Select(x => new
            {
                x.CreationTime,
                x.ModelId,
                x.TokenUsage.TotalTokenCount
            })
            .ToListAsync();

        // 在内存中按小时和模型分组统计
        var hourlyGrouped = messages
            .GroupBy(x => new
            {
                Hour = new DateTime(x.CreationTime.Year, x.CreationTime.Month, x.CreationTime.Day, x.CreationTime.Hour,
                    0, 0),
                x.ModelId
            })
            .Select(g => new
            {
                g.Key.Hour,
                g.Key.ModelId,
                Tokens = g.Sum(x => x.TotalTokenCount)
            })
            .ToList();

        // 生成完整的24小时数据
        List<HourlyTokenUsageDto> result = [];
        for (int i = 0; i < 24; i++)
        {
            DateTime hour = startHour.AddHours(i);
            var hourData = hourlyGrouped.Where(x => x.Hour == hour).ToList();

            List<ModelTokenBreakdownDto> modelBreakdown = [.. hourData.Select(x => new ModelTokenBreakdownDto
            {
                ModelId = x.ModelId,
                Tokens = x.Tokens
            })];

            result.Add(new HourlyTokenUsageDto
            {
                Hour = hour,
                TotalTokens = modelBreakdown.Sum(x => x.Tokens),
                ModelBreakdown = modelBreakdown
            });
        }

        return result;
    }

    /// <summary>
    /// 获取当前用户今日各模型使用量统计（卡片列表）
    /// </summary>
    /// <returns>模型今日使用量列表，包含使用次数和总Token</returns>
    public async Task<List<ModelTodayUsageDto>> GetTodayModelUsageAsync([FromQuery] UsageStatisticsGetInput input)
    {
        Guid userId = CurrentUser.GetId();
        DateTime todayStart = DateTime.Today; // 今天凌晨0点
        DateTime tomorrowStart = todayStart.AddDays(1);

        // 从Message表查询今天的数据，只选择需要的字段
        var messages = await _messageRepository._DbQueryable
            .Where(x => x.UserId == userId)
            .Where(x => x.Role == "system")
            .Where(x => x.CreationTime >= todayStart && x.CreationTime < tomorrowStart)
            .WhereIF(input.TokenId.HasValue, x => x.TokenId == input.TokenId)
            .Select(x => new
            {
                x.ModelId,
                x.TokenUsage.TotalTokenCount
            })
            .ToListAsync();

        // 在内存中按模型分组统计
        List<ModelTodayUsageDto> modelStats = [.. messages
            .GroupBy(x => x.ModelId)
            .Select(g => new ModelTodayUsageDto
            {
                ModelId = g.Key,
                UsageCount = g.Count(),
                TotalTokens = g.Sum(x => x.TotalTokenCount)
            })
            .OrderByDescending(x => x.TotalTokens)];

        if (modelStats.Count > 0)
        {
            List<string> modelIds = [.. modelStats.Select(x => x.ModelId)];
            Dictionary<string, string> modelDic = await _aiModelRepository._DbQueryable.Where(x => modelIds.Contains(x.ModelId!))
                .Distinct()
                .Where(x => x.IsEnabled)
                .ToDictionaryAsync<string>(x => x.ModelId, y => y.IconUrl);
            modelStats.ForEach(x =>
            {
                if (modelDic.TryGetValue(x.ModelId, out string icon))
                {
                    x.IconUrl = icon;
                }
            });
        }

        return modelStats;
    }
}
