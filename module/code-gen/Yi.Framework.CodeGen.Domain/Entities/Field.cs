using SqlSugar;
using Volo.Abp.Domain.Entities;
using Yi.Framework.CodeGen.Domain.Shared.Enums;
using Volo.Abp.Domain.Entities.Auditing;



namespace Yi.Framework.CodeGen.Domain.Entities;

/// <summary>
/// 字段定义实体
/// 领域定义：描述数据库表中的一个列，用于代码生成
/// </summary>
[SugarTable("gen_field")]
// 索引：同一个表下，字段名必须唯一
[SugarIndex($"index_gen_field_table_name", nameof(TableId), OrderByType.Asc, nameof(Name), OrderByType.Asc, IsUnique = true)]
// 索引：生成代码时需要按顺序遍历字段
[SugarIndex($"index_gen_field_table_sort", nameof(TableId), OrderByType.Asc, nameof(OrderNum), OrderByType.Asc)]
public class Field : FullAuditedEntity<Guid>
{
    /// <summary>
    /// 主键
    /// </summary>
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public override Guid Id { get; protected set; }

    /// <summary>
    /// 所属表 ID
    /// 规则：必填，不可变
    /// </summary>
    [SugarColumn(ColumnName = "table_id", IsNullable = false)]
    public Guid TableId { get; set;  }

    /// <summary>
    /// 字段名称 (列名)
    /// 规则：必填，长度 64，同一表下唯一
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 64, IsNullable = false)]
    public string Name { get; set;  }

    /// <summary>
    /// 字段描述/注释
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 512, IsNullable = true)]
    public string? Description { get; set;  }

    /// <summary>
    /// 字段类型 (如 String, Int, Guid)
    /// </summary>
    [SugarColumn(ColumnName = "field_type")]
    public FieldType FieldType { get; set;  }

    /// <summary>
    /// 数据长度 (如 varchar(64) 中的 64)
    /// </summary>
    [SugarColumn(ColumnName = "length")]
    public int Length { get; set;  }

    /// <summary>
    /// 排序权重
    /// </summary>
    [SugarColumn(ColumnName = "order_num")]
    public int OrderNum { get; set;  }

    /// <summary>
    /// 是否必填 (Nullable = false)
    /// </summary>
    [SugarColumn(ColumnName = "is_required")]
    public bool IsRequired { get; set;  }

    /// <summary>
    /// 是否主键
    /// </summary>
    [SugarColumn(ColumnName = "is_key")]
    public bool IsKey { get; set;  }

    /// <summary>
    /// 是否自增
    /// </summary>
    [SugarColumn(ColumnName = "is_auto_add")]
    public bool IsAutoAdd { get; set;  }

    /// <summary>
    /// 是否公共字段 (如 CreationTime, IsDeleted)
    /// 场景：生成 DTO 时通常会忽略公共字段
    /// </summary>
    [SugarColumn(ColumnName = "is_public")]
    public bool IsPublic { get; set;  }

    // IsDeleted, CreationTime 等由 FullAuditedEntity 自动实现

    /// <summary>
    /// ORM 专用
    /// </summary>
    public Field() { }

    /// <summary>
    /// 创建字段定义
    /// </summary>
    public Field(Guid id, Guid tableId, string name, FieldType fieldType) : base(id)
    {
        TableId = tableId;
        SetName(name);
        FieldType = fieldType;

        // 默认值
        OrderNum = 0;
        Length = 0;
        IsRequired = false;
        IsKey = false;
        IsAutoAdd = false;
        IsPublic = false;
    }

    #region 领域行为

    /// <summary>
    /// 更新基本信息
    /// </summary>
    public void UpdateBasic(string name, string? description, int length, int orderNum, FieldType type)
    {
        SetName(name);
        Description = Volo.Abp.Check.Length(description, nameof(description), 512);
        Length = length < 0 ? 0 : length;
        OrderNum = orderNum;
        FieldType = type;
    }

    /// <summary>
    /// 设置字段属性/标记
    /// </summary>
    public void SetFlags(bool isRequired, bool isKey, bool isAutoAdd, bool isPublic)
    {
        IsRequired = isRequired;
        IsKey = isKey;
        IsAutoAdd = isAutoAdd;
        IsPublic = isPublic;

        // 业务规则保护：如果是主键，通常也是必填的
        if (IsKey)
        {
            IsRequired = true;
        }
    }

    private void SetName(string name)
    {
        Name = Volo.Abp.Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 64);
    }

    #endregion
}
