using SharpFort.Ddd.Application.Contracts;
using SharpFort.TenantManagement.Application.Contracts.Dtos;

namespace SharpFort.TenantManagement.Application.Contracts
{
    public interface ITenantService:ISfCrudAppService< TenantGetOutputDto, TenantGetListOutputDto, Guid, TenantGetListInput, TenantCreateInput, TenantUpdateInput>
    {
    }
}
