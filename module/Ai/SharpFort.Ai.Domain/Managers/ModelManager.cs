using Microsoft.Extensions.Logging;

using Volo.Abp.Domain.Services;
using SharpFort.Ai.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Domain.Managers;

/// <summary>
/// 模型管理器
/// </summary>
public class ModelManager : DomainService
{
    public readonly ISqlSugarRepository<AiModel> _aiModelRepository;
    private readonly ILogger<ModelManager> _logger;
    public ModelManager(
        ISqlSugarRepository<AiModel> aiModelRepository,
        ILogger<ModelManager> logger)
    {
        _aiModelRepository = aiModelRepository;
        _logger = logger;
    }


}
