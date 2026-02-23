using Volo.Abp.Application.Dtos;
using SharpFort.FileManagement.Domain.Shared.Enums;

namespace SharpFort.FileManagement.Application.Contracts.Dtos.FileDescriptor
{
    public class FileDescriptorGetListInput : PagedAndSortedResultRequestDto
    {
        public string? Name { get; set; }
        public Guid? DirectoryId { get; set; }
        public FileType? FileType { get; set; }
    }
}
