using Volo.Abp.Application.Dtos;

namespace SharpFort.CodeGen.Application.Contracts.Dtos.Table
{
    public class TableGetListInput : PagedAndSortedResultRequestDto
    {
        /// <summary>
        /// 表名模糊筛选
        /// </summary>
        public string? Name { get; set; }
    }
}
