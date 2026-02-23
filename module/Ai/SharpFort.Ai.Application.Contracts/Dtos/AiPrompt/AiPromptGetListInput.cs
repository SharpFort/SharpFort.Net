using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.Ai.Application.Contracts.Dtos.AiPrompt;

public class AiPromptGetListInput : PagedAllResultRequestDto
{
    /// <summary>
    /// 搜索编码或描述
    /// </summary>
    public string? SearchKey { get; set; }
}
