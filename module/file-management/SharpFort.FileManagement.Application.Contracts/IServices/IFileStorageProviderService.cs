using SharpFort.Ddd.Application.Contracts;
using SharpFort.FileManagement.Application.Contracts.Dtos.FileStorageProvider;

namespace SharpFort.FileManagement.Application.Contracts.IServices
{
    /// <summary>
    /// 文件存储提供者服务接口
    /// 继承 SfCrudAppService 标准 CRUD 模式
    /// </summary>
    public interface IFileStorageProviderService : ISfCrudAppService<
        FileStorageProviderGetOutputDto,
        FileStorageProviderGetListOutputDto,
        Guid,
        FileStorageProviderGetListInput,
        FileStorageProviderCreateInput,
        FileStorageProviderUpdateInput>
    {
        /// <summary>
        /// 设为默认提供者
        /// </summary>
        Task SetDefaultAsync(Guid id);
    }
}
