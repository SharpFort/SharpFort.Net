using SharpFort.CasbinRbac.Domain.Shared.Enums;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Task;

public class TaskUpdateInput
{
    public required string AssemblyName { get; set; }
    public required string JobType { get; set; }
    public required string JobId { get; set; }
    public string? GroupName { get; set; }
    public JobType Type { get; set; }
    public string? Cron { get; set; }
    public int? Millisecond { get; set; }
    public bool Concurrent { get; set; }
    // public Dictionary<string, object>? Properties { get; set; }
    public string? Description { get; set; }
}
