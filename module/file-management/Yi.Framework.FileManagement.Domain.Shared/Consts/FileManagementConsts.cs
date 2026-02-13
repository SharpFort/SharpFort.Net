namespace Yi.Framework.FileManagement.Domain.Shared.Consts
{
    public static class FileManagementConsts
    {
        /// <summary>
        /// 数据库表前缀
        /// </summary>
        public const string DbTablePrefix = "fm_";

        /// <summary>
        /// 最大文件名长度
        /// </summary>
        public const int MaxFileNameLength = 256;

        /// <summary>
        /// 最大 BlobName 长度
        /// </summary>
        public const int MaxBlobNameLength = 256;

        /// <summary>
        /// 最大 MimeType 长度
        /// </summary>
        public const int MaxMimeTypeLength = 128;

        /// <summary>
        /// 最大 SHA256 Hash 长度
        /// </summary>
        public const int MaxHashLength = 64;

        /// <summary>
        /// 最大 URL 长度
        /// </summary>
        public const int MaxUrlLength = 1024;

        /// <summary>
        /// 最大目录名长度
        /// </summary>
        public const int MaxDirectoryNameLength = 256;

        /// <summary>
        /// 最大提供者名称长度
        /// </summary>
        public const int MaxProviderNameLength = 64;

        /// <summary>
        /// 最大 Bucket 名称长度
        /// </summary>
        public const int MaxBucketNameLength = 128;

        /// <summary>
        /// 最大密钥长度
        /// </summary>
        public const int MaxKeyLength = 256;

        /// <summary>
        /// 最大端点长度
        /// </summary>
        public const int MaxEndpointLength = 512;

        /// <summary>
        /// 最大备注长度
        /// </summary>
        public const int MaxRemarkLength = 512;

        /// <summary>
        /// 默认本地存储 Bucket 名
        /// </summary>
        public const string DefaultLocalBucket = "wwwroot";

        /// <summary>
        /// 缩略图子目录
        /// </summary>
        public const string ThumbnailDirectory = "thumbnail";
    }
}
