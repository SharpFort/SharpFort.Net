using Volo.Abp.Application.Services;

namespace Yi.Framework.Ai.Application.Contracts.IServices;

/// <summary>
/// AI工具服务接口 (翻译, 搜索, 总结)
/// </summary>
public interface IAiToolService : IApplicationService
{
    /// <summary>
    /// 翻译文本
    /// </summary>
    /// <param name="text">原文</param>
    /// <param name="targetLang">目标语言</param>
    /// <param name="modelId">使用的模型ID (可选)</param>
    Task<string> TranslateAsync(string text, string targetLang, string? modelId = null);

    /// <summary>
    /// 总结内容
    /// </summary>
    /// <param name="content">内容</param>
    /// <param name="modelId">使用的模型ID (可选)</param>
    Task<string> SummarizeAsync(string content, string? modelId = null);

    /// <summary>
    /// AI搜索
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="modelId">使用的模型ID (可选)</param>
    Task<string> SearchAsync(string query, string? modelId = null);
}
