using System.Text;
using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;

namespace Yi.Framework.Ai.Domain.Entities;

[SugarTable("Ai_Token")]
[SugarIndex($"index_{{table}}_{nameof(UserId)}", nameof(UserId), OrderByType.Asc)]
public class Token : FullAuditedAggregateRoot<Guid>
{
    public Token()
    {
    }

    public Token(Guid userId, string name)
    {
        UserId = userId;
        Name = name;
        TokenKey = GenerateToken();
        IsDisabled = false;
    }

    /// <summary>
    /// Token密钥
    /// </summary>
    [SugarColumn(ColumnName = "Token")]
    public string TokenKey { get; set; }

    /// <summary>
    /// 用户Id
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(Length = 100)]
    public string Name { get; set; }

    /// <summary>
    /// 过期时间（空为永不过期）
    /// </summary>
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// 尊享包额度限制（空为不限制）
    /// </summary>
    public long? PremiumQuotaLimit { get; set; }

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 是否启用请求日志记录（仅数据库手动修改）
    /// </summary>
    public bool IsEnableLog { get; set; }

    /// <summary>
    /// 检查Token是否可用
    /// </summary>
    public bool IsAvailable()
    {
        if (IsDisabled)
        {
            return false;
        }

        if (ExpireTime.HasValue && ExpireTime.Value < DateTime.Now)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 禁用Token
    /// </summary>
    public void Disable()
    {
        IsDisabled = true;
    }

    /// <summary>
    /// 启用Token
    /// </summary>
    public void Enable()
    {
        IsDisabled = false;
    }

    private string GenerateToken(int length = 36)
    {
        // 定义可能的字符集：大写字母、小写字母和数字
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        // 创建随机数生成器
        Random random = new Random();

        // 使用StringBuilder高效构建字符串
        StringBuilder sb = new StringBuilder(length);

        // 生成指定长度的随机字符串
        for (int i = 0; i < length; i++)
        {
            // 从字符集中随机选择一个字符
            int index = random.Next(chars.Length);
            sb.Append(chars[index]);
        }

        return "yi-" + sb.ToString();
    }
}
