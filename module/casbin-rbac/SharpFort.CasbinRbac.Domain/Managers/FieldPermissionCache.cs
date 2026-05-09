using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.Domain.Managers
{
    [ExposeServices(typeof(IFieldPermissionCache))]
    public class FieldPermissionCache : IFieldPermissionCache, ISingletonDependency
    {
        private readonly IServiceScopeFactory _scopeFactory;

        // Key: RoleId
        // Value: Map<ResourceName, Set<FieldName>>
        private ConcurrentDictionary<Guid, Dictionary<string, HashSet<string>>> _cache
            = new();

        // Key: RoleCode (Case-insensitive), Value: RoleId
        private ConcurrentDictionary<string, Guid> _roleCodeMap
            = new(StringComparer.OrdinalIgnoreCase);

        public FieldPermissionCache(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _ = RefreshCacheAsync();
        }

        public async Task RefreshCacheAsync()
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ISqlSugarRepository<RoleField> repo = scope.ServiceProvider.GetRequiredService<ISqlSugarRepository<RoleField>>();
            ISqlSugarRepository<Role> roleRepo = scope.ServiceProvider.GetRequiredService<ISqlSugarRepository<Role>>();

            // 1. Load Rules
            List<RoleField> allRules = await repo.GetListAsync();
            ConcurrentDictionary<Guid, Dictionary<string, HashSet<string>>> newCache = new ConcurrentDictionary<Guid, Dictionary<string, HashSet<string>>>();

            IEnumerable<IGrouping<Guid, RoleField>> grouped = allRules.GroupBy(x => x.RoleId);
            foreach (IGrouping<Guid, RoleField> group in grouped)
            {
                Guid roleId = group.Key;
                Dictionary<string, HashSet<string>> resourceMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                foreach (RoleField? rule in group)
                {
                    if (!resourceMap.TryGetValue(rule.TableName, out HashSet<string>? fieldSet))
                    {
                        fieldSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        resourceMap[rule.TableName] = fieldSet;
                    }
                    fieldSet.Add(rule.FieldName);
                }
                newCache[roleId] = resourceMap;
            }

            // 2. Load Roles for Code Mapping
            List<Role> allRoles = await roleRepo.GetListAsync();
            ConcurrentDictionary<string, Guid> newRoleMap = new ConcurrentDictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (Role role in allRoles)
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
            HashSet<string> denyList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (roleIds == null || !roleIds.Any())
            {
                return denyList;
            }

            foreach (Guid roleId in roleIds)
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
            HashSet<string> denyList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (roleCodes == null || !roleCodes.Any())
            {
                return denyList;
            }

            foreach (string code in roleCodes)
            {
                if (_roleCodeMap.TryGetValue(code, out Guid roleId))
                {
                    MergeDenyFields(denyList, roleId, resourceName);
                }
            }
            return denyList;
        }

        private void MergeDenyFields(HashSet<string> denyList, Guid roleId, string resourceName)
        {
            if (_cache.TryGetValue(roleId, out Dictionary<string, HashSet<string>>? resourceMap))
            {
                if (resourceMap.TryGetValue(resourceName, out HashSet<string>? fields))
                {
                    foreach (string f in fields)
                    {
                        denyList.Add(f);
                    }
                }
            }
        }
    }
}
