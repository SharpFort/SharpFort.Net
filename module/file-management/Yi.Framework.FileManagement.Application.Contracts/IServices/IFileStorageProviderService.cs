using Yi.Framework.Ddd.Application.Contracts;
using Yi.Framework.FileManagement.Application.Contracts.Dtos.FileStorageProvider;

namespace Yi.Framework.FileManagement.Application.Contracts.IServices
{
    /// <summary>
    /// 文件存储提供者服务接口
    /// 继承 YiCrudAppService 标准 CRUD 模式
    /// </summary>
    public interface IFileStorageProviderService : IYiCrudAppService<
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
