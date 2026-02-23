using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Task
{
    public class TaskGetListInput : PagedAllResultRequestDto
    {
        public string? JobId { get; set; }
        public string? GroupName { get; set; }
    }
}
