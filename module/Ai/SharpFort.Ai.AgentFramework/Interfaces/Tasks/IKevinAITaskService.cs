using System.ComponentModel;

namespace SharpFort.Ai.AgentFramework.Interfaces.Tasks;

/// <summary>
/// 用于给AI使用的自动任务服务接口，提供自动任务相关的功能和操作
/// </summary>
[Description("用于给AI使用的自动任务服务接口，提供自动任务相关的功能和操作")]
public interface IKevinAITaskService : IBaseAIToolService
{
    Task<string> AddOrUpdateCronTask(
        [Description("name：可传入具体的任务名称，不可为空")] string name,
        [Description("content：可传入具体的任务内容，不可为空")] string content,
        [Description("cron表达式：用于定义任务的执行周期，不可为空")] string cronExpression);

    Task<string> RemoveCronTask(
        [Description("name：可传入具体的任务名称，不可为空")] string name);

    Task<string> TriggerCronTask(
        [Description("name：可传入具体的任务名称，不可为空")] string name);

    Task<List<string>> GetTaskList();

    Task<string> RunTask(string userId, string taskName, string taskContent, object taskdata);
}
