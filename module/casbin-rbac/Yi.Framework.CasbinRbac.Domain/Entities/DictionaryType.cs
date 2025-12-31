using SqlSugar;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Yi.Framework.Core.Data;

namespace Yi.Framework.CasbinRbac.Domain.Entities
{
    /// <summary>
    /// 字典类型聚合根
    /// (对应 sys_dictionary_type 表，管理字典的分类定义)
    /// </summary>
    [SugarTable("sys_dictionary_type")]
    // 核心约束：字典类型编码必须唯一，否则数据无法正确归类
    [SugarIndex($"index_{nameof(DictType)}", nameof(DictType), OrderByType.Asc, IsUnique = true)]
    public class DictionaryType : FullAuditedAggregateRoot<Guid>, IOrderNum, IState
    {
        #region 构造函数

        /// <summary>
        /// ORM 专用无参构造函数
        /// </summary>
        public DictionaryType() { }

        /// <summary>
        /// 创建字典类型
        /// </summary>
        /// <param name="id">主键</param>
        /// <param name="dictName">字典名称 (如: 用户性别)</param>
        /// <param name="dictType">字典类型编码 (如: sys_user_sex)</param>
        /// <param name="orderNum">排序</param>
        /// <param name="remark">备注</param>
        public DictionaryType(Guid id, string dictName, string dictType, int orderNum = 0, string? remark = null)
            : base(id)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(dictName, nameof(dictName));
            Volo.Abp.Check.NotNullOrWhiteSpace(dictType, nameof(dictType));

            DictName = dictName;
            DictType = dictType;
            OrderNum = orderNum;
            Remark = remark;

            // 默认启用
            State = true;
        }

        #endregion

        #region 核心属性

        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public override Guid Id { get; protected set; }

        /// <summary>
        /// 字典名称
        /// </summary>
        [SugarColumn(Length = 100)]
        public string DictName { get; protected set; }

        /// <summary>
        /// 字典类型编码
        /// 系统的核心标识，创建后不可变更
        /// </summary>
        [SugarColumn(Length = 100)]
        public string DictType { get; protected set; }

        /// <summary>
        /// 状态 (IState 实现)
        /// True: 正常, False: 停用
        /// </summary>
        public bool State { get; set; }

        /// <summary>
        /// 排序 (IOrderNum 实现)
        /// </summary>
        public int OrderNum { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(IsNullable = true, Length = 500)]
        public string? Remark { get; set; }

        #endregion

        #region 业务方法

        /// <summary>
        /// 更新字典类型信息
        /// 注意：不允许修改 DictType，防止破坏数据关联性
        /// </summary>
        /// <param name="dictName">字典名称</param>
        /// <param name="state">状态</param>
        /// <param name="remark">备注</param>
        /// <param name="orderNum">排序</param>
        public void Update(string dictName, bool state, string? remark, int orderNum)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(dictName, nameof(dictName));

            DictName = dictName;
            State = state;
            Remark = remark;
            OrderNum = orderNum;
        }

        #endregion

        /* 
         * 关于 ExtraProperties:
         * AggregateRoot 基类已包含 ExtraProperties。
         * 除非您有特定的 JSON 映射需求需要重写 Attribute，否则无需在子类中显式声明。
         * SqlSugar 默认通常会忽略它，除非配置了 Json 转换器。
         */
    }
}