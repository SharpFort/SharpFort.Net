using System.Threading.Tasks;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Monitor;
using Volo.Abp.Application.Services;

namespace SharpFort.CasbinRbac.Application.Contracts.IServices
{
    public interface IMonitorServerService : IApplicationService
    {
        Task<MonitorServerInfoDto> GetServerInfoAsync();
    }
}
