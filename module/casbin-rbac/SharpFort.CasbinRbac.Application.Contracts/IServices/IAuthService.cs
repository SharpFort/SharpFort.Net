using Volo.Abp.Application.Dtos;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Account;

namespace SharpFort.CasbinRbac.Application.Contracts.IServices;

public interface IAuthService
{
    Task<AuthOutputDto?> TryGetAuthInfoAsync(string? openId, string authType, Guid? userId = null);
    Task<AuthOutputDto> CreateAsync(AuthCreateOrUpdateInputDto input);
}