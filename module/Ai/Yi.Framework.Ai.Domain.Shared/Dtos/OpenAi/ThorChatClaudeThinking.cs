using System.Text.Json.Serialization;

namespace Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;

public class ThorChatClaudeThinking
{
    [JsonPropertyName("type")]
    public string? Type { get; set; } 
    
    [JsonPropertyName("budget_tokens")]
    public int? BudgetToken { get; set; }
}
