using System;

namespace SharpFort.FileManagement.Application.Contracts.Dtos.FileDescriptor
{
    public class VerifyHashInput
    {
        public required string Hash { get; set; }
    }
}
