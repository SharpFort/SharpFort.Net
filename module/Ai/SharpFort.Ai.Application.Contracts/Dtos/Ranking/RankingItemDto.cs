namespace SharpFort.Ai.Application.Contracts.Dtos.Ranking;

public class RankingItemDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string? Avatar { get; set; }
    public int Rank { get; set; }
    public long Value { get; set; }
    public string FormattedValue { get; set; } = null!;
}
