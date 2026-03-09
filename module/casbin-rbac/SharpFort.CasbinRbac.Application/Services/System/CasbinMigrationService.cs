using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Managers;
using Volo.Abp.Application.Services;

namespace SharpFort.CasbinRbac.Application.Services.System
{
    /// <summary>
    /// Casbin 迁移服务
    /// 用于将 casbin_sys_role、casbin_sys_menu、casbin_sys_role_menu、casbin_sys_user_role 表的数据
    /// 迁移到 casbin_rule 表中，生成 Casbin 权限策略
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
        /// 将角色、菜单、用户角色关系迁移到 Casbin 策略表
        /// 注意：此操作会清空 casbin_rule 表并重新生成所有策略
        /// </summary>
        /// <returns>迁移结果</returns>
        [HttpPost]
        [Route("api/app/casbin-migration/migrate-all")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous] // 临时允许匿名访问，生产环境应该移除
        public async Task<object> MigrateAllAsync()
        {
            try
            {
                await _casbinSeedService.MigrateAllAsync();

                return new
                {
                    Success = true,
                    Message = "Casbin 权限数据迁移成功！请查看日志了解详细信息。",
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Casbin 权限数据迁移失败");

                return new
                {
                    Success = false,
                    Message = $"迁移失败：{ex.Message}",
                    Error = ex.ToString(),
                    Timestamp = DateTime.Now
                };
            }
        }
    }
}
