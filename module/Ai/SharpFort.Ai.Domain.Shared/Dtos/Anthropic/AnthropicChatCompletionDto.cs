// 属性名均使用 [JsonPropertyName] 和 PascalCase，满足 CA1707 指导原则
using System.Text.Json.Serialization;

namespace SharpFort.Ai.Domain.Shared.Dtos.Anthropic;

public class AnthropicStreamDto
{
    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("index")] public int? Index { get; set; }

    [JsonPropertyName("content_block")] public AnthropicChatCompletionDtoContentBlock? ContentBlock { get; set; }

    [JsonPropertyName("delta")] public AnthropicChatCompletionDtoDelta? Delta { get; set; }

    [JsonPropertyName("message")] public AnthropicChatCompletionDto? Message { get; set; }

    [JsonPropertyName("usage")] public AnthropicCompletionDtoUsage? Usage { get; set; }

    [JsonPropertyName("error")] public AnthropicStreamErrorDto? Error { get; set; }

}

public class AnthropicStreamErrorDto
{
    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("message")] public string? Message { get; set; }
}

public class AnthropicChatCompletionDtoDelta
{
    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("text")] public string? Text { get; set; }

    [JsonPropertyName("thinking")] public string? Thinking { get; set; }

    [JsonPropertyName("partial_json")] public string? PartialJson { get; set; }

    [JsonPropertyName("stop_reason")] public string? StopReason { get; set; }

    [JsonPropertyName("signature")] public string? Signature { get; set; }

    [JsonPropertyName("stop_sequence")] public string? StopSequence { get; set; }

}

public class AnthropicChatCompletionDtoContentBlock
{
    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("thinking")] public string? Thinking { get; set; }

    [JsonPropertyName("signature")] public string? Signature { get; set; }

    [JsonPropertyName("id")] public string? Id { get; set; }

    [JsonPropertyName("name")] public string? Name { get; set; }

    [JsonPropertyName("input")] public object? Input { get; set; }

    [JsonPropertyName("server_name")] public string? ServerName { get; set; }

    [JsonPropertyName("is_error")] public bool? IsError { get; set; }

    [JsonPropertyName("tool_use_id")] public string? ToolUseId { get; set; }

    [JsonPropertyName("content")] public object? Content { get; set; }

    [JsonPropertyName("text")] public string? Text { get; set; }
}

public class AnthropicChatCompletionDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = null!;

    [JsonPropertyName("type")] public string Type { get; set; } = null!;

    [JsonPropertyName("role")] public string Role { get; set; } = null!;

    [JsonPropertyName("content")] public AnthropicChatCompletionDtoContent[] Content { get; set; } = null!;

    [JsonPropertyName("model")] public string Model { get; set; } = null!;

    [JsonPropertyName("stop_reason")] public string StopReason { get; set; } = null!;

    [JsonPropertyName("stop_sequence")] public object StopSequence { get; set; } = null!;

    public AnthropicCompletionDtoUsage? Usage { get; set; }

}

public class AnthropicChatCompletionDtoContent
{
    [JsonPropertyName("type")] public string Type { get; set; } = null!;

    [JsonPropertyName("text")] public string? Text { get; set; }

    [JsonPropertyName("id")] public string? Id { get; set; }

    [JsonPropertyName("name")] public string? Name { get; set; }

    [JsonPropertyName("input")] public object? Input { get; set; }

    [JsonPropertyName("thinking")] public string? Thinking { get; set; }

    [JsonPropertyName("partial_json")] public string? PartialJson { get; set; }

    [JsonPropertyName("signature")] public string? Signature { get; set; }

}

public class AnthropicCompletionDtoUsage
{
    [JsonPropertyName("input_tokens")] public int? InputTokens { get; set; }

    [JsonPropertyName("cache_creation_input_tokens")]
    public int? CacheCreationInputTokens { get; set; }

    [JsonPropertyName("cache_read_input_tokens")]
    public int? CacheReadInputTokens { get; set; }

    [JsonPropertyName("output_tokens")] public int? OutputTokens { get; set; }

    [JsonPropertyName("server_tool_use")] public AnthropicServerToolUse? ServerToolUse { get; set; }

    [JsonPropertyName("cache_creation")] public object? CacheCreation { get; set; }

    [JsonPropertyName("service_tier")] public string? ServiceTier { get; set; }


}

public class AnthropicServerToolUse
{
    [JsonPropertyName("web_search_requests")]
    public int? WebSearchRequests { get; set; }
}
