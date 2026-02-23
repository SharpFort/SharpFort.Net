using Volo.Abp.SettingManagement;
using SharpFort.FileManagement.Domain.Shared;
using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.FileManagement.Application.Contracts
{
    [DependsOn(
        typeof(SharpFortFileManagementDomainSharedModule),

        typeof(AbpSettingManagementApplicationContractsModule),

        typeof(SharpFortDddApplicationContractsModule))]
    public class SharpFortFileManagementApplicationContractsModule:AbpModule
    {

    }
}