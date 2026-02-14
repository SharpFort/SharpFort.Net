using Volo.Abp.SettingManagement;
using FluidSequence.Domain.Shared;
using Yi.Framework.Ddd.Application.Contracts;

namespace FluidSequence.Application.Contracts
{
    [DependsOn(
        typeof(FluidSequenceDomainSharedModule),

        typeof(AbpSettingManagementApplicationContractsModule),

        typeof(YiFrameworkDddApplicationContractsModule))]
    public class FluidSequenceApplicationContractsModule:AbpModule
    {

    }
}