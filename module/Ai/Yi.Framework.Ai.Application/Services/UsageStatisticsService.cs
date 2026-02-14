using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using Yi.Framework.Ai.Application.Contracts.Dtos.UsageStatistics;
using Yi.Framework.Ai.Application.Contracts.IServices;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.Ai.Domain.Extensions;
using Yi.Framework.Ai.Domain.Managers;
using Yi.Framework.Ai.Domain.Shared.Consts;
using Yi.Framework.Ddd.Application.Contracts;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Application.Services;

/// <summary>
/// 使用量统计服务
/// </summary>
[Authorize]
public class UsageStatisticsService : ApplicationService, IUsageStatisticsService
{
    private readonly ISqlSugarRepository<ChatMessage> _messageRepository;
    private readonly ISqlSugarRepository<AiUsage> _usageStatisticsRepository;
    // private readonly ISqlSugarRepository<PremiumPackageAggregateRoot> _premiumPackageRepository;
    private readonly ISqlSugarRepository<Token> _tokenRepository;
    private readonly ModelManager _modelManager;

    public UsageStatisticsService(
        ISqlSugarRepository<ChatMessage> messageRepository,
        ISqlSugarRepository<AiUsage> usageStatisticsRepository,
        // ISqlSugarRepository<PremiumPackageAggregateRoot> premiumPackageRepository,
        ISqlSugarRepository<Token> tokenRepository,
        ModelManager modelManager)
    {
        _messageRepository = messageRepository;
        _usageStatisticsRepository = usageStatisticsRepository;
        // _premiumPackageRepository = premiumPackageRepository;
        _tokenRepository = tokenRepository;
        _modelManager = modelManager;
    }

