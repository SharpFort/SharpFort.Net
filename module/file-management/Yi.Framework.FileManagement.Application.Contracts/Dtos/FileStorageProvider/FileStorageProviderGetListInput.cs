using Volo.Abp.Application.Dtos;
using Yi.Framework.FileManagement.Domain.Shared.Enums;

namespace Yi.Framework.FileManagement.Application.Contracts.Dtos.FileStorageProvider
{
    public class FileStorageProviderGetListInput : PagedAndSortedResultRequestDto
    {
        public string? Name { get; set; }
        public StorageProviderType? ProviderType { get; set; }
        public bool? IsEnabled { get; set; }
    }
}
