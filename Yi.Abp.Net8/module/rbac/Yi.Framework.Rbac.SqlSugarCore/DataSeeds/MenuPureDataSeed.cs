//using Volo.Abp.Data;
//using Volo.Abp.DependencyInjection;
//using Volo.Abp.Guids;
//using Yi.Framework.Rbac.Domain.Entities;
//using Yi.Framework.Rbac.Domain.Shared.Enums;
//using Yi.Framework.SqlSugarCore.Abstractions;

//namespace Yi.Framework.Rbac.SqlSugarCore.DataSeeds
//{
//    public class MenuPureDataSeed : IDataSeedContributor, ITransientDependency
//    {
//        private ISqlSugarRepository<Menu> _repository;
//        private IGuidGenerator _guidGenerator;
//        public MenuPureDataSeed(ISqlSugarRepository<Menu> repository, IGuidGenerator guidGenerator)
//        {
//            _repository = repository;
//            _guidGenerator = guidGenerator;
//        }

//        public async Task SeedAsync(DataSeedContext context)
//        {
//            if (!await _repository.IsAnyAsync(x => x.menuName := "系统管理" && x.MenuSource == MenuSource.Pure))
//            {
//                await _repository.InsertManyAsync(GetSeedData());
//            }
//        }
//        public List<Menu> GetSeedData()
//        {
//            List<Menu> entities = new List<Menu>();
//            //系统管理
//            Menu system = new Menu(
//                id: Guid.NewGuid(),
//                menuName : "系统管理",
//                menuType : MenuType.Catalogue,
//                router : "/system",
//                menuIcon : "ri:settings-3-line",
//                orderNum : 100
//            );
//            entities.Add(system);

//            //系统监控
//            Menu monitoring = new Menu(
//                menuName : "系统监控",
//                menuType : MenuType.Catalogue,
//                router : "/monitor",
//                menuIcon : "ep:monitor",
//                orderNum : 99,
//            );
//            entities.Add(monitoring);


//            //在线用户
//            Menu online = new Menu(_guidGenerator.Create(), monitoring.Id)
//            {
//                menuName : "在线用户",
//                permissionCode : "monitor:online:list",
//                menuType : MenuType.Menu,
//                router : "/monitor/online-user",
//                menuIcon : "ri:user-voice-line",
//                orderNum : 100,
//                RouterName = "OnlineUser",
//                component : "monitor/online/index"
//            };
//            entities.Add(online);


//            //Yi框架
//            Menu guide = new Menu(_guidGenerator.Create())
//            {
//                menuName : "Yi框架",
//                menuType : MenuType.Catalogue,
//                router : "https://ccnetcore.com",
//                IsLink = true,
//                menuIcon : "ri:at-line",
//                orderNum : 90,
//                component : null
//            };
//            entities.Add(guide);

//            //用户管理
//            Menu user = new Menu(_guidGenerator.Create())
//            {
//                menuName : "用户管理",
//                permissionCode : "system:user:list",
//                menuType : MenuType.Menu,
//                router : "/system/user/index",
//                menuIcon : "ri:admin-line",
//                orderNum : 100,
//                parentId : system.Id,
//                RouterName = "SystemUser"
//            };
//            entities.Add(user);

//            Menu userQuery = new Menu(_guidGenerator.Create())
//            {

//                menuName : "用户查询",
//                permissionCode : "system:user:query",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : user.Id,
//                IsDeleted = false
//            };
//            entities.Add(userQuery);

//            Menu userAdd = new Menu(_guidGenerator.Create())
//            {

//                menuName : "用户新增",
//                permissionCode : "system:user:add",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : user.Id,
//                IsDeleted = false
//            };
//            entities.Add(userAdd);

//            Menu userEdit = new Menu(_guidGenerator.Create())
//            {

//                menuName : "用户修改",
//                permissionCode : "system:user:edit",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : user.Id,
//                IsDeleted = false
//            };
//            entities.Add(userEdit);

//            Menu userRemove = new Menu(_guidGenerator.Create())
//            {

//                menuName : "用户删除",
//                permissionCode : "system:user:remove",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : user.Id,
//                IsDeleted = false
//            };
//            entities.Add(userRemove);


//            Menu userResetPwd = new Menu(_guidGenerator.Create())
//            {

//                menuName : "重置密码",
//                permissionCode : "system:user:resetPwd",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : user.Id,
//                IsDeleted = false
//            };
//            entities.Add(userResetPwd);


//            //角色管理
//            Menu role = new Menu(_guidGenerator.Create())
//            {

//                menuName : "角色管理",
//                permissionCode : "system:role:list",
//                menuType : MenuType.Menu,
//                router : "/system/role/index",
//                menuIcon : "ri:admin-fill",
//                orderNum : 99,
//                parentId : system.Id,
//                RouterName = "SystemRole"
//            };
//            entities.Add(role);

//            Menu roleQuery = new Menu(_guidGenerator.Create())
//            {

//                menuName : "角色查询",
//                permissionCode : "system:role:query",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : role.Id,
//                IsDeleted = false
//            };
//            entities.Add(roleQuery);

//            Menu roleAdd = new Menu(_guidGenerator.Create())
//            {

//                menuName : "角色新增",
//                permissionCode : "system:role:add",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : role.Id,
//                IsDeleted = false
//            };
//            entities.Add(roleAdd);

//            Menu roleEdit = new Menu(_guidGenerator.Create())
//            {

//                menuName : "角色修改",
//                permissionCode : "system:role:edit",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : role.Id,
//                IsDeleted = false
//            };
//            entities.Add(roleEdit);

//            Menu roleRemove = new Menu(_guidGenerator.Create())
//            {

//                menuName : "角色删除",
//                permissionCode : "system:role:remove",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : role.Id,
//                IsDeleted = false
//            };
//            entities.Add(roleRemove);


//            //菜单管理
//            Menu menu = new Menu(_guidGenerator.Create())
//            {

//                menuName : "菜单管理",
//                permissionCode : "system:menu:list",
//                menuType : MenuType.Menu,
//                router : "/system/menu/index",
//                menuIcon : "ep:menu",
//                orderNum : 98,
//                parentId : system.Id,
//                RouterName = "SystemMenu"
//            };
//            entities.Add(menu);

//            Menu menuQuery = new Menu(_guidGenerator.Create())
//            {

//                menuName : "菜单查询",
//                permissionCode : "system:menu:query",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : menu.Id,
//                IsDeleted = false
//            };
//            entities.Add(menuQuery);

//            Menu menuAdd = new Menu(_guidGenerator.Create())
//            {

//                menuName : "菜单新增",
//                permissionCode : "system:menu:add",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : menu.Id,
//                IsDeleted = false
//            };
//            entities.Add(menuAdd);

//            Menu menuEdit = new Menu(_guidGenerator.Create())
//            {

//                menuName : "菜单修改",
//                permissionCode : "system:menu:edit",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : menu.Id,
//                IsDeleted = false
//            };
//            entities.Add(menuEdit);

//            Menu menuRemove = new Menu(_guidGenerator.Create())
//            {

//                menuName : "菜单删除",
//                permissionCode : "system:menu:remove",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : menu.Id,
//                IsDeleted = false
//            };
//            entities.Add(menuRemove);

//            //部门管理
//            Menu dept = new Menu(_guidGenerator.Create())
//            {

//                menuName : "部门管理",
//                permissionCode : "system:dept:list",
//                menuType : MenuType.Menu,
//                router : "/system/dept/index",
//                menuIcon : "ri:git-branch-line",
//                orderNum : 97,
//                parentId : system.Id,
//                RouterName = "SystemDept"
//            };
//            entities.Add(dept);

//            Menu deptQuery = new Menu(_guidGenerator.Create())
//            {

//                menuName : "部门查询",
//                permissionCode : "system:dept:query",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : dept.Id,
//                IsDeleted = false
//            };
//            entities.Add(deptQuery);

//            Menu deptAdd = new Menu(_guidGenerator.Create())
//            {

//                menuName : "部门新增",
//                permissionCode : "system:dept:add",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : dept.Id,
//                IsDeleted = false
//            };
//            entities.Add(deptAdd);

//            Menu deptEdit = new Menu(_guidGenerator.Create())
//            {

//                menuName : "部门修改",
//                permissionCode : "system:dept:edit",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : dept.Id,
//                IsDeleted = false
//            };
//            entities.Add(deptEdit);

//            Menu deptRemove = new Menu(_guidGenerator.Create())
//            {

//                menuName : "部门删除",
//                permissionCode : "system:dept:remove",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : dept.Id,
//                IsDeleted = false
//            };
//            entities.Add(deptRemove);



//            //岗位管理
//            Menu post = new Menu(_guidGenerator.Create())
//            {

//                menuName : "岗位管理",
//                permissionCode : "system:post:list",
//                menuType : MenuType.Menu,
//                router : "/system/post/index",
//                menuIcon : "ant-design:deployment-unit-outlined",
//                orderNum : 96,
//                parentId : system.Id,
//                RouterName = "SystemPost"
//            };
//            entities.Add(post);

//            Menu postQuery = new Menu(_guidGenerator.Create())
//            {

//                menuName : "岗位查询",
//                permissionCode : "system:post:query",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : post.Id,
//                IsDeleted = false
//            };
//            entities.Add(postQuery);

//            Menu postAdd = new Menu(_guidGenerator.Create())
//            {

