using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Uow;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.SqlSugarCore.Uow
{
    /// <summary>
    /// SqlSugar数据库API实现
    /// </summary>
    public class SqlSugarDatabaseApi : IDatabaseApi
    {
        /// <summary>
        /// 数据库上下文
        /// </summary>
        public ISqlSugarDbContext DbContext { get; }

        /// <summary>
        /// 初始化SqlSugar数据库API
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        public SqlSugarDatabaseApi(ISqlSugarDbContext dbContext)
        {
            DbContext = dbContext;
        }
    }
}