namespace SharpFort.Ai.Domain.Shared.Enums;

/// <summary>
/// AI技能/工具类型枚举
/// </summary>
public enum AiSkillToolType
{
    /// <summary>
    /// 工具（函数级能力，AI可直接调用的C#方法）
    /// </summary>
    Tool = 1,

    /// <summary>
    /// 技能（文件型能力，含指令+脚本+资源）
    /// </summary>
    Skill = 2
}
