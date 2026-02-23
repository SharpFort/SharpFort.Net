using Volo.Abp.Application.Dtos;
using SharpFort.FileManagement.Domain.Shared.Enums;

namespace SharpFort.FileManagement.Application.Contracts.Dtos.FileStorageProvider
{
    public class FileStorageProviderGetListInput : PagedAndSortedResultRequestDto
    {
        public string? Name { get; set; }
        public StorageProviderType? ProviderType { get; set; }
        public bool? IsEnabled { get; set; }
    }
}
