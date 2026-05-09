using Microsoft.Extensions.Logging;

using Volo.Abp.Domain.Services;
using SharpFort.Ai.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Domain.Managers;

/// <summary>
/// 模型管理器
/// </summary>
public class ModelManager(
    ISqlSugarRepository<AiModel> aiModelRepository,
    ILogger<ModelManager> logger) : DomainService
{
    private readonly ISqlSugarRepository<AiModel> _aiModelRepository = aiModelRepository;
    private readonly ILogger<ModelManager> _logger = logger;
}
