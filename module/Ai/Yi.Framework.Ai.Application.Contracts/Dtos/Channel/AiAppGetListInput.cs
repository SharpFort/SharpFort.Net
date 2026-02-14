using Yi.Framework.Ddd.Application.Contracts;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.Channel;

/// <summary>
/// 获取AI应用列表输入
/// </summary>
public class AiAppGetListInput : PagedAllResultRequestDto
{
    /// <summary>
    /// 搜索关键词（搜索应用名称）
    /// </summary>
    public string? SearchKey { get; set; }
}
