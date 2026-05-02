using System;

namespace SharpFort.FileManagement.Application.Contracts.Dtos.FileDescriptor
{
    public class QuickUploadInput
    {
        public required string Hash { get; set; }
        public required string FileName { get; set; }
        public Guid? DirectoryId { get; set; }
    }
}
