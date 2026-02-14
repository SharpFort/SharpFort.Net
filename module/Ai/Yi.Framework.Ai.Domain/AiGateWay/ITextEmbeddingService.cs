using Yi.Framework.Ai.Domain.Shared.Dtos;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi.Embeddings;

namespace Yi.Framework.Ai.Domain.AiGateWay;

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
