using System;

namespace SharpFort.FileManagement.Application.Contracts.Dtos.FileDescriptor
{
    public class FileVerifyResultDto
    {
        public bool CanQuickUpload { get; set; }
        public Guid? FileId { get; set; }
        public string? Url { get; set; }
    }
}
