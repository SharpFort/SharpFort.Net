using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace SharpFort.Ai.Application.Contracts.Dtos.AiApp;

/// <summary>
/// AI应用配置DTO
/// </summary>
public class AiAppDto : EntityDto<Guid>
{
    /// <summary>
    /// 应用名称
    /// </summary>
    [Required]
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
    /// 温度 (0.1-2.0, 显示值 10-200)
    /// </summary>
    [DefaultValue(70)]
    public double Temperature { get; set; } = 70;

    /// <summary>
    /// 知识库ID
    /// </summary>
    public Guid? KmsId { get; set; }

    /// <summary>
    /// API调用秘钥
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// 向量检索相似度阈值
    /// </summary>
    [DefaultValue(60)]
    public double Relevance { get; set; } = 60;

    /// <summary>
    /// 提问最大Token数
    /// </summary>
    [DefaultValue(2048)]
    public int MaxAskPromptSize { get; set; } = 2048;

    /// <summary>
    /// 向量匹配数量
    /// </summary>
    [DefaultValue(3)]
    public int MaxMatchesCount { get; set; } = 3;

    /// <summary>
    /// Rerank数量
    /// </summary>
    [DefaultValue(20)]
    public int RerankCount { get; set; } = 20;

    /// <summary>
    /// 回答最大Token数
    /// </summary>
    [DefaultValue(2048)]
    public int AnswerTokens { get; set; } = 2048;

    /// <summary>
    /// 绑定提示词ID
    /// </summary>
    public Guid? AiPromptId { get; set; }

    /// <summary>
    /// 绑定提示词名称
    /// </summary>
    public string? AiPromptName { get; set; }

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

    /// <summary>
    /// 绑定的工具列表
    /// </summary>
    public List<AiAppBindSkillToolDto> Tools { get; set; } = [];

    /// <summary>
    /// 绑定的技能列表
    /// </summary>
    public List<AiAppBindSkillToolDto> Skills { get; set; } = [];
}
