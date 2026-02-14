using Yi.Framework.Ai.Domain.Shared.Dtos.Anthropic;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;

namespace Yi.Framework.Ai.Domain.AiGateWay;

public interface ISpecialCompatible
{
    public void Compatible(ThorChatCompletionsRequest request);
    public void AnthropicCompatible(AnthropicInput request);
}
