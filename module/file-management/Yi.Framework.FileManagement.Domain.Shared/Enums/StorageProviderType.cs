namespace Yi.Framework.FileManagement.Domain.Shared.Enums
{
    /// <summary>
    /// 存储提供者类型
    /// </summary>
    public enum StorageProviderType
    {
        /// <summary>
        /// 本地存储
        /// </summary>
        Local = 0,

        /// <summary>
        /// S3兼容存储 (Cloudflare R2 / MinIO / AWS S3)
        /// </summary>
        S3Compatible = 1,

        /// <summary>
        /// 阿里云 OSS
        /// </summary>
        Aliyun = 2,

        /// <summary>
        /// 腾讯云 COS
        /// </summary>
        TencentCloud = 3
    }
}
