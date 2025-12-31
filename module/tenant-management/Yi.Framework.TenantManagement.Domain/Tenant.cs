using JetBrains.Annotations;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.TenantManagement;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.TenantManagement.Domain
{
    [SugarTable("sys_tenant")]
    [DefaultTenantTable]
    public class Tenant : FullAuditedAggregateRoot<Guid>, IHasEntityVersion
    {
        public Tenant()
        {

        }
        protected internal Tenant(Guid id, [NotNull] string name)
    : base(id)
        {
            SetName(name);
        }

        [SugarColumn(IsPrimaryKey = true)]
        public override Guid Id { get; protected set; }
        public virtual string Name { get; protected set; }
        public int EntityVersion { get; protected set; }

        public string TenantConnectionString { get; protected set; }

        public DbType DbType { get; protected set; }

        [SugarColumn(IsIgnore = true)]
        public override ExtraPropertyDictionary ExtraProperties { get => base.ExtraProperties; protected set => base.ExtraProperties = value; }
        public virtual void SetConnectionString(DbType dbType, string connectionString)
        {
            DbType = dbType;
            TenantConnectionString = connectionString;
        }

        protected internal virtual void SetName([NotNull] string name)
        {
            Name = Volo.Abp.Check.NotNullOrWhiteSpace(name, nameof(name), TenantConsts.MaxNameLength);
        }

    }
}
