using System;

namespace SharpFort.FileManagement.Application.Contracts.Dtos.FileDescriptor
{
    public class QuickUploadInput
    {
        public string Hash { get; set; }
        public string FileName { get; set; }
        public Guid? DirectoryId { get; set; }
    }
}
