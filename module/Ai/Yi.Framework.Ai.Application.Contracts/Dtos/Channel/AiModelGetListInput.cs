using Yi.Framework.Ddd.Application.Contracts;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.Channel;

/// <summary>
/// 获取AI模型列表输入
/// </summary>
public class AiModelGetListInput : PagedAllResultRequestDto
{
    /// <summary>
    /// 搜索关键词(搜索模型名称、模型ID)
    /// </summary>
    public string? SearchKey { get; set; }

    /// <summary>
    /// AI应用ID筛选
    /// </summary>
    public Guid? AiAppId { get; set; }

}
