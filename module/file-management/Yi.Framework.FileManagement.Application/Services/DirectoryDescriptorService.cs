using Mapster;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Yi.Framework.FileManagement.Application.Contracts.Dtos.DirectoryDescriptor;
using Yi.Framework.FileManagement.Application.Contracts.IServices;
using Yi.Framework.FileManagement.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.FileManagement.Application.Services
{
    /// <summary>
    /// 目录描述符服务
    /// </summary>
    [Authorize]
    public class DirectoryDescriptorService : ApplicationService, IDirectoryDescriptorService
    {
        private readonly ISqlSugarRepository<DirectoryDescriptor> _repository;
        private readonly ISqlSugarRepository<FileDescriptor> _fileRepository;
        private readonly IGuidGenerator _guidGenerator;

        public DirectoryDescriptorService(
            ISqlSugarRepository<DirectoryDescriptor> repository,
            ISqlSugarRepository<FileDescriptor> fileRepository,
            IGuidGenerator guidGenerator)
        {
            _repository = repository;
            _fileRepository = fileRepository;
            _guidGenerator = guidGenerator;
        }

        /// <summary>
        /// 创建目录
        /// </summary>
        public async Task<DirectoryDescriptorGetOutputDto> CreateAsync(DirectoryDescriptorCreateInput input)
        {
            // 检查同级目录下是否重名
            var exists = await _repository.AnyAsync(x =>
                x.ParentId == input.ParentId && x.Name == input.Name);
            if (exists)
            {
                throw new UserFriendlyException("同级目录下已存在同名目录");
            }

            var entity = new DirectoryDescriptor(_guidGenerator.Create(), input.Name, input.ParentId);
            await _repository.InsertAsync(entity);

            return entity.Adapt<DirectoryDescriptorGetOutputDto>();
        }

        /// <summary>
        /// 获取目录信息
        /// </summary>
        public async Task<DirectoryDescriptorGetOutputDto> GetAsync(Guid id)
        {
            var entity = await _repository.GetAsync(x => x.Id == id);
            return entity.Adapt<DirectoryDescriptorGetOutputDto>();
        }

        /// <summary>
        /// 获取指定父级下的子目录列表
        /// </summary>
        public async Task<List<DirectoryDescriptorGetOutputDto>> GetListAsync(Guid? parentId = null)
        {
            var entities = await _repository.GetListAsync(x => x.ParentId == parentId);
            return entities.Adapt<List<DirectoryDescriptorGetOutputDto>>();
        }

        /// <summary>
        /// 更新目录
        /// </summary>
        public async Task<DirectoryDescriptorGetOutputDto> UpdateAsync(Guid id, DirectoryDescriptorUpdateInput input)
        {
            var entity = await _repository.GetAsync(x => x.Id == id);
            if (entity == null)
            {
                throw new UserFriendlyException("目录不存在");
            }

            // 检查同级目录下是否重名
            var exists = await _repository.AnyAsync(x =>
                x.ParentId == entity.ParentId && x.Name == input.Name && x.Id != id);
            if (exists)
            {
                throw new UserFriendlyException("同级目录下已存在同名目录");
            }

            entity.Rename(input.Name);
            await _repository.UpdateAsync(entity);

            return entity.Adapt<DirectoryDescriptorGetOutputDto>();
        }

        /// <summary>
        /// 删除目录（含子目录下的文件和子目录）
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            var entity = await _repository.GetAsync(x => x.Id == id);
            if (entity == null)
            {
                throw new UserFriendlyException("目录不存在");
            }

            // 检查是否有子目录
            var hasChildren = await _repository.AnyAsync(x => x.ParentId == id);
            if (hasChildren)
            {
                throw new UserFriendlyException("目录下存在子目录，请先删除子目录");
            }

            // 检查是否有文件
            var hasFiles = await _fileRepository.AnyAsync(x => x.DirectoryId == id);
            if (hasFiles)
            {
                throw new UserFriendlyException("目录下存在文件，请先删除文件");
            }

            await _repository.DeleteAsync(entity);
        }

        /// <summary>
        /// 移动目录
        /// </summary>
        public async Task MoveAsync(Guid id, Guid? newParentId)
        {
            var entity = await _repository.GetAsync(x => x.Id == id);
            if (entity == null)
            {
                throw new UserFriendlyException("目录不存在");
            }

            entity.MoveTo(newParentId);
            await _repository.UpdateAsync(entity);
        }
    }
}
