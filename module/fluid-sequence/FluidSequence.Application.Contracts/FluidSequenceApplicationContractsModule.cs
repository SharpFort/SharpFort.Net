using Volo.Abp.SettingManagement;
using SharpFort.FluidSequence.Domain.Shared;
using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.FluidSequence.Application.Contracts
{
    [DependsOn(
        typeof(FluidSequenceDomainSharedModule),

        typeof(AbpSettingManagementApplicationContractsModule),

        typeof(SharpFortDddApplicationContractsModule))]
    public class FluidSequenceApplicationContractsModule : AbpModule
    {

    }
}