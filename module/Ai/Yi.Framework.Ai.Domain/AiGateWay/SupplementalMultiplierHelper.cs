using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;

namespace Yi.Framework.Ai.Domain.AiGateWay;

public static class SupplementalMultiplierHelper
{
    public static void SetSupplementalMultiplier(this ThorUsageResponse? usage,decimal multiplier)
    {
        if (usage is not null)
        {
            usage.InputTokens =
                (int)Math.Round((usage.InputTokens ?? 0) * multiplier);
            usage.OutputTokens =
                (int)Math.Round((usage.OutputTokens ?? 0) * multiplier);
            usage.CompletionTokens =
                (int)Math.Round((usage.CompletionTokens ?? 0) * multiplier);
            usage.PromptTokens =
                (int)Math.Round((usage.PromptTokens ?? 0) * multiplier);
            usage.TotalTokens =
                (int)Math.Round((usage.TotalTokens ?? 0) * multiplier);
        }
    }
}
