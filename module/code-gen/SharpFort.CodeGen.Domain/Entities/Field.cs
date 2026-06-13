using SqlSugar;
using SharpFort.CodeGen.Domain.Shared.Enums;
using Volo.Abp.Domain.Entities.Auditing;



namespace SharpFort.CodeGen.Domain.Entities;

/// <summary>
/// 实体字段定义实体 (YiField)
/// 领域定义：描述 C# Entity 类中的一个属性，用于代码生成时控制 DTO/Service 的字段输出
/// 职责：存储字段结构信息 + UI 配置（IsQueryField/IsListDisplay/IsFormItem/HtmlType/OrderNum）
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
    /// 所属实体注册表 ID (外键)
    /// 规则：必填，不可变
    /// </summary>
    [SugarColumn(ColumnName = "table_id", IsNullable = false)]
    public Guid TableId { get; set; }

    /// <summary>
    /// 字段名称 (C# 属性名，如: UserName)
    /// 规则：必填，长度 64，同一实体下唯一
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 64, IsNullable = false)]
    public string? Name { get; set; } = null;

    /// <summary>
    /// 字段描述/注释
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 512, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 字段类型枚举 (String/Int/Long/Bool/Decimal/DateTime/Guid/Float/Double)
    /// 映射：Scriban 模板中通过 csharp_type 函数转为 C# 类型字符串
    /// </summary>
    [SugarColumn(ColumnName = "field_type")]
    public FieldType FieldType { get; set; }

    /// <summary>
    /// 数据长度 (如 SugarColumn(Length=64) 中的 64)
    /// 用途：Scriban 模板中的 sugar_column 函数生成 [SugarColumn(Length=N)]
    /// </summary>
    [SugarColumn(ColumnName = "length")]
    public int Length { get; set; }

    /// <summary>
    /// 排序权重 (控制生成代码中字段的顺序)
    /// </summary>
    [SugarColumn(ColumnName = "order_num")]
    public int OrderNum { get; set; }

    /// <summary>
    /// 是否必填 (对应 C# Nullable&lt;T&gt; 判断)
    /// </summary>
    [SugarColumn(ColumnName = "is_required")]
    public bool IsRequired { get; set; }

    /// <summary>
    /// 是否主键字段 (对应 [SugarColumn(IsPrimaryKey=true)])
    /// </summary>
    [SugarColumn(ColumnName = "is_key")]
    public bool IsKey { get; set; }

    /// <summary>
    /// 是否自增字段 (对应 [SugarColumn(IsIdentity=true)])
    /// </summary>
    [SugarColumn(ColumnName = "is_auto_add")]
    public bool IsAutoAdd { get; set; }

    /// <summary>
    /// 是否公共/审计字段 (如 CreationTime, IsDeleted, CreatorId)
    /// 场景：生成 DTO 时 Scriban 模板通常跳过公共字段
    /// </summary>
    [SugarColumn(ColumnName = "is_public")]
    public bool IsPublic { get; set; }

    /// <summary>
    /// 是否作为查询条件字段 (UI 配置)
    /// 用途：控制是否在 GetListInput DTO 中生成查询属性
    /// </summary>
    [SugarColumn(ColumnName = "is_query_field")]
    public bool IsQueryField { get; set; }

    /// <summary>
    /// 是否在列表中显示 (UI 配置)
    /// 用途：控制是否在 GetListOutputDto 中生成此字段
    /// </summary>
    [SugarColumn(ColumnName = "is_list_display")]
    public bool IsListDisplay { get; set; }

    /// <summary>
    /// 是否出现在表单中 (UI 配置)
    /// 用途：控制是否在 CreateInput/UpdateInput DTO 中生成此字段
    /// </summary>
    [SugarColumn(ColumnName = "is_form_item")]
    public bool IsFormItem { get; set; }

    /// <summary>
    /// 前端渲染控件类型 (UI 配置)
    /// 示例：Input, Select, DatePicker, Textarea, Switch
    /// </summary>
    [SugarColumn(ColumnName = "html_type", Length = 32)]
    public string? HtmlType { get; set; }

    // IsDeleted, CreationTime 等由 FullAuditedAggregateRootEntity 自动实现

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
    /// 更新字段基本信息
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
    /// 设置字段属性标记 (必填/主键/自增/公共)
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
