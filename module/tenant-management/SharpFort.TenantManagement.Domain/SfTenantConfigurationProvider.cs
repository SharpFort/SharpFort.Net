// IDE0130: ABP 框架覆盖, 必须使用 Volo.Abp 命名空间才能被 ABP DI 扫描替换 (ReplaceServices=true)
#pragma warning disable IDE0130
using Microsoft.Extensions.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy.Localization;

namespace Volo.Abp.MultiTenancy;

[Dependency(ReplaceServices = true)]
public class SfTenantConfigurationProvider(
    ITenantResolver tenantResolver,
    ITenantStore tenantStore,
    ITenantResolveResultAccessor tenantResolveResultAccessor,
    IStringLocalizer<AbpMultiTenancyResource> stringLocalizer) : ITenantConfigurationProvider, ITransientDependency
{
    protected virtual ITenantResolver TenantResolver { get; } = tenantResolver;
    protected virtual ITenantStore TenantStore { get; } = tenantStore;
    protected virtual ITenantResolveResultAccessor TenantResolveResultAccessor { get; } = tenantResolveResultAccessor;
    protected virtual IStringLocalizer<AbpMultiTenancyResource> StringLocalizer { get; } = stringLocalizer;

    public virtual async Task<TenantConfiguration?> GetAsync(bool saveResolveResult = false)
    {
        //租户解析器获取到当前解析成功的租户
        var resolveResult = await TenantResolver.ResolveTenantIdOrNameAsync();

        if (saveResolveResult)
        {
            TenantResolveResultAccessor.Result = resolveResult;
        }

        TenantConfiguration? tenant = null;
        if (resolveResult.TenantIdOrName != null)
        {
            //根据租户信息获取租户
            tenant = await FindTenantAsync(resolveResult.TenantIdOrName) ?? throw new BusinessException(
                    code: "Volo.AbpIo.MultiTenancy:010001",
                    message: StringLocalizer["TenantNotFoundMessage"],
                    details: StringLocalizer["TenantNotFoundDetails", resolveResult.TenantIdOrName]
                );
            if (!tenant.IsActive)
            {
                throw new BusinessException(
                    code: "Volo.AbpIo.MultiTenancy:010002",
                    message: StringLocalizer["TenantNotActiveMessage"],
                    details: StringLocalizer["TenantNotActiveDetails", resolveResult.TenantIdOrName]
                );
            }
        }

        return tenant;
    }

    protected virtual async Task<TenantConfiguration?> FindTenantAsync(string tenantIdOrName)
    {
        return Guid.TryParse(tenantIdOrName, out var parsedTenantId)
            ? await TenantStore.FindAsync(parsedTenantId)
            : await TenantStore.FindAsync(tenantIdOrName);
    }
}
