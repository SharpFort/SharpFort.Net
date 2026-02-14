using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;

namespace Yi.Framework.Ai.Domain.Entities;

[SugarTable("Ai_AgentStore")]
[SugarIndex($"index_{{table}}_{nameof(SessionId)}",
    nameof(SessionId), OrderByType.Desc
)]
public class AgentStoreAggregateRoot : FullAuditedAggregateRoot<Guid>
{
    public AgentStoreAggregateRoot()
    {
    }
    
    /// <summary>
    /// 构建
    /// </summary>
    /// <param name="sessionId"></param>
    public AgentStoreAggregateRoot(Guid sessionId)
    {
        SessionId = sessionId;
    }

    /// <summary>
    /// 会话id
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// 存储
    /// </summary>
    [SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? Store { get; set; }

    /// <summary>
    /// 设置存储
    /// </summary>
    public void SetStore()
    {
        this.Store = Store;
    }
}
