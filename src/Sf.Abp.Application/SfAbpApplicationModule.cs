using Volo.Abp.SettingManagement;
using Sf.Abp.Application.Contracts;
using Sf.Abp.Domain;
using SharpFort.CodeGen.Application;
using SharpFort.Ddd.Application;
using SharpFort.SettingManagement.Application;
using SharpFort.TenantManagement.Application;
using SharpFort.CasbinRbac.Application;
using SharpFort.FileManagement.Application;
using SharpFort.Ai.Application;
using SharpFort.FluidSequence.Application;

namespace Sf.Abp.Application
{
    [DependsOn(
        typeof(SfAbpApplicationContractsModule),
        typeof(SfAbpDomainModule),
        typeof(SharpFortCasbinRbacApplicationModule),
        typeof(SharpFortTenantManagementApplicationModule),
        typeof(SharpFortCodeGenApplicationModule),
        typeof(SharpFortSettingManagementApplicationModule),
        typeof(SharpFortDddApplicationModule),
        typeof(SharpFortFileManagementApplicationModule),
        typeof(SharpFortAiApplicationModule),
        typeof(FluidSequenceApplicationModule)
        )]
    public class SfAbpApplicationModule : AbpModule
    {
    }
}
