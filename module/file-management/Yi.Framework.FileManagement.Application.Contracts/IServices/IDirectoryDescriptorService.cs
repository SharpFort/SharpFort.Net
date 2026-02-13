using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Yi.Framework.FileManagement.Application.Contracts.Dtos.DirectoryDescriptor;

namespace Yi.Framework.FileManagement.Application.Contracts.IServices
{
    /// <summary>
    /// 目录描述符服务接口
    /// </summary>
    public interface IDirectoryDescriptorService : IApplicationService
    {
        /// <summary>
        /// 创建目录
        /// </summary>
        Task<DirectoryDescriptorGetOutputDto> CreateAsync(DirectoryDescriptorCreateInput input);

        /// <summary>
        /// 获取目录信息
        /// </summary>
        Task<DirectoryDescriptorGetOutputDto> GetAsync(Guid id);

        /// <summary>
        /// 获取目录列表（指定父级下的子目录）
        /// </summary>
        Task<List<DirectoryDescriptorGetOutputDto>> GetListAsync(Guid? parentId = null);

        /// <summary>
        /// 更新目录
        /// </summary>
        Task<DirectoryDescriptorGetOutputDto> UpdateAsync(Guid id, DirectoryDescriptorUpdateInput input);

        /// <summary>
        /// 删除目录
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// 移动目录
        /// </summary>
        Task MoveAsync(Guid id, Guid? newParentId);
    }
}
