using SqlSugar;
using System.Globalization;
using Volo.Abp.Application.Dtos;
using SharpFort.Ddd.Application;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Menu;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Managers;
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
        private readonly ISqlSugarRepository<RoleMenu> _roleMenuRepository;
        private readonly ICasbinPolicyManager _casbinPolicyManager;
        private readonly ISqlSugarRepository<Role, Guid> _roleRepository;

        public MenuService(
            ISqlSugarRepository<Menu, Guid> repository,
            ISqlSugarRepository<RoleMenu> roleMenuRepository,
            ICasbinPolicyManager casbinPolicyManager,
            ISqlSugarRepository<Role, Guid> roleRepository) : base(repository)
        {
            _repository = repository;
            _roleMenuRepository = roleMenuRepository;
            _casbinPolicyManager = casbinPolicyManager;
            _roleRepository = roleRepository;
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

            // 最小权限原则：如果提供了ApiUrl但没有ApiMethod，默认为GET
            if (!string.IsNullOrWhiteSpace(input.ApiUrl) && string.IsNullOrWhiteSpace(input.ApiMethod))
            {
                input.ApiMethod = "GET";
            }

            // 处理 ApiMethod 转大写
            if (!string.IsNullOrEmpty(input.ApiMethod))
            {
                input.ApiMethod = input.ApiMethod.ToUpper(global::System.Globalization.CultureInfo.InvariantCulture);  // CA1304
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
            // 最小权限原则：如果提供了ApiUrl但没有ApiMethod，默认为GET
            if (!string.IsNullOrWhiteSpace(input.ApiUrl) && string.IsNullOrWhiteSpace(input.ApiMethod))
            {
                input.ApiMethod = "GET";
            }

            // 获取旧菜单数据
            var oldMenu = await _repository.GetByIdAsync(id);
            bool isApiChanged = oldMenu != null &&
                (oldMenu.ApiUrl != input.ApiUrl || (oldMenu.ApiMethod?.ToUpper(CultureInfo.InvariantCulture) ?? "") != (input.ApiMethod?.ToUpper(CultureInfo.InvariantCulture) ?? ""));

            // /* 原代码注释保留 */
            // // TODO: 如果菜单的 ApiUrl/ApiMethod 变更，需要同步更新 Casbin 策略
            // // 这涉及到复杂的策略查找与替换，建议后续完善
            // // 现阶段，如果是修改，建议先手动在界面删除再添加，或开发专门的策略同步功能
            
            var result = await base.UpdateAsync(id, input);

            // 如果 API 路由发生了变化，找到所有拥有此菜单的角色，并刷新其 Casbin 权限
            if (isApiChanged)
            {
                var roleIds = await _roleMenuRepository._DbQueryable.Where(x => x.MenuId == id).Select(x => x.RoleId).ToListAsync();
                if (roleIds.Count > 0)  // CA1860: prefer Count > 0
                {
                    var roles = await _roleRepository.GetListAsync(x => roleIds.Contains(x.Id));
                    foreach (var role in roles)
                    {
                        var menuIds = await _roleMenuRepository._DbQueryable.Where(x => x.RoleId == role.Id).Select(x => x.MenuId).ToListAsync();
                        var menus = await _repository.GetListAsync(x => menuIds.Contains(x.Id));
                        await _casbinPolicyManager.SetRolePermissionsAsync(role, menus);
                    }
                }
            }

            return result;
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
                        .OrderBy(x => x.OrderNum)
                        .OrderBy(x => x.CreationTime)
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
        public override async Task DeleteAsync(IEnumerable<Guid> ids)
        {
            // 在物理删除之前，找出这些被删除菜单影响到的角色
            var affectedRoleIds = await _roleMenuRepository._DbQueryable
                .Where(x => ids.Contains(x.MenuId))
                .Select(x => x.RoleId)
                .Distinct()
                .ToListAsync();

            // 原始代码: await base.DeleteAsync(ids);
            await base.DeleteAsync(ids);

            // 物理删除后，刷新受影响角色的 Casbin 权限
            if (affectedRoleIds.Count > 0)  // CA1860: prefer Count > 0
            {
                var roles = await _roleRepository.GetListAsync(x => affectedRoleIds.Contains(x.Id));
                foreach (var role in roles)
                {
                    // 获取删除剩余的有效菜单
                    var menuIds = await _roleMenuRepository._DbQueryable.Where(x => x.RoleId == role.Id).Select(x => x.MenuId).ToListAsync();
                    var menus = await _repository.GetListAsync(x => menuIds.Contains(x.Id));
                    await _casbinPolicyManager.SetRolePermissionsAsync(role, menus);
                }
            }
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
