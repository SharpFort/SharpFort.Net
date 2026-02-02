using SqlSugar;
using Volo.Abp.Application.Dtos;
using Yi.Framework.Ddd.Application;
using Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Menu;
using Yi.Framework.CasbinRbac.Application.Contracts.IServices;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.CasbinRbac.Domain.Shared.Consts;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.CasbinRbac.Application.Services.System
{
    /// <summary>
    /// Menu服务实现
    /// </summary>
    public class MenuService : YiCrudAppService<Menu, MenuGetOutputDto, MenuGetListOutputDto, Guid, MenuGetListInputVo, MenuCreateInputVo, MenuUpdateInputVo>,
       IMenuService
    {
        private readonly ISqlSugarRepository<Menu, Guid> _repository;
        public MenuService(ISqlSugarRepository<Menu, Guid> repository) : base(repository)
        {
            _repository = repository;
        }

        public override async Task<MenuGetOutputDto> CreateAsync(MenuCreateInputVo input)
        {
            // 防止前端传入重复ID导致唯一约束报错
            input.Id = Guid.NewGuid();

            // 处理 ApiMethod 转大写
            if (!string.IsNullOrEmpty(input.ApiMethod))
            {
                input.ApiMethod = input.ApiMethod.ToUpper();
            }
            return await base.CreateAsync(input);
        }

        public override async Task<MenuGetOutputDto> UpdateAsync(Guid id, MenuUpdateInputVo input)
        {
            // TODO: 如果菜单的 ApiUrl/ApiMethod 变更，需要同步更新 Casbin 策略
            // 这涉及到复杂的策略查找与替换，建议后续完善
            // 现阶段，如果是修改，建议先手动在界面删除再添加，或开发专门的策略同步功能
            return await base.UpdateAsync(id, input);
        }

        public override async Task<PagedResultDto<MenuGetListOutputDto>> GetListAsync(MenuGetListInputVo input)
        {
            RefAsync<int> total = 0;
            var entities = await _repository._DbQueryable.WhereIF(!string.IsNullOrEmpty(input.MenuName), x => x.MenuName.Contains(input.MenuName!))
                        .WhereIF(input.State is not null, x => x.State == input.State)
                        .Where(x=>x.MenuSource==input.MenuSource)
                        .OrderByDescending(x => x.OrderNum)
                        .ToListAsync();
            return new PagedResultDto<MenuGetListOutputDto>(total, await MapToGetListOutputDtosAsync(entities));
        }

        /// <summary>
        /// 查询当前角色的菜单
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public async Task<List<MenuGetListOutputDto>> GetListRoleIdAsync(Guid roleId)
        {
            var entities = await _repository._DbQueryable.Where(m => SqlFunc.Subqueryable<RoleMenu>().Where(rm => rm.RoleId == roleId && rm.MenuId == m.Id).Any()).ToListAsync();

            return await MapToGetListOutputDtosAsync(entities);
        }
    }
}
