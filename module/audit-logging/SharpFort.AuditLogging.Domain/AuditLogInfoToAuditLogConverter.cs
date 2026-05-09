using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.Auditing;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Http;
using Volo.Abp.Json;
using SharpFort.AuditLogging.Domain.Entities;

namespace SharpFort.AuditLogging.Domain;

public class AuditLogInfoToAuditLogConverter(IGuidGenerator guidGenerator, IExceptionToErrorInfoConverter exceptionToErrorInfoConverter, IJsonSerializer jsonSerializer, IOptions<AbpExceptionHandlingOptions> exceptionHandlingOptions) : IAuditLogInfoToAuditLogConverter, ITransientDependency
{
    protected IGuidGenerator GuidGenerator { get; } = guidGenerator;
    protected IExceptionToErrorInfoConverter ExceptionToErrorInfoConverter { get; } = exceptionToErrorInfoConverter;
    protected IJsonSerializer JsonSerializer { get; } = jsonSerializer;
    protected AbpExceptionHandlingOptions ExceptionHandlingOptions { get; } = exceptionHandlingOptions.Value;

    public virtual Task<AuditLog> ConvertAsync(AuditLogInfo auditLogInfo)
    {
        Guid auditLogId = GuidGenerator.Create();

        ExtraPropertyDictionary extraProperties = new ExtraPropertyDictionary();
        if (auditLogInfo.ExtraProperties != null)
        {
            foreach (KeyValuePair<string, object?> pair in auditLogInfo.ExtraProperties)
            {
                extraProperties.Add(pair.Key, pair.Value);
            }
        }

        List<EntityChange> entityChanges = auditLogInfo
                                .EntityChanges?
                                .Select(entityChangeInfo => new EntityChange(GuidGenerator, auditLogId, entityChangeInfo, tenantId: auditLogInfo.TenantId))
                                .ToList()
                            ?? [];

        List<AuditLogAction> actions = auditLogInfo
                          .Actions?
                          .Select(auditLogActionInfo => new AuditLogAction(GuidGenerator.Create(), auditLogId, auditLogActionInfo, tenantId: auditLogInfo.TenantId))
                          .ToList()
                      ?? [];

        IEnumerable<RemoteServiceErrorInfo> remoteServiceErrorInfos = auditLogInfo.Exceptions?.Select(exception => ExceptionToErrorInfoConverter.Convert(exception, options =>
                                          {
                                              options.SendExceptionsDetailsToClients = ExceptionHandlingOptions.SendExceptionsDetailsToClients;
                                              options.SendStackTraceToClients = ExceptionHandlingOptions.SendStackTraceToClients;
                                          }))
                                      ?? new List<RemoteServiceErrorInfo>();

        string? exceptions = remoteServiceErrorInfos.Any()
            ? JsonSerializer.Serialize(remoteServiceErrorInfos, indented: true)
            : null;

        string? comments = auditLogInfo
            .Comments?
            .JoinAsString(Environment.NewLine);

        AuditLog auditLog = new AuditLog(
            auditLogId,
            auditLogInfo.ApplicationName ?? string.Empty,
            auditLogInfo.TenantId,
            auditLogInfo.TenantName ?? string.Empty,
            auditLogInfo.UserId,
            auditLogInfo.UserName ?? string.Empty,
            auditLogInfo.ExecutionTime,
            auditLogInfo.ExecutionDuration,
            auditLogInfo.ClientIpAddress ?? string.Empty,
            auditLogInfo.ClientName ?? string.Empty,
            auditLogInfo.ClientId ?? string.Empty,
            auditLogInfo.CorrelationId ?? string.Empty,
            auditLogInfo.BrowserInfo ?? string.Empty,
            auditLogInfo.HttpMethod ?? string.Empty,
            auditLogInfo.Url ?? string.Empty,
            auditLogInfo.HttpStatusCode,
            auditLogInfo.ImpersonatorUserId,
            auditLogInfo.ImpersonatorUserName ?? string.Empty,
            auditLogInfo.ImpersonatorTenantId,
            auditLogInfo.ImpersonatorTenantName ?? string.Empty,
            extraProperties,
            entityChanges,
            actions,
            exceptions ?? string.Empty,
            comments ?? string.Empty
        );

        return Task.FromResult(auditLog);
    }
}
