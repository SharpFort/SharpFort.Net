using Volo.Abp.Application.Dtos;
using Yi.Framework.Ddd.Application.Contracts;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.UsageStatistics;

public class PremiumTokenUsageGetListOutput : CreationAuditedEntityDto
{
   /// <summary>
   /// id
   /// </summary>
   public Guid Id { get; set; }

   /// <summary>
   /// 用户ID
   /// </summary>
   public Guid UserId { get; set; }

   /// <summary>
   /// 包名称
   /// </summary>
   public string PackageName { get; set; }

   /// <summary>
   /// 总用量（总token数）
   /// </summary>
   public long TotalTokens { get; set; }

   /// <summary>
   /// 剩余用量（剩余token数）
   /// </summary>
   public long RemainingTokens { get; set; }

   /// <summary>
   /// 已使用token数
   /// </summary>
   public long UsedTokens { get; set; }

   /// <summary>
   /// 到期时间
   /// </summary>
   public DateTime? ExpireDateTime { get; set; }

   /// <summary>
   /// 是否激活
   /// </summary>
   public bool IsActive { get; set; }

   /// <summary>
   /// 购买金额
   /// </summary>
   public decimal PurchaseAmount { get; set; }

   /// <summary>
   /// 备注
   /// </summary>
   public string? Remark { get; set; }
}
