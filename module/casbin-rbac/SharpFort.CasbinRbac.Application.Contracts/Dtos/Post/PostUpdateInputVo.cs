namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Post;

public class PostUpdateInputVo
{
    public bool? State { get; set; }
    public int OrderNum { get; set; }
    public required string PostCode { get; set; }
    public required string PostName { get; set; }
    public string? Remark { get; set; }
}
