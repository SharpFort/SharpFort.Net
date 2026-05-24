namespace SharpFort.Ai.Domain.RAG;

/// <summary>
/// 文档块模型，用于向量数据库存储
/// </summary>
public class DocumentChunkDto
{
    public long Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public float[] ContentVector { get; set; } = [];
    public string SourceFile { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public double? Score { get; set; }
}
