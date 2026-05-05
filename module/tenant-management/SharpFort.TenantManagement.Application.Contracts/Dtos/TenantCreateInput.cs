
namespace SharpFort.TenantManagement.Application.Contracts.Dtos
{
    public class TenantCreateInput
    {
        public required string Name { get;  set; }
        public required string TenantConnectionString { get;  set; }

        public SqlSugar.DbType DbType { get;  set; }
    }
}
