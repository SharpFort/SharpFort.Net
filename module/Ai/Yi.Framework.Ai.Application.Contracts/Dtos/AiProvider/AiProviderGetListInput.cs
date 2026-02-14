using Yi.Framework.Ddd.Application.Contracts;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.AiProvider;

/// <summary>
/// 获取AI供应商列表输入
/// </summary>
public class AiProviderGetListInput : PagedAllResultRequestDto
{
    /// <summary>
    /// 搜索关键词（搜索名称）
    /// </summary>
    public string? SearchKey { get; set; }
}
