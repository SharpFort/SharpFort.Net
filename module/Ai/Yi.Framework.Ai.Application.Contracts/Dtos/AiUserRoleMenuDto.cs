using Volo.Abp.Application.Dtos;
using Yi.Framework.Rbac.Domain.Shared.Dtos;

namespace Yi.Framework.Ai.Application.Contracts.Dtos;

public class AiUserRoleMenuDto : UserRoleMenuDto
{
    /// <summary>
    /// 是否绑定服务号
    /// </summary>
    public bool IsBindFuwuhao { get; set; }

    /// <summary>
    /// 是否为VIP用户
    /// </summary>
    public bool IsVip { get; set; }

    /// <summary>
    /// VIP到期时间
    /// </summary>
    public DateTime? VipExpireTime { get; set; }
}