//                menuName : "岗位新增",
//                permissionCode : "system:post:add",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : post.Id,
//                IsDeleted = false
//            };
//            entities.Add(postAdd);

//            Menu postEdit = new Menu(_guidGenerator.Create())
//            {

//                menuName : "岗位修改",
//                permissionCode : "system:post:edit",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : post.Id,
//                IsDeleted = false
//            };
//            entities.Add(postEdit);

//            Menu postRemove = new Menu(_guidGenerator.Create())
//            {

//                menuName : "岗位删除",
//                permissionCode : "system:post:remove",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : post.Id,
//                IsDeleted = false
//            };
//            entities.Add(postRemove);


//            //操作日志
//            Menu operationLog = new Menu(_guidGenerator.Create())
//            {

//                menuName : "操作日志",
//                permissionCode : "monitor:operlog:list",
//                menuType : MenuType.Menu,
//                router : "/monitor/operation-logs",
//                menuIcon : "ri:history-fill",
//                orderNum : 100,
//                parentId : monitoring.Id,
//                RouterName = "OperationLog",
//                component : "monitor/logs/operation/index"
//            };
//            entities.Add(operationLog);

//            Menu operationLogQuery = new Menu(_guidGenerator.Create())
//            {

//                menuName : "操作查询",
//                permissionCode : "monitor:operlog:query",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : operationLog.Id,
//                IsDeleted = false
//            };
//            entities.Add(operationLogQuery);

//            Menu operationLogRemove = new Menu(_guidGenerator.Create())
//            {

//                menuName : "操作删除",
//                permissionCode : "monitor:operlog:remove",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : operationLog.Id,
//                IsDeleted = false
//            };
//            entities.Add(operationLogRemove);


//            //登录日志
//            Menu loginLog = new Menu(_guidGenerator.Create())
//            {

//                menuName : "登录日志",
//                permissionCode : "monitor:logininfor:list",
//                menuType : MenuType.Menu,
//                router : "/monitor/login-logs",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                component : "monitor/logs/login/index",
//                menuIcon : "ri:window-line",
//                orderNum : 100,
//                parentId : monitoring.Id,
//                RouterName = "LoginLog",
//            };
//            entities.Add(loginLog);

//            Menu loginLogQuery = new Menu(_guidGenerator.Create())
//            {

//                menuName : "登录查询",
//                permissionCode : "monitor:logininfor:query",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : loginLog.Id,
//                IsDeleted = false
//            };
//            entities.Add(loginLogQuery);

//            Menu loginLogRemove = new Menu(_guidGenerator.Create())
//            {

//                menuName : "登录删除",
//                permissionCode : "monitor:logininfor:remove",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : loginLog.Id,
//                IsDeleted = false,

//            };
//            entities.Add(loginLogRemove);

//            //参数设置
//            Menu config = new Menu(_guidGenerator.Create())
//            {

//                menuName : "参数设置",
//                permissionCode : "system:config:list",
//                menuType : MenuType.Menu,
//                router : "config",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                component : "/system/config/index",
//                menuIcon : "ri:edit-box-line",
//                orderNum : 94,
//                parentId : system.Id,
//                IsDeleted = false
//            };
//            entities.Add(config);

//            Menu configQuery = new Menu(_guidGenerator.Create())
//            {

//                menuName : "参数查询",
//                permissionCode : "system:config:query",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : config.Id,
//                IsDeleted = false
//            };
//            entities.Add(configQuery);

//            Menu configAdd = new Menu(_guidGenerator.Create())
//            {

//                menuName : "参数新增",
//                permissionCode : "system:config:add",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : config.Id,
//                IsDeleted = false
//            };
//            entities.Add(configAdd);

//            Menu configEdit = new Menu(_guidGenerator.Create())
//            {

//                menuName : "参数修改",
//                permissionCode : "system:config:edit",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : config.Id,
//                IsDeleted = false
//            };
//            entities.Add(configEdit);

//            Menu configRemove = new Menu(_guidGenerator.Create())
//            {

//                menuName : "参数删除",
//                permissionCode : "system:config:remove",
//                menuType : MenuType.Component,
//                orderNum : 100,
//                parentId : config.Id,
//                IsDeleted = false
//            };
//            entities.Add(configRemove);


//            //默认值
//            entities.ForEach(m =>
//            {
//                m.IsDeleted = false;
//                m.State = true;
//                m.MenuSource = MenuSource.Pure;
//                m.IsShow = true;
//            });

//            var p = entities.GroupBy(x => x.Id);
//            foreach (var k in p)
//            {
//                if (k.ToList().Count > 1)
//                {
//                    Console.WriteLine("菜单id重复");
//                }

//            }
//            return entities;
//        }
//    }
//}
