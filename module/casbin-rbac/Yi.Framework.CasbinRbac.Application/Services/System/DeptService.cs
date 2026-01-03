using SqlSugar;
using Volo.Abp.Application.Dtos;
using Yi.Framework.Ddd.Application;
using Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Dept;
using Yi.Framework.CasbinRbac.Application.Contracts.IServices;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.CasbinRbac.Domain.Repositories;
using Yi.Framework.CasbinRbac.Domain.Shared.Consts;

namespace Yi.Framework.CasbinRbac.Application.Services.System
{
    /// <summary>
    /// Dept服务实现
    /// </summary>
    public class DeptService : YiCrudAppService<Department, DeptGetOutputDto, DeptGetListOutputDto, Guid,
        DeptGetListInputVo, DeptCreateInputVo, DeptUpdateInputVo>, IDeptService
    {
        private IDeptRepository _repository;

        public DeptService(IDeptRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [RemoteService(false)]
        public async Task<List<Guid>> GetChildListAsync(Guid deptId)
        {
            return await _repository.GetChildListAsync(deptId);
        }

        /// <summary>
        /// 通过角色id查询该角色全部部门
        /// </summary>
        /// <returns></returns>
        //[Route("{roleId}")]
        public async Task<List<DeptGetListOutputDto>> GetRoleIdAsync(Guid roleId)
        {
            var entities = await _repository.GetListRoleIdAsync(roleId);
            return await MapToGetListOutputDtosAsync(entities);
        }

        /// <summary>
        /// 多查
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override async Task<PagedResultDto<DeptGetListOutputDto>> GetListAsync(DeptGetListInputVo input)
        {
            RefAsync<int> total = 0;
            var entities = await _repository._DbQueryable
                .WhereIF(!string.IsNullOrEmpty(input.DeptName), u => u.DeptName.Contains(input.DeptName!))
                .WhereIF(input.State is not null, u => u.State == input.State)
                .OrderBy(u => u.OrderNum, OrderByType.Asc)
                .ToListAsync();
            return new PagedResultDto<DeptGetListOutputDto>
            {
                Items = await MapToGetListOutputDtosAsync(entities),
                TotalCount = total
            };
        }

        public override async Task<DeptGetOutputDto> CreateAsync(DeptCreateInputVo input)
        {
            await CheckCreateInputDtoAsync(input);
            var entity = await MapToEntityAsync(input);
            
            // 处理 Ancestors
            string ancestors = Guid.Empty.ToString();
            Guid parentId = input.ParentId ?? Guid.Empty;

            if (parentId != Guid.Empty)
            {
                var parent = await _repository.GetByIdAsync(parentId);
                if (parent != null)
                {
                    ancestors = string.IsNullOrEmpty(parent.Ancestors) 
                        ? parent.Id.ToString() 
                        : $"{parent.Ancestors},{parent.Id}";
                }
            }
            
            // 使用 InitPath 设置受保护的属性
            entity.InitPath(parentId, ancestors);

            await _repository.InsertAsync(entity);
            return await MapToGetOutputDtoAsync(entity);
        }

        protected override async Task CheckCreateInputDtoAsync(DeptCreateInputVo input)
        {
            var isExist =
                await _repository.IsAnyAsync(x => x.DeptCode == input.DeptCode);
            if (isExist)
            {
                throw new UserFriendlyException(DeptConst.Exist);
            }
        }

        protected override async Task CheckUpdateInputDtoAsync(Department entity, DeptUpdateInputVo input)
        {
            var isExist = await _repository._DbQueryable.Where(x => x.Id != entity.Id)
                .AnyAsync(x => x.DeptCode == input.DeptCode);
            if (isExist)
            {
                throw new UserFriendlyException(DeptConst.Exist);
            }
        }
    }
}