using Volo.Abp.Uow;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.SqlSugarCore.Uow
{
    /// <summary>
    /// SqlSugar数据库API实现
    /// </summary>
    /// <remarks>
    /// 初始化SqlSugar数据库API
    /// </remarks>
    /// <param name="dbContext">数据库上下文</param>
    public class SqlSugarDatabaseApi(ISqlSugarDbContext dbContext) : IDatabaseApi
    {
        /// <summary>
        /// 数据库上下文
        /// </summary>
        public ISqlSugarDbContext DbContext { get; } = dbContext;
    }
}