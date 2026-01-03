using System;

namespace Yi.Framework.CasbinRbac.Domain.Shared.Attributes
{
    /// <summary>
    /// 安全资源标记
    /// 标记在 DTO 或 Entity 上，用于指示该类型需要进行字段级权限控制。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SecureResourceAttribute : Attribute
    {
        /// <summary>
        /// 资源名称 (对应 RoleField 中的 TableName)
        /// </summary>
        public string ResourceName { get; }

        public SecureResourceAttribute(string resourceName)
        {
            ResourceName = resourceName;
        }
    }
}
