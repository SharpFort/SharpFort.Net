using SharpFort.Tool.Application.Contracts;
using SharpFort.Tool.Domain;

namespace SharpFort.Tool.Application
{
    [DependsOn(typeof(SfAbpToolApplicationContractsModule),
        typeof(SfAbpToolDomainModule))]
    public class SfAbpToolApplicationModule:AbpModule
    {

    }
}
