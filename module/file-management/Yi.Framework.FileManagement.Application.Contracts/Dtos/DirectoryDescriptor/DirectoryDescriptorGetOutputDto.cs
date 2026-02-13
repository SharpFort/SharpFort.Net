using Volo.Abp.Application.Dtos;

namespace Yi.Framework.FileManagement.Application.Contracts.Dtos.DirectoryDescriptor
{
    public class DirectoryDescriptorGetOutputDto : EntityDto<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public DateTime CreationTime { get; set; }
        public Guid? CreatorId { get; set; }
    }
}
