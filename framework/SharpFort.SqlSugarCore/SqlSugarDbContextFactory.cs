using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Options;
using SqlSugar;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Threading;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.SqlSugarCore
{
    /// <summary>
    /// SqlSugar数据库上下文工厂类
    /// 负责创建和配置SqlSugar客户端实例
    /// </summary>
    public class SqlSugarDbContextFactory : ISqlSugarDbContext
    {
        #region Properties

        /// <summary>
        /// SqlSugar客户端实例
        /// </summary>
        public ISqlSugarClient SqlSugarClient { get; private set; }

        /// <summary>
        /// 延迟服务提供者
        /// </summary>
        private IAbpLazyServiceProvider LazyServiceProvider { get; }

        /// <summary>
        /// 租户配置包装器
        /// </summary>
        private TenantConfigurationWrapper TenantConfigurationWrapper =>
            LazyServiceProvider.LazyGetRequiredService<TenantConfigurationWrapper>();

        /// <summary>
        /// 数据库连接配置选项
        /// </summary>
        private DbConnOptions DbConnectionOptions =>
            LazyServiceProvider.LazyGetRequiredService<IOptions<DbConnOptions>>().Value;

        /// <summary>
        /// 序列化服务
        /// </summary>
        private ISerializeService SerializeService =>
            LazyServiceProvider.LazyGetRequiredService<ISerializeService>();

        /// <summary>
        /// SqlSugar上下文依赖项集合
        /// </summary>
        private IEnumerable<ISqlSugarDbContextDependencies> SqlSugarDbContextDependencies =>
            LazyServiceProvider.LazyGetRequiredService<IEnumerable<ISqlSugarDbContextDependencies>>();

        /// <summary>
        /// 连接配置缓存字典
        /// </summary>
        private static readonly ConcurrentDictionary<string, ConnectionConfig> ConnectionConfigCache = new();

        /// <summary>
        /// 数据过滤服务
        /// </summary>
        private IDataFilter DataFilter =>
            LazyServiceProvider.LazyGetRequiredService<IDataFilter>();

        /// <summary>
        /// 当前租户服务
        /// </summary>
        private ICurrentTenant CurrentTenant =>
            LazyServiceProvider.LazyGetRequiredService<ICurrentTenant>();

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="lazyServiceProvider">延迟服务提供者</param>
        public SqlSugarDbContextFactory(IAbpLazyServiceProvider lazyServiceProvider)
        {
            LazyServiceProvider = lazyServiceProvider;

            // 异步获取租户配置
            TenantConfiguration? tenantConfiguration = AsyncHelper.RunSync(() => TenantConfigurationWrapper.GetAsync());

            // 构建数据库连接配置
            ConnectionConfig connectionConfig = BuildConnectionConfig(options =>
            {
                options.ConnectionString = tenantConfiguration!.GetCurrentConnectionString();
                options.DbType = GetCurrentDbType(tenantConfiguration!.GetCurrentConnectionName());
            });

            // 使用 SqlSugarScope 构造函数回调注册全局过滤器，
            // 确保 SqlSugarScope 创建的每个内部连接实例都自动获得过滤器配置
            SqlSugarClient = new SqlSugarScope(connectionConfig, ConfigureGlobalFilters);

            // 配置数据库AOP（日志、审计、数据权限等）
            ConfigureDbAop(SqlSugarClient);
        }

        /// <summary>
        /// 注册核心全局过滤器（软删除、多租户），
        /// 通过 SqlSugarScope 构造函数回调确保每个连接实例都生效
        /// </summary>
        private void ConfigureGlobalFilters(ISqlSugarClient db)
        {
            if (DataFilter.IsEnabled<ISoftDelete>())
            {
                db.QueryFilter.AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted);
            }

            if (DataFilter.IsEnabled<IMultiTenant>())
            {
                Guid? tenantId = CurrentTenant.Id;
                db.QueryFilter.AddTableFilter<IMultiTenant>(entity => entity.TenantId == tenantId);
            }
        }

        /// <summary>
        /// 配置数据库AOP操作
        /// </summary>
        /// <param name="sqlSugarClient">SqlSugar客户端实例</param>
        protected virtual void ConfigureDbAop(ISqlSugarClient sqlSugarClient)
        {
            // 配置序列化服务
            sqlSugarClient.CurrentConnectionConfig.ConfigureExternalServices.SerializeService = SerializeService;

            // 初始化AOP事件处理器
            Action<string, SugarParameter[]>? onLogExecuting = null;
            Action<string, SugarParameter[]>? onLogExecuted = null;
            Action<object, DataFilterModel>? dataExecuting = null;
            Action<object, DataAfterModel>? dataExecuted = null;
            Action<ISqlSugarClient>? onClientConfig = null;

            // 按执行顺序聚合所有依赖项的AOP处理器
            foreach (ISqlSugarDbContextDependencies? dependency in SqlSugarDbContextDependencies.OrderBy(x => x.ExecutionOrder))
            {
                onLogExecuting += dependency.OnLogExecuting;
                onLogExecuted += dependency.OnLogExecuted;
                dataExecuting += dependency.DataExecuting;
                dataExecuted += dependency.DataExecuted;
                onClientConfig += dependency.OnSqlSugarClientConfig;
            }

            // 配置SqlSugar客户端
            onClientConfig?.Invoke(sqlSugarClient);

            // 设置AOP事件
            sqlSugarClient.Aop.OnLogExecuting = onLogExecuting;
            sqlSugarClient.Aop.OnLogExecuted = onLogExecuted;
            sqlSugarClient.Aop.DataExecuting = dataExecuting;
            sqlSugarClient.Aop.DataExecuted = dataExecuted;
        }

        /// <summary>
        /// 构建数据库连接配置
        /// </summary>
        /// <param name="configAction">配置操作委托</param>
        /// <returns>连接配置对象</returns>
        protected virtual ConnectionConfig BuildConnectionConfig(Action<ConnectionConfig>? configAction = null)
        {
            DbConnOptions dbConnOptions = DbConnectionOptions;

            // 验证数据库类型配置
            if (dbConnOptions.DbType is null)
            {
                throw new ArgumentException("未配置数据库类型(DbType)");
            }

            // 配置读写分离
            List<SlaveConnectionConfig> slaveConfigs = [];
            if (dbConnOptions.EnabledReadWrite)
            {
                if (dbConnOptions.ReadUrl is null)
                {
                    throw new ArgumentException("启用读写分离但未配置读库连接字符串");
                }

                slaveConfigs.AddRange(dbConnOptions.ReadUrl.Select(url =>
                    new SlaveConnectionConfig { ConnectionString = url }));
            }

            // 创建连接配置
            ConnectionConfig connectionConfig = new()
            {
                ConfigId = ConnectionStrings.DefaultConnectionStringName,
                DbType = dbConnOptions.DbType ?? DbType.Sqlite,
                ConnectionString = dbConnOptions.Url,
                // 针对slqite数据库将其设置为false，避免database is locked错误
                IsAutoCloseConnection = true,
                // IsAutoCloseConnection = false,
                SlaveConnectionConfigs = slaveConfigs,
                ConfigureExternalServices = CreateExternalServices(dbConnOptions)
            };

            // 应用额外配置
            configAction?.Invoke(connectionConfig);

            return connectionConfig;
        }

        /// <summary>
        /// 创建外部服务配置
        /// </summary>
        private ConfigureExternalServices CreateExternalServices(DbConnOptions dbConnOptions)
        {
            return new ConfigureExternalServices
            {
                EntityNameService = (type, entity) =>
                {
                    if (dbConnOptions.EnableUnderLine && !entity.DbTableName.Contains('_'))
                    {
                        entity.DbTableName = UtilMethods.ToUnderLine(entity.DbTableName);
                    }
                },
                EntityService = (propertyInfo, columnInfo) =>
                {
                    // 配置空值处理
                    if (new NullabilityInfoContext().Create(propertyInfo).WriteState
                        is NullabilityState.Nullable)
                    {
                        columnInfo.IsNullable = true;
                    }

                    // 处理下划线命名
                    if (dbConnOptions.EnableUnderLine && !columnInfo.IsIgnore
                        && !columnInfo.DbColumnName.Contains('_'))
                    {
                        columnInfo.DbColumnName = UtilMethods.ToUnderLine(columnInfo.DbColumnName);
                    }

                    // 聚合所有依赖项的实体服务
                    Action<PropertyInfo, EntityColumnInfo>? entityService = null;
                    foreach (ISqlSugarDbContextDependencies? dependency in SqlSugarDbContextDependencies.OrderBy(x => x.ExecutionOrder))
                    {
                        entityService += dependency.EntityService;
                    }

                    entityService?.Invoke(propertyInfo, columnInfo);
                }
            };
        }

        /// <summary>
        /// 获取当前数据库类型
        /// </summary>
        /// <param name="tenantName">租户名称</param>
        /// <returns>数据库类型</returns>
        protected virtual DbType GetCurrentDbType(string tenantName)
        {
            return tenantName == ConnectionStrings.DefaultConnectionStringName
                ? DbConnectionOptions.DbType!.Value
                : GetDbTypeFromTenantName(tenantName)
                  ?? throw new ArgumentException($"无法从租户名称{tenantName}中解析数据库类型");
        }

        /// <summary>
        /// 从租户名称解析数据库类型
        /// 格式：TenantName@DbType
        /// </summary>
        private static DbType? GetDbTypeFromTenantName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            int atIndex = name.LastIndexOf('@');
            if (atIndex == -1 || atIndex == name.Length - 1)
            {
                return null;
            }

            string dbTypeString = name[(atIndex + 1)..];
            return Enum.TryParse(dbTypeString, out DbType dbType)
                ? dbType
                : throw new ArgumentException($"不支持的数据库类型: {dbTypeString}");
        }

        /// <summary>
        /// 备份数据库
        /// </summary>
        public virtual void BackupDataBase()
        {
            const string backupDirectory = "database_backup";
            string fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{SqlSugarClient.Ado.Connection.Database}";

            Directory.CreateDirectory(backupDirectory);

            switch (DbConnectionOptions.DbType)
            {
                case DbType.MySql:
                    SqlSugarClient.DbMaintenance.BackupDataBase(
                        SqlSugarClient.Ado.Connection.Database,
                        Path.Combine(backupDirectory, $"{fileName}.sql"));
                    break;

                case DbType.Sqlite:
                    SqlSugarClient.DbMaintenance.BackupDataBase(
                        null,
                        $"{fileName}.db");
                    break;

                case DbType.SqlServer:
                    SqlSugarClient.DbMaintenance.BackupDataBase(
                        SqlSugarClient.Ado.Connection.Database,
                        Path.Combine(backupDirectory, $"{fileName}.bak"));
                    break;
                case DbType.Oracle:
                    break;
                case DbType.PostgreSQL:
                    break;
                case DbType.Dm:
                    break;
                case DbType.Kdbndp:
                    break;
                case DbType.Oscar:
                    break;
                case DbType.MySqlConnector:
                    break;
                case DbType.Access:
                    break;
                case DbType.OpenGauss:
                    break;
                case DbType.QuestDB:
                    break;
                case DbType.HG:
                    break;
                case DbType.ClickHouse:
                    break;
                case DbType.GBase:
                    break;
                case DbType.Odbc:
                    break;
                case DbType.OceanBaseForOracle:
                    break;
                case DbType.TDengine:
                    break;
                case DbType.GaussDB:
                    break;
                case DbType.OceanBase:
                    break;
                case DbType.Tidb:
                    break;
                case DbType.Vastbase:
                    break;
                case DbType.PolarDB:
                    break;
                case DbType.Doris:
                    break;
                case DbType.Xugu:
                    break;
                case DbType.GoldenDB:
                    break;
                case DbType.TDSQLForPGODBC:
                    break;
                case DbType.TDSQL:
                    break;
                case DbType.HANA:
                    break;
                case DbType.DB2:
                    break;
                case DbType.GaussDBNative:
                    break;
                case DbType.DuckDB:
                    break;
                case DbType.MongoDb:
                    break;
                case DbType.Custom:
                    break;
                case null:
                    break;
                default:
                    throw new NotImplementedException($"数据库类型 {DbConnectionOptions.DbType} 的备份操作尚未实现");
            }
        }
    }
}