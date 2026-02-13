using Microsoft.AspNetCore.Http;
using Yi.Framework.FileManagement.Domain.Entities;

namespace Yi.Framework.FileManagement.Domain.Managers
{
    /// <summary>
    /// 文件管理器接口
    /// </summary>
    public interface IFileManager
    {
        /// <summary>
        /// 创建文件描述符并保存文件到存储
        /// </summary>
        Task<List<FileDescriptor>> CreateAsync(IEnumerable<IFormFile> files, Guid? directoryId = null);

        /// <summary>
        /// 保存文件到 Blob 存储
        /// </summary>
        Task SaveFileAsync(FileDescriptor file, Stream fileStream);

        /// <summary>
        /// 获取文件流
        /// </summary>
        Task<Stream?> GetFileStreamAsync(FileDescriptor file);

        /// <summary>
        /// 删除文件 (从存储和数据库)
        /// </summary>
        Task DeleteFileAsync(FileDescriptor file);

        /// <summary>
        /// 获取文件的公开访问 URL
        /// </summary>
        Task<string?> GetFileUrlAsync(FileDescriptor file);
    }
}
