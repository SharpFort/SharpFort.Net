using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Yi.Framework.Core.Enums;
using Yi.Framework.Core.Helper;
using Yi.Framework.CasbinRbac.Application.Contracts.Dtos.FileManager;
using Yi.Framework.CasbinRbac.Application.Contracts.IServices;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.CasbinRbac.Domain.Managers;

namespace Yi.Framework.CasbinRbac.Application.Services
{
    public class FileService : ApplicationService, IFileService
    {
        private readonly IRepository<FileStorage> _repository;
        private readonly FileManager _fileManager;

        public FileService(IRepository<FileStorage> repository, FileManager fileManager)
        {
            _repository = repository;
            _fileManager = fileManager;
        }

        /// <summary>
        /// 下载文件,支持缩略图
        /// </summary>
        /// <returns></returns>
        [Route("file/{code}/{isThumbnail?}")]
        public async Task<IActionResult> Get([FromRoute] Guid code, [FromRoute] bool? isThumbnail)
        {
            var file = await _repository.GetAsync(x => x.Id == code);
            var path = file?.GetQueryFileSavePath(isThumbnail);
            if (path is null || !File.Exists(path))
            {
                return new NotFoundResult();
            }
            var steam = await File.ReadAllBytesAsync(path);
            return new FileContentResult(steam, file.GetMimeMapping());
        }
        
        /// <summary>
        /// 上传文件
        /// </summary>
        /// <returns></returns>
        public async Task<List<FileGetListOutputDto>> Post([FromForm] IFormFileCollection file)
        {
            var entities = await _fileManager.CreateAsync(file);

            for (int i = 0; i < file.Count; i++)
            {
               var entity= entities[i];
               using (var steam = file[i].OpenReadStream())
               {
                   await _fileManager.SaveFileAsync(entity,steam); 
               }
            }
            return entities.Adapt<List<FileGetListOutputDto>>();
        }
    }
}