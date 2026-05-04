using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;
using SharpFort.AuditLogging.Domain.Repositories;
using SharpFort.Core.Helper;

namespace SharpFort.AuditLogging.Domain;

public partial class AuditingStore : IAuditingStore, ITransientDependency
{
    public ILogger<AuditingStore> Logger { get; set; }
    protected IAuditLogRepository AuditLogRepository { get; }
    protected IUnitOfWorkManager UnitOfWorkManager { get; }
    protected AbpAuditingOptions Options { get; }
    protected IAuditLogInfoToAuditLogConverter Converter { get; }

    public AuditingStore(
        IAuditLogRepository auditLogRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IOptions<AbpAuditingOptions> options,
        IAuditLogInfoToAuditLogConverter converter)
    {
        AuditLogRepository = auditLogRepository;
        UnitOfWorkManager = unitOfWorkManager;
        Converter = converter;
        Options = options.Value;

        Logger = NullLogger<AuditingStore>.Instance;
    }

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
            var auditInfoJson = JsonConvert.SerializeObject(auditInfo, new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-dd HH:mm:ss"
            });
            LogRequestTracking(auditInfoJson);
        }
        using (var uow = UnitOfWorkManager.Begin())
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