using SqlSugar;
using Volo.Abp.Application.Dtos;
using SharpFort.Ddd.Application;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Post;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Shared.Consts;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.Application.Services.System
{
    /// <summary>
    /// Post服务实现
    /// </summary>
    public class PostService(ISqlSugarRepository<Position, Guid> repository) : SfCrudAppService<Position, PostGetOutputDto, PostGetListOutputDto, Guid,
            PostGetListInputVo, PostCreateInputVo, PostUpdateInputVo>(repository),
        IPostService
    {
        private readonly ISqlSugarRepository<Position, Guid> _repository = repository;

        public override async Task<PagedResultDto<PostGetListOutputDto>> GetListAsync(PostGetListInputVo input)
        {
            RefAsync<int> total = 0;

            List<Position> entities = await _repository._DbQueryable.WhereIF(!string.IsNullOrEmpty(input.PostName),
                    x => x.PostName.Contains(input.PostName!))
                .WhereIF(input.State is not null, x => x.State == input.State)
                .OrderByDescending(x => x.OrderNum)
                .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
            return new PagedResultDto<PostGetListOutputDto>(total, await MapToGetListOutputDtosAsync(entities));
        }

        protected override async Task CheckCreateInputDtoAsync(PostCreateInputVo input)
        {
            bool isExist =
                await _repository.IsAnyAsync(x => x.PostCode == input.PostCode);
            if (isExist)
            {
                throw new UserFriendlyException(PostConst.Exist);
            }
        }

        protected override async Task CheckUpdateInputDtoAsync(Position entity, PostUpdateInputVo input)
        {
            bool isExist = await _repository._DbQueryable.Where(x => x.Id != entity.Id)
                .AnyAsync(x => x.PostCode == input.PostCode);
            if (isExist)
            {
                throw new UserFriendlyException(RoleConst.Exist);
            }
        }
    }
}