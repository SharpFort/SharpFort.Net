using Volo.Abp.SettingManagement;
using SharpFort.Ai.Domain.Shared;
using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.Ai.Application.Contracts
{
    [DependsOn(
        typeof(SharpFortAiDomainSharedModule),

        typeof(AbpSettingManagementApplicationContractsModule),

        typeof(SharpFortDddApplicationContractsModule))]
    public class SharpFortAiApplicationContractsModule:AbpModule
    {

    }
}