using SqlSugar;
using Volo.Abp.Domain.Entities;
using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Yi.Framework.CodeGen.Domain.Entities;

/// <summary>
/// 代码生成模板聚合根
/// 领域定义：定义用于生成代码的模板内容（如 .cs, .vue, .js 的模板）及生成路径规则
/// </summary>
[SugarTable("gen_template")]
// 索引：模板名称必须唯一，便于前端选择和识别
[SugarIndex($"index_gen_template_name", nameof(Name), OrderByType.Asc, IsUnique = true)]
public class Template : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// 主键
    /// </summary>
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public override Guid Id { get; protected set; }

    /// <summary>
    /// 模板名称
    /// 示例：Service.cs, Index.vue
    /// 规则：必填，唯一，长度 64
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 64, IsNullable = false)]
    public string Name { get; private set; }

    /// <summary>
    /// 建议生成路径
    /// 示例：aspnet-core/src/Domain/Entities/
    /// 规则：必填，长度 256
    /// </summary>
    [SugarColumn(ColumnName = "build_path", Length = 256, IsNullable = false)]
    public string BuildPath { get; private set; }

    /// <summary>
    /// 模板内容
    /// 规则：大文本，存储 Razor/Scriban 脚本
    /// </summary>
    [SugarColumn(ColumnName = "content", ColumnDataType = "text", IsNullable = false)]
    public string Content { get; private set; }

    /// <summary>
    /// 备注/说明
    /// </summary>
    [SugarColumn(ColumnName = "remarks", Length = 512, IsNullable = true)]
    public string? Remarks { get; private set; }

    // IsDeleted, CreationTime, CreatorId, LastModificationTime 由基类自动实现

    /// <summary>
    /// ORM 专用
    /// </summary>
    public Template() { }

    /// <summary>
    /// 创建模板
    /// </summary>
    /// <param name="id">ID</param>
    /// <param name="name">模板名</param>
    /// <param name="buildPath">生成路径</param>
    /// <param name="content">模板代码内容</param>
    public Template(Guid id, string name, string buildPath, string content) : base(id)
    {
        SetName(name);
        SetBuildPath(buildPath);
        SetContent(content);
    }

    #region 领域行为

    /// <summary>
    /// 更新模板基本信息
    /// </summary>
    public void UpdateBasic(string name, string buildPath, string? remarks)
    {
        SetName(name);
        SetBuildPath(buildPath);
        Remarks = Volo.Abp.Check.Length(remarks, nameof(remarks), 512);
    }

    /// <summary>
    /// 更新模板内容
    /// </summary>
    public void SetContent(string content)
    {
        // 模板内容不能为空，否则生成文件无意义
        Content = Volo.Abp.Check.NotNullOrWhiteSpace(content, nameof(content));
    }

    private void SetName(string name)
    {
        Name = Volo.Abp.Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 64);
    }

    private void SetBuildPath(string buildPath)
    {
        // 路径允许为空字符串吗？通常不允许，但视业务而定。此处假定必须有建议路径。
        BuildPath = Volo.Abp.Check.NotNullOrWhiteSpace(buildPath, nameof(buildPath), maxLength: 256);
    }

    #endregion
}