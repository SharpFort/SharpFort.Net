namespace SharpFort.Core.Options;

public class SemanticKernelOptions
{
    public List<string> ModelIds { get; set; } = [];
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}