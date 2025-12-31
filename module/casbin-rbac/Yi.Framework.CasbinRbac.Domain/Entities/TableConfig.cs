using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;

namespace Yi.Framework.CasbinRbac.Domain.Entities;

/// <summary>
/// 表字段元数据配置
/// 用于列出系统中所有需要进行权限控制的表和字段的中文描述，供前端配置界面选择。
/// </summary>
[SugarTable("sys_table_config")]
public class TableConfig : CreationAuditedEntity<Guid>
{
    public TableConfig() { }

    public TableConfig(Guid id, string tableName, string fieldName, string fieldRemark) : base(id)
    {
        TableName = tableName;
        FieldName = fieldName;
        FieldRemark = fieldRemark;
    }

    /// <summary>
    /// 表名 (如 sys_user)
    /// </summary>
    [SugarColumn(Length = 64)]
    public string TableName { get; set; }

    /// <summary>
    /// 字段名 (如 phone)
    /// </summary>
    [SugarColumn(Length = 64)]
    public string FieldName { get; set; }

    /// <summary>
    /// 字段中文描述 (如 手机号)
    /// </summary>
    [SugarColumn(Length = 64)]
    public string FieldRemark { get; set; }
}
