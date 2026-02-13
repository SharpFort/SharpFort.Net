using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Yi.Framework.FileManagement.Application.Contracts.Dtos.FileDescriptor;

namespace Yi.Framework.FileManagement.Application.Contracts.IServices
{
    /// <summary>
    /// 文件描述符服务接口
    /// </summary>
    public interface IFileDescriptorService : IApplicationService
    {
        /// <summary>
        /// 上传文件
        /// </summary>
        Task<List<FileDescriptorGetOutputDto>> UploadAsync([FromForm] IFormFileCollection files, [FromQuery] Guid? directoryId = null);

        /// <summary>
        /// 下载文件
        /// </summary>
        Task<IActionResult> DownloadAsync(Guid id);

        /// <summary>
        /// 获取文件信息
        /// </summary>
        Task<FileDescriptorGetOutputDto> GetAsync(Guid id);

        /// <summary>
        /// 获取文件列表
        /// </summary>
        Task<PagedResultDto<FileDescriptorGetListOutputDto>> GetListAsync(FileDescriptorGetListInput input);

        /// <summary>
        /// 删除文件
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// 批量删除文件
        /// </summary>
        Task DeleteManyAsync(List<Guid> ids);

        /// <summary>
        /// 移动文件到指定目录
        /// </summary>
        Task MoveAsync(Guid id, Guid? directoryId);

        /// <summary>
        /// 重命名文件
        /// </summary>
        Task RenameAsync(Guid id, string newName);
    }
}
