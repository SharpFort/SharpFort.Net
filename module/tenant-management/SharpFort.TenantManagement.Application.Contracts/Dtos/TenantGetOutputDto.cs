using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace SharpFort.TenantManagement.Application.Contracts.Dtos
{
    public class TenantGetOutputDto:EntityDto<Guid>
    {
        public required string Name { get;  set; }
        public int EntityVersion { get;  set; }

        public required string TenantConnectionString { get;  set; }

        public SqlSugar.DbType DbType { get;  set; }

        public DateTime CreationTime { get; set; }
    }
}