    /// <summary>
    /// 获取当前用户近7天的Token消耗统计
    /// </summary>
    /// <returns>每日Token使用量列表</returns>
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
            .WhereIF(input.TokenId.HasValue, x => x.TokenId == input.TokenId)
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
    public async Task<List<ModelTokenUsageDto>> GetModelTokenUsageAsync([FromQuery] UsageStatisticsGetInput input)
    {
        var userId = CurrentUser.GetId();

        // 从UsageStatistics表获取各模型的token消耗统计（按ModelId聚合，因为同一模型可能有多个TokenId的记录）
        // AiUsage: ModelId, TotalTokenCount
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

    /// <summary>
    /// 获取当前用户尊享服务Token用量统计
    /// </summary>
    /// <returns>尊享服务Token用量统计</returns>
    public async Task<PremiumTokenUsageDto> GetPremiumTokenUsageAsync()
    {
        // Placeholder implementation until PremiumPackageManager is restored
        return new PremiumTokenUsageDto();
        
        /*
        var userId = CurrentUser.GetId();

        // 获取尊享包Token信息
        var premiumPackages = await _premiumPackageRepository._DbQueryable
            .Where(x => x.UserId == userId && x.IsActive)
            .ToListAsync();

        var result = new PremiumTokenUsageDto();

        if (premiumPackages.Any())
        {
            // 过滤掉已过期、禁用的包，不过滤用量负数的包
            var validPackages = premiumPackages
                .Where(p => p.IsAvailable(false))
                .ToList();

            result.PremiumTotalTokens = validPackages.Sum(x => x.TotalTokens);
            result.PremiumUsedTokens = validPackages.Sum(x => x.UsedTokens);
            result.PremiumRemainingTokens = validPackages.Sum(x => x.RemainingTokens);
        }

        return result;
        */
    }

    /// <summary>
    ///  获取当前用户尊享服务token用量统计列表
    /// </summary>
    /// <returns></returns>
    [HttpGet("usage-statistics/premium-token-usage/list")]
    public async Task<PagedResultDto<PremiumTokenUsageGetListOutput>> GetPremiumTokenUsageListAsync(
        PremiumTokenUsageGetListInput input)
    {
         // Placeholder implementation until PremiumPackageManager is restored
        return new PagedResultDto<PremiumTokenUsageGetListOutput>();
        
        /*
        var userId = CurrentUser.GetId();
        RefAsync<int> total = 0;
        // 获取尊享包Token信息
        var entities = await _premiumPackageRepository._DbQueryable
            .Where(x => x.UserId == userId)
            .WhereIF(input.IsFree == false, x => x.PurchaseAmount > 0)
            .WhereIF(input.IsFree == true, x => x.PurchaseAmount == 0)
            .WhereIF(input.StartTime is not null && input.EndTime is not null,
                x => x.CreationTime >= input.StartTime && x.CreationTime <= input.EndTime)
            .OrderByDescending(x => x.CreationTime)
            .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
        return new PagedResultDto<PremiumTokenUsageGetListOutput>(total,
            entities.Adapt<List<PremiumTokenUsageGetListOutput>>());
        */
    }

    /// <summary>
    /// 获取当前用户尊享包不同Token用量占比（饼图）
    /// </summary>
    /// <returns>各Token的尊享模型用量及占比</returns>
    [HttpGet("usage-statistics/premium-token-usage/by-token")]
    public async Task<List<TokenPremiumUsageDto>> GetPremiumTokenUsageByTokenAsync()
    {
        // Placeholder implementation until PremiumPackageManager is restored
        return new List<TokenPremiumUsageDto>();

        /*
        var userId = CurrentUser.GetId();

        // 通过ModelManager获取所有尊享模型的ModelId列表
        var premiumModelIds = await _modelManager.GetPremiumModelIdsAsync();

        // 从UsageStatistics表获取尊享模型的token消耗统计（按TokenId聚合）
        var tokenUsages = await _usageStatisticsRepository._DbQueryable
            .Where(x => x.UserId == userId && premiumModelIds.Contains(x.ModelId))
            .GroupBy(x => x.TokenId)
            .Select(x => new
            {
                TokenId = x.TokenId,
                TotalTokenCount = SqlFunc.AggregateSum(x.TotalTokenCount)
            })
            .ToListAsync();

        if (!tokenUsages.Any())
        {
            return new List<TokenPremiumUsageDto>();
        }

        // 获取用户的所有Token信息用于名称映射
        var tokenIds = tokenUsages.Select(x => x.TokenId).ToList();
        var tokens = await _tokenRepository._DbQueryable
            .Where(x => x.UserId == userId && tokenIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        var tokenNameDict = tokens.ToDictionary(x => x.Id, x => x.Name);

        // 计算总token数
        var totalTokens = tokenUsages.Sum(x => x.TotalTokenCount);

        // 计算各Token占比
        var result = tokenUsages.Select(x => new TokenPremiumUsageDto
        {
            TokenId = x.TokenId,
            TokenName = x.TokenId == Guid.Empty
                ? "默认"
                : (tokenNameDict.TryGetValue(x.TokenId, out var name) ? name : "其他"),
            Tokens = x.TotalTokenCount,
            Percentage = totalTokens > 0 ? Math.Round((decimal)x.TotalTokenCount / totalTokens * 100, 2) : 0
        }).OrderByDescending(x => x.Tokens).ToList();

        return result;
        */
    }

    /// <summary>
    /// 获取当前用户近24小时每小时Token消耗统计（柱状图）
    /// </summary>
    /// <returns>每小时Token使用量列表，包含各模型堆叠数据</returns>
    public async Task<List<HourlyTokenUsageDto>> GetLast24HoursTokenUsageAsync(
        [FromQuery] UsageStatisticsGetInput input)
    {
        var userId = CurrentUser.GetId();
        var now = DateTime.Now;
        var startTime = now.AddHours(-23); // 滚动24小时，从23小时前到现在
        var startHour = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, 0, 0);

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
        var result = new List<HourlyTokenUsageDto>();
        for (int i = 0; i < 24; i++)
        {
            var hour = startHour.AddHours(i);
            var hourData = hourlyGrouped.Where(x => x.Hour == hour).ToList();

            var modelBreakdown = hourData.Select(x => new ModelTokenBreakdownDto
            {
                ModelId = x.ModelId,
                Tokens = x.Tokens
            }).ToList();

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
        var userId = CurrentUser.GetId();
        var todayStart = DateTime.Today; // 今天凌晨0点
        var tomorrowStart = todayStart.AddDays(1);

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
        var modelStats = messages
            .GroupBy(x => x.ModelId)
            .Select(g => new ModelTodayUsageDto
            {
                ModelId = g.Key,
                UsageCount = g.Count(),
                TotalTokens = g.Sum(x => x.TotalTokenCount)
            })
            .OrderByDescending(x => x.TotalTokens)
            .ToList();

        if (modelStats.Count > 0)
        {
            var modelIds = modelStats.Select(x => x.ModelId).ToList();
            var modelDic = await _modelManager._aiModelRepository._DbQueryable.Where(x => modelIds.Contains(x.ModelId))
                .Distinct()
                .Where(x=>x.IsEnabled)
                .ToDictionaryAsync<string>(x => x.ModelId, y => y.IconUrl);
            modelStats.ForEach(x =>
            {
                if (modelDic.TryGetValue(x.ModelId, out var icon))
                {
                    x.IconUrl = icon;
                }
            });
        }

        return modelStats;
    }
}
