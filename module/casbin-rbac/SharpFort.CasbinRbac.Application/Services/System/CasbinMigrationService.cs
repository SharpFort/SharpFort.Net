using System.Threading.Tasks;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Managers;
using Volo.Abp.Application.Services;

namespace SharpFort.CasbinRbac.Application.Services.System
{
    /// <summary>
    /// Casbin 迁移服务
    /// </summary>
    public class CasbinMigrationService : ApplicationService, ICasbinMigrationService
    {
        private readonly CasbinSeedService _casbinSeedService;

        public CasbinMigrationService(CasbinSeedService casbinSeedService)
        {
            _casbinSeedService = casbinSeedService;
        }

        /// <summary>
        /// 全量数据迁移
        /// </summary>
        /// <returns></returns>
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task MigrateAllAsync()
        {
            await _casbinSeedService.MigrateAllAsync();
        }
    }
}
