using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Globalization;
using Volo.Abp.Application.Services;
using Yi.Framework.Ai.Application.Contracts.Dtos.SystemStatistics;
using Yi.Framework.Ai.Application.Contracts.IServices;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Application.Services;

/// <summary>
/// 系统使用量统计服务实现
/// </summary>
[Authorize(Roles = "admin")]
public class SystemUsageStatisticsService : ApplicationService, ISystemUsageStatisticsService
{
    private readonly ISqlSugarRepository<AiRecharge> _rechargeRepository;
    private readonly ISqlSugarRepository<ChatMessage> _messageRepository;
    private readonly ISqlSugarRepository<AiModel, Guid> _modelRepository;

    public SystemUsageStatisticsService(
        ISqlSugarRepository<AiRecharge> rechargeRepository,
        ISqlSugarRepository<ChatMessage> messageRepository,
        ISqlSugarRepository<AiModel, Guid> modelRepository)
    {
        _rechargeRepository = rechargeRepository;
        _messageRepository = messageRepository;
        _modelRepository = modelRepository;
    }



    /// <summary>
    /// 获取指定日期各模型Token统计
    /// </summary>
    [HttpPost("system-statistics/token")]
    public async Task<TokenStatisticsOutput> GetTokenStatisticsAsync(TokenStatisticsInput input)
    {
        var day = input.Date.Date;
        var nextDay = day.AddDays(1);

        // 1. 获取所有模型,按ModelId去重
        var models = await _modelRepository._DbQueryable
            .ToListAsync();

        if (models.Count == 0)
        {
            return new TokenStatisticsOutput
            {
                Date = FormatDate(day),
                ModelStatistics = new List<ModelTokenStatisticsDto>()
            };
        }

        // 按ModelId去重,保留第一个模型的名称
        var distinctModels = models
            .GroupBy(x => x.ModelId)
            .Select(g => g.First())
            .ToList();

        var modelIds = distinctModels.Select(x => x.ModelId).ToList();

        // 2. 查询指定日期内各模型的Token使用统计
        var modelStats = await _messageRepository._DbQueryable
            .Where(x => modelIds.Contains(x.ModelId))
            .Where(x => x.CreationTime >= day && x.CreationTime < nextDay)
            .Where(x => x.Role == "system")
            .GroupBy(x => x.ModelId)
            .Select(x => new
            {
                ModelId = x.ModelId,
                Tokens = SqlFunc.AggregateSum(x.TokenUsage.TotalTokenCount),
                Count = SqlFunc.AggregateCount(x.Id)
            })
            .ToListAsync();

        var modelStatDict = modelStats.ToDictionary(x => x.ModelId, x => x);

        // 3. 构建结果列表,使用去重后的模型列表
        var result = new List<ModelTokenStatisticsDto>();
        foreach (var model in distinctModels)
        {
            modelStatDict.TryGetValue(model.ModelId, out var stat);
            long tokens = stat?.Tokens ?? 0;
            long count = stat?.Count ?? 0;

            // 这里成本设为0,因为需要前端传入或者从配置中获取
            decimal cost = 0;
            decimal costPerHundredMillion = tokens > 0 && cost > 0
                ? cost / (tokens / 100000000m)
                : 0;

            result.Add(new ModelTokenStatisticsDto
            {
                ModelId = model.ModelId,
                ModelName = model.Name,
                Tokens = tokens,
                TokensInWan = tokens / 10000m,
                Count = count,
                Cost = cost,
                CostPerHundredMillion = costPerHundredMillion
            });
        }

        return new TokenStatisticsOutput
        {
            Date = FormatDate(day),
            ModelStatistics = result
        };
    }

    private string FormatDate(DateTime date)
    {
        string dayOfWeek = date.ToString("dddd", new CultureInfo("zh-CN"));
        string weekDay = dayOfWeek switch
        {
            "星期一" => "周1",
            "星期二" => "周2",
            "星期三" => "周3",
            "星期四" => "周4",
            "星期五" => "周5",
            "星期六" => "周6",
            "星期日" => "周日",
            _ => dayOfWeek
        };
        return $"{date:M月d日} {weekDay}";
    }
}
