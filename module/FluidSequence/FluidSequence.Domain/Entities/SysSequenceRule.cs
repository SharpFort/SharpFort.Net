using SqlSugar;
using System;
using System.Collections.Generic;
using System.Globalization;
using Volo.Abp.Domain.Entities.Auditing;
using FluidSequence.Domain.Shared.Enums;

namespace FluidSequence.Domain.Entities
{
    /// <summary>
    /// 流水号规则表
    /// </summary>
    [SugarTable("sys_sequence_rule", "流水号规则配置")]
    [SugarIndex("index_rule_code", nameof(RuleCode), OrderByType.Asc, true)]
    public class SysSequenceRule : FullAuditedAggregateRoot<long>
    {
        public SysSequenceRule()
        {
        }

        public SysSequenceRule(long id) : base(id)
        {
        }

        /// <summary>
        /// 规则名称 (如：采购订单号)
        /// </summary>
        [SugarColumn(Length = 50, ColumnDescription = "规则名称", IsNullable = false)]
        public string RuleName { get; set; }

        /// <summary>
        /// 规则编码 (业务唯一键，如：PO_NO)
        /// </summary>
        [SugarColumn(Length = 50, ColumnDescription = "规则编码", IsNullable = false)]
        public string RuleCode { get; set; }

        /// <summary>
        /// 生成模板 (如：PO-{DeptCode}-{yyyy}{MM}-{SEQ})
        /// </summary>
        [SugarColumn(Length = 100, ColumnDescription = "生成模板", IsNullable = false)]
        public string Template { get; set; }

        /// <summary>
        /// 当前计数值 (核心状态，持久化存储)
        /// </summary>
        [SugarColumn(ColumnDescription = "当前值", IsNullable = false)]
        public long CurrentValue { get; set; }

        /// <summary>
        /// 步长 (默认为 1)
        /// </summary>
        [SugarColumn(ColumnDescription = "步长", DefaultValue = "1")]
        public int Step { get; set; } = 1;

        /// <summary>
        /// 序列号长度 (用于左补0，如 6 表示 000001)
        /// </summary>
        [SugarColumn(ColumnDescription = "序列长度", DefaultValue = "6")]
        public int SeqLength { get; set; } = 6;

        /// <summary>
        /// 最小值 (重置后的起始值)
        /// </summary>
        [SugarColumn(ColumnDescription = "最小值", DefaultValue = "1")]
        public long MinValue { get; set; } = 1;

        /// <summary>
        /// 最大值 (防止溢出)
        /// </summary>
        [SugarColumn(ColumnDescription = "最大值", DefaultValue = "999999999")]
        public long MaxValue { get; set; } = 999999999;

        /// <summary>
        /// 重置规则 (0:不重置, 1:按日, 2:按月, 3:按年...)
        /// </summary>
        [SugarColumn(ColumnDescription = "重置规则")]
        public SequenceResetType ResetType { get; set; }

        /// <summary>
        /// 最后重置时间 (用于判断是否跨周期)
        /// </summary>
        [SugarColumn(ColumnDescription = "最后重置时间", IsNullable = true)]
        public DateTime? LastResetTime { get; set; }

        /// <summary>
        /// 乐观锁版本号 (并发控制核心)
        /// </summary>
        [SugarColumn(IsEnableUpdateVersionValidation = true, ColumnDescription = "版本号")]
        public long Version { get; set; }

        /// <summary>
        /// 租户ID (多租户隔离)
        /// </summary>
        [SugarColumn(ColumnDescription = "租户ID", IsNullable = true)]
        public Guid? TenantId { get; set; }
        
        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = true)]
        public string Remark { get; set; }

        /// <summary>
        /// 扩展属性 (JSON格式)
        /// </summary>
        [SugarColumn(IsJson = true, ColumnDescription = "扩展属性", IsNullable = true)]
        public Dictionary<string, object> ExtensionProps { get; set; }

        public bool TryReset(DateTime now)
        {
            if (ResetType == SequenceResetType.None) return false;
            
            bool shouldReset = false;
            // Handle first time initialization
            if (LastResetTime == null)
            {
                LastResetTime = now;
                // If current value is default/initial, we don't necessarily 'reset', 
                // but we mark the time. If it was used, it should be at least MinValue.
                // Assuming TryReset is called before NextValue.
                return false; 
            }

            var last = LastResetTime.Value;
            switch (ResetType)
            {
                case SequenceResetType.Daily:
                    shouldReset = last.Date != now.Date;
                    break;
                case SequenceResetType.Monthly:
                    shouldReset = last.Year != now.Year || last.Month != now.Month;
                    break;
                case SequenceResetType.Yearly:
                    shouldReset = last.Year != now.Year;
                    break;
                case SequenceResetType.Weekly:
                     var cal = CultureInfo.CurrentCulture.Calendar;
                     var rule = CalendarWeekRule.FirstFourDayWeek; // Customize if needed
                     var dow = DayOfWeek.Monday; // Customize if needed
                     int lastWeek = cal.GetWeekOfYear(last, rule, dow);
                     int currentWeek = cal.GetWeekOfYear(now, rule, dow);
                     shouldReset = last.Year != now.Year || lastWeek != currentWeek;
                    break;
                case SequenceResetType.Quarterly:
                    int lastQ = (last.Month - 1) / 3 + 1;
                    int currentQ = (now.Month - 1) / 3 + 1;
                    shouldReset = last.Year != now.Year || lastQ != currentQ;
                    break;
                case SequenceResetType.FiscalYearly:
                     // Needs ExtensionProps configuration
                     int startMonth = 1;
                     if (ExtensionProps != null && ExtensionProps.ContainsKey("FiscalYearStartMonth"))
                     {
                         startMonth = Convert.ToInt32(ExtensionProps["FiscalYearStartMonth"]);
                     }
                     int lastFy = last.Year;
                     if (last.Month < startMonth) lastFy--;
                     int currentFy = now.Year;
                     if (now.Month < startMonth) currentFy--;
                     shouldReset = lastFy != currentFy;
                    break;
            }

            if (shouldReset)
            {
                CurrentValue = MinValue;
                LastResetTime = now;
                return true;
            }
            return false;
        }

        public void NextValue()
        {
            CurrentValue += Step;
            if (CurrentValue > MaxValue)
            {
                throw new OverflowException($"Sequence {RuleCode} exceeded MaxValue {MaxValue}");
            }
        }
    }
}
