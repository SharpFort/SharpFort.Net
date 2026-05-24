using System.Text.RegularExpressions;

namespace SharpFort.Ai.Domain.RAG;

/// <summary>
/// 文档处理服务，负责清洗和分块
/// </summary>
public class DocumentProcessor
{
    private readonly int _chunkSize;
    private readonly int _chunkOverlap;

    public DocumentProcessor(int chunkSize = 500, int chunkOverlap = 50)
    {
        _chunkSize = chunkSize;
        _chunkOverlap = chunkOverlap;
    }

    /// <summary>
    /// 清洗文档内容
    /// </summary>
    public string CleanDocument(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;
        content = content.Replace("\r\n", "\n").Replace("\r", "\n");
        content = Regex.Replace(content, @"[ \t]+", " ");
        content = Regex.Replace(content, @"\n{3,}", "\n\n");
        content = content.Trim();
        return content;
    }

    /// <summary>
    /// 按段落分块
    /// </summary>
    public List<string> ChunkByParagraph(string content)
    {
        var chunks = new List<string>();
        var paragraphs = content.Split(["\n\n"], StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new List<string>();
        var currentLength = 0;

        foreach (var paragraph in paragraphs)
        {
            var paragraphLength = paragraph.Length;
            if (currentLength + paragraphLength > _chunkSize && currentChunk.Count > 0)
            {
                chunks.Add(string.Join("\n\n", currentChunk));
                if (_chunkOverlap > 0 && currentChunk.Count > 0)
                {
                    currentChunk = [currentChunk[^1]];
                    currentLength = currentChunk[0].Length;
                }
                else
                {
                    currentChunk.Clear();
                    currentLength = 0;
                }
            }
            currentChunk.Add(paragraph);
            currentLength += paragraphLength;
        }

        if (currentChunk.Count > 0)
            chunks.Add(string.Join("\n\n", currentChunk));

        return chunks;
    }

    /// <summary>
    /// 按固定大小分块
    /// </summary>
    public List<string> ChunkBySize(string content)
    {
        var chunks = new List<string>();
        var start = 0;
        while (start < content.Length)
        {
            var length = Math.Min(_chunkSize, content.Length - start);
            if (start + length < content.Length)
            {
                var lastPeriod = content.LastIndexOfAny(['.', '!', '?'], start + length, length);
                if (lastPeriod > start)
                    length = lastPeriod - start + 1;
            }
            chunks.Add(content.Substring(start, length).Trim());
            start += length - _chunkOverlap;
        }
        return chunks;
    }
}
