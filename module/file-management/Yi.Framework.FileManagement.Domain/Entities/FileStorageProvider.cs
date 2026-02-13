using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Yi.Framework.FileManagement.Domain.Shared.Consts;
using Yi.Framework.FileManagement.Domain.Shared.Enums;

namespace Yi.Framework.FileManagement.Domain.Entities
{
    /// <summary>
    /// 文件存储提供者配置
    /// 管理各种存储后端的连接信息 (参考 Admin.NET SysFileProvider)
    /// </summary>
    [SugarTable(FileManagementConsts.DbTablePrefix + "storage_provider")]
    [SugarIndex($"index_IsDefault", nameof(IsDefault), OrderByType.Desc)]
    [SugarIndex($"index_IsEnabled", nameof(IsEnabled), OrderByType.Desc)]
    public class FileStorageProvider : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        #region 构造函数

        public FileStorageProvider() { }

        /// <summary>
        /// 创建存储提供者配置
        /// </summary>
        public FileStorageProvider(
            Guid id,
            string name,
            StorageProviderType providerType,
            string bucketName)
            : base(id)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(name, nameof(name));
            Volo.Abp.Check.NotNullOrWhiteSpace(bucketName, nameof(bucketName));

            Name = name;
            ProviderType = providerType;
            BucketName = bucketName;
            IsEnabled = true;
            IsDefault = false;
        }

        #endregion

        #region 核心属性

        /// <summary>
        /// 主键 (UUID7)
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public override Guid Id { get; protected set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        public Guid? TenantId { get; protected set; }

        /// <summary>
        /// 提供者显示名称
        /// </summary>
        [SugarColumn(Length = FileManagementConsts.MaxProviderNameLength)]
        public string Name { get; protected set; }

        /// <summary>
        /// 存储提供者类型
        /// </summary>
        public StorageProviderType ProviderType { get; protected set; }

        /// <summary>
        /// 存储桶名称 (Bucket)
        /// </summary>
        [SugarColumn(Length = FileManagementConsts.MaxBucketNameLength)]
        public string BucketName { get; protected set; }

        /// <summary>
        /// 访问密钥 (AccessKey / SecretId)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = FileManagementConsts.MaxKeyLength)]
        public string? AccessKey { get; protected set; }

        /// <summary>
        /// 密钥 (SecretKey)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = FileManagementConsts.MaxKeyLength)]
        public string? SecretKey { get; protected set; }

        /// <summary>
        /// 端点地址 (Endpoint)
        /// 例如 R2: https://{account-id}.r2.cloudflarestorage.com
        /// </summary>
        [SugarColumn(IsNullable = true, Length = FileManagementConsts.MaxEndpointLength)]
        public string? Endpoint { get; protected set; }

        /// <summary>
        /// 地域 (Region)
        /// 例如 R2 默认为 "auto"
        /// </summary>
        [SugarColumn(IsNullable = true, Length = FileManagementConsts.MaxProviderNameLength)]
        public string? Region { get; protected set; }

        /// <summary>
        /// 自定义域名 (CDN 域名)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = FileManagementConsts.MaxEndpointLength)]
        public string? CustomDomain { get; protected set; }

        /// <summary>
        /// 是否启用 HTTPS
        /// </summary>
        public bool IsEnableHttps { get; protected set; } = true;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; protected set; } = true;

        /// <summary>
        /// 是否默认提供者
        /// </summary>
        public bool IsDefault { get; protected set; }

        /// <summary>
        /// 排序号
        /// </summary>
        public int OrderNum { get; set; } = 100;

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(IsNullable = true, Length = FileManagementConsts.MaxRemarkLength)]
        public string? Remark { get; protected set; }

        #endregion

        #region 计算属性

        /// <summary>
        /// 获取显示名称
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public string DisplayName => $"{ProviderType}-{BucketName}";

        #endregion

        #region 业务方法

        /// <summary>
        /// 更新配置信息
        /// </summary>
        public void Update(
            string name,
            string bucketName,
            string? accessKey,
            string? secretKey,
            string? endpoint,
            string? region,
            string? customDomain,
            bool isEnableHttps,
            string? remark)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(name, nameof(name));
            Volo.Abp.Check.NotNullOrWhiteSpace(bucketName, nameof(bucketName));

            Name = name;
            BucketName = bucketName;
            AccessKey = accessKey;
            SecretKey = secretKey;
            Endpoint = endpoint;
            Region = region;
            CustomDomain = customDomain;
            IsEnableHttps = isEnableHttps;
            Remark = remark;
        }

        /// <summary>
        /// 设为默认提供者
        /// </summary>
        public void SetDefault(bool isDefault)
        {
            IsDefault = isDefault;
        }

        /// <summary>
        /// 启用/禁用
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }

        #endregion
    }
}
