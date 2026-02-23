using Volo.Abp.SettingManagement;
using Sf.Abp.Domain.Shared;
using SharpFort.Ddd.Application.Contracts;
using SharpFort.TenantManagement.Application.Contracts;
using SharpFort.CasbinRbac.Application.Contracts;
using SharpFort.Ai.Application.Contracts;

namespace Sf.Abp.Application.Contracts
{
    [DependsOn(
        typeof(SfAbpDomainSharedModule),
        typeof(AbpSettingManagementApplicationContractsModule),
        typeof(SharpFortTenantManagementApplicationContractsModule),
        typeof(SharpFortDddApplicationContractsModule),
        typeof(SharpFortCasbinRbacApplicationContractsModule),
        typeof(SharpFortAiApplicationContractsModule)
        )]
    public class SfAbpApplicationContractsModule:AbpModule
    {

    }
}