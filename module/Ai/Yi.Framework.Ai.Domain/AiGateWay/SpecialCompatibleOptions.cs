using Yi.Framework.Ai.Domain.Shared.Dtos.Anthropic;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;

namespace Yi.Framework.Ai.Domain.AiGateWay;

public class SpecialCompatibleOptions
{
    public List<Action<ThorChatCompletionsRequest>> Handles { get; set; } = new();
    public List<Action<AnthropicInput>> AnthropicHandles { get; set; } = new();
}
