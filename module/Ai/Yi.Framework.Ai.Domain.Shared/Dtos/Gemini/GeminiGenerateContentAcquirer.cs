using System.Text.Json;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;
using Yi.Framework.Ai.Domain.Shared.Extensions;

namespace Yi.Framework.Ai.Domain.Shared.Dtos.Gemini;

public static class GeminiGenerateContentAcquirer
{
    /// <summary>
    /// 从请求体中提取用户最后一条消息内容
    /// 路径: contents[last].parts[last].text
    /// </summary>
    public static string GetLastUserContent(JsonElement request)
    {
        var contents = request.GetPath("contents");
        if (!contents.HasValue || contents.Value.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var contentsArray = contents.Value.EnumerateArray().ToList();
        if (contentsArray.Count == 0)
        {
            return string.Empty;
        }

        var lastContent = contentsArray[^1];
        var parts = lastContent.GetPath("parts");
        if (!parts.HasValue || parts.Value.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var partsArray = parts.Value.EnumerateArray().ToList();
        if (partsArray.Count == 0)
        {
            return string.Empty;
        }

        // 获取最后一个 part 的 text
        var lastPart = partsArray[^1];
        return lastPart.GetPath("text").GetString() ?? string.Empty;
    }

    /// <summary>
    /// 从响应中提取文本内容（非 thought 类型）
    /// 路径: candidates[0].content.parts[].text (where thought != true)
    /// </summary>
    public static string GetTextContent(JsonElement response)
    {
        var candidates = response.GetPath("candidates");
        if (!candidates.HasValue || candidates.Value.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var candidatesArray = candidates.Value.EnumerateArray().ToList();
        if (candidatesArray.Count == 0)
        {
            return string.Empty;
        }

        var parts = candidatesArray[0].GetPath("content", "parts");
        if (!parts.HasValue || parts.Value.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        // 遍历所有 parts，只取非 thought 的 text
        foreach (var part in parts.Value.EnumerateArray())
        {
            var isThought = part.GetPath("thought").GetBool();
            if (!isThought)
            {
                var text = part.GetPath("text").GetString();
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }
            }
        }

        return string.Empty;
    }

    public static ThorUsageResponse? GetUsage(JsonElement response)
    {
        var usage = response.GetPath("usageMetadata");
        if (!usage.HasValue)
        {
            return null;
        }

        var inputTokens = usage.Value.GetPath("promptTokenCount").GetInt();
        var outputTokens = usage.Value.GetPath("candidatesTokenCount").GetInt()
                           + usage.Value.GetPath("cachedContentTokenCount").GetInt()
                           + usage.Value.GetPath("thoughtsTokenCount").GetInt()
                           + usage.Value.GetPath("toolUsePromptTokenCount").GetInt();

        return new ThorUsageResponse
        {
            PromptTokens = inputTokens,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            CompletionTokens = outputTokens,
            TotalTokens = inputTokens + outputTokens,
        };
    }

    /// <summary>
    /// 获取图片 base64（包含 data:image 前缀）
    /// Step 1: 递归遍历整个 JSON 查找最后一个 base64
    /// Step 2: 从 text 中查找 markdown 图片
    /// </summary>
    public static string GetImagePrefixBase64(JsonElement response)
    {
        // Step 1: 递归遍历整个 JSON 查找最后一个 base64
        string? lastBase64 = null;
        string? lastMimeType = null;
        CollectLastBase64(response, ref lastBase64, ref lastMimeType);

        if (!string.IsNullOrEmpty(lastBase64))
        {
            var mimeType = lastMimeType ?? "image/png";
            return $"data:{mimeType};base64,{lastBase64}";
        }

        // Step 2: 从 text 中查找 markdown 图片
        return FindMarkdownImageInResponse(response);
    }

    /// <summary>
    /// 递归遍历 JSON 查找最后一个 base64
    /// </summary>
    private static void CollectLastBase64(JsonElement element, ref string? lastBase64, ref string? lastMimeType, int minLength = 100)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                string? currentMimeType = null;
                string? currentData = null;

                foreach (var prop in element.EnumerateObject())
                {
                    var name = prop.Name.ToLowerInvariant();

                    // 记录 mimeType / mime_type
                    if (name is "mimetype" or "mime_type" && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        currentMimeType = prop.Value.GetString();
                    }
                    // 记录 data 字段（检查是否像 base64）
                    else if (name == "data" && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var val = prop.Value.GetString();
                        if (!string.IsNullOrEmpty(val) && val.Length >= minLength && LooksLikeBase64(val))
                        {
                            currentData = val;
                        }
                    }
                    else
                    {
                        // 递归处理其他属性
                        CollectLastBase64(prop.Value, ref lastBase64, ref lastMimeType, minLength);
                    }
                }

                // 如果当前对象有 data，更新为"最后一个"
                if (currentData != null)
                {
                    lastBase64 = currentData;
                    lastMimeType = currentMimeType;
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectLastBase64(item, ref lastBase64, ref lastMimeType, minLength);
                }
                break;
        }
    }

    /// <summary>
    /// 检查字符串是否像 base64
    /// </summary>
    private static bool LooksLikeBase64(string str)
    {
        // 常见图片 base64 开头: JPEG(/9j/), PNG(iVBOR), GIF(R0lGO), WebP(UklGR)
        if (str.StartsWith("/9j/") || str.StartsWith("iVBOR") ||
            str.StartsWith("R0lGO") || str.StartsWith("UklGR"))
        {
            return true;
        }

        // 检查前100个字符是否都是 base64 合法字符
        return str.Take(100).All(c => char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '=');
    }

    /// <summary>
    /// 递归查找 text 字段中的 markdown 图片
    /// </summary>
    private static string FindMarkdownImageInResponse(JsonElement element)
    {
        var allTexts = new List<string>();
        CollectTextFields(element, allTexts);

        // 从最后一个 text 开始查找
        for (int i = allTexts.Count - 1; i >= 0; i--)
        {
            var text = allTexts[i];

            // markdown 图片格式: ![image](data:image/png;base64,xxx)
            var startMarker = "(data:image/";
            var startIndex = text.IndexOf(startMarker, StringComparison.Ordinal);
            if (startIndex < 0)
            {
                continue;
            }

            startIndex += 1; // 跳过 "("
            var endIndex = text.IndexOf(')', startIndex);
            if (endIndex > startIndex)
            {
                return text.Substring(startIndex, endIndex - startIndex);
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// 递归收集所有 text 字段
    /// </summary>
    private static void CollectTextFields(JsonElement element, List<string> texts)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.Name == "text" && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var val = prop.Value.GetString();
                        if (!string.IsNullOrEmpty(val))
                        {
                            texts.Add(val);
                        }
                    }
                    else
                    {
                        CollectTextFields(prop.Value, texts);
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectTextFields(item, texts);
                }
                break;
        }
    }
}
