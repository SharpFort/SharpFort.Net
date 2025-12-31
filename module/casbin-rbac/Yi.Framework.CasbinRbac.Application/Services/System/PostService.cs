using SqlSugar;
using Volo.Abp.Application.Dtos;
using Yi.Framework.Ddd.Application;
using Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Post;
using Yi.Framework.CasbinRbac.Application.Contracts.IServices;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.CasbinRbac.Domain.Shared.Consts;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.CasbinRbac.Application.Services.System
{
    /// <summary>
    /// Post服务实现
    /// </summary>
    public class PostService : YiCrudAppService<Position, PostGetOutputDto, PostGetListOutputDto, Guid,
            PostGetListInputVo, PostCreateInputVo, PostUpdateInputVo>,
        IPostService
    {
        private readonly ISqlSugarRepository<Position, Guid> _repository;

        public PostService(ISqlSugarRepository<Position, Guid> repository) : base(repository)
        {
            _repository = repository;
        }

        public override async Task<PagedResultDto<PostGetListOutputDto>> GetListAsync(PostGetListInputVo input)
        {
            RefAsync<int> total = 0;

            var entities = await _repository._DbQueryable.WhereIF(!string.IsNullOrEmpty(input.PostName),
                    x => x.PostName.Contains(input.PostName!))
                .WhereIF(input.State is not null, x => x.State == input.State)
                .OrderByDescending(x => x.OrderNum)
                .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
            return new PagedResultDto<PostGetListOutputDto>(total, await MapToGetListOutputDtosAsync(entities));
        }

        protected override async Task CheckCreateInputDtoAsync(PostCreateInputVo input)
        {
            var isExist =
                await _repository.IsAnyAsync(x => x.PostCode == input.PostCode);
            if (isExist)
            {
                throw new UserFriendlyException(PostConst.Exist);
            }
        }

        protected override async Task CheckUpdateInputDtoAsync(Position entity, PostUpdateInputVo input)
        {
            var isExist = await _repository._DbQueryable.Where(x => x.Id != entity.Id)
                .AnyAsync(x => x.PostCode == input.PostCode);
            if (isExist)
            {
                throw new UserFriendlyException(RoleConst.Exist);
            }
        }
    }
}