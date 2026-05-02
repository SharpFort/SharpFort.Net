namespace SharpFort.CasbinRbac.Domain.Shared.Dtos;

public class Vue3PureRouterDto
{
    public Guid Id { get; set; }
    public int OrderNum { get; set; }
    public Guid ParentId { get; set; }
    public required string Path { get; set; }
    public required string Name { get; set; }
    public MetaPureRouterDto Meta { get; set; } = new MetaPureRouterDto();

    public string? component { get; set; }
    public List<Vue3PureRouterDto>? Children { get; set; }
}

public class MetaPureRouterDto
{
    /// <summary>菜单图标，由路由构建框架赋值</summary>
    public string Icon { get; set; } = string.Empty;
    /// <summary>菜单标题，由路由构建框架赋值</summary>
    public string Title { get; set; } = string.Empty;

    public List<string>? Roles { get; set; }

    public List<string>? Auths { get; set; }

    public string? FrameSrc { get; set; }

    public string? FrameLoading { get; set; }

    public bool? KeepAlive { get; set; }

    public bool? showLink { get; set; }
}