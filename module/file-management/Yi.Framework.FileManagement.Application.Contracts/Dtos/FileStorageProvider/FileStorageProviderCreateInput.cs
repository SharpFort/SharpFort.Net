using Yi.Framework.FileManagement.Domain.Shared.Enums;

namespace Yi.Framework.FileManagement.Application.Contracts.Dtos.FileStorageProvider
{
    public class FileStorageProviderCreateInput
    {
        public string Name { get; set; } = string.Empty;
        public StorageProviderType ProviderType { get; set; }
        public string BucketName { get; set; } = string.Empty;
        public string? AccessKey { get; set; }
        public string? SecretKey { get; set; }
        public string? Endpoint { get; set; }
        public string? Region { get; set; }
        public string? CustomDomain { get; set; }
        public bool IsEnableHttps { get; set; } = true;
        public string? Remark { get; set; }
    }
}
