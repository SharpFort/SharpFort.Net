using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace SharpFort.CodeGen.Domain.Shared
{
    [DependsOn(typeof(AbpDddDomainSharedModule))]
    public class SharpFortCodeGenDomainSharedModule : AbpModule
    {

    }
}
