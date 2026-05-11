using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;
using Volo.Abp.Imaging;
using SharpFort.FileManagement.Domain.Entities;
using SharpFort.FileManagement.Domain.Services;
using SharpFort.FileManagement.Domain.Shared.Consts;
using SharpFort.FileManagement.Domain.Shared.Enums;

namespace SharpFort.FileManagement.Domain.Managers
{
    /// <summary>
    /// 文件管理器领域服务
    /// 迁移自 CasbinRbac.Domain.Managers.FileManager 并增强
    /// </summary>
    public partial class FileManager(
        IGuidGenerator guidGenerator,
        IRepository<FileDescriptor> fileRepository,
        IRepository<FileStorageProvider> providerRepository,
        IImageCompressor imageCompressor,
        IEnumerable<IBlobStorageProvider> blobProviders) : DomainService, IFileManager
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "未找到名为 {ProviderName} 的 Blob 存储提供者，回退到本地存储")]
        private static partial void LogProviderNotFound(ILogger logger, string providerName);

        [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "生成缩略图失败，文件ID: {FileId}")]
        private static partial void LogThumbnailGenerationFailed(ILogger logger, Exception ex, Guid fileId);
        private readonly IGuidGenerator _guidGenerator = guidGenerator;
        private readonly IRepository<FileDescriptor> _fileRepository = fileRepository;
        private readonly IRepository<FileStorageProvider> _providerRepository = providerRepository;
        private readonly IImageCompressor _imageCompressor = imageCompressor;
        private readonly IEnumerable<IBlobStorageProvider> _blobProviders = blobProviders;

        /// <summary>
        /// 批量上传文件并创建记录
        /// </summary>
        public async Task<List<FileDescriptor>> CreateAsync(IEnumerable<IFormFile> files, Guid? directoryId = null)
        {
            if (!files.Any())
            {
                throw new UserFriendlyException("文件上传为空！");
            }

            // 获取默认存储提供者
            FileStorageProvider? defaultProvider = await GetDefaultProviderAsync();
            string providerName = defaultProvider?.ProviderType.ToString() ?? "Local";

            List<FileDescriptor> entities = [];
            foreach (IFormFile file in files)
            {
                string mimeType = FileDescriptor.GetMimeType(file.FileName);
                FileDescriptor entity = new(
                    _guidGenerator.Create(),
                    file.FileName,
                    mimeType,
                    file.Length,
                    providerName: providerName,
                    directoryId: directoryId);

                entities.Add(entity);
            }

            await _fileRepository.InsertManyAsync(entities);
            return entities;
        }

        /// <summary>
        /// 保存文件到存储后端
        /// </summary>
        public async Task SaveFileAsync(FileDescriptor file, Stream fileStream)
        {
            // 获取存储提供者
            FileStorageProvider? providerConfig = await GetDefaultProviderAsync();
            IBlobStorageProvider blobProvider = GetBlobProvider(file.ProviderName);

            string containerName = GetContainerName(file);

            // 计算 SHA-256 哈希
            await file.ComputeAndSetHashAsync(fileStream);

            // 保存文件到 Blob 存储
            await blobProvider.SaveAsync(containerName, file.BlobName, fileStream, providerConfig);

            // 获取并设置文件 URL
            string? url = await blobProvider.GetUrlAsync(containerName, file.BlobName, providerConfig);
            if (url != null)
            {
                file.SetUrl(url);
            }

            // 如果是图片类型，生成缩略图
            if (file.FileType == FileType.Image)
            {
                await TrySaveThumbnailAsync(file, fileStream, blobProvider, providerConfig);
            }

            // 更新文件记录
            await _fileRepository.UpdateAsync(file);
        }

        /// <summary>
        /// 获取文件流
        /// </summary>
        public async Task<Stream?> GetFileStreamAsync(FileDescriptor file, bool isThumbnail = false)
        {
            FileStorageProvider? providerConfig = await GetProviderConfigAsync(file.ProviderName);
            IBlobStorageProvider blobProvider = GetBlobProvider(file.ProviderName);
            string containerName = GetContainerName(file);

            // 如果是获取缩略图
            if (isThumbnail)
            {
                // 只有图片才有缩略图
                if (file.FileType != FileType.Image)
                {
                    return null;
                }
                containerName = FileManagementConsts.ThumbnailDirectory;
            }

            return await blobProvider.GetAsync(containerName, file.BlobName, providerConfig);
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        public async Task DeleteFileAsync(FileDescriptor file)
        {
            FileStorageProvider? providerConfig = await GetProviderConfigAsync(file.ProviderName);
            IBlobStorageProvider blobProvider = GetBlobProvider(file.ProviderName);
            string containerName = GetContainerName(file);

            // 删除 Blob
            await blobProvider.DeleteAsync(containerName, file.BlobName, providerConfig);

            // 删除缩略图 (如果有)
            if (file.FileType == FileType.Image)
            {
                await blobProvider.DeleteAsync(FileManagementConsts.ThumbnailDirectory, file.BlobName, providerConfig);
            }
        }

        /// <summary>
        /// 获取文件 URL
        /// </summary>
        public async Task<string?> GetFileUrlAsync(FileDescriptor file)
        {
            if (!string.IsNullOrEmpty(file.Url))
            {
                return file.Url;
            }

            FileStorageProvider? providerConfig = await GetProviderConfigAsync(file.ProviderName);
            IBlobStorageProvider blobProvider = GetBlobProvider(file.ProviderName);
            string containerName = GetContainerName(file);

            return await blobProvider.GetUrlAsync(containerName, file.BlobName, providerConfig);
        }

        #region 私有方法

        /// <summary>
        /// 获取默认存储提供者配置
        /// </summary>
        private async Task<FileStorageProvider?> GetDefaultProviderAsync()
        {
            return await _providerRepository
                .FindAsync(x => x.IsDefault && x.IsEnabled);
        }

        /// <summary>
        /// 根据提供者名称获取配置
        /// </summary>
        private async Task<FileStorageProvider?> GetProviderConfigAsync(string? providerName)
        {
            return string.IsNullOrEmpty(providerName) || providerName == "Local"
                ? null
                : await _providerRepository
                .FindAsync(x => x.ProviderType.ToString() == providerName && x.IsEnabled);
        }

        /// <summary>
        /// 获取 Blob 存储提供者实现
        /// </summary>
        private IBlobStorageProvider GetBlobProvider(string? providerName)
        {
            if (string.IsNullOrEmpty(providerName) || providerName == "Local")
            {
                return _blobProviders.First(x => x.ProviderName == "Local");
            }

            IBlobStorageProvider? provider = _blobProviders.FirstOrDefault(x => x.ProviderName == providerName);
            if (provider == null)
            {
                LogProviderNotFound(LoggerFactory.CreateLogger<FileManager>(), providerName);
                return _blobProviders.First(x => x.ProviderName == "Local");
            }

            return provider;
        }

        /// <summary>
        /// 获取容器名（根据文件类型分类存储）
        /// </summary>
        private static string GetContainerName(FileDescriptor file)
        {
            return file.FileType switch
            {
                FileType.Image => "image",
                FileType.Video => "video",
                FileType.Audio => "audio",
                FileType.Document => "document",
                FileType.Archive => "archive",
                _ => "file"
            };
        }

        /// <summary>
        /// 尝试生成并保存缩略图
        /// </summary>
        private async Task TrySaveThumbnailAsync(
            FileDescriptor file,
            Stream fileStream,
            IBlobStorageProvider blobProvider,
            FileStorageProvider? providerConfig)
        {
            try
            {
                fileStream.Position = 0;
                ImageCompressResult<Stream> compressResult = await _imageCompressor.CompressAsync(fileStream, file.MimeType);
                Stream thumbnailStream;

                if (compressResult.State == ImageProcessState.Done)
                {
                    thumbnailStream = compressResult.Result;
                }
                else
                {
                    // 压缩失败，使用原图复制一份作为缩略图
                    fileStream.Position = 0;
                    thumbnailStream = fileStream;
                }

                await blobProvider.SaveAsync(
                    FileManagementConsts.ThumbnailDirectory,
                    file.BlobName,
                    thumbnailStream,
                    providerConfig);

                // 设置缩略图 URL
                string? thumbnailUrl = await blobProvider.GetUrlAsync(
                    FileManagementConsts.ThumbnailDirectory,
                    file.BlobName,
                    providerConfig);
                if (thumbnailUrl != null)
                {
                    file.SetUrl(file.Url!, thumbnailUrl);
                }
            }
            catch (Exception ex)
            {
                LogThumbnailGenerationFailed(LoggerFactory.CreateLogger<FileManager>(), ex, file.Id);
            }
        }

        #endregion
    }
}
