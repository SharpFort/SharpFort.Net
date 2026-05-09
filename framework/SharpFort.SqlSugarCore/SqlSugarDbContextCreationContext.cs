using System.Data.Common;

namespace SharpFort.SqlSugarCore;

/// <summary>
/// SqlSugar数据库上下文创建上下文
/// </summary>
/// <remarks>
/// 构造函数
/// </remarks>
public class SqlSugarDbContextCreationContext(
    string connectionStringName,
    string connectionString)
{
    private static readonly AsyncLocal<SqlSugarDbContextCreationContext> CurrentContextHolder =
        new();

    /// <summary>
    /// 获取当前上下文
    /// </summary>
    public static SqlSugarDbContextCreationContext Current => CurrentContextHolder.Value!;

    /// <summary>
    /// 连接字符串名称
    /// </summary>
    public string ConnectionStringName { get; } = connectionStringName;

    /// <summary>
    /// 连接字符串
    /// </summary>
    public string ConnectionString { get; } = connectionString;

    /// <summary>
    /// 现有数据库连接
    /// </summary>
    public DbConnection? ExistingConnection { get; internal set; }

    /// <summary>
    /// 使用指定的上下文
    /// </summary>
    public static IDisposable Use(SqlSugarDbContextCreationContext context)
    {
        var previousContext = Current;
        CurrentContextHolder.Value = context;
        return new DisposeAction(() => CurrentContextHolder.Value = previousContext);
    }
}
