using SqlSugar;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Yi.Framework.Core.Data;

namespace Yi.Framework.CasbinRbac.Domain.Entities
{
    /// <summary>
    /// 系统配置聚合根
    /// 用于存储系统运行所需的键值对参数
    /// 配置项通常是系统关键数据，支持软删除（防止误删系统参数导致崩溃）和完整审计（追踪谁修改了关键参数，如支付开关、费率等）是非常必要的。
    /// </summary>
    [SugarTable("casbin_sys_config")]
    // 配置键必须唯一，这是查询配置的核心依据
    [SugarIndex($"index_{nameof(ConfigKey)}", nameof(ConfigKey), OrderByType.Asc, IsUnique = true)]
    public class Config : FullAuditedAggregateRoot<Guid>, IOrderNum
    {
        #region 构造函数

        /// <summary>
        /// ORM 专用无参构造函数
        /// </summary>
        public Config() { }

        /// <summary>
        /// 创建新配置
        /// </summary>
        /// <param name="id">主键</param>
        /// <param name="configName">配置名称</param>
        /// <param name="configKey">配置键（唯一）</param>
        /// <param name="configValue">配置值</param>
        /// <param name="configType">配置分类（可选）</param>
        /// <param name="remark">备注</param>
        /// <param name="orderNum">排序</param>
        public Config(Guid id, string configName, string configKey, string configValue, string? configType = null, string? remark = null, int orderNum = 0)
            : base(id)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(configName, nameof(configName));
            Volo.Abp.Check.NotNullOrWhiteSpace(configKey, nameof(configKey));

            ConfigName = configName;
            ConfigKey = configKey;
            ConfigValue = configValue ?? string.Empty; // 允许空值，但建议非Null
            ConfigType = configType;
            Remark = remark;
            OrderNum = orderNum;
        }

        #endregion

        #region 核心属性

        ///// <summary>
        ///// 主键
        ///// </summary>
        //[SugarColumn(IsPrimaryKey = true)]
        //public override Guid Id { get;  set; }

        /// <summary>
        /// 配置名称
        /// 用于前端显示，如 "系统名称"
        /// </summary>
        [SugarColumn(Length = 64)]
        public string ConfigName { get; protected set; }

        /// <summary>
        /// 配置键
        /// 用于程序读取，如 "sys.app.name"，不可重复
        /// </summary>
        [SugarColumn(Length = 64)]
        public string ConfigKey { get; protected set; }

        /// <summary>
        /// 配置值
        /// 实际参数值，可能包含 JSON 或长字符串，建议设置较长
        /// </summary>
        [SugarColumn(Length = 2000)]
        public string ConfigValue { get; protected set; }

        /// <summary>
        /// 配置分类/类别
        /// 用于分组展示，如 "System", "Oss", "Pay"
        /// </summary>
        [SugarColumn(Length = 64, IsNullable = true)]
        public string? ConfigType { get; protected set; }

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true)]
        public string? Remark { get; protected set; }

        /// <summary>
        /// 排序 (IOrderNum 实现)
        /// </summary>
        public int OrderNum { get; set; }

        #endregion

        #region 业务方法

        /// <summary>
        /// 修改配置值
        /// 这是最常用的操作，单独封装
        /// </summary>
        /// <param name="value">新值</param>
        public void SetValue(string value)
        {
            ConfigValue = value ?? string.Empty;
        }

        /// <summary>
        /// 更新配置基础信息
        /// 通常仅由开发人员或超级管理员操作
        /// </summary>
        /// <param name="configName">配置名称</param>
        /// <param name="configKey">配置键</param>
        /// <param name="configType">分类</param>
        /// <param name="remark">备注</param>
        /// <param name="orderNum">排序</param>
        public void UpdateInfo(string configName, string configKey, string? configType, string? remark, int orderNum)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(configName, nameof(configName));
            Volo.Abp.Check.NotNullOrWhiteSpace(configKey, nameof(configKey));

            ConfigName = configName;
            ConfigKey = configKey;
            ConfigType = configType;
            Remark = remark;
            OrderNum = orderNum;
        }

        #endregion
    }
}
