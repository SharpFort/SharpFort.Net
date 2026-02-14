using System.ClientModel;
using System.Collections.Concurrent;
using Azure.AI.OpenAI;
using Yi.Framework.Ai.Domain.Shared.Dtos;

namespace Yi.Framework.Ai.Domain.AiGateWay.Impl.ThorAzureOpenAI;

public static class AzureOpenAIFactory
{
    private const string AddressTemplate = "{0}/openai/deployments/{1}/chat/completions?api-version={2}";
    private const string EditImageAddressTemplate = "{0}/openai/deployments/{1}/images/edits?api-version={2}";
    private const string AudioSpeechTemplate = "{0}/openai/deployments/{1}/audio/speech?api-version={2}";

    private const string AudioTranscriptions =
        "{0}/openai/deployments/{1}/audio/transcriptions?api-version={2}";

    private static readonly ConcurrentDictionary<string, AzureOpenAIClient> Clients = new();

    public static string GetAudioTranscriptionsAddress(AiModelDescribe options, string model)
    {
        if (string.IsNullOrEmpty(options.AppExtraUrl))
        {
            options.AppExtraUrl = "2025-03-01-preview";
        }

        return string.Format(AudioTranscriptions, options.Endpoint.TrimEnd('/'), model, options.AppExtraUrl);
    }

    public static string GetAudioSpeechAddress(AiModelDescribe options, string model)
    {
        if (string.IsNullOrEmpty(options.AppExtraUrl))
        {
            options.AppExtraUrl = "2025-03-01-preview";
        }

        return string.Format(AudioSpeechTemplate, options.Endpoint.TrimEnd('/'), model, options.AppExtraUrl);
    }

    public static string GetAddress(AiModelDescribe options, string model)
    {
        if (string.IsNullOrEmpty(options.AppExtraUrl))
        {
            options.AppExtraUrl = "2025-03-01-preview";
        }

        return string.Format(AddressTemplate, options.Endpoint.TrimEnd('/'), model, options.AppExtraUrl);
    }

    public static string GetEditImageAddress(AiModelDescribe options, string model)
    {
        if (string.IsNullOrEmpty(options.AppExtraUrl))
        {
            options.AppExtraUrl = "2025-03-01-preview";
        }

        return string.Format(EditImageAddressTemplate, options.Endpoint.TrimEnd('/'), model, options.AppExtraUrl);
    }

    public static AzureOpenAIClient CreateClient(AiModelDescribe options)
    {
        return Clients.GetOrAdd($"{options.ApiKey}_{options.Endpoint}_{options.AppExtraUrl}", (_) =>
        {
            const AzureOpenAIClientOptions.ServiceVersion version = AzureOpenAIClientOptions.ServiceVersion.V2024_06_01;

            var client = new AzureOpenAIClient(new Uri(options.Endpoint), new ApiKeyCredential(options.ApiKey),
                new AzureOpenAIClientOptions(version));

            return client;
        });
    }
}
