using Yi.Framework.CasbinRbac.Domain.Shared.Enums;

namespace Yi.Framework.CasbinRbac.Domain.Shared.OperLog
{
    [AttributeUsage(AttributeTargets.Method)]
    public class OperLogAttribute : Attribute
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public OperationType OperationType { get; set; }

        /// <summary>
        /// 日志标题（模块）
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 是否保存请求数据
        /// </summary>
        public bool IsSaveRequestData { get; set; } = true;

        /// <summary>
        /// 是否保存返回数据
        /// </summary>
        public bool IsSaveResponseData { get; set; } = true;

        public OperLogAttribute(string title, OperationType operationType)
        {
            Title = title;
            operationType = operationType;
        }
    }
}
