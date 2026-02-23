using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpFort.CasbinRbac.Domain.Shared.Enums;

namespace SharpFort.CasbinRbac.Domain.Shared.Model
{
    public class RoleTokenInfoModel
    {
        public Guid Id { get; set; }
        public DataScope DataScope { get; set; }
    }
}
