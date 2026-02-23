using SharpFort.Ai.Domain.Shared.Dtos.Anthropic;
using SharpFort.Ai.Domain.Shared.Dtos.OpenAi;

namespace SharpFort.Ai.Domain.AiGateWay;

public class SpecialCompatibleOptions
{
    public List<Action<ThorChatCompletionsRequest>> Handles { get; set; } = new();
    public List<Action<AnthropicInput>> AnthropicHandles { get; set; } = new();
}
