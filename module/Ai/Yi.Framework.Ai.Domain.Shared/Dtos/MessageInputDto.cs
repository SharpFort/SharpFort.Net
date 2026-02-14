using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;

namespace Yi.Framework.Ai.Domain.Shared.Dtos;

public class MessageInputDto
{
    public string? Content { get; set; }
    public string Role { get; set; }
    public string ModelId { get; set; }
    public string? Remark { get; set; }

    public ThorUsageResponse? TokenUsage { get; set; }
}
