using SharpFort.FileManagement.Application.Contracts;
using SharpFort.FileManagement.Domain;
using SharpFort.Ddd.Application;

namespace SharpFort.FileManagement.Application
{
    [DependsOn(
        typeof(SharpFortFileManagementApplicationContractsModule),
        typeof(SharpFortFileManagementDomainModule),
        
        typeof(SharpFortDddApplicationModule)

    )]
    public class SharpFortFileManagementApplicationModule : AbpModule
    {
    }
}