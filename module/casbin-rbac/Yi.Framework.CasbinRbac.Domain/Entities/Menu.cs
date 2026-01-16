using NUglify.Helpers;
using SqlSugar;
using System.Web;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Yi.Framework.Core.Data;
using Yi.Framework.Core.Helper;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.CasbinRbac.Domain.Shared.Dtos;
using Yi.Framework.CasbinRbac.Domain.Shared.Enums;

namespace Yi.Framework.CasbinRbac.Domain.Entities
{
    /// <summary>
    /// 菜单/权限聚合根
    /// 核心系统资源，采用树形结构设计
    /// </summary>
    [SugarTable("casbin_sys_menu")]
    // 索引1：加速父子级递归查询
    [SugarIndex($"index_{nameof(ParentId)}", nameof(ParentId), OrderByType.Asc)]
    // 索引2：加速权限验证查询 (如: system:user:list)
    [SugarIndex($"index_{nameof(PermissionCode)}", nameof(PermissionCode), OrderByType.Asc)]
    public class Menu : FullAuditedAggregateRoot<Guid>, IOrderNum, IState
    {
        #region 构造函数

        /// <summary>
        /// ORM 专用
        /// </summary>
        public Menu() { }

        /// <summary>
        /// 创建菜单
        /// </summary>
        public Menu(
            Guid id,
            string menuName,
            string? router,
            MenuType menuType,
            Guid parentId,
            string? permissionCode = null,
            string? menuIcon = null,
            string? component= null,
            int orderNum = 0)
            : base(id)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(menuName, nameof(menuName));

            MenuName = menuName;
            Router = router;
            MenuType = menuType;
            ParentId = parentId;
            PermissionCode = permissionCode;
            Component = component;
            MenuIcon = menuIcon;
            OrderNum = orderNum;

            // 默认值
            State = true;
            IsShow = true;
            IsCache = true;
            IsLink = false;
            IsDeleted= false;
            MenuSource = MenuSource.Ruoyi; // 默认来源，可调整
        }

        #endregion

        #region 核心属性

        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public override Guid Id { get; protected set; }

        /// <summary>
        /// 菜单名称 (Title)
        /// </summary>
        [SugarColumn(Length = 64)]
        public string MenuName { get; protected set; }

        /// <summary>
        /// 菜单类型 (目录/菜单/按钮)
        /// </summary>
        public MenuType MenuType { get; protected set; }

        /// <summary>
        /// 权限标识
        /// 如: system:user:add
        /// </summary>
        [SugarColumn(Length = 128, IsNullable = true)]
        public string? PermissionCode { get; protected set; }

        /// <summary>
        /// 父级ID
        /// 根节点为 Guid.Empty
        /// </summary>
        public Guid ParentId { get; protected set; }

        /// <summary>
        /// 路由地址 (Path)
        /// </summary>
        [SugarColumn(Length = 255, IsNullable = true)]
        public string? Router { get; protected set; }
        
        /// <summary>
        /// API 路径 (用于 Casbin 鉴权)
        /// 例如: /api/system/user
        /// </summary>
        [SugarColumn(Length = 255, IsNullable = true)]
        public string? ApiUrl { get; set; }

        /// <summary>
        /// API 方法 (用于 Casbin 鉴权)
        /// 例如: GET, POST, PUT, DELETE
        /// </summary>
        [SugarColumn(Length = 10, IsNullable = true)]
        public string? ApiMethod { get; set; }

        /// <summary>
        /// 路由名称 (Name)
        /// Vue 路由的唯一标识，建议英文
        /// </summary>
        [SugarColumn(Length = 128, IsNullable = true)]
        public string? RouterName { get; set; }

        /// <summary>
        /// 组件路径 (Component)
        /// 如: system/user/index
        /// </summary>
        [SugarColumn(Length = 255, IsNullable = true)]
        public string? Component { get; set; }

        /// <summary>
        /// 菜单图标
        /// </summary>
        [SugarColumn(Length = 128, IsNullable = true)]
        public string? MenuIcon { get; set; }

        /// <summary>
        /// 路由参数 (Query)
        /// </summary>
        [SugarColumn(Length = 255, IsNullable = true)]
        public string? Query { get; set; }

        #endregion

        #region 状态控制

        /// <summary>
        /// 是否外链
        /// </summary>
        public bool IsLink { get; set; }

        /// <summary>
        /// 是否缓存 (KeepAlive)
        /// </summary>
        public bool IsCache { get; set; }

        /// <summary>
        /// 是否显示 (Hidden)
        /// </summary>
        public bool IsShow { get; set; }

        /// <summary>
        /// 状态 (IState)
        /// True: 启用, False: 禁用
        /// </summary>
        public bool State { get; set; }

        /// <summary>
        /// 排序 (IOrderNum)
        /// </summary>
        public int OrderNum { get; set; }

