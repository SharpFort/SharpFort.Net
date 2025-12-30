using SqlSugar;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Yi.Framework.CodeGen.Domain.Entities;

/// <summary>
/// 表定义聚合根
/// 领域定义：描述数据库中的一张表，作为代码生成的源头配置
/// </summary>
[SugarTable("gen_table")]
// 索引：表名必须唯一，防止生成的代码类名冲突
[SugarIndex($"index_gen_table_name", nameof(Name), OrderByType.Asc, IsUnique = true)]
public class Table : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// 主键
    /// </summary>
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public override Guid Id { get; protected set;  }

    /// <summary>
    /// 表名称 (如: SystemUser)
    /// 规则：必填，唯一，建议 PascalCase
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 64, IsNullable = false)]
    public string Name { get;  set; }

    /// <summary>
    /// 表描述/备注
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 512, IsNullable = true)]
    public string? Description { get;  set; }

    /// <summary>
    /// 扩展属性 (ABP Feature)
    /// 场景：存储生成器的配置参数（如：是否覆盖、输出路径、命名空间前缀等）
    /// 映射：Postgres JSONB
    /// </summary>
    [SugarColumn(ColumnName = "extra_properties", IsJson = true)]
    public override ExtraPropertyDictionary ExtraProperties { get; protected set;  }

    /// <summary>
    /// 包含的字段列表 (聚合子项)
    /// 关系：1对多
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(Field.TableId))]
    public List<Field> Fields { get;  set; }

    // IsDeleted, CreationTime, CreatorId 由基类自动实现

    /// <summary>
    /// ORM 专用
    /// </summary>
    public  Table()
    {
        Fields = new List<Field>();
        ExtraProperties = new ExtraPropertyDictionary();
    }

    /// <summary>
    /// 创建表定义
    /// </summary>
    public Table(Guid id, string name, string? description = null) : base(id)
    {
        SetName(name);
        Description = Volo.Abp.Check.Length(description, nameof(description), 512);

        Fields = new List<Field>();
        ExtraProperties = new ExtraPropertyDictionary();
    }

    #region 领域行为

    /// <summary>
    /// 更新基本信息
    /// </summary>
    public void UpdateInfo(string name, string? description)
    {
        SetName(name);
        Description = Volo.Abp.Check.Length(description, nameof(description), 512);
    }

    private void SetName(string name)
    {
        Name = Volo.Abp.Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 64);
    }

    #endregion
}