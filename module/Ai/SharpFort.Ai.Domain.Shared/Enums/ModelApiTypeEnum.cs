using System.ComponentModel;

namespace SharpFort.Ai.Domain.Shared.Enums;

public enum ModelApiTypeEnum
{
    [Description("OpenAi Completions")]
    Completions,

    [Description("Claude Messages")]
    Messages,
    
    [Description("OpenAi Responses")]
    Responses,
    
    [Description("Gemini GenerateContent")]
    GenerateContent
}
