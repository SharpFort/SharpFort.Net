// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Casbin;
// using Microsoft.Extensions.Logging;
// using Volo.Abp.Domain.Services;
// using Yi.Framework.CasbinRbac.Domain.Entities;
// using Yi.Framework.SqlSugarCore.Abstractions;

// namespace Yi.Framework.CasbinRbac.Domain.Managers
// {
//     /// <summary>
//     /// Casbin Data Migration Service
//     /// Migrates existing RBAC data (Role-Menu-Permission) to CasbinRule table
//     /// </summary>
//     public class CasbinSeedService : DomainService
//     {
//         private readonly IEnforcer _enforcer;
//         private readonly ISqlSugarRepository<Role> _roleRepo;
//         private readonly ISqlSugarRepository<Menu> _menuRepo;
//         private readonly ISqlSugarRepository<RoleMenu> _roleMenuRepo;
//         private readonly ISqlSugarRepository<UserRole> _userRoleRepo;
//         private readonly ILogger<CasbinSeedService> _logger;

//         public CasbinSeedService(
//             IEnforcer enforcer,
//             ISqlSugarRepository<Role> roleRepo,
//             ISqlSugarRepository<Menu> menuRepo,
//             ISqlSugarRepository<RoleMenu> roleMenuRepo,
//             ISqlSugarRepository<UserRole> userRoleRepo,
//             ILogger<CasbinSeedService> logger)
//         {
//             _enforcer = enforcer;
//             _roleRepo = roleRepo;
//             _menuRepo = menuRepo;
//             _roleMenuRepo = roleMenuRepo;
//             _userRoleRepo = userRoleRepo;
//             _logger = logger;
//         }

//         /// <summary>
//         /// Perform Full Migration
//         /// </summary>
//         public async Task MigrateAllAsync()
//         {
//             _logger.LogInformation("Starting Casbin Data Migration...");

//             // 1. Clear existing rules? (Optional, be careful in production)
//             // For safety, we might assume empty or additive. 
//             // Or remove all p and g policies first.
//             // await _enforcer.RemoveFilteredPolicyAsync(0, ""); // Remove all p
//             // await _enforcer.RemoveFilteredGroupingPolicyAsync(0, ""); // Remove all g

//             // 2. Migrate Role Permissions (p policies)
//             await MigrateRolePermissionsAsync();

//             // 3. Migrate User Roles (g policies)
//             await MigrateUserRolesAsync();

//             // 4. Save
//             await _enforcer.SavePolicyAsync();
            
//             _logger.LogInformation("Casbin Data Migration Completed.");
//         }

//         private async Task MigrateRolePermissionsAsync()
//         {
//             var roleMenus = await _roleMenuRepo.GetListAsync();
//             var menus = await _menuRepo.GetListAsync();
            
//             // Dictionary for faster lookup
//             var menuDic = new Dictionary<Guid, Menu>();
//             foreach (var m in menus) menuDic[m.Id] = m;

//             var policies = new List<string[]>();
//             string domain = "default"; // Default domain

//             foreach (var rm in roleMenus)
//             {
//                 if (menuDic.TryGetValue(rm.MenuId, out var menu))
//                 {
//                     // Only migrate menus that have API info (Url as Path)
//                     // Assuming Url field contains path, e.g. "/api/user"
//                     // And we need a method. If not present, default to GET or ALL (represented by *)
                    
//                     if (!string.IsNullOrEmpty(menu.ApiUrl) || !string.IsNullOrEmpty(menu.Router)) // Support both for now
//                     {
//                         var path = !string.IsNullOrEmpty(menu.ApiUrl) ? menu.ApiUrl : menu.Router;
//                         var method = !string.IsNullOrEmpty(menu.ApiMethod) ? menu.ApiMethod : "GET"; // Default GET if unknown
                        
//                         // Ignore frontend routes if they don't look like APIs?
//                         // Simple heuristic: starts with /api
//                         // if (!path.StartsWith("/api")) continue; 
                        
//                         // Plan V1.2: p = sub, dom, obj, act
//                         // sub = roleId
//                         policies.Add(new[] { rm.RoleId.ToString(), domain, path, method });
//                     }
//                 }
//             }

//             if (policies.Count > 0)
//             {
//                 await _enforcer.AddPoliciesAsync(policies);
//                 _logger.LogInformation($"Migrated {policies.Count} role-permission policies.");
//             }
//         }

//         private async Task MigrateUserRolesAsync()
//         {
//             var userRoles = await _userRoleRepo.GetListAsync();
//             var policies = new List<string[]>();
//             string domain = "default";

//             foreach (var ur in userRoles)
//             {
//                 // Plan V1.2: g = user, role, domain
//                 // user = userId
//                 // role = roleId
//                 policies.Add(new[] { ur.UserId.ToString(), ur.RoleId.ToString(), domain });
//             }

//             if (policies.Count > 0)
//             {
//                 await _enforcer.AddGroupingPoliciesAsync(policies);
//                 _logger.LogInformation($"Migrated {policies.Count} user-role policies.");
//             }
//         }
//     }
// }