        /// <summary>
        /// 菜单来源
        /// 用于兼容不同前端框架的路由生成逻辑
        /// </summary>
        public MenuSource MenuSource { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true)]
        public string? Remark { get; set; }

        #endregion

        #region 树形导航 (SqlSugar)

        /// <summary>
        /// 子菜单集合
        /// [Navigate] 用于递归加载树
        /// </summary>
        [Navigate(NavigateType.OneToMany, nameof(ParentId))]
        public List<Menu> Children { get; set; }

        #endregion

        #region 业务方法

        /// <summary>
        /// 更新菜单基本信息
        /// </summary>
        public void Update(string menuName, string? router, string? component, string? permissionCode, string? menuIcon, int orderNum, bool isShow, bool state)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(menuName, nameof(menuName));

            MenuName = menuName;
            Router = router;
            Component = component;
            PermissionCode = permissionCode;
            MenuIcon = menuIcon;
            OrderNum = orderNum;
            IsShow = isShow;
            State = state;
        }

        /// <summary>
        /// 变更父节点
        /// </summary>
        public void ChangeParent(Guid newParentId)
        {
            if (newParentId == Id)
            {
                throw new BusinessException("Rbac:Menu:RecursionError", "父节点不能是自己");
            }
            ParentId = newParentId;
        }

        #endregion
    }
}

/// <summary>
/// 实体扩展
/// </summary>
public static class MenuEntityExtensions
{
    /// <summary>
    /// 构建vue3路由
    /// </summary>
    /// <param name="menus"></param>
    /// <returns></returns>
    public static List<Vue3RouterDto> Vue3RuoYiRouterBuild(this List<Menu> menus)
    {
        menus = menus
            .Where(m => m.State == true)
            .Where(m => m.MenuType != MenuType.Component)
            .Where(m => m.MenuSource == MenuSource.Ruoyi)
            .ToList();
        List<Vue3RouterDto> routers = new();
        foreach (var m in menus)
        {
            var r = new Vue3RouterDto();
            r.OrderNum = m.OrderNum;
            var routerName = m.Router?.Split("/").LastOrDefault();
            r.Id = m.Id;
            r.ParentId = m.ParentId;

            //开头大写
            r.Name = routerName?.First().ToString().ToUpper() + routerName?.Substring(1);
            r.Path = m.Router!;
            r.Hidden = !m.IsShow;


            if (m.MenuType == MenuType.Catalogue)
            {
                r.Redirect = "noRedirect";
                r.AlwaysShow = true;

                //判断是否为最顶层的路由
                if (Guid.Empty == m.ParentId)
                {
                    r.Component = "Layout";
                }
                else
                {
                    r.Component = "ParentView";
                }
            }

            if (m.MenuType == MenuType.Menu)
            {
                r.Redirect = "noRedirect";
                r.AlwaysShow = true;
                r.Component = m.Component!;
                r.AlwaysShow = false;
            }

            r.Meta = new Meta
            {
                Title = m.MenuName!,
                Icon = m.MenuIcon!,
                NoCache = !m.IsCache
            };
            if (m.IsLink)
            {
                r.Meta.link = m.Router!;
                r.AlwaysShow = false;
            }

            routers.Add(r);
        }

        return TreeHelper.SetTree(routers);
    }


    /// <summary>
    /// 构建vue3  pure路由
    /// </summary>
    /// <param name="menus"></param>
    /// <returns></returns>
    public static List<Vue3PureRouterDto> Vue3PureRouterBuild(this List<Menu> menus)
    {
        //pure的菜单为树形
        var allRouters = menus
            .Where(m => m.State == true)
            .Where(m => m.MenuType != MenuType.Component)
            .Where(m => m.MenuSource == MenuSource.Pure)
            .Select(m => new Vue3PureRouterDto
            {
                Path = m.Router.StartsWith("/") ? m.Router : "/" + m.Router,
                Name = m.IsLink == true ? "Link" : m.RouterName,
                component = m.Component,
                Meta = new MetaPureRouterDto()
                {
                    showLink = m.IsShow,
                    FrameSrc = m.IsLink == true ? m.Router : null,
                    Auths = new List<string>() { m.PermissionCode },
                    Icon = m.MenuIcon,
                    Title = m.MenuName,

                },
                Children = null,
                Id = m.Id,
                ParentId = m.ParentId
            })
            .ToList();


        var routerDic = allRouters.GroupBy(x => x.ParentId).ToDictionary(x => x.Key, y => y.ToList());
        //根路由
        if (!routerDic.TryGetValue(Guid.Empty, out var rootRouters))
        {
            return new List<Vue3PureRouterDto>();
        }
        Stack<Vue3PureRouterDto> stack = new Stack<Vue3PureRouterDto>(rootRouters);
        while (stack.Count > 0)
        {
            var currentRouter = stack.Pop();
            if (routerDic.TryGetValue(currentRouter.Id, out var items))
            {
                currentRouter.Children = items;
                items?.ForEach(x => stack.Push(x));
            }
        }

        return rootRouters;
    }
}
