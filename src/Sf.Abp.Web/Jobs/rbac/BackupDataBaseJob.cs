using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers.Hangfire;
using SharpFort.CasbinRbac.Domain.Shared.Options;
using SharpFort.SqlSugarCore.Abstractions;

namespace Sf.Abp.Web.Jobs.rbac
{
    public sealed partial class BackupDataBaseJob : HangfireBackgroundWorkerBase
    {
        private readonly ISqlSugarDbContext _dbContext;
        private readonly IOptions<RbacOptions> _options;
        public BackupDataBaseJob(ISqlSugarDbContext dbContext, IOptions<RbacOptions> options)
        {

            _options = options;
            _dbContext = dbContext;

            RecurringJobId = "数据库备份";
            //每天00点与24点进行备份
            CronExpression = "0 0 0,12 * * ? ";
        }

        [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "正在进行数据库备份")]
        private static partial void LogBackingUp(ILogger logger);

        [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "数据库备份已完成")]
        private static partial void LogBackupCompleted(ILogger logger);

        public override Task DoWorkAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            if (_options.Value.EnableDataBaseBackup)
            {
                ILogger<BackupDataBaseJob> logger = LoggerFactory.CreateLogger<BackupDataBaseJob>();
                LogBackingUp(logger);
                _dbContext.BackupDataBase();
                LogBackupCompleted(logger);
            }
            return Task.CompletedTask;
        }
    }
}
