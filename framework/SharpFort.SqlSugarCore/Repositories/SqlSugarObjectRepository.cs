using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.SqlSugarCore.Repositories
{
    public class SqlSugarObjectRepository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
    {
#pragma warning disable CA1707 // 框架约定命名，与接口 ISqlSugarRepository._Db 保持一致
        public ISqlSugarClient _Db => GetDbContextAsync().Result;
#pragma warning restore CA1707
        private readonly ISugarDbContextProvider<ISqlSugarDbContext> _sugarDbContextProvider = null!;
        /// <summary>
        /// 获取DB
        /// </summary>
        /// <returns></returns>
        public virtual async Task<ISqlSugarClient> GetDbContextAsync()
        {

            var db = (await _sugarDbContextProvider.GetDbContextAsync()).SqlSugarClient;
            //await Console.Out.WriteLineAsync("获取的id：" + db.ContextID);
            return db;
        }

        public IAsyncQueryableExecuter AsyncExecuter => throw new NotImplementedException();

        public bool? IsChangeTrackingEnabled => throw new NotImplementedException();

        /// <summary>
        /// 实体名称
        /// </summary>
#pragma warning disable CS8767 // ABP IRepository 接口 setter 参数可空性不匹配
        public string EntityName { get; set; } = typeof(TEntity).Name;
#pragma warning restore CS8767

        /// <summary>
        /// 提供程序名称
        /// </summary>
        public string ProviderName => "SqlSugar";

        public Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteDirectAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = true, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = true, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<long> GetCountAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<TEntity>> GetListAsync(bool includeDetails = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<TEntity>> GetPagedListAsync(int skipCount, int maxResultCount, string sorting, bool includeDetails = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IQueryable<TEntity>> GetQueryableAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<TEntity> InsertAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            await (await GetDbContextAsync()).InsertableByObject(entity).ExecuteCommandAsync();
            return entity;
        }

        public Task InsertManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TEntity> WithDetails()
        {
            throw new NotImplementedException();
        }

        public IQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors)
        {
            throw new NotImplementedException();
        }

        public Task<IQueryable<TEntity>> WithDetailsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IQueryable<TEntity>> WithDetailsAsync(params Expression<Func<TEntity, object>>[] propertySelectors)
        {
            throw new NotImplementedException();
        }
    }

#pragma warning disable CS8767 // ABP IRepository 接口 setter 参数可空性不匹配（继承自基类）
    public class SqlSugarObjectRepository<TEntity, TKey> : SqlSugarObjectRepository<TEntity>,  IRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>
    {
        public Task DeleteAsync(TKey id, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteManyAsync(IEnumerable<TKey> ids, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity?> FindAsync(TKey id, bool includeDetails = true, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> GetAsync(TKey id, bool includeDetails = true, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
