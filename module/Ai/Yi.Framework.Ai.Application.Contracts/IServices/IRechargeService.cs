using Volo.Abp.Application.Dtos;
using Yi.Framework.Ai.Application.Contracts.Dtos.Recharge;

namespace Yi.Framework.Ai.Application.Contracts.IServices;

public interface IRechargeService
{
    /// <summary>
    /// 查询已登录的账户充值记录（分页）
    /// </summary>
    Task<PagedResultDto<RechargeGetListOutput>> GetListByAccountAsync(RechargeGetListInput input);

    /// <summary>
    /// 移除用户vip及角色
    /// </summary>
    Task RemoveVipRoleByExpireAsync();

    /// <summary>
    /// 给用户充值VIP
    /// </summary>
    /// <param name="input">充值输入参数</param>
    /// <returns></returns>
    Task RechargeVipAsync(RechargeCreateInput input);
}
