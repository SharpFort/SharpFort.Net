using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

public class AuditLogInfoToAuditLogConverter : IAuditLogInfoToAuditLogConverter, ITransientDependency
{
    protected IGuidGenerator GuidGenerator { get; }
    protected IExceptionToErrorInfoConverter ExceptionToErrorInfoConverter { get; }
    protected IJsonSerializer JsonSerializer { get; }
    protected AbpExceptionHandlingOptions ExceptionHandlingOptions { get; }

    public AuditLogInfoToAuditLogConverter(IGuidGenerator guidGenerator, IExceptionToErrorInfoConverter exceptionToErrorInfoConverter, IJsonSerializer jsonSerializer, IOptions<AbpExceptionHandlingOptions> exceptionHandlingOptions)
    {
        GuidGenerator = guidGenerator;
        ExceptionToErrorInfoConverter = exceptionToErrorInfoConverter;
        JsonSerializer = jsonSerializer;
        ExceptionHandlingOptions = exceptionHandlingOptions.Value;
    }

    public virtual Task<AuditLog> ConvertAsync(AuditLogInfo auditLogInfo)
    {
        var auditLogId = GuidGenerator.Create();

        var extraProperties = new ExtraPropertyDictionary();
        if (auditLogInfo.ExtraProperties != null)
        {
            foreach (var pair in auditLogInfo.ExtraProperties)
            {
                extraProperties.Add(pair.Key, pair.Value);
            }
        }

        var entityChanges = auditLogInfo
                                .EntityChanges?
                                .Select(entityChangeInfo => new EntityChange(GuidGenerator, auditLogId, entityChangeInfo, tenantId: auditLogInfo.TenantId))
                                .ToList()
                            ?? new List<EntityChange>();

        var actions = auditLogInfo
                          .Actions?
                          .Select(auditLogActionInfo => new AuditLogAction(GuidGenerator.Create(), auditLogId, auditLogActionInfo, tenantId: auditLogInfo.TenantId))
                          .ToList()
                      ?? new List<AuditLogAction>();

        var remoteServiceErrorInfos = auditLogInfo.Exceptions?.Select(exception => ExceptionToErrorInfoConverter.Convert(exception, options =>
                                          {
                                              options.SendExceptionsDetailsToClients = ExceptionHandlingOptions.SendExceptionsDetailsToClients;
                                              options.SendStackTraceToClients = ExceptionHandlingOptions.SendStackTraceToClients;
                                          }))
                                      ?? new List<RemoteServiceErrorInfo>();

        var exceptions = remoteServiceErrorInfos.Any()
            ? JsonSerializer.Serialize(remoteServiceErrorInfos, indented: true)
            : null;

        var comments = auditLogInfo
            .Comments?
            .JoinAsString(Environment.NewLine);

        var auditLog = new AuditLog(
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
