using Volo.Abp.BackgroundWorkers.Hangfire;
using Volo.Abp.Data;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;
using SqlSugar;

namespace Sf.Abp.Web.Jobs
{
    public sealed partial class DemoResetJob : HangfireBackgroundWorkerBase
    {
        private readonly ISqlSugarDbContext _dbContext;
        private ILogger<DemoResetJob> _logger => LoggerFactory.CreateLogger<DemoResetJob>();
        private readonly IDataSeeder _dataSeeder;
        private readonly IConfiguration _configuration;
        public DemoResetJob(ISqlSugarDbContext dbContext, IDataSeeder dataSeeder, IConfiguration configuration)
        {
            _dbContext = dbContext;
            RecurringJobId = "重置demo环境";
            //每天1点和13点进行重置demo环境
            CronExpression = "0 0 1,13 * * ?";

            _dataSeeder = dataSeeder;
            _configuration = configuration;
        }

        [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "演示环境正在还原！")]
        private static partial void LogDemoResetting(ILogger logger);

        [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "演示环境还原成功！")]
        private static partial void LogDemoResetSuccess(ILogger logger);

        public override async Task DoWorkAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            //开启演示环境重置功能
            if (_configuration.GetSection("EnableDemoReset").Get<bool>())
            {
                //定时任务，非常简单
                LogDemoResetting(_logger);
                SqlSugarClient db = _dbContext.SqlSugarClient.CopyNew();
                db.DbMaintenance.TruncateTable<User>();
                db.DbMaintenance.TruncateTable<UserRole>();
                db.DbMaintenance.TruncateTable<Role>();
                db.DbMaintenance.TruncateTable<RoleMenu>();
                db.DbMaintenance.TruncateTable<Menu>();
                db.DbMaintenance.TruncateTable<RoleDepartment>();
                db.DbMaintenance.TruncateTable<Department>();
                db.DbMaintenance.TruncateTable<Position>();
                db.DbMaintenance.TruncateTable<UserPosition>();

                //删除种子数据
                await _dataSeeder.SeedAsync();
                LogDemoResetSuccess(_logger);

            }

        }
    }
}
