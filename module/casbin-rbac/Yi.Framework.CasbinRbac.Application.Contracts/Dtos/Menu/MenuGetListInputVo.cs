using Volo.Abp.Application.Dtos;
using Yi.Framework.CasbinRbac.Domain.Shared.Enums;

namespace Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Menu
{
    public class MenuGetListInputVo : PagedAndSortedResultRequestDto
    {

        public bool? State { get; set; }
        public string? MenuName { get; set; }
        public MenuSource MenuSource { get; set; } = MenuSource.Ruoyi;
    }
}
