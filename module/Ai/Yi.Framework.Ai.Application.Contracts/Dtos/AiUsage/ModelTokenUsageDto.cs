namespace Yi.Framework.Ai.Application.Contracts.Dtos.AiUsage;

public class ModelTokenUsageDto
{
    public string Model { get; set; }
    public long Tokens { get; set; }
    public decimal Percentage { get; set; }
}
