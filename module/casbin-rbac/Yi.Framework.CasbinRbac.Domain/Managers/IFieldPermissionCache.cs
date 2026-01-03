namespace Yi.Framework.CasbinRbac.Domain.Managers
{
    /// <summary>
    /// 字段权限缓存服务接口
    /// 提供高性能的字段黑名单查询
    /// </summary>
    public interface IFieldPermissionCache
    {
        /// <summary>
        /// 获取指定角色对指定资源的禁止字段列表
        /// </summary>
        /// <param name="roleIds">角色ID列表</param>
        /// <param name="resourceName">资源名 (TableName)</param>
        /// <returns>禁止访问的字段集合 (Case-insensitive)</returns>
        HashSet<string> GetDenyFields(IEnumerable<Guid> roleIds, string resourceName);
        
        /// <summary>
        /// 根据角色代码获取禁止字段 (给 CurrentUser 使用)
        /// </summary>
        HashSet<string> GetDenyFieldsByCodes(IEnumerable<string> roleCodes, string resourceName);

        /// <summary>
        /// 刷新缓存 (通常在 RoleField 变更后调用)
        /// </summary>
        Task RefreshCacheAsync();
    }
}
