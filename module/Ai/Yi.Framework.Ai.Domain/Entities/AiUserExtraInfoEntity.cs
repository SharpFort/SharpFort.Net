using SqlSugar;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;

namespace Yi.Framework.Ai.Domain.Entities;

/// <summary>
/// ai用户表
/// </summary>
[SugarTable("Ai_UserExtraInfo")]
[SugarIndex($"index_{nameof(UserId)}", nameof(UserId), OrderByType.Asc)]
public class AiUserExtraInfoEntity : Entity<Guid>, IHasCreationTime, ISoftDelete
{
    public AiUserExtraInfoEntity()
    {
    }

    public AiUserExtraInfoEntity(Guid userId, string fuwuhaoOpenId)
    {
        this.UserId = userId;
        this.FuwuhaoOpenId = fuwuhaoOpenId;
    }

    /// <summary>
    /// 用户id
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 服务号，openid
    /// </summary>
    public string FuwuhaoOpenId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreationTime { get; set; }

    public bool IsDeleted { get; set; }
}
