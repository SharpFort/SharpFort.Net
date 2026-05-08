using Volo.Abp.Application.Dtos;

namespace SharpFort.TenantManagement.Application.Contracts.Dtos
{
    public class TenantSelectOutputDto : EntityDto<Guid>
    {
        public required string Name { get; set; }
    }
}
