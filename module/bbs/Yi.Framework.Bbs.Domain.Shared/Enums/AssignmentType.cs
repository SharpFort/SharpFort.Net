using System.ComponentModel;

namespace Yi.Framework.Bbs.Domain.Shared.Enums;
/// <summary>
/// 任务类型
/// </summary>
public enum AssignmentType
{
    /// <summary>
    /// 未知/默认
    /// </summary>
    [Description("未知")]
    None = 0,

    /// <summary>
    /// 新手任务 (一次性)
    /// </summary>
    [Description("新手任务")]
    Novice = 10,

    /// <summary>
    /// 每日任务
    /// </summary>
    [Description("每日任务")]
    Daily = 20,

    /// <summary>
    /// 每周任务
    /// </summary>
    [Description("每周任务")]
    Weekly = 30
}

/// <summary>
/// 任务类型扩展方法
/// </summary>
public static class AssignmentTypeExtensions
{
    /// <summary>
    /// 获取任务的过期时间点
    /// </summary>
    /// <param name="AssignmentType">任务类型</param>
    /// <returns>过期时间 (null 表示永不过期)</returns>
    public static DateTime? GetExpireTime(this AssignmentType AssignmentType)
    {
        return AssignmentType switch
        {
            AssignmentType.Novice => null,

            // 每日任务：明天凌晨 00:00 过期
            AssignmentType.Daily => DateTime.Today.AddDays(1),

            // 每周任务：下周一凌晨 00:00 过期
            AssignmentType.Weekly => GetNextMonday(),

            // 默认/未知：抛出异常或返回 null，视业务严格程度而定
            _ => throw new ArgumentOutOfRangeException(nameof(AssignmentType), AssignmentType, "不支持的任务类型")
        };
    }

    /// <summary>
    /// 判断指定时间生成的任务是否已过期
    /// </summary>
    /// <param name="AssignmentType">任务类型</param>
    /// <param name="creationTime">任务创建时间</param>
    /// <returns>是否过期</returns>
    public static bool IsExpired(this AssignmentType AssignmentType, DateTime creationTime)
    {
        return AssignmentType switch
        {
            // 新手任务永不过期
            AssignmentType.Novice => false,

            // 每日任务：如果创建日期小于今天(即昨天及之前)，则已过期
            AssignmentType.Daily => creationTime.Date < DateTime.Today,

            // 每周任务：如果创建时间小于本周一凌晨，则已过期
            AssignmentType.Weekly => creationTime < GetThisMonday(),

            _ => false
        };
    }

    #region 私有辅助算法 (Private Helper Methods)

    /// <summary>
    /// 计算下个周一的日期
    /// </summary>
    private static DateTime GetNextMonday()
    {
        var today = DateTime.Today;
        // 计算距离下周一还有几天
        // (DayOfWeek.Monday - today.DayOfWeek + 7) % 7 
        // 结果：周一->0, 周二->6, 周日->1. 
        // 但我们需要的是：周一->7(下周一), 周二->6, 周日->1
        int daysUntilNextMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;

        // 如果计算结果是0（说明今天是周一），则过期时间是下周一（+7天）
        if (daysUntilNextMonday == 0)
        {
            daysUntilNextMonday = 7;
        }

        return today.AddDays(daysUntilNextMonday);
    }

    /// <summary>
    /// 计算本周一的日期 (以周一为一周开始)
    /// </summary>
    private static DateTime GetThisMonday()
    {
        var today = DateTime.Today;
        // C# DayOfWeek: Sunday=0, Monday=1 ... Saturday=6
        // 如果今天是周日(0)，我们需要减去6天回到本周一
        // 如果今天是周一(1)，我们需要减去0天
        int daysSinceMonday = (int)today.DayOfWeek - (int)DayOfWeek.Monday;

        // 如果是负数（即周日 0 - 1 = -1），说明是上一周的结尾（按中国习惯），加7修正
        if (daysSinceMonday < 0)
        {
            daysSinceMonday += 7;
        }

        return today.AddDays(-daysSinceMonday);
    }

    #endregion
}

//public static class AssignmentTypeExtension
//{
//    public static DateTime? GetExpireTime(this AssignmentType assignmentType)
//    {
//        switch (assignmentType)
//        {
//            case AssignmentType.Novice:
//                return null;
//            case AssignmentType.Daily:
//                return DateTime.Now.Date.AddDays(1);
//            case AssignmentType.Weekly:
//                DateTime currentDate = DateTime.Now; // 获取当前日期和时间
//                // 计算今天是周几
//                int daysUntilNextMonday = ((int)DayOfWeek.Monday - (int)currentDate.DayOfWeek + 7) % 7;
//                // 如果今天是周一，则获取下下周一
//                if (daysUntilNextMonday == 0)
//                {
//                    daysUntilNextMonday = 7;
//                }
//                // 计算下个周一的日期
//                DateTime nextMonday = currentDate.AddDays(daysUntilNextMonday);
//                // 返回下个周一的凌晨 0 点时间
//                return nextMonday.Date;
//            default:
//                throw new ArgumentOutOfRangeException(nameof(assignmentType), assignmentType, null);
//        }
//    }

//    public static bool IsExpire(this AssignmentType assignmentType,DateTime time)
//    {
//        switch (assignmentType)
//        {
//            case AssignmentType.Novice:
//                return false;
//            case AssignmentType.Daily:
//                //昨天之前发的，算过期
//                return time.Date < DateTime.Now.Date;
//            case AssignmentType.Weekly:
//                // 获取当前日期
//                DateTime now = DateTime.Now;
//                // 计算本周一的日期
//                int daysToSubtract = (int)now.DayOfWeek - (int)DayOfWeek.Monday;
//                if (daysToSubtract < 0) daysToSubtract += 7; // 如果今天是周日，则需要调整
//                DateTime startOfWeek = now.AddDays(-daysToSubtract).Date;
//                // 获取本周一的凌晨 00:00
//                DateTime mondayMidnight = startOfWeek; // .Date 默认为 00:00
//                //本周一之前发的
//                return  time<mondayMidnight ;
//            default:
//                throw new ArgumentOutOfRangeException(nameof(assignmentType), assignmentType, null);
//        }
//    }
//}