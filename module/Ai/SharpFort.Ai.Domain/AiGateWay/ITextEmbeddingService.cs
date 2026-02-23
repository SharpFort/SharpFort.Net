using SharpFort.Ai.Domain.Shared.Dtos;
using SharpFort.Ai.Domain.Shared.Dtos.OpenAi.Embeddings;

namespace SharpFort.Ai.Domain.AiGateWay;

public interface ITextEmbeddingService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="createEmbeddingModel"></param>
    /// <param name="aiModelDescribe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<EmbeddingCreateResponse> EmbeddingAsync(
        EmbeddingCreateRequest createEmbeddingModel,
        AiModelDescribe? aiModelDescribe = null,
        CancellationToken cancellationToken = default);
}
