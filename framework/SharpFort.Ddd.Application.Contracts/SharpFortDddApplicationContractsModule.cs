using Volo.Abp.Application;
using Volo.Abp.Modularity;

namespace SharpFort.Ddd.Application.Contracts
{
    /// <summary>
    /// Sf框架DDD应用层契约模块
    /// </summary>
    [DependsOn(typeof(AbpDddApplicationContractsModule))]
    public class SharpFortDddApplicationContractsModule : AbpModule
    {
    }
}
