namespace Yi.Framework.FileManagement.Domain.Services
{
    /// <summary>
    /// Blob 存储提供者抽象接口
    /// 负责实际的文件 I/O 操作
    /// </summary>
    public interface IBlobStorageProvider
    {
        /// <summary>
        /// 提供者名称标识，用于匹配 FileStorageProvider.ProviderType
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// 保存 Blob
        /// </summary>
        /// <param name="containerName">容器/桶名称</param>
        /// <param name="blobName">Blob 名称</param>
        /// <param name="stream">文件流</param>
        /// <param name="config">存储提供者配置 (可选)</param>
        Task SaveAsync(string containerName, string blobName, Stream stream, Entities.FileStorageProvider? config = null);

        /// <summary>
        /// 获取 Blob
        /// </summary>
        /// <param name="containerName">容器/桶名称</param>
        /// <param name="blobName">Blob 名称</param>
        /// <param name="config">存储提供者配置 (可选)</param>
        /// <returns>文件流，如果文件不存在返回 null</returns>
        Task<Stream?> GetAsync(string containerName, string blobName, Entities.FileStorageProvider? config = null);

        /// <summary>
        /// 删除 Blob
        /// </summary>
        /// <param name="containerName">容器/桶名称</param>
        /// <param name="blobName">Blob 名称</param>
        /// <param name="config">存储提供者配置 (可选)</param>
        Task DeleteAsync(string containerName, string blobName, Entities.FileStorageProvider? config = null);

        /// <summary>
        /// 获取 Blob 的公开访问 URL
        /// </summary>
        /// <param name="containerName">容器/桶名称</param>
        /// <param name="blobName">Blob 名称</param>
        /// <param name="config">存储提供者配置 (可选)</param>
        /// <returns>公开访问 URL</returns>
        Task<string?> GetUrlAsync(string containerName, string blobName, Entities.FileStorageProvider? config = null);

        /// <summary>
        /// 检查 Blob 是否存在
        /// </summary>
        Task<bool> ExistsAsync(string containerName, string blobName, Entities.FileStorageProvider? config = null);
    }
}
