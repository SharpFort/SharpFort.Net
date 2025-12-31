using Volo.Abp.Application.Dtos;
using Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Account;

namespace Yi.Framework.CasbinRbac.Application.Contracts.IServices;

public interface IAuthService
{
    Task<AuthOutputDto?> TryGetAuthInfoAsync(string? openId, string authType, Guid? userId = null);
    Task<AuthOutputDto> CreateAsync(AuthCreateOrUpdateInputDto input);
}