using SharpFort.Ai.Domain.Shared.Dtos.Anthropic;
using SharpFort.Ai.Domain.Shared.Dtos.OpenAi;

namespace SharpFort.Ai.Domain.AiGateWay;

public interface ISpecialCompatible
{
    void Compatible(ThorChatCompletionsRequest request);
    void AnthropicCompatible(AnthropicInput request);
}
