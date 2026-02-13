using Volo.Abp.Application.Dtos;
using Yi.Framework.FileManagement.Domain.Shared.Enums;

namespace Yi.Framework.FileManagement.Application.Contracts.Dtos.FileDescriptor
{
    public class FileDescriptorGetListInput : PagedAndSortedResultRequestDto
    {
        public string? Name { get; set; }
        public Guid? DirectoryId { get; set; }
        public FileType? FileType { get; set; }
    }
}
