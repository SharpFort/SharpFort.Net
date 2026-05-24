using System.Text;
using SqlSugar;

namespace SharpFort.Ai.Domain.RAG;

/// <summary>
/// 基于 PostgreSQL pgvector 扩展的向量存储服务
/// </summary>
public class PgVectorStorageService : IRAGStorageService
{
    private readonly ISqlSugarClient _db;

    public PgVectorStorageService(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<bool> AddDataAsync(string collectionName, List<DocumentChunkDto> data, ulong embeddingValueSize = 2048)
    {
        var tableName = SanitizeTableName(collectionName);

        await EnsureTableExistsAsync(tableName, embeddingValueSize);

        foreach (var chunk in data)
        {
            var vectorStr = FormatVector(chunk.ContentVector);
            await _db.Ado.ExecuteCommandAsync(
                $"INSERT INTO \"{tableName}\" (id, content, source_file, title, category, chunk_index, created_at, content_vector) " +
                $"VALUES (@id, @content, @sourceFile, @title, @category, @chunkIndex, @createdAt, {vectorStr}) " +
                $"ON CONFLICT (id) DO UPDATE SET content=@content, source_file=@sourceFile, content_vector={vectorStr}",
                new
                {
                    id = chunk.Id,
                    content = chunk.Content,
                    sourceFile = chunk.SourceFile,
                    title = chunk.Title,
                    category = chunk.Category,
                    chunkIndex = chunk.ChunkIndex,
                    createdAt = chunk.CreatedAt
                });
        }

        return true;
    }

    public async Task<List<DocumentChunkDto>> SearchAsync(string collectionName, float[] queryVector, ulong limit = 10, double? score = null)
    {
        var tableName = SanitizeTableName(collectionName);
        var vectorStr = FormatVector(queryVector);
        var scoreCondition = score.HasValue ? $"AND 1 - (content_vector <=> {vectorStr}) >= {score.Value}" : "";

        var sql = $@"SELECT id, content, source_file, title, category, chunk_index, created_at,
                      1 - (content_vector <=> {vectorStr}) AS score
                      FROM ""{tableName}""
                      WHERE content_vector IS NOT NULL {scoreCondition}
                      ORDER BY content_vector <=> {vectorStr}
                      LIMIT {limit}";

        var rows = await _db.Ado.SqlQueryAsync<dynamic>(sql);

        return rows.Select(row => new DocumentChunkDto
        {
            Id = (long)row.id,
            Content = (string)row.content,
            SourceFile = (string)row.source_file,
            Title = (string)row.title,
            Category = (string)row.category,
            ChunkIndex = (int)row.chunk_index,
            CreatedAt = (DateTimeOffset)row.created_at,
            Score = (double)row.score
        }).ToList();
    }

    private async Task EnsureTableExistsAsync(string tableName, ulong dimensions)
    {
        var exists = await _db.Ado.GetDataTableAsync(
            $"SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = '{tableName}')");

        if (exists.Rows[0][0] is bool b && !b)
        {
            var sql = $@"CREATE TABLE ""{tableName}"" (
                id BIGINT PRIMARY KEY,
                content TEXT,
                source_file TEXT,
                title TEXT,
                category TEXT,
                chunk_index INTEGER,
                created_at TIMESTAMPTZ,
                content_vector vector({dimensions})
            );
            CREATE INDEX IF NOT EXISTS idx_{tableName}_vector ON ""{tableName}"" USING ivfflat (content_vector vector_cosine_ops);";
            await _db.Ado.ExecuteCommandAsync(sql);
        }
    }

    private static string FormatVector(float[] vector)
    {
        var sb = new StringBuilder("'[");
        for (int i = 0; i < vector.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(vector[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        sb.Append("]'::vector");
        return sb.ToString();
    }

    private static string SanitizeTableName(string name)
    {
        // Only allow alphanumeric, underscore, and hyphen
        return new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
    }

    public void Dispose() { }
}
