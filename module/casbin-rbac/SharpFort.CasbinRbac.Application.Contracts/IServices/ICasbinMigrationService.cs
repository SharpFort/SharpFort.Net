using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SharpFort.CasbinRbac.Application.Contracts.IServices
{
    /// <summary>
    /// Casbin 迁移服务接口
    /// </summary>
    public interface ICasbinMigrationService : IApplicationService
    {
        /// <summary>
        /// 全量数据迁移
        /// 将角色、菜单、用户角色关系迁移到 Casbin 策略表
        /// </summary>
        Task<object> MigrateAllAsync();
    }
}
