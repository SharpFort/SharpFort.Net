using Volo.Abp.Application.Dtos;
using Yi.Framework.FileManagement.Domain.Shared.Enums;

namespace Yi.Framework.FileManagement.Application.Contracts.Dtos.FileStorageProvider
{
    public class FileStorageProviderGetListOutputDto : EntityDto<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public StorageProviderType ProviderType { get; set; }
        public string BucketName { get; set; } = string.Empty;
        public string? Endpoint { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsDefault { get; set; }
        public int OrderNum { get; set; }
        public string? Remark { get; set; }
        public DateTime CreationTime { get; set; }
    }
}
