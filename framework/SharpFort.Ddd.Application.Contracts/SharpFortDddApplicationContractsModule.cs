using Volo.Abp.Application;

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
