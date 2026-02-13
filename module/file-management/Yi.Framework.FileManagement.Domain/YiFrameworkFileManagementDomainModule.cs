using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Caching;
using Volo.Abp.Domain;
using Volo.Abp.Imaging;
using Yi.Framework.FileManagement.Domain.Shared;
using Yi.Framework.FileManagement.Domain.Services;
using Yi.Framework.Mapster;

namespace Yi.Framework.FileManagement.Domain
{
    [DependsOn(
        typeof(YiFrameworkFileManagementDomainSharedModule),

        typeof(YiFrameworkMapsterModule),
        typeof(AbpDddDomainModule),
        typeof(AbpCachingModule),
        typeof(AbpImagingImageSharpModule)
    )]
    public class YiFrameworkFileManagementDomainModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // 注册 Blob 存储提供者
            context.Services.AddTransient<IBlobStorageProvider, LocalBlobStorageProvider>();
            context.Services.AddTransient<IBlobStorageProvider, S3BlobStorageProvider>();
        }
    }
}