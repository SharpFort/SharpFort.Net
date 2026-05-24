namespace SharpFort.Ai.Domain.RAG;

public interface IRAGStorageService : IDisposable
{
    Task<bool> AddDataAsync(string collectionName, List<DocumentChunkDto> data, ulong embeddingValueSize = 2048);
    Task<List<DocumentChunkDto>> SearchAsync(string collectionName, float[] queryVector, ulong limit = 10, double? score = null);
}
