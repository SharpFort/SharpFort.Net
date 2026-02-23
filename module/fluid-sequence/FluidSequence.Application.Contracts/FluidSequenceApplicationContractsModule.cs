using Volo.Abp.SettingManagement;
using FluidSequence.Domain.Shared;
using SharpFort.Ddd.Application.Contracts;

namespace FluidSequence.Application.Contracts
{
    [DependsOn(
        typeof(FluidSequenceDomainSharedModule),

        typeof(AbpSettingManagementApplicationContractsModule),

        typeof(SharpFortDddApplicationContractsModule))]
    public class FluidSequenceApplicationContractsModule:AbpModule
    {

    }
}