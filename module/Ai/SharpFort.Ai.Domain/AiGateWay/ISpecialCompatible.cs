using SharpFort.Ai.Domain.Shared.Dtos.Anthropic;
using SharpFort.Ai.Domain.Shared.Dtos.OpenAi;

namespace SharpFort.Ai.Domain.AiGateWay;

public interface ISpecialCompatible
{
    public void Compatible(ThorChatCompletionsRequest request);
    public void AnthropicCompatible(AnthropicInput request);
}
