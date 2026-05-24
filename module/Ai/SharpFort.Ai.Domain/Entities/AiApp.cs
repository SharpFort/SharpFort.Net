using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;

namespace SharpFort.Ai.Domain.Entities;

/// <summary>
/// AI应用配置 - 定义智能体的完整参数
/// </summary>
[SugarTable("Ai_App")]
public class AiApp : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// 应用名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 应用描述
    /// </summary>
    public string? Describe { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; } = "windows";

    /// <summary>
    /// 应用类型
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 会话模型ID
    /// </summary>
    public Guid? ChatModelId { get; set; }

    /// <summary>
    /// 重排模型ID
    /// </summary>
    public Guid? RerankModelId { get; set; }

    /// <summary>
    /// 温度 (0.1-2.0)
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// 知识库ID
    /// </summary>
    public Guid? KmsId { get; set; }

    /// <summary>
    /// API调用秘钥
    /// </summary>
    [SugarColumn(Length = 200)]
    public string? SecretKey { get; set; }

    /// <summary>
    /// 向量检索相似度阈值
    /// </summary>
    public double Relevance { get; set; } = 0.6;

    /// <summary>
    /// 提问最大Token数
    /// </summary>
    public int MaxAskPromptSize { get; set; } = 2048;

    /// <summary>
    /// 向量匹配数量
    /// </summary>
    public int MaxMatchesCount { get; set; } = 3;

    /// <summary>
    /// Rerank数量
    /// </summary>
    public int RerankCount { get; set; } = 20;

    /// <summary>
    /// 回答最大Token数
    /// </summary>
    public int AnswerTokens { get; set; } = 2048;

    /// <summary>
    /// 绑定提示词ID
    /// </summary>
    public Guid? AiPromptId { get; set; }

    /// <summary>
    /// 输出消息类型: 1=非流式文本 2=流式文本 3=图片 4=音频 5=视频 6=文件 7=链接 8=卡片
    /// </summary>
    public int MsgType { get; set; } = 1;

    /// <summary>
    /// 是否开启AI工具
    /// </summary>
    public bool IsAiTools { get; set; } = true;

    /// <summary>
    /// 是否开启Skill技能
    /// </summary>
    public bool IsSkill { get; set; } = true;

    /// <summary>
    /// 是否开启AI请求日志
    /// </summary>
    public bool IsHttpLog { get; set; }

    /// <summary>
    /// AI请求最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// AI请求超时时间（分钟）
    /// </summary>
    public int NetworkTimeout { get; set; } = 10;
}
