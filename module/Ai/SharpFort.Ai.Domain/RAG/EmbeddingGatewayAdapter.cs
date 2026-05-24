using System.Text.Json;
using SharpFort.Ai.Domain.AiGateWay;
using SharpFort.Ai.Domain.Shared.Dtos;
using SharpFort.Ai.Domain.Shared.Dtos.OpenAi.Embeddings;

namespace SharpFort.Ai.Domain.RAG;

/// <summary>
/// 将SharpFort现有Embedding Gateway适配为RAG使用的IEmbeddingService
/// </summary>
public class EmbeddingGatewayAdapter : IEmbeddingService
{
    private readonly ITextEmbeddingService _embeddingService;
    private readonly string _modelId;
    private readonly AiModelDescribe? _modelDescribe;

    public EmbeddingGatewayAdapter(
        ITextEmbeddingService embeddingService,
        string modelId,
        AiModelDescribe? modelDescribe = null)
    {
        _embeddingService = embeddingService;
        _modelId = modelId;
        _modelDescribe = modelDescribe;
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var request = new EmbeddingCreateRequest
        {
            Input = text,
            Model = _modelId
        };

        var response = await _embeddingService.EmbeddingAsync(request, _modelDescribe);

        if (response.Data.Count == 0)
            throw new InvalidOperationException("Embedding returned no data");

        return response.Data[0].Embedding switch
        {
            float[] floats => floats,
            IList<double> doubles => doubles.Select(d => (float)d).ToArray(),
            JsonElement json when json.ValueKind == JsonValueKind.Array =>
                json.EnumerateArray().Select(e => (float)e.GetDouble()).ToArray(),
            _ => throw new InvalidOperationException($"Unknown embedding type: {response.Data[0].Embedding?.GetType()}")
        };
    }

    public void Dispose() { }
}
