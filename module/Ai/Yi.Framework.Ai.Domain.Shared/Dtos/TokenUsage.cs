namespace Yi.Framework.Ai.Domain.Shared.Dtos;

public class TokenUsage
{
    public int OutputTokenCount { get; set; }

    public int InputTokenCount { get; set; }

    public int TotalTokenCount { get; set; }
}
