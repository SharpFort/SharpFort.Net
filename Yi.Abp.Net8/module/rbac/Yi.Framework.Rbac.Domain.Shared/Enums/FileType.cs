using System.ComponentModel;

namespace Yi.Framework.Rbac.Domain.Shared.Enums
{
    /// <summary>
    /// 文件类型枚举
    /// </summary>
    public enum FileType
    {
        /// <summary>
        /// 其他/未知
        /// </summary>
        [Description("其他")]
        OTHER = 0,

        /// <summary>
        /// 图片
        /// </summary>
        [Description("图片")]
        IMAGE = 1,

        /// <summary>
        /// 视频
        /// </summary>
        [Description("视频")]
        VIDEO = 2,

        /// <summary>
        /// 音频
        /// </summary>
        [Description("音频")]
        AUDIO = 3,

        /// <summary>
        /// 文档 (txt, doc, pdf)
        /// </summary>
        [Description("文档")]
        DOCUMENT = 4
    }
}