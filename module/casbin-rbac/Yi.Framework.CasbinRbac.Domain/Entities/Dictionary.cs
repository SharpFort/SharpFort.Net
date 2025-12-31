using Microsoft.AspNetCore.Server.IISIntegration;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Yi.Framework.Core.Data;

namespace Yi.Framework.CasbinRbac.Domain.Entities
{
    /// <summary>
    /// 字典数据聚合根
    /// (对应 sys_dict_data 表，存储具体的字典键值对)
    /// </summary>
    /// <remarks>
    /// 注意：类名 Dictionary 与 C# 系统集合类型冲突，使用时请注意命名空间引用。
    /// </remarks>
    [SugarTable("sys_dictionary")]
    // 索引1：根据字典类型查询列表（高频操作）
    [SugarIndex($"index_{nameof(DictType)}", nameof(DictType), OrderByType.Asc)]
    // 索引2：同一字典类型下，字典值必须唯一（防止重复定义）
    [SugarIndex($"index_unique_{nameof(DictType)}_{nameof(DictValue)}", nameof(DictType), OrderByType.Asc, nameof(DictValue), OrderByType.Asc, IsUnique = true)]
    public class Dictionary : FullAuditedAggregateRoot<Guid>, IOrderNum, IState
    {
        #region 构造函数

        /// <summary>
        /// ORM 专用无参构造函数
        /// </summary>
        public Dictionary() { }

        /// <summary>
        /// 创建字典项
        /// </summary>
        /// <param name="id">主键</param>
        /// <param name="dictType">字典类型 (如: sys_user_gender)</param>
        /// <param name="dictLabel">字典标签 (如: 男)</param>
        /// <param name="dictValue">字典键值 (如: 1)</param>
        /// <param name="orderNum">排序</param>
        /// <param name="remark">备注</param>
        public Dictionary(Guid id, string dictType, string dictLabel, string dictValue, int orderNum = 0, string? remark = null, string? listClass = null)
            : base(id)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(dictType, nameof(dictType));
            Volo.Abp.Check.NotNullOrWhiteSpace(dictLabel, nameof(dictLabel));
            Volo.Abp.Check.NotNullOrWhiteSpace(dictValue, nameof(dictValue));

            DictType = dictType;
            DictLabel = dictLabel;
            DictValue = dictValue;
            OrderNum = orderNum;
            Remark = remark;
            ListClass = listClass;

        // 默认值
            IsDeleted = false;
            State = true;
            IsDefault = false;
        }

        #endregion

        #region 核心属性

        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public override Guid Id { get; protected set; }

        /// <summary>
        /// 字典类型
        /// 核心分组字段，如 "sys_user_sex"
        /// </summary>
        [SugarColumn(Length = 100)]
        public string DictType { get; protected set; }

        /// <summary>
        /// 字典标签
        /// 用于前端展示，如 "男"
        /// </summary>
        [SugarColumn(Length = 100)]
        public string DictLabel { get; protected set; }

        /// <summary>
        /// 字典键值
        /// 实际存储值，如 "1" 或 "male"
        /// </summary>
        [SugarColumn(Length = 100)]
        public string DictValue { get; protected set; }

        /// <summary>
        /// 是否默认值
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// 样式属性 (ListClass)
        /// 如: default, primary, success, info...
        /// </summary>
        [SugarColumn(IsNullable = true, Length = 100)]
        public string? ListClass { get; set; }

        /// <summary>
        /// CSS 样式类 (CssClass)
        /// 用于前端自定义样式
        /// </summary>
        [SugarColumn(IsNullable = true, Length = 100)]
        public string? CssClass { get; set; }

        /// <summary>
        /// 排序 (IOrderNum 实现)
        /// </summary>
        public int OrderNum { get; set; }

        /// <summary>
        /// 状态 (IState 实现)
        /// </summary>
        public bool State { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(IsNullable = true, Length = 500)]
        public string? Remark { get; set; }

        #endregion

        #region 业务方法

        /// <summary>
        /// 更新字典项基本信息
        /// </summary>
        public void Update(string dictLabel, string dictValue, string? listClass, string? cssClass, string? remark, int orderNum, bool state)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(dictLabel, nameof(dictLabel));
            Volo.Abp.Check.NotNullOrWhiteSpace(dictValue, nameof(dictValue));

            DictLabel = dictLabel;
            DictValue = dictValue;
            ListClass = listClass;
            CssClass = cssClass;
            Remark = remark;
            OrderNum = orderNum;
            State = state;

            // 注意：通常不允许直接修改 DictType，因为这可能导致关联数据失效。
            // 如果必须修改 Type，建议删除后重建或提供专门的迁移方法。
        }

        /// <summary>
        /// 设置为默认值
        /// 注意：调用此方法前，应用服务层应负责将同 Type 下的其他项设为 IsDefault = false
        /// </summary>
        public void SetDefault(bool isDefault)
        {
            IsDefault = isDefault;
        }

        #endregion
    }
}