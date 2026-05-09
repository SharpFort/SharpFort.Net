using System.Net.Http.Json;
using SharpFort.Ai.Domain.Shared.Dtos;
using SharpFort.Ai.Domain.Shared.Dtos.OpenAi.Embeddings;

namespace SharpFort.Ai.Domain.AiGateWay.Impl.ThorSiliconFlow.Embeddings;

public sealed class SiliconFlowTextEmbeddingService(IHttpClientFactory httpClientFactory)
    : ITextEmbeddingService
{
    public async Task<EmbeddingCreateResponse> EmbeddingAsync(
        EmbeddingCreateRequest createEmbeddingModel,
        AiModelDescribe? options = null,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClientFactory.CreateClient().PostJsonAsync(
            options?.Endpoint.TrimEnd('/') + "/v1/embeddings",
            createEmbeddingModel, options!.ApiKey);

        EmbeddingCreateResponse? result =
            await response.Content.ReadFromJsonAsync<EmbeddingCreateResponse>(cancellationToken: cancellationToken);

        return result!;
    }
}
