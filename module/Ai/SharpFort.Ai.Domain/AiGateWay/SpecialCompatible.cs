using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using SharpFort.Ai.Domain.Shared.Dtos.Anthropic;
using SharpFort.Ai.Domain.Shared.Dtos.OpenAi;

namespace SharpFort.Ai.Domain.AiGateWay;

public class SpecialCompatible : ISpecialCompatible,ISingletonDependency
{
    private readonly IOptions<SpecialCompatibleOptions> _options;

    public SpecialCompatible(IOptions<SpecialCompatibleOptions> options)
    {
        _options = options;
    }
    
    public void Compatible(ThorChatCompletionsRequest request)
    {
        foreach (var handle in _options.Value.Handles)
        {
            handle(request);
        }
    }

    public void AnthropicCompatible(AnthropicInput request)
    {
        foreach (var handle in _options.Value.AnthropicHandles)
        {
            handle(request);
        }
    }
}
