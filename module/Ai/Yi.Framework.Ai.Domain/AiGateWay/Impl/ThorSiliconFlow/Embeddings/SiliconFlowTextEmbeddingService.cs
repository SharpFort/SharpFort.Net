using System.Net.Http.Json;
using Yi.Framework.Ai.Domain.Shared.Dtos;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi.Embeddings;

namespace Yi.Framework.Ai.Domain.AiGateWay.Impl.ThorSiliconFlow.Embeddings;

public sealed class SiliconFlowTextEmbeddingService(IHttpClientFactory httpClientFactory)
    : ITextEmbeddingService
{
    public async Task<EmbeddingCreateResponse> EmbeddingAsync(
        EmbeddingCreateRequest createEmbeddingModel,
        AiModelDescribe? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClientFactory.CreateClient().PostJsonAsync(
            options?.Endpoint.TrimEnd('/') + "/v1/embeddings",
            createEmbeddingModel, options!.ApiKey);

        var result =
            await response.Content.ReadFromJsonAsync<EmbeddingCreateResponse>(cancellationToken: cancellationToken);

        return result;
    }
}
