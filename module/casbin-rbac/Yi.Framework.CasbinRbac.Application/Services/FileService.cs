using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Services;
using Yi.Framework.CasbinRbac.Application.Contracts.Dtos.FileManager;
using Yi.Framework.CasbinRbac.Application.Contracts.IServices;
using Yi.Framework.FileManagement.Application.Contracts.IServices;

namespace Yi.Framework.CasbinRbac.Application.Services
{
    public class FileService : ApplicationService, IFileService
    {
        private readonly IFileDescriptorService _fileDescriptorService;

        public FileService(IFileDescriptorService fileDescriptorService)
        {
            _fileDescriptorService = fileDescriptorService;
        }

        /// <summary>
        /// 下载文件,支持缩略图
        /// </summary>
        /// <returns></returns>
        [Route("file/{code}/{isThumbnail?}")]
        public async Task<IActionResult> Get([FromRoute] Guid code, [FromRoute] bool? isThumbnail)
        {
            return await _fileDescriptorService.DownloadAsync(code, isThumbnail ?? false);
        }
        
        /// <summary>
        /// 上传文件
        /// </summary>
        /// <returns></returns>
        public async Task<List<FileGetListOutputDto>> Post([FromForm] IFormFileCollection file)
        {
            var result = await _fileDescriptorService.UploadAsync(file);
            return result.Adapt<List<FileGetListOutputDto>>();
        }
    }
}