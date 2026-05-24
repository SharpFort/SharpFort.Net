namespace SharpFort.Ai.Domain.RAG;

public interface IEmbeddingService : IDisposable
{
    Task<float[]> GetEmbeddingAsync(string text);
}
