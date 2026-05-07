using SharpFort.Ai.Domain.Shared.Dtos.OpenAi;

namespace SharpFort.Ai.Domain.Shared.Dtos;

public class MessageInputDto
{
    public string? Content { get; set; }
    public string Role { get; set; } = null!;
    public string ModelId { get; set; } = null!;
    public string? Remark { get; set; }

    public ThorUsageResponse? TokenUsage { get; set; }
}
