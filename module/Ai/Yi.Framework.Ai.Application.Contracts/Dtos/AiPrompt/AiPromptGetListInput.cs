using Yi.Framework.Ddd.Application.Contracts;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.AiPrompt;

public class AiPromptGetListInput : PagedAllResultRequestDto
{
    /// <summary>
    /// 搜索编码或描述
    /// </summary>
    public string? SearchKey { get; set; }
}
