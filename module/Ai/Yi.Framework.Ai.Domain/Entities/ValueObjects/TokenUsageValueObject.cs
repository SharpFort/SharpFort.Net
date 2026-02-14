namespace Yi.Framework.Ai.Domain.Entities.ValueObjects;

public class TokenUsageValueObject
{
    public long OutputTokenCount { get; set; }

    public long InputTokenCount { get; set; }

    public long TotalTokenCount { get; set; }
}
