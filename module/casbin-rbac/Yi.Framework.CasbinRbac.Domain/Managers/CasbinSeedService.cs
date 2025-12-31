using Volo.Abp.Domain.Services;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.CasbinRbac.Domain.Managers
{
    /// <summary>
    /// Casbin 权限种子数据初始化服务
    /// </summary>
    public class CasbinSeedService : DomainService
    {
        private readonly ICasbinPolicyManager _casbinManager;
        private readonly ISqlSugarRepository<Role> _roleRepo;

        public CasbinSeedService(ICasbinPolicyManager casbinManager, ISqlSugarRepository<Role> roleRepo)
        {
            _casbinManager = casbinManager;
            _roleRepo = roleRepo;
        }

        /// <summary>
        /// 初始化 Casbin 基础策略
        /// </summary>
        public async Task SeedAsync()
        {
            // 1. 初始化超级管理员权限 (admin)
            // 假设 admin 角色的 Code 为 "admin"
            var adminRole = await _roleRepo.GetFirstAsync(r => r.RoleCode == "admin");
            if (adminRole != null)
            {
                await _casbinManager.InitAdminPermissionAsync(adminRole);
            }
            
            // 这里还可以初始化其他默认角色的权限...
        }
    }
}
