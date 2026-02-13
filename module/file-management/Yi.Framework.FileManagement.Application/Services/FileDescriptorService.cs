using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Yi.Framework.FileManagement.Application.Contracts.Dtos.FileDescriptor;
using Yi.Framework.FileManagement.Application.Contracts.IServices;
using Yi.Framework.FileManagement.Domain.Entities;
using Yi.Framework.FileManagement.Domain.Managers;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.FileManagement.Application.Services
{
    /// <summary>
    /// 文件描述符服务
    /// 迁移自 CasbinRbac.Application.Services.FileService 并增强
    /// </summary>
    [Authorize]
    public class FileDescriptorService : ApplicationService, IFileDescriptorService
    {
        private readonly ISqlSugarRepository<FileDescriptor> _repository;
        private readonly IFileManager _fileManager;

        public FileDescriptorService(
            ISqlSugarRepository<FileDescriptor> repository,
            IFileManager fileManager)
        {
            _repository = repository;
            _fileManager = fileManager;
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        [AllowAnonymous]
        public async Task<List<FileDescriptorGetOutputDto>> UploadAsync(
            [FromForm] IFormFileCollection files,
            [FromQuery] Guid? directoryId = null)
        {
            var entities = await _fileManager.CreateAsync(files, directoryId);

            // 保存每个文件到 Blob 存储
            for (int i = 0; i < files.Count; i++)
            {
                using var stream = files[i].OpenReadStream();
                await _fileManager.SaveFileAsync(entities[i], stream);
            }

            return entities.Adapt<List<FileDescriptorGetOutputDto>>();
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> DownloadAsync(Guid id, bool isThumbnail = false)
        {
            var file = await _repository.GetAsync(x => x.Id == id);
            if (file == null)
            {
                throw new UserFriendlyException("文件不存在");
            }

            var stream = await _fileManager.GetFileStreamAsync(file, isThumbnail);
            if (stream == null)
            {
                throw new UserFriendlyException("文件内容不存在");
            }

            return new FileStreamResult(stream, file.MimeType)
            {
                FileDownloadName = file.Name
            };
        }

        /// <summary>
        /// 获取文件信息
        /// </summary>
        public async Task<FileDescriptorGetOutputDto> GetAsync(Guid id)
        {
            var entity = await _repository.GetAsync(x => x.Id == id);
            var dto = entity.Adapt<FileDescriptorGetOutputDto>();
            dto.SizeInfo = entity.GetSizeInfo();
            return dto;
        }

        /// <summary>
        /// 获取文件列表
        /// </summary>
        public async Task<PagedResultDto<FileDescriptorGetListOutputDto>> GetListAsync(FileDescriptorGetListInput input)
        {
            RefAsync<int> total = 0;
            var entities = await _repository._DbQueryable
                .WhereIF(!string.IsNullOrEmpty(input.Name), x => x.Name.Contains(input.Name!))
                .WhereIF(input.DirectoryId.HasValue, x => x.DirectoryId == input.DirectoryId)
                .WhereIF(input.FileType.HasValue, x => x.FileType == input.FileType)
                .OrderByDescending(x => x.CreationTime)
                .ToPageListAsync(input.SkipCount + 1, input.MaxResultCount, total);

            var dtos = entities.Adapt<List<FileDescriptorGetListOutputDto>>();

            // 填充 SizeInfo
            for (int i = 0; i < entities.Count; i++)
            {
                dtos[i].SizeInfo = entities[i].GetSizeInfo();
            }

            return new PagedResultDto<FileDescriptorGetListOutputDto>(total, dtos);
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            var file = await _repository.GetAsync(x => x.Id == id);
            if (file == null)
            {
                throw new UserFriendlyException("文件不存在");
            }

            await _fileManager.DeleteFileAsync(file);
            await _repository.DeleteAsync(file);
        }

        /// <summary>
        /// 批量删除文件
        /// </summary>
        public async Task DeleteManyAsync(List<Guid> ids)
        {
            var files = await _repository.GetListAsync(x => ids.Contains(x.Id));
            foreach (var file in files)
            {
                await _fileManager.DeleteFileAsync(file);
            }
            await _repository.DeleteManyAsync(files);
        }

        /// <summary>
        /// 移动文件到指定目录
        /// </summary>
        public async Task MoveAsync(Guid id, Guid? directoryId)
        {
            var file = await _repository.GetAsync(x => x.Id == id);
            if (file == null)
            {
                throw new UserFriendlyException("文件不存在");
            }

            file.MoveTo(directoryId);
            await _repository.UpdateAsync(file);
        }

        /// <summary>
        /// 重命名文件
        /// </summary>
        public async Task RenameAsync(Guid id, string newName)
        {
            var file = await _repository.GetAsync(x => x.Id == id);
            if (file == null)
            {
                throw new UserFriendlyException("文件不存在");
            }

            file.Rename(newName);
            await _repository.UpdateAsync(file);
        }
    }
}
