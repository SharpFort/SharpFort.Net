using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Caching;
using Volo.Abp.Domain;
using Volo.Abp.Imaging;
using SharpFort.FileManagement.Domain.Shared;
using SharpFort.FileManagement.Domain.Services;
using SharpFort.Mapster;

namespace SharpFort.FileManagement.Domain
{
    [DependsOn(
        typeof(SharpFortFileManagementDomainSharedModule),

        typeof(SharpFortMapsterModule),
        typeof(AbpDddDomainModule),
        typeof(AbpCachingModule),
        typeof(AbpImagingImageSharpModule)
    )]
    public class SharpFortFileManagementDomainModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // 注册 Blob 存储提供者
            context.Services.AddTransient<IBlobStorageProvider, LocalBlobStorageProvider>();
            context.Services.AddTransient<IBlobStorageProvider, S3BlobStorageProvider>();
        }
    }
}