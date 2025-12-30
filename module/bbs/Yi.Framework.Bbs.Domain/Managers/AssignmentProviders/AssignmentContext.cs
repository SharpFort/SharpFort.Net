using Yi.Framework.Bbs.Domain.Entities.Assignment;

namespace Yi.Framework.Bbs.Domain.Managers.AssignmentProviders;

public class AssignmentContext
{
    public AssignmentContext( Guid currentUserId,List<AssignmentDefine> allAssignmentDefine, List<Assignment> currentUserAssignments)
    {
        AllAssignmentDefine = allAssignmentDefine;
        CurrentUserAssignments = currentUserAssignments;
        CurrentUserId = currentUserId;
    }

    /// <summary>
    /// 全部的任务定义
    /// </summary>
    public List<AssignmentDefine> AllAssignmentDefine { get; }

    /// <summary>
    /// 当前用户的全部任务数据
    /// </summary>
    public List<Assignment> CurrentUserAssignments { get; }

    /// <summary>
    /// 当前用户id
    /// </summary>
    public Guid CurrentUserId { get; }
}