using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;

namespace Yi.Framework.CasbinRbac.Domain.Entities;

/// <summary>
/// 角色-字段权限 (Field Level Security)
/// 存储角色的字段黑名单。
/// 逻辑：如果存在记录 (RoleId + TableName + FieldName)，则表示该角色【无权访问】该字段。
/// </summary>
[SugarTable("casbin_sys_role_field")]
public class RoleField : CreationAuditedEntity<Guid>
{
    public RoleField() { }

    public RoleField(Guid id, Guid roleId, string tableName, string fieldName) : base(id)
    {
        RoleId = roleId;
        TableName = tableName;
        FieldName = fieldName;
    }

    /// <summary>
    /// 角色ID
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// 表名 (数据库表名，如 sys_user)
    /// </summary>
    [SugarColumn(Length = 64)]
    public string TableName { get; set; }

    /// <summary>
    /// 字段名 (数据库列名，如 phone)
    /// </summary>
    [SugarColumn(Length = 64)]
    public string FieldName { get; set; }
}

