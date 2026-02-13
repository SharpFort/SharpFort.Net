# FileManagement 模块

`FileManagement` 模块提供了统一的文件存储操作接口，支持本地文件系统和云存储（兼容 S3）提供商。

## 核心概念

- **FileDescriptor**: 表示文件的元数据（名称、大小、类型等）。
- **DirectoryDescriptor**: 表示用于组织文件的目录结构。
- **FileStorageProvider**: 存储后端配置（本地、S3、阿里云等）。

## 使用可能

### 1. 依赖注入

在你的应用模块中，添加对 `YiFrameworkFileManagementApplicationContractsModule` 的依赖：

```csharp
[DependsOn(
    typeof(Yi.Framework.FileManagement.Application.Contracts.YiFrameworkFileManagementApplicationContractsModule)
)]
public class YourModule : AbpModule
{
}
```

### 2. 服务注入

注入 `IFileDescriptorService` 来管理文件：

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

### 3. 常用操作

#### 上传文件

```csharp
public async Task UploadFileAsync(IFormFile file)
{
    var files = new FormFileCollection { file };
    // directoryId is optional (目录ID是可选的)
    var result = await _fileService.UploadAsync(files, directoryId: null);
    var fileId = result.First().Id;
}
```

#### 下载文件

```csharp
public async Task<IActionResult> DownloadFileAsync(Guid fileId)
{
    // 返回 FileStreamResult 或 FileContentResult
    return await _fileService.DownloadAsync(fileId);
}
```

#### 获取文件信息

```csharp
public async Task GetFileInfoAsync(Guid fileId)
{
    var fileDto = await _fileService.GetAsync(fileId);
    Console.WriteLine($"文件名: {fileDto.Name}, Url: {fileDto.Url}");
}
```

## 配置

### 本地存储（默认）

如果没有配置云存储提供商，系统默认使用 **本地存储**。

- **存储位置**: 文件存储在 `wwwroot/local-bucket/{containerName}/{blobName}`。
- **URL**: 文件通过相对路径访问，如 `/api/app/wwwroot/{containerName}/{blobName}`。

**本地存储无需额外配置。** 开箱即用。请确保你的应用程序对 `wwwroot` 目录具有写入权限。

### 云存储 (S3/阿里云/MinIO)

要使用云存储，需要在系统中配置 `FileStorageProvider`（通常通过 UI 或数据库种子数据）。

1.  **提供者类型**: 设置为 `S3` 或 `Aliyun`。
2.  **配置**:
    *   **Endpoint**: 服务端点（例如 `https://oss-cn-hangzhou.aliyuncs.com` 或 MinIO URL）。
    *   **BucketName**: 存储桶名称。
    *   **AccessKey**: API 访问密钥。
    *   **SecretKey**: API 密钥。
    *   **CustomDomain** (可选): 文件访问的自定义域名。

### 回退机制

系统使用 `FileStorageProvider` 实体来确定使用哪个提供商。该逻辑由领域层的 `FileManager` 处理。
