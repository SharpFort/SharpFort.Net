using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using SharpFort.CodeGen.Domain.Shared;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CodeGen.Domain
{
    [DependsOn(typeof(SharpFortCodeGenDomainSharedModule),
        typeof(AbpDddDomainModule),
        typeof(SharpFortSqlSugarCoreAbstractionsModule))]
    public class SharpFortCodeGenDomainModule : AbpModule
    {

    }
}
