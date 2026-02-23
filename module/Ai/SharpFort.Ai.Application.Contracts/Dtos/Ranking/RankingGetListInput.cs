using SharpFort.Ai.Domain.Shared.Enums;

namespace SharpFort.Ai.Application.Contracts.Dtos.Ranking;

public class RankingGetListInput
{
    public RankingTypeEnum RankingType { get; set; }
    public int Top { get; set; } = 10;
}
