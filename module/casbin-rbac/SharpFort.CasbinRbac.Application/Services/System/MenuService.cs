using SqlSugar;
using Volo.Abp.Application.Dtos;
using SharpFort.Ddd.Application;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Menu;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Shared.Consts;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.Application.Services.System
{
    /// <summary>
    /// Menu服务实现
    /// </summary>
    public class MenuService : SfCrudAppService<Menu, MenuGetOutputDto, MenuGetListOutputDto, Guid, MenuGetListInputVo, MenuCreateInputVo, MenuUpdateInputVo>,
       IMenuService
    {
        private readonly ISqlSugarRepository<Menu, Guid> _repository;
        public MenuService(ISqlSugarRepository<Menu, Guid> repository) : base(repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 新增菜单
        /// </summary>
        /// <param name="input">菜单创建信息</param>
        /// <returns>创建后的菜单信息</returns>

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

        /// <summary>
        /// 修改菜单
        /// </summary>
        /// <param name="id">菜单ID</param>
        /// <param name="input">菜单更新信息</param>
        /// <returns>更新后的菜单信息</returns>

        public override async Task<MenuGetOutputDto> UpdateAsync(Guid id, MenuUpdateInputVo input)
        {
            // TODO: 如果菜单的 ApiUrl/ApiMethod 变更，需要同步更新 Casbin 策略
            // 这涉及到复杂的策略查找与替换，建议后续完善
            // 现阶段，如果是修改，建议先手动在界面删除再添加，或开发专门的策略同步功能
            return await base.UpdateAsync(id, input);
        }

        /// <summary>
        /// 分页查询菜单列表
        /// </summary>
        /// <param name="input">查询条件</param>
        /// <returns>菜单分页列表数据</returns>

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

        /// <summary>
        /// 获取单个菜单详情
        /// </summary>
        /// <param name="id">菜单ID</param>
        /// <returns>菜单详情数据</returns>
        public override Task<MenuGetOutputDto> GetAsync(Guid id)
        {
            return base.GetAsync(id);
        }

        /// <summary>
        /// 批量删除菜单
        /// </summary>
        /// <param name="ids">菜单ID集合</param>
        public override Task DeleteAsync(IEnumerable<Guid> ids)
        {
            return base.DeleteAsync(ids);
        }

        /// <summary>
        /// 获取菜单动态下拉框列表
        /// </summary>
        /// <param name="keywords">查询关键字</param>
        /// <returns>菜单下拉框数据列表</returns>
        public override Task<PagedResultDto<MenuGetListOutputDto>> GetSelectDataListAsync(string? keywords = null)
        {
            return base.GetSelectDataListAsync(keywords);
        }

        /// <summary>
        /// 导出菜单Excel
        /// </summary>
        /// <param name="input">查询条件</param>
        /// <returns>Excel文件流</returns>
        public override Task<Microsoft.AspNetCore.Mvc.IActionResult> GetExportExcelAsync(MenuGetListInputVo input)
        {
            return base.GetExportExcelAsync(input);
        }

        /// <summary>
        /// 导入菜单Excel
        /// </summary>
        /// <param name="input">菜单列表数据</param>
        public override Task PostImportExcelAsync(List<MenuCreateInputVo> input)
        {
            return base.PostImportExcelAsync(input);
        }
    }
}
