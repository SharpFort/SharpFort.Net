namespace Yi.Framework.FileManagement.Domain.Shared.Enums
{
    /// <summary>
    /// 文件类型分类
    /// </summary>
    public enum FileType
    {
        /// <summary>
        /// 普通文件
        /// </summary>
        File = 0,

        /// <summary>
        /// 图片
        /// </summary>
        Image = 1,

        /// <summary>
        /// 视频
        /// </summary>
        Video = 2,

        /// <summary>
        /// 音频
        /// </summary>
        Audio = 3,

        /// <summary>
        /// 文档 (PDF, Word, Excel 等)
        /// </summary>
        Document = 4,

        /// <summary>
        /// 压缩包
        /// </summary>
        Archive = 5
    }
}
