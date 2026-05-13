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
            // CA1848/CA1873 suppressed: Logger is a public property (ABP convention), [LoggerMessage] requires _logger field
#pragma warning disable CA1848, CA1873
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(ex, "Could not save the audit log object: {AuditInfo}", auditInfo.ToString());
            }
#pragma warning restore CA1848, CA1873
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
            Logger.LogDebug("Sf-请求追踪:{AuditInfoJson}", auditInfoJson);
        }
        using (IUnitOfWork uow = UnitOfWorkManager.Begin())
        {
            await AuditLogRepository.InsertAsync(await Converter.ConvertAsync(auditInfo));
            await uow.CompleteAsync();
        }
    }
}