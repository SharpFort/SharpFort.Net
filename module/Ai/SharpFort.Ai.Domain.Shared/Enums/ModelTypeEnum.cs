using System.ComponentModel;

namespace SharpFort.Ai.Domain.Shared.Enums;

public enum ModelTypeEnum
{
    [Description("聊天")]
    Chat = 0,

    [Description("图片")]
    Image = 1,

    [Description("嵌入")]
    Embedding = 2
}
