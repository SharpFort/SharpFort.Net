using Microsoft.AspNetCore.Authorization;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Yi.Framework.Ddd.Application;
using Yi.Framework.FileManagement.Application.Contracts.Dtos.FileStorageProvider;
using Yi.Framework.FileManagement.Application.Contracts.IServices;
using Yi.Framework.FileManagement.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.FileManagement.Application.Services
{
    /// <summary>
    /// 文件存储提供者服务
    /// 继承 YiCrudAppService 标准 CRUD 模式（参考 DeptService、RoleService）
    /// </summary>
    [Authorize]
    public class FileStorageProviderService : YiCrudAppService<
        FileStorageProvider,
        FileStorageProviderGetOutputDto,
        FileStorageProviderGetListOutputDto,
        Guid,
        FileStorageProviderGetListInput,
        FileStorageProviderCreateInput,
        FileStorageProviderUpdateInput>,
        IFileStorageProviderService
    {
        private readonly ISqlSugarRepository<FileStorageProvider, Guid> _repository;

        public FileStorageProviderService(ISqlSugarRepository<FileStorageProvider, Guid> repository)
            : base(repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 获取存储提供者列表（支持筛选）
        /// </summary>
        public override async Task<PagedResultDto<FileStorageProviderGetListOutputDto>> GetListAsync(FileStorageProviderGetListInput input)
        {
            RefAsync<int> total = 0;
            var entities = await _repository._DbQueryable
                .WhereIF(!string.IsNullOrEmpty(input.Name), x => x.Name.Contains(input.Name!))
                .WhereIF(input.ProviderType.HasValue, x => x.ProviderType == input.ProviderType)
                .WhereIF(input.IsEnabled.HasValue, x => x.IsEnabled == input.IsEnabled)
                .OrderBy(x => x.OrderNum, OrderByType.Asc)
                .ToPageListAsync(input.SkipCount + 1, input.MaxResultCount, total);

            return new PagedResultDto<FileStorageProviderGetListOutputDto>
            {
                Items = await MapToGetListOutputDtosAsync(entities),
                TotalCount = total
            };
        }

        /// <summary>
        /// 设为默认提供者
        /// </summary>
        public async Task SetDefaultAsync(Guid id)
        {
            var entity = await _repository.GetAsync(x => x.Id == id);
            if (entity == null)
            {
                throw new UserFriendlyException("存储提供者不存在");
            }

            // 取消其他默认
            var currentDefaults = await _repository.GetListAsync(x => x.IsDefault);
            foreach (var item in currentDefaults)
            {
                item.SetDefault(false);
            }
            await _repository.UpdateManyAsync(currentDefaults);

            // 设置新默认
            entity.SetDefault(true);
            await _repository.UpdateAsync(entity);
        }
    }
}
