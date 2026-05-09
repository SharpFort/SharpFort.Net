using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;
using SharpFort.AuditLogging.Domain.Repositories;

namespace SharpFort.AuditLogging.Domain;

public partial class AuditingStore(
    IAuditLogRepository auditLogRepository,
    IUnitOfWorkManager unitOfWorkManager,
    IOptions<AbpAuditingOptions> options,
    IAuditLogInfoToAuditLogConverter converter) : IAuditingStore, ITransientDependency
{
    public ILogger<AuditingStore> Logger { get; set; } = NullLogger<AuditingStore>.Instance;
    protected IAuditLogRepository AuditLogRepository { get; } = auditLogRepository;
    protected IUnitOfWorkManager UnitOfWorkManager { get; } = unitOfWorkManager;
    protected AbpAuditingOptions Options { get; } = options.Value;
    protected IAuditLogInfoToAuditLogConverter Converter { get; } = converter;

    public virtual async Task SaveAsync(AuditLogInfo auditInfo)
    {
        if (!Options.HideErrors)
        {
            await SaveLogAsync(auditInfo);
            return;
        }

        try
        {
            await SaveLogAsync(auditInfo);
        }
        catch (Exception ex)
        {
            LogCouldNotSaveAuditLog(auditInfo.ToString());
            Logger.LogException(ex, LogLevel.Error);
        }
    }

    protected virtual async Task SaveLogAsync(AuditLogInfo auditInfo)
    {
        if (Logger.IsEnabled(LogLevel.Debug))
        {
            string auditInfoJson = JsonConvert.SerializeObject(auditInfo, new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-dd HH:mm:ss"
            });
            LogRequestTracking(auditInfoJson);
        }
        using (IUnitOfWork uow = UnitOfWorkManager.Begin())
        {
            await AuditLogRepository.InsertAsync(await Converter.ConvertAsync(auditInfo));
            await uow.CompleteAsync();
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Could not save the audit log object: {AuditInfo}")]
    private partial void LogCouldNotSaveAuditLog(string auditInfo);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Sf-请求追踪:{AuditInfoJson}")]
    private partial void LogRequestTracking(string auditInfoJson);
}