using System.ComponentModel;

namespace SharpFort.Ai.Domain.Shared.Enums;

/// <summary>
/// 知识库文档导入状态
/// </summary>
public enum ImportKmsStatus
{
    /// <summary>
    /// 导入中
    /// </summary>
    [Description("导入中")]
    Loading,

    /// <summary>
    /// 导入成功
    /// </summary>
    [Description("导入成功")]
    Success,

    /// <summary>
    /// 导入失败
    /// </summary>
    [Description("导入失败")]
    Fail
}
