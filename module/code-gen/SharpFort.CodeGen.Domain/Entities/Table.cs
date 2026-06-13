using SqlSugar;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities.Auditing;

namespace SharpFort.CodeGen.Domain.Entities;

/// <summary>
/// 实体注册表聚合根 (YiTable)
/// 领域定义：收集并管理所有 C# Entity 类的元数据信息，作为代码生成的配置源头
/// 职责：存储“源”信息——实体类名、所属模块/项目、物理表名、命名空间等
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
    public override Guid Id { get; protected set; }

    /// <summary>
    /// 实体类名称 (如: SystemUser)
    /// 来源：C# Entity 类名或 SugarTable 特性值
    /// 规则：必填，唯一，建议 PascalCase
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 64, IsNullable = false)]
    public string? Name { get; set; } = null;

    /// <summary>
    /// 实体描述/备注 (如: 系统用户)
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 512, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 所属模块名称 (如: Rbac)
    /// 用途：拼接生成路径 SharpFort.{ModuleName}.Application.Contracts
    /// </summary>
    [SugarColumn(ColumnName = "module_name", Length = 128, IsNullable = true)]
    public string? ModuleName { get; set; }

    /// <summary>
    /// 解决方案根命名空间 (如: Sf.Abp)
    /// 用途：生成代码中的 namespace 声明前缀
    /// </summary>
    [SugarColumn(ColumnName = "root_namespace", Length = 256, IsNullable = true)]
    public string? RootNamespace { get; set; }

    /// <summary>
    /// 生成代码时是否覆盖已有文件
    /// </summary>
    [SugarColumn(ColumnName = "is_overwrite", IsNullable = false)]
    public bool IsOverwrite { get; set; }

    /// <summary>
    /// 物理数据库表名 (如: sys_user)
    /// 来源：[SugarTable("sys_user")] 特性中的 TableName
    /// </summary>
    [SugarColumn(ColumnName = "physical_table_name", Length = 128, IsNullable = true)]
    public string? PhysicalTableName { get; set; }

    /// <summary>
    /// 所属项目名称 (从实体命名空间推断)
    /// </summary>
    [SugarColumn(ColumnName = "project_name", Length = 128, IsNullable = true)]
    public string? ProjectName { get; set; }

    /// <summary>
    /// 最后同步时间 (Code→Web 扫描时更新)
    /// </summary>
    [SugarColumn(ColumnName = "last_sync_time", IsNullable = true)]
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// 最后代码生成时间 (Web→Code 生成时更新)
    /// </summary>
    [SugarColumn(ColumnName = "last_build_time", IsNullable = true)]
    public DateTime? LastBuildTime { get; set; }

    /// <summary>
    /// 扩展属性 (ABP Feature)
    /// 场景：存储生成器的配置参数（如：是否覆盖、输出路径、命名空间前缀等）
    /// 映射：Postgres JSONB
    /// </summary>
    [SugarColumn(ColumnName = "extra_properties", IsJson = true)]
    public override ExtraPropertyDictionary ExtraProperties { get; protected set; }

    /// <summary>
    /// 字段列表 (聚合子项)
    /// 关系：1:N (一个实体注册表条目对应多个字段)
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(Field.TableId))]
    public List<Field> Fields { get; set; } = null!;

    // IsDeleted, CreationTime, CreatorId 由基类自动实现

    /// <summary>
    /// ORM 专用
    /// </summary>
    public Table()
    {
        Fields = [];
        ExtraProperties = [];
    }

    /// <summary>
    /// 创建实体注册表条目
    /// </summary>
    public Table(Guid id, string name, string? description = null) : base(id)
    {
        SetName(name);
        Description = Volo.Abp.Check.Length(description, nameof(description), 512);

        Fields = [];
        ExtraProperties = [];
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