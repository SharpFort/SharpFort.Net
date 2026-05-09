using Volo.Abp.Domain;
using Volo.Abp.SettingManagement;

namespace SharpFort.FluidSequence.Domain.Shared
{
    [DependsOn(

        typeof(AbpSettingManagementDomainSharedModule),
        typeof(AbpDddDomainSharedModule))]
    public class FluidSequenceDomainSharedModule : AbpModule
    {

    }
}