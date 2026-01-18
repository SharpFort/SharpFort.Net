using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Yi.Framework.CasbinRbac.Application.Contracts.IServices
{
    public interface ICasbinMigrationService : IApplicationService
    {
        Task MigrateAllAsync();
    }
}
