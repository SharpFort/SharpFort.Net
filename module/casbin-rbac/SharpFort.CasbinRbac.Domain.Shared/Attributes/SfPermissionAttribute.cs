using System;

namespace SharpFort.CasbinRbac.Domain.Shared.Attributes
{
    /// <summary>
    /// Sf 权限标识特性
    /// 用于标记 API 接口对应的稳定权限代码 (Permission Code)，例如 "user:list", "role:create"。
    /// 解决 URL 变更导致权限失效的问题。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SfPermissionAttribute : Attribute
    {
        /// <summary>
        /// 权限代码 (e.g. "user:list")
        /// </summary>
        public string Code { get; }

        public SfPermissionAttribute(string code)
        {
            Code = code;
        }
    }
}
