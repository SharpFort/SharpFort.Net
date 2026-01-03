using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.CasbinRbac.Domain.Managers
{
    [ExposeServices(typeof(IFieldPermissionCache))]
    public class FieldPermissionCache : IFieldPermissionCache, ISingletonDependency
    {
        private readonly IServiceScopeFactory _scopeFactory;
        
        // Key: RoleId
        // Value: Map<ResourceName, Set<FieldName>>
        private ConcurrentDictionary<Guid, Dictionary<string, HashSet<string>>> _cache 
            = new ConcurrentDictionary<Guid, Dictionary<string, HashSet<string>>>();
            
        // Key: RoleCode (Case-insensitive), Value: RoleId
        private ConcurrentDictionary<string, Guid> _roleCodeMap 
            = new ConcurrentDictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        public FieldPermissionCache(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _ = RefreshCacheAsync();
        }

        public async Task RefreshCacheAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ISqlSugarRepository<RoleField>>();
            var roleRepo = scope.ServiceProvider.GetRequiredService<ISqlSugarRepository<Role>>();
            
            // 1. Load Rules
            var allRules = await repo.GetListAsync();
            var newCache = new ConcurrentDictionary<Guid, Dictionary<string, HashSet<string>>>();

            var grouped = allRules.GroupBy(x => x.RoleId);
            foreach (var group in grouped)
            {
                var roleId = group.Key;
                var resourceMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                foreach (var rule in group)
                {
                    if (!resourceMap.ContainsKey(rule.TableName))
                    {
                        resourceMap[rule.TableName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    }
                    resourceMap[rule.TableName].Add(rule.FieldName);
                }
                newCache[roleId] = resourceMap;
            }

            // 2. Load Roles for Code Mapping
            var allRoles = await roleRepo.GetListAsync();
            var newRoleMap = new ConcurrentDictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var role in allRoles)
            {
                if (!string.IsNullOrEmpty(role.RoleCode))
                {
                    newRoleMap[role.RoleCode] = role.Id;
                }
            }

            // Atomic Replace
            _cache = newCache;
            _roleCodeMap = newRoleMap;
        }

        public HashSet<string> GetDenyFields(IEnumerable<Guid> roleIds, string resourceName)
        {
            var denyList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (roleIds == null || !roleIds.Any()) return denyList;

            foreach (var roleId in roleIds)
            {
                MergeDenyFields(denyList, roleId, resourceName);
            }
            return denyList;
        }
        
        /// <summary>
        /// 根据角色代码获取禁止字段 (给 CurrentUser 使用)
        /// </summary>
        public HashSet<string> GetDenyFieldsByCodes(IEnumerable<string> roleCodes, string resourceName)
        {
            var denyList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (roleCodes == null || !roleCodes.Any()) return denyList;
            
            foreach (var code in roleCodes)
            {
                if (_roleCodeMap.TryGetValue(code, out var roleId))
                {
                    MergeDenyFields(denyList, roleId, resourceName);
                }
            }
            return denyList;
        }

        private void MergeDenyFields(HashSet<string> denyList, Guid roleId, string resourceName)
        {
             if (_cache.TryGetValue(roleId, out var resourceMap))
            {
                if (resourceMap.TryGetValue(resourceName, out var fields))
                {
                    foreach (var f in fields)
                    {
                        denyList.Add(f);
                    }
                }
            }
        }
    }
}
