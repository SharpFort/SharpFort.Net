using Yi.Framework.FileManagement.Application.Contracts;
using Yi.Framework.FileManagement.Domain;
using Yi.Framework.Ddd.Application;

namespace Yi.Framework.FileManagement.Application
{
    [DependsOn(
        typeof(YiFrameworkFileManagementApplicationContractsModule),
        typeof(YiFrameworkFileManagementDomainModule),
        
        typeof(YiFrameworkDddApplicationModule)

    )]
    public class YiFrameworkFileManagementApplicationModule : AbpModule
    {
    }
}