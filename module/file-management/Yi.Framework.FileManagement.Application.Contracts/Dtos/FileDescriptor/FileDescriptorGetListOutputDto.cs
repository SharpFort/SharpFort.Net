using Volo.Abp.Application.Dtos;
using Yi.Framework.FileManagement.Domain.Shared.Enums;

namespace Yi.Framework.FileManagement.Application.Contracts.Dtos.FileDescriptor
{
    public class FileDescriptorGetListOutputDto : EntityDto<Guid>
    {
        public Guid? DirectoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long Size { get; set; }
        public FileType FileType { get; set; }
        public string? ProviderName { get; set; }
        public string? Url { get; set; }
        public string? ThumbnailUrl { get; set; }
        public bool IsPublic { get; set; }
        public string? SizeInfo { get; set; }
        public DateTime CreationTime { get; set; }
        public Guid? CreatorId { get; set; }
    }
}
