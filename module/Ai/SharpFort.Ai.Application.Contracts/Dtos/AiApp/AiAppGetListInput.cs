using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace SharpFort.Ai.Application.Contracts.Dtos.AiApp;

/// <summary>
/// AI应用分页查询输入
/// </summary>
public class AiAppGetListInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string? Keyword { get; set; }
}
