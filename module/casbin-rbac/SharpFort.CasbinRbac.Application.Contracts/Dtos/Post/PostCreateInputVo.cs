namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Post;

/// <summary>Post输入创建对象</summary>
public class PostCreateInputVo
{
    public bool? State { get; set; }
    public required string PostCode { get; set; }
    public required string PostName { get; set; }
    public string? Remark { get; set; }
}
