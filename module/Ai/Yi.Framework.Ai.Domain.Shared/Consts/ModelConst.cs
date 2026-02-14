namespace Yi.Framework.Ai.Domain.Shared.Consts;

public class ModelConst
{
    /// <summary>
    /// 需要移除的模型前缀列表
    /// </summary>
    private static readonly List<string> ModelPrefixesToRemove =
    [
        "yi-",
        "ma-"
    ];
    
    /// <summary>
    /// 获取模型ID的前缀（如果存在）
    /// </summary>
    private static string? GetModelPrefix(string? modelId)
    {
        if (string.IsNullOrEmpty(modelId)) return null;

        return ModelPrefixesToRemove.FirstOrDefault(prefix =>
            modelId!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 移除模型ID的前缀，返回标准模型ID
    /// </summary>
    public static string RemoveModelPrefix(string? modelId)
    {
        if (string.IsNullOrEmpty(modelId)) return string.Empty;

        var prefix = GetModelPrefix(modelId);
        if (prefix != null)
        {
            return modelId[prefix.Length..];
        }
        return modelId;
    }

    /// <summary>
    /// 处理模型ID，如有前缀则移除并返回新字符串
    /// </summary>
    public static string ProcessModelId(string? modelId)
    {
        return RemoveModelPrefix(modelId);
    }
}
