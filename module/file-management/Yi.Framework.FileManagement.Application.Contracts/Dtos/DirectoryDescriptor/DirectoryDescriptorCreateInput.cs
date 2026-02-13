namespace Yi.Framework.FileManagement.Application.Contracts.Dtos.DirectoryDescriptor
{
    public class DirectoryDescriptorCreateInput
    {
        public string Name { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
    }
}
