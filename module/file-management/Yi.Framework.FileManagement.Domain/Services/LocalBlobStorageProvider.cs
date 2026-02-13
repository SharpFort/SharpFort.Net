using Microsoft.Extensions.Logging;
using Yi.Framework.FileManagement.Domain.Shared.Consts;

namespace Yi.Framework.FileManagement.Domain.Services
{
    /// <summary>
    /// 本地文件系统 Blob 存储提供者
    /// </summary>
    public class LocalBlobStorageProvider : IBlobStorageProvider
    {
        private readonly ILogger<LocalBlobStorageProvider> _logger;

        public LocalBlobStorageProvider(ILogger<LocalBlobStorageProvider> logger)
        {
            _logger = logger;
        }

        public string ProviderName => "Local";

        public async Task SaveAsync(string containerName, string blobName, Stream stream, Entities.FileStorageProvider? config = null)
        {
            var dirPath = GetDirectoryPath(containerName);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            var filePath = Path.Combine(dirPath, blobName);
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fileStream);

            _logger.LogDebug("File saved to local: {FilePath}", filePath);
        }

        public Task<Stream?> GetAsync(string containerName, string blobName, Entities.FileStorageProvider? config = null)
        {
            var filePath = GetFilePath(containerName, blobName);
            if (!File.Exists(filePath))
            {
                return Task.FromResult<Stream?>(null);
            }

            Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return Task.FromResult<Stream?>(stream);
        }

        public Task DeleteAsync(string containerName, string blobName, Entities.FileStorageProvider? config = null)
        {
            var filePath = GetFilePath(containerName, blobName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("File deleted from local: {FilePath}", filePath);
            }
            return Task.CompletedTask;
        }

        public Task<string?> GetUrlAsync(string containerName, string blobName, Entities.FileStorageProvider? config = null)
        {
            // 本地文件使用相对路径作为 URL
            var url = $"/api/app/wwwroot/{containerName}/{blobName}";
            return Task.FromResult<string?>(url);
        }

        public Task<bool> ExistsAsync(string containerName, string blobName, Entities.FileStorageProvider? config = null)
        {
            var filePath = GetFilePath(containerName, blobName);
            return Task.FromResult(File.Exists(filePath));
        }

        private static string GetDirectoryPath(string containerName)
        {
            return Path.Combine(FileManagementConsts.DefaultLocalBucket, containerName);
        }

        private static string GetFilePath(string containerName, string blobName)
        {
            return Path.Combine(FileManagementConsts.DefaultLocalBucket, containerName, blobName);
        }
    }
}
