namespace Yi.Framework.Ai.Application.Contracts.Dtos.Chat;

/// <summary>
/// 图片生成输入
/// </summary>
public class ImageGenerationInput
{
    /// <summary>
    /// 密钥id
    /// </summary>
    public Guid? TokenId { get; set; }

    /// <summary>
    /// 提示词
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// 参考图PrefixBase64列表（可选，包含前缀如 data:image/png;base64,...）
    /// </summary>
    public List<string>? ReferenceImagesPrefixBase64 { get; set; }
}
