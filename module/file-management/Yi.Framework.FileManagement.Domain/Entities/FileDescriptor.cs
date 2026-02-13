using System.Security.Cryptography;
using Microsoft.AspNetCore.StaticFiles;
using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Yi.Framework.FileManagement.Domain.Shared.Consts;
using Yi.Framework.FileManagement.Domain.Shared.Enums;

namespace Yi.Framework.FileManagement.Domain.Entities
{
    /// <summary>
    /// 文件描述符聚合根
    /// 存储文件元数据，实际文件内容由 Blob 存储提供者管理
    /// </summary>
    [SugarTable(FileManagementConsts.DbTablePrefix + "file_descriptor")]
    [SugarIndex($"index_DirectoryId", nameof(DirectoryId), OrderByType.Asc)]
    [SugarIndex($"index_Hash", nameof(Hash), OrderByType.Asc)]
    [SugarIndex($"index_ProviderName", nameof(ProviderName), OrderByType.Asc)]
    public class FileDescriptor : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        #region 构造函数

        public FileDescriptor() { }

        /// <summary>
        /// 创建文件描述符
        /// </summary>
        /// <param name="id">文件ID (UUID7)</param>
        /// <param name="name">原始文件名</param>
        /// <param name="mimeType">MIME 类型</param>
        /// <param name="size">文件大小 (字节)</param>
        /// <param name="hash">SHA-256 哈希</param>
        /// <param name="providerName">存储提供者名称</param>
        /// <param name="directoryId">所属目录ID</param>
        public FileDescriptor(
            Guid id,
            string name,
            string mimeType,
            long size,
            string? hash = null,
            string? providerName = null,
            Guid? directoryId = null)
            : base(id)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(name, nameof(name));

            Name = name;
            MimeType = mimeType ?? "application/octet-stream";
            Size = size;
            Hash = hash;
            ProviderName = providerName;
            DirectoryId = directoryId;

            // BlobName 使用 ID + 扩展名，避免文件名冲突
            var extension = Path.GetExtension(name);
            BlobName = id.ToString() + extension;

            // 自动推断文件类型
            FileType = InferFileType(extension);
        }

        #endregion

        #region 核心属性

        /// <summary>
        /// 主键 (UUID7)
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public override Guid Id { get; protected set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        public Guid? TenantId { get; protected set; }

        /// <summary>
        /// 所属目录ID
        /// </summary>
        public Guid? DirectoryId { get; protected set; }

        /// <summary>
        /// 原始文件名（用户上传时的文件名）
        /// </summary>
        [SugarColumn(Length = FileManagementConsts.MaxFileNameLength)]
        public string Name { get; protected set; }

        /// <summary>
        /// Blob 存储名称（实际存储时使用的名称，通常为 {Id}{Extension}）
        /// </summary>
        [SugarColumn(Length = FileManagementConsts.MaxBlobNameLength)]
        public string BlobName { get; protected set; }

        /// <summary>
        /// MIME 类型
        /// </summary>
        [SugarColumn(Length = FileManagementConsts.MaxMimeTypeLength)]
        public string MimeType { get; protected set; }

        /// <summary>
        /// 文件大小 (字节)
        /// </summary>
        public long Size { get; protected set; }

        /// <summary>
        /// 文件 SHA-256 哈希值
        /// 用于完整性校验和去重
        /// </summary>
        [SugarColumn(IsNullable = true, Length = FileManagementConsts.MaxHashLength)]
        public string? Hash { get; protected set; }

        /// <summary>
        /// 文件类型分类
        /// </summary>
        public FileType FileType { get; protected set; }

        /// <summary>
        /// 存储提供者名称 (e.g., "Local", "CloudflareR2", "Aliyun")
        /// </summary>
        [SugarColumn(IsNullable = true, Length = FileManagementConsts.MaxProviderNameLength)]
        public string? ProviderName { get; protected set; }

        /// <summary>
        /// 外链地址（OSS 上传后生成的公开访问地址）
        /// </summary>
        [SugarColumn(IsNullable = true, Length = FileManagementConsts.MaxUrlLength)]
        public string? Url { get; protected set; }

        /// <summary>
        /// 缩略图外链地址（图片类型文件的缩略图）
        /// </summary>
        [SugarColumn(IsNullable = true, Length = FileManagementConsts.MaxUrlLength)]
        public string? ThumbnailUrl { get; protected set; }

        /// <summary>
        /// 是否公开访问
        /// </summary>
        public bool IsPublic { get; protected set; }

        #endregion

        #region 导航属性

        /// <summary>
        /// 所属目录
        /// </summary>
        [Navigate(NavigateType.OneToOne, nameof(DirectoryId))]
        public DirectoryDescriptor? Directory { get; set; }

        #endregion

        #region 业务方法

        /// <summary>
        /// 设置外链地址
        /// </summary>
        public void SetUrl(string url, string? thumbnailUrl = null)
        {
            Url = url;
            if (thumbnailUrl != null)
            {
                ThumbnailUrl = thumbnailUrl;
            }
        }

        /// <summary>
        /// 设置文件哈希值
        /// </summary>
        public void SetHash(string hash)
        {
            Hash = hash;
        }

        /// <summary>
        /// 设为公开
        /// </summary>
        public void SetPublic(bool isPublic)
        {
            IsPublic = isPublic;
        }

        /// <summary>
        /// 移动到指定目录
        /// </summary>
        public void MoveTo(Guid? directoryId)
        {
            DirectoryId = directoryId;
        }

        /// <summary>
        /// 重命名
        /// </summary>
        public void Rename(string newName)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(newName, nameof(newName));
            Name = newName;
        }

        /// <summary>
        /// 获取格式化的文件大小信息
        /// </summary>
        public string GetSizeInfo()
        {
            return Size switch
            {
                < 1024 => $"{Size} B",
                < 1024 * 1024 => $"{Size / 1024.0:F2} KB",
                < 1024 * 1024 * 1024 => $"{Size / (1024.0 * 1024.0):F2} MB",
                _ => $"{Size / (1024.0 * 1024.0 * 1024.0):F2} GB"
            };
        }

        /// <summary>
        /// 获取文件扩展名
        /// </summary>
        public string GetExtension()
        {
            return Path.GetExtension(Name) ?? string.Empty;
        }

        /// <summary>
        /// 计算流的 SHA-256 哈希并设置
        /// </summary>
        public async Task ComputeAndSetHashAsync(Stream stream)
        {
            var position = stream.Position;
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream);
            Hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            stream.Position = position;
        }

        /// <summary>
        /// 根据扩展名推断文件类型
        /// </summary>
        private static FileType InferFileType(string? extension)
        {
            if (string.IsNullOrEmpty(extension))
                return FileType.File;

            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" or ".ico" or ".avif"
                    => FileType.Image,
                ".mp4" or ".avi" or ".mov" or ".wmv" or ".flv" or ".mkv" or ".webm"
                    => FileType.Video,
                ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or ".wma"
                    => FileType.Audio,
                ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt" or ".csv" or ".md"
                    => FileType.Document,
                ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2"
                    => FileType.Archive,
                _ => FileType.File
            };
        }

        /// <summary>
        /// 获取文件的 MIME 类型（如果未设置则从文件名推断）
        /// </summary>
        public static string GetMimeType(string fileName)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (provider.TryGetContentType(fileName, out var contentType))
            {
                return contentType;
            }
            return "application/octet-stream";
        }

        #endregion
    }
}
