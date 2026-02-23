using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SharpFort.CasbinRbac.Application.Contracts.IServices
{
    public interface ICasbinMigrationService : IApplicationService
    {
        Task MigrateAllAsync();
    }
}
