#nullable disable
// CS8767: ITreeModel<T> 接口来自第三方框架，未标注 nullable，禁用文件级 nullable 以对齐接口签名
using static SharpFort.Core.Helper.TreeHelper;

namespace SharpFort.CasbinRbac.Domain.Shared.Dtos;

public class Vue3RouterDto : ITreeModel<Vue3RouterDto>
{
    public Guid Id { get; set; }
    public Guid ParentId { get; set; }
    public int OrderNum { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool Hidden { get; set; }
    public string Redirect { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public bool AlwaysShow { get; set; }
    public Meta Meta { get; set; } = new Meta();

    /// <summary>子节点列表，由树形构建逻辑填充，不由构造函数初始化</summary>
    public List<Vue3RouterDto> Children { get; set; } = [];
}

public class Meta
{
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool NoCache { get; set; }
    public string link { get; set; } = string.Empty;
}
