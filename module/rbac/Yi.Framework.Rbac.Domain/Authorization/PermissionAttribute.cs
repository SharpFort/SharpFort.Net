namespace Yi.Framework.Rbac.Domain.Authorization
{
    /// <summary>
    /// 旧版权限属性标记 - 已废弃
    /// 请使用 Casbin 中间件进行权限控制，无需在方法上添加此属性
    /// </summary>
    [Obsolete("此属性已废弃，系统已切换到 Casbin 中间件进行权限控制。请勿在新代码中使用，现有代码将在后续版本中移除。", false)]
    [AttributeUsage(AttributeTargets.Method)]
    public class PermissionAttribute : Attribute
    {
        internal string Code { get; set; }

        public PermissionAttribute(string code)
        {
            Code = code;
        }
    }
}
