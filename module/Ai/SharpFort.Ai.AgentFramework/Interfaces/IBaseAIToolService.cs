namespace SharpFort.Ai.AgentFramework.Interfaces;

public interface IBaseAIToolService
{
    /// <summary>
    /// 初始化数据，用于AI调用前传递上下文数据
    /// </summary>
    void InitData(object data);
}
