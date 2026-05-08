using Volo.Abp.Domain;
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
