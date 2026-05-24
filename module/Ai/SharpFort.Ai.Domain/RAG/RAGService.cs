namespace SharpFort.Ai.Domain.RAG;

public class RAGService
{
    private readonly IRAGStorageService _storageService;

    public RAGService(IRAGStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<(bool Found, string PromptText, List<DocumentChunkDto> Chunks)> GetSystemPromptAsync(
        string collectionName, float[] queryVector, int topK = 3, double? score = null)
    {
        var documents = await _storageService.SearchAsync(collectionName, queryVector, (ulong)topK, score);

        if (documents.Count == 0)
            return (false, "\n文档信息列表：\n无相关信息", []);

        var context = string.Join("\n\n---\n\n", documents.Select((doc, index) =>
            $"文档 {doc.Title}-{index + 1} (来源: {doc.SourceFile}) (相似度: {doc.Score}):\n{doc.Content}"));

        var userPrompt = $@"文档信息列表：
文档内容：
{context}";

        return (true, "\n" + userPrompt, documents);
    }
}
