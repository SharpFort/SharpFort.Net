using Volo.Abp.Application.Dtos;

namespace SharpFort.CodeGen.Application.Contracts.Dtos.Table
{
    public class TableGetListInput : PagedAndSortedResultRequestDto
    {
        /// <summary>
        /// 实体名称模糊筛选 (如: System)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 所属模块精确筛选 (如: Rbac)，值来自搜索栏下拉框
        /// </summary>
        public string? ModuleName { get; set; }

        /// <summary>
        /// 所属项目精确筛选 (如: Rbac)，值来自搜索栏下拉框
        /// </summary>
        public string? ProjectName { get; set; }
    }
}
