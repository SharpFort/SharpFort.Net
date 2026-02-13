# FileManagement Module

The `FileManagement` module provides a unified interface for file storage operations, supporting both local filesystem and cloud storage (S3-compatible) providers.

## Key Concepts

- **FileDescriptor**: Represents a file's metadata (name, size, type, etc.).
- **DirectoryDescriptor**: Represents a directory structure for organizing files.
- **FileStorageProvider**: Configuration for storage backends (Local, S3, Aliyun, etc.).

## Usage

### 1. Dependency Injection

In your application module, add a dependency on `YiFrameworkFileManagementApplicationContractsModule`:

```csharp
[DependsOn(
    typeof(Yi.Framework.FileManagement.Application.Contracts.YiFrameworkFileManagementApplicationContractsModule)
)]
public class YourModule : AbpModule
{
}
```

### 2. Service Injection

Inject `IFileDescriptorService` to manage files:

```csharp
using Yi.Framework.FileManagement.Application.Contracts.IServices;

public class YourService : ApplicationService
{
    private readonly IFileDescriptorService _fileService;

    public YourService(IFileDescriptorService fileService)
    {
        _fileService = fileService;
    }
}
```

### 3. Common Operations

#### Upload File

```csharp
public async Task UploadFileAsync(IFormFile file)
{
    var files = new FormFileCollection { file };
    // directoryId is optional
    var result = await _fileService.UploadAsync(files, directoryId: null);
    var fileId = result.First().Id;
}
```

#### Download File

```csharp
public async Task<IActionResult> DownloadFileAsync(Guid fileId)
{
    // Returns a FileStreamResult or FileContentResult
    return await _fileService.DownloadAsync(fileId);
}
```

#### Get File Info

```csharp
public async Task GetFileInfoAsync(Guid fileId)
{
    var fileDto = await _fileService.GetAsync(fileId);
    Console.WriteLine($"File Name: {fileDto.Name}, Url: {fileDto.Url}");
}
```

## Configuration

### Local Storage (Default)

If no cloud storage provider is configured, the system defaults to **Local Storage**.

- **Storage Location**: Files are stored in `wwwroot/local-bucket/{containerName}/{blobName}`.
- **URL**: Files are accessible via relative paths like `/api/app/wwwroot/{containerName}/{blobName}`.

**No additional configuration is required for local storage.** It works out-of-the-box. Ensure your application has write permissions to the `wwwroot` directory.

### Cloud Storage (S3/Aliyun/MinIO)

To use cloud storage, you need to configure a `FileStorageProvider` in the system (usually via the UI or database seed).

1.  **Provider Type**: Set to `S3` or `Aliyun`.
2.  **Configuration**:
    *   **Endpoint**: Service endpoint (e.g., `https://oss-cn-hangzhou.aliyuncs.com` or MinIO URL).
    *   **BucketName**: Name of the bucket.
    *   **AccessKey**: API Access Key.
    *   **SecretKey**: API Secret Key.
    *   **CustomDomain** (Optional): custom domain for file access.

### Fallback Mechanism

The system uses the `FileStorageProvider` entity to determine which provider to use. logic is handled by `FileManager` in the domain layer.
