namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Dept;

/// <summary>Dept输入创建对象</summary>
public class DeptCreateInputVo
{
    public bool State { get; set; }
    public required string DeptName { get; set; }
    public required string DeptCode { get; set; }
    public string? Leader { get; set; }
    public Guid? ParentId { get; set; }
    public string? Remark { get; set; }
}
