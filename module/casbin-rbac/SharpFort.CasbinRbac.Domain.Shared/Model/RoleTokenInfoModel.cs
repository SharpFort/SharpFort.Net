using SharpFort.CasbinRbac.Domain.Shared.Enums;

namespace SharpFort.CasbinRbac.Domain.Shared.Model
{
    public class RoleTokenInfoModel
    {
        public Guid Id { get; set; }
        public DataScope DataScope { get; set; }
    }
}
