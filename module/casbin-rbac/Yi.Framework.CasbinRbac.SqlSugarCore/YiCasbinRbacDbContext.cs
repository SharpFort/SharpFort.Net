using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;
using Yi.Framework.CasbinRbac.Domain.Authorization;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.CasbinRbac.Domain.Extensions;
using Yi.Framework.CasbinRbac.Domain.Shared.Consts;
using Yi.Framework.CasbinRbac.Domain.Shared.Enums;
using Yi.Framework.SqlSugarCore;

namespace Yi.Framework.CasbinRbac.SqlSugarCore
{
    public class YiCasbinRbacDbContext : SqlSugarDbContext
    {
        public YiCasbinRbacDbContext(IAbpLazyServiceProvider lazyServiceProvider) : base(lazyServiceProvider)
        {
        }

        protected IDataFilter DataFilter => LazyServiceProvider.LazyGetRequiredService<IDataFilter>();
        protected ICurrentUser CurrentUser => LazyServiceProvider.GetRequiredService<ICurrentUser>();

        protected override void CustomDataFilter(ISqlSugarClient sqlSugarClient)
        {
            if (DataFilter.IsEnabled<IDataPermission>())
            {
                DataPermissionFilter(sqlSugarClient);
            }
        }

        /// <summary>
        /// 数据权限过滤器
        /// </summary>
        protected void DataPermissionFilter(ISqlSugarClient sqlSugarClient)
        {
            if (CurrentUser.Id == null) return;

            // 管理员跳过过滤
            if (CurrentUser.UserName == UserConst.Admin || CurrentUser.Roles.Contains(UserConst.AdminRolesCode)) return;

            var roleInfo = CurrentUser.GetRoleInfo();
            var expUser = Expressionable.Create<User>();
            
            // 如果无角色信息，默认只能看自己
            if (roleInfo == null || !roleInfo.Any())
            {
                expUser.Or(u => u.Id == CurrentUser.Id);
                sqlSugarClient.QueryFilter.AddTableFilter(expUser.ToExpression());
                return;
            }

            // 检查是否有 "全部数据权限" (只要有一个角色拥有 ALL 权限，就跳过所有过滤)
            if (roleInfo.Any(r => r.DataScope == DataScope.ALL))
            {
                return; 
            }

            // 拼接 OR 条件 (取并集)
            foreach (var role in roleInfo)
            {
                switch (role.DataScope)
                {
                    case DataScope.CUSTOM:
                        // 自定义：查询 RoleDepartment 表
                        expUser.Or(u => SqlFunc.Subqueryable<RoleDepartment>()
                            .Where(rd => rd.RoleId == role.Id && rd.DepartmentId == u.DepartmentId)
                            .Any());
                        break;

                    case DataScope.DEPT:
                        // 本部门
                        expUser.Or(u => u.DepartmentId == CurrentUser.GetDepartmentId());
                        break;

                    case DataScope.DEPT_FOLLOW:
                        // 本部门及以下
                        var currentDeptId = CurrentUser.GetDepartmentId();
                        if (currentDeptId != null)
                        {
                            // 优化：利用 Ancestors 字段进行子查询
                            // 这里的 Contains 对于 Guid 来说是相对安全的
                            string currentDeptIdStr = currentDeptId.ToString();
                            expUser.Or(u => SqlFunc.Subqueryable<Department>()
                                .Where(d => d.Id == u.DepartmentId && 
                                           (d.Id == currentDeptId || d.Ancestors.Contains(currentDeptIdStr)))
                                .Any());
                        }
                        break;

                    case DataScope.SELF:
                        // 仅本人
                        expUser.Or(u => u.Id == CurrentUser.Id);
                        break;
                }
            }

            sqlSugarClient.QueryFilter.AddTableFilter(expUser.ToExpression());
            
            // 针对 Role 表的过滤 (只能看自己拥有的角色)
            var expRole = Expressionable.Create<Role>();
            var roleIds = roleInfo.Select(x => x.Id).ToList();
            expRole.Or(r => roleIds.Contains(r.Id));
            sqlSugarClient.QueryFilter.AddTableFilter(expRole.ToExpression());
        }
    }
}
