//using Volo.Abp.Data;
//using Volo.Abp.DependencyInjection;
//using Volo.Abp.Guids;
//using Yi.Framework.CasbinRbac.Domain.Entities;
//using Yi.Framework.CasbinRbac.Domain.Shared.Enums;
//using Yi.Framework.SqlSugarCore.Abstractions;

//namespace Yi.Framework.CasbinRbac.SqlSugarCore.DataSeeds
//{
//    public class MenuRuoYiDataSeed : IDataSeedContributor, ITransientDependency
//    {
//        private ISqlSugarRepository<Menu> _repository;
//        private IGuidGenerator _guidGenerator;
//        public MenuRuoYiDataSeed(ISqlSugarRepository<Menu> repository, IGuidGenerator guidGenerator)
//        {
//            _repository = repository;
//            _guidGenerator = guidGenerator;
//        }

//        public async Task SeedAsync(DataSeedContext context)
//        {
//            if (!await _repository.IsAnyAsync(x => x.MenuName == "系统管理"&&x.MenuSource==MenuSource.Ruoyi))
//            {
//                await _repository.InsertManyAsync(GetSeedData());
//            }
//        }
//        public List<Menu> GetSeedData()
//        {
//            List<Menu> entities = new List<Menu>();



//            //系统管理
//            Menu system = new Menu(_guidGenerator.Create(), Guid.Empty)
//            {
//                MenuName = "系统管理",
//                MenuType = MenuType.Catalogue,
//                Router = "/system",
//                IsShow = true,
//                IsLink = false,
//                MenuIcon = "system",
//                OrderNum = 100,
//                IsDeleted = false
//            };
//            entities.Add(system);

//            //代码生成
//            Menu code = new Menu(_guidGenerator.Create(), Guid.Empty)
//            {
//                MenuName = "代码生成",
//                MenuType = MenuType.Catalogue,
//                Router = "/code",
//                IsShow = true,
//                IsLink = false,
//                MenuIcon = "build",
//                OrderNum = 91,
//                IsDeleted = false,
//            };
//            entities.Add(code);

//            //数据表管理
//            Menu table = new Menu(_guidGenerator.Create(), code.Id)
//            {
//                MenuName = "数据表管理",
//                PermissionCode = "code:table:list",
//                MenuType = MenuType.Menu,
//                Router = "table",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "code/table/index",
//                MenuIcon = "online",
//                OrderNum = 100,
//                IsDeleted = false
//            };
//            entities.Add(table);

//            //字段管理
//            Menu field = new Menu(_guidGenerator.Create(), code.Id)
//            {
//                MenuName = "字段管理",
//                PermissionCode = "code:field:list",
//                MenuType = MenuType.Menu,
//                Router = "field",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "code/field/index",
//                MenuIcon = "number",
//                OrderNum = 99,
//                ParentId = code.Id,
//                IsDeleted = false
//            };
//            entities.Add(field);


//            //模板管理
//            Menu template = new Menu(_guidGenerator.Create(), code.Id)
//            {
//                MenuName = "模板管理",
//                PermissionCode = "code:template:list",
//                MenuType = MenuType.Menu,
//                Router = "template",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "code/template/index",
//                MenuIcon = "documentation",
//                OrderNum = 98,
//                IsDeleted = false
//            };
//            entities.Add(template);







//            //系统监控
//            Menu monitoring = new Menu(_guidGenerator.Create())
//            {
//                MenuName = "系统监控",
//                MenuType = MenuType.Catalogue,
//                Router = "/monitor",
//                IsShow = true,
//                IsLink = false,
//                MenuIcon = "monitor",
//                OrderNum = 99,
//                IsDeleted = false
//            };
//            entities.Add(monitoring);


//            //在线用户
//            Menu online = new Menu(_guidGenerator.Create(), monitoring.Id)
//            {
//                MenuName = "在线用户",
//                PermissionCode = "monitor:online:list",
//                MenuType = MenuType.Menu,
//                Router = "online",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "monitor/online/index",
//                MenuIcon = "online",
//                OrderNum = 100,
//                IsDeleted = false
//            };
//            entities.Add(online);

//            //缓存列表
//            Menu cache = new Menu(_guidGenerator.Create(), monitoring.Id)
//            {
//                MenuName = "缓存列表",
//                PermissionCode = "monitor:cache:list",
//                MenuType = MenuType.Menu,
//                Router = "cacheList",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "monitor/cache/list",
//                MenuIcon = "redis-list",
//                OrderNum = 99,
//                IsDeleted = false
//            };
//            entities.Add(cache);

//            //服务监控
//            Menu server = new Menu(_guidGenerator.Create(), monitoring.Id)
//            {
//                MenuName = "服务监控",
//                PermissionCode = "monitor:server:list",
//                MenuType = MenuType.Menu,
//                Router = "server",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "monitor/server/index",
//                MenuIcon = "server",
//                OrderNum = 98,
//                IsDeleted = false
//            };
//            entities.Add(server);
            

//            //定时任务
//            Menu task = new Menu(_guidGenerator.Create(), monitoring.Id)
//            {
//                MenuName = "定时任务",
//                MenuType = MenuType.Menu,
//                Router = "http://ccnetcore.com:16001/hangfire",
//                IsShow = true,
//                IsLink = true,
//                MenuIcon = "job",
//                OrderNum = 97,
//                IsDeleted = false
//            };
//            entities.Add(task);


//            //系统工具
//            Menu tool = new Menu(_guidGenerator.Create())
//            {
//                MenuName = "系统工具",
//                MenuType = MenuType.Catalogue,
//                Router = "/tool",
//                IsShow = true,
//                IsLink = false,
//                MenuIcon = "tool",
//                OrderNum = 98,
//                IsDeleted = false
//            };
//            entities.Add(tool);
//            //swagger文档
//            Menu swagger = new Menu(_guidGenerator.Create(), tool.Id)
//            {
//                MenuName = "接口文档",
//                MenuType = MenuType.Menu,
//                Router = "http://ccnetcore.com:16001/swagger",
//                IsShow = true,
//                IsLink = true,
//                MenuIcon = "list",
//                OrderNum = 100,
//                IsDeleted = false,
//            };
//            entities.Add(swagger);
            
//            //表单构建
//            Menu builder = new Menu(_guidGenerator.Create(), tool.Id)
//            {
//                MenuName = "表单生成器",
//                MenuType = MenuType.Menu,
//                Router = "build",
//                IsShow = true,
//                IsLink = false,
//                MenuIcon = "form",
//                Component = "tool/build/index",
//				IsCache = true,
//				OrderNum = 101,
//                IsDeleted = false,
//            };
//            entities.Add(builder);

//            // //ERP
//            // Menu erp = new Menu(_guidGenerator.Create())
//            // {
//            //     MenuName = "ERP(待更新)",
//            //     MenuType = MenuType.Catalogue,
//            //     Router = "/erp",
//            //     IsShow = true,
//            //     IsLink = false,
//            //     MenuIcon = "international",
//            //     OrderNum = 96,
//            //     IsDeleted = false
//            // };
//            // entities.Add(erp);
//            //
//            //
//            //
//            // //供应商定义
//            // Menu supplier = new Menu(_guidGenerator.Create(), erp.Id)
//            // {
//            //     MenuName = "供应商定义",
//            //     PermissionCode = "erp:supplier:list",
//            //     MenuType = MenuType.Menu,
//            //     Router = "supplier",
//            //     IsShow = true,
//            //     IsLink = false,
//            //     IsCache = true,
//            //     Component = "erp/supplier/index",
//            //     MenuIcon = "education",
//            //     OrderNum = 100,
//            //     IsDeleted = false
//            // };
//            // entities.Add(supplier);
//            //
//            // Menu supplierQuery = new Menu(_guidGenerator.Create(), supplier.Id)
//            // {
//            //     MenuName = "供应商查询",
//            //     PermissionCode = "erp:supplier:query",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     IsDeleted = false
//            // };
//            // entities.Add(supplierQuery);
//            //
//            // Menu supplierAdd = new Menu(_guidGenerator.Create(), supplier.Id)
//            // {
//            //     MenuName = "供应商新增",
//            //     PermissionCode = "erp:supplier:add",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //
//            //     IsDeleted = false
//            // };
//            // entities.Add(supplierAdd);
//            //
//            // Menu supplierEdit = new Menu(_guidGenerator.Create(), supplier.Id)
//            // {
//            //     MenuName = "供应商修改",
//            //     PermissionCode = "erp:supplier:edit",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     IsDeleted = false
//            // };
//            // entities.Add(supplierEdit);
//            //
//            // Menu supplierRemove = new Menu(_guidGenerator.Create(), supplier.Id)
//            // {
//            //     MenuName = "供应商删除",
//            //     PermissionCode = "erp:supplier:remove",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     IsDeleted = false
//            // };
//            // entities.Add(supplierRemove);
//            //
//            //
//            // //仓库定义
//            // Menu warehouse = new Menu(_guidGenerator.Create(), erp.Id)
//            // {
//            //     MenuName = "仓库定义",
//            //     PermissionCode = "erp:warehouse:list",
//            //     MenuType = MenuType.Menu,
//            //     Router = "warehouse",
//            //     IsShow = true,
//            //     IsLink = false,
//            //     IsCache = true,
//            //     Component = "erp/warehouse/index",
//            //     MenuIcon = "education",
//            //     OrderNum = 100,
//            //     IsDeleted = false
//            // };
//            // entities.Add(warehouse);
//            //
//            // Menu warehouseQuery = new Menu(_guidGenerator.Create(), warehouse.Id)
//            // {
//            //     MenuName = "仓库查询",
//            //     PermissionCode = "erp:warehouse:query",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     ParentId = warehouse.Id,
//            //     IsDeleted = false
//            // };
//            // entities.Add(warehouseQuery);
//            //
//            // Menu warehouseAdd = new Menu(_guidGenerator.Create(), warehouse.Id)
//            // {
//            //     MenuName = "仓库新增",
//            //     PermissionCode = "erp:warehouse:add",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     IsDeleted = false
//            // };
//            // entities.Add(warehouseAdd);
//            //
//            // Menu warehouseEdit = new Menu(_guidGenerator.Create(), warehouse.Id)
//            // {
//            //     MenuName = "仓库修改",
//            //     PermissionCode = "erp:warehouse:edit",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     IsDeleted = false
//            // };
//            // entities.Add(warehouseEdit);
//            //
//            // Menu warehouseRemove = new Menu(_guidGenerator.Create(), warehouse.Id)
//            // {
//            //     MenuName = "仓库删除",
//            //     PermissionCode = "erp:warehouse:remove",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     IsDeleted = false
//            // };
//            // entities.Add(warehouseRemove);
//            //
//            //
//            // //单位定义
//            // Menu unit = new Menu(_guidGenerator.Create(), erp.Id)
//            // {
//            //     MenuName = "单位定义",
//            //     PermissionCode = "erp:unit:list",
//            //     MenuType = MenuType.Menu,
//            //     Router = "unit",
//            //     IsShow = true,
//            //     IsLink = false,
//            //     IsCache = true,
//            //     Component = "erp/unit/index",
//            //     MenuIcon = "education",
//            //     OrderNum = 100,
//            //     IsDeleted = false
//            // };
//            // entities.Add(unit);
//            //
//            // Menu unitQuery = new Menu(_guidGenerator.Create(), unit.Id)
//            // {
//            //     MenuName = "单位查询",
//            //     PermissionCode = "erp:unit:query",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     IsDeleted = false
//            // };
//            // entities.Add(unitQuery);
//            //
//            // Menu unitAdd = new Menu(_guidGenerator.Create(), unit.Id)
//            // {
//            //     MenuName = "单位新增",
//            //     PermissionCode = "erp:unit:add",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     IsDeleted = false
//            // };
//            // entities.Add(unitAdd);
//            //
//            // Menu unitEdit = new Menu(_guidGenerator.Create(), unit.Id)
//            // {
//            //     MenuName = "单位修改",
//            //     PermissionCode = "erp:unit:edit",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     IsDeleted = false
//            // };
//            // entities.Add(unitEdit);
//            //
//            // Menu unitRemove = new Menu(_guidGenerator.Create(), unit.Id)
//            // {
//            //     MenuName = "单位删除",
//            //     PermissionCode = "erp:unit:remove",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     IsDeleted = false
//            // };
//            // entities.Add(unitRemove);
//            //
//            //
//            // //物料定义
//            // Menu material = new Menu(_guidGenerator.Create())
//            // {
//            //
//            //     MenuName = "物料定义",
//            //     PermissionCode = "erp:material:list",
//            //     MenuType = MenuType.Menu,
//            //     Router = "material",
//            //     IsShow = true,
//            //     IsLink = false,
//            //     IsCache = true,
//            //     Component = "erp/material/index",
//            //     MenuIcon = "education",
//            //     OrderNum = 100,
//            //     ParentId = erp.Id,
//            //     IsDeleted = false
//            // };
//            // entities.Add(material);
//            //
//            // Menu materialQuery = new Menu(_guidGenerator.Create())
//            // {
//            //
//            //     MenuName = "物料查询",
//            //     PermissionCode = "erp:material:query",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     ParentId = material.Id,
//            //     IsDeleted = false
//            // };
//            // entities.Add(materialQuery);
//            //
//            // Menu materialAdd = new Menu(_guidGenerator.Create())
//            // {
//            //
//            //     MenuName = "物料新增",
//            //     PermissionCode = "erp:material:add",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     ParentId = material.Id,
//            //     IsDeleted = false
//            // };
//            // entities.Add(materialAdd);
//            //
//            // Menu materialEdit = new Menu(_guidGenerator.Create())
//            // {
//            //
//            //     MenuName = "物料修改",
//            //     PermissionCode = "erp:material:edit",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     ParentId = material.Id,
//            //     IsDeleted = false
//            // };
//            // entities.Add(materialEdit);
//            //
//            // Menu materialRemove = new Menu(_guidGenerator.Create())
//            // {
//            //
//            //     MenuName = "物料删除",
//            //     PermissionCode = "erp:material:remove",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     ParentId = material.Id,
//            //     IsDeleted = false
//            // };
//            // entities.Add(materialRemove);
//            //
//            //
//            // //采购订单
//            // Menu purchase = new Menu(_guidGenerator.Create())
//            // {
//            //
//            //     MenuName = "采购订单",
//            //     PermissionCode = "erp:purchase:list",
//            //     MenuType = MenuType.Menu,
//            //     Router = "purchase",
//            //     IsShow = true,
//            //     IsLink = false,
//            //     IsCache = true,
//            //     Component = "erp/purchase/index",
//            //     MenuIcon = "education",
//            //     OrderNum = 100,
//            //     ParentId = erp.Id,
//            //     IsDeleted = false
//            // };
//            // entities.Add(purchase);
//            //
//            // Menu purchaseQuery = new Menu(_guidGenerator.Create())
//            // {
//            //
//            //     MenuName = "采购订单查询",
//            //     PermissionCode = "erp:purchase:query",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     ParentId = purchase.Id,
//            //     IsDeleted = false
//            // };
//            // entities.Add(purchaseQuery);
//            //
//            // Menu purchaseAdd = new Menu(_guidGenerator.Create())
//            // {
//            //
//            //     MenuName = "采购订单新增",
//            //     PermissionCode = "erp:purchase:add",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     ParentId = purchase.Id,
//            //     IsDeleted = false
//            // };
//            // entities.Add(purchaseAdd);
//            //
//            // Menu purchaseEdit = new Menu(_guidGenerator.Create())
//            // {
//            //
//            //     MenuName = "采购订单修改",
//            //     PermissionCode = "erp:purchase:edit",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     ParentId = purchase.Id,
//            //     IsDeleted = false
//            // };
//            // entities.Add(purchaseEdit);
//            //
//            // Menu purchaseRemove = new Menu(_guidGenerator.Create())
//            // {
//            //
//            //     MenuName = "采购订单删除",
//            //     PermissionCode = "erp:purchase:remove",
//            //     MenuType = MenuType.Component,
//            //     OrderNum = 100,
//            //     ParentId = purchase.Id,
//            //     IsDeleted = false
//            // };
//            // entities.Add(purchaseRemove);



//            //Yi框架
//            Menu guide = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "Yi框架",
//                MenuType = MenuType.Catalogue,
//                Router = "https://ccnetcore.com",
//                IsShow = true,
//                IsLink = true,
//                MenuIcon = "guide",
//                OrderNum = 90,
//                IsDeleted = false,
//            };
//            entities.Add(guide);

//            //租户管理
//            Menu tenant = new Menu(_guidGenerator.Create())
//            {
//                MenuName = "租户管理",
//                PermissionCode = "system:tenant:list",
//                MenuType = MenuType.Menu,
//                Router = "tenant",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "system/tenant/index",
//                MenuIcon = "list",
//                OrderNum = 101,
//                ParentId = system.Id,
//                IsDeleted = false
//            };
//            entities.Add(tenant);

//            Menu tenantQuery = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "租户查询",
//                PermissionCode = "system:tenant:query",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = tenant.Id,
//                IsDeleted = false
//            };
//            entities.Add(tenantQuery);

//            Menu tenantAdd = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "租户新增",
//                PermissionCode = "system:tenant:add",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = tenant.Id,
//                IsDeleted = false
//            };
//            entities.Add(tenantAdd);

//            Menu tenantEdit = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "租户修改",
//                PermissionCode = "system:tenant:edit",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = tenant.Id,
//                IsDeleted = false
//            };
//            entities.Add(tenantEdit);

//            Menu tenantRemove = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "租户删除",
//                PermissionCode = "system:tenant:remove",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = tenant.Id,
//                IsDeleted = false
//            };
//            entities.Add(tenantRemove);












//            //用户管理
//            Menu user = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "用户管理",
//                PermissionCode = "system:user:list",
//                MenuType = MenuType.Menu,
//                Router = "user",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "system/user/index",
//                MenuIcon = "user",
//                OrderNum = 100,
//                ParentId = system.Id,
//                IsDeleted = false
//            };
//            entities.Add(user);

//            Menu userQuery = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "用户查询",
//                PermissionCode = "system:user:query",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = user.Id,
//                IsDeleted = false
//            };
//            entities.Add(userQuery);

//            Menu userAdd = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "用户新增",
//                PermissionCode = "system:user:add",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = user.Id,
//                IsDeleted = false
//            };
//            entities.Add(userAdd);

//            Menu userEdit = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "用户修改",
//                PermissionCode = "system:user:edit",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = user.Id,
//                IsDeleted = false
//            };
//            entities.Add(userEdit);

//            Menu userRemove = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "用户删除",
//                PermissionCode = "system:user:remove",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = user.Id,
//                IsDeleted = false
//            };
//            entities.Add(userRemove);


//             Menu userResetPwd = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "重置密码",
//                PermissionCode = "system:user:resetPwd",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = user.Id,
//                IsDeleted = false
//            };
//            entities.Add(userResetPwd);


//            //角色管理
//            Menu role = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "角色管理",
//                PermissionCode = "system:role:list",
//                MenuType = MenuType.Menu,
//                Router = "role",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "system/role/index",
//                MenuIcon = "peoples",
//                OrderNum = 99,
//                ParentId = system.Id,
//                IsDeleted = false
//            };
//            entities.Add(role);

//            Menu roleQuery = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "角色查询",
//                PermissionCode = "system:role:query",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = role.Id,
//                IsDeleted = false
//            };
//            entities.Add(roleQuery);

//            Menu roleAdd = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "角色新增",
//                PermissionCode = "system:role:add",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = role.Id,
//                IsDeleted = false
//            };
//            entities.Add(roleAdd);

//            Menu roleEdit = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "角色修改",
//                PermissionCode = "system:role:edit",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = role.Id,
//                IsDeleted = false
//            };
//            entities.Add(roleEdit);

//            Menu roleRemove = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "角色删除",
//                PermissionCode = "system:role:remove",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = role.Id,
//                IsDeleted = false
//            };
//            entities.Add(roleRemove);


//            //菜单管理
//            Menu menu = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "菜单管理",
//                PermissionCode = "system:menu:list",
//                MenuType = MenuType.Menu,
//                Router = "menu",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "system/menu/index",
//                MenuIcon = "tree-table",
//                OrderNum = 98,
//                ParentId = system.Id,
//                IsDeleted = false
//            };
//            entities.Add(menu);

//            Menu menuQuery = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "菜单查询",
//                PermissionCode = "system:menu:query",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = menu.Id,
//                IsDeleted = false
//            };
//            entities.Add(menuQuery);

//            Menu menuAdd = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "菜单新增",
//                PermissionCode = "system:menu:add",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = menu.Id,
//                IsDeleted = false
//            };
//            entities.Add(menuAdd);

//            Menu menuEdit = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "菜单修改",
//                PermissionCode = "system:menu:edit",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = menu.Id,
//                IsDeleted = false
//            };
//            entities.Add(menuEdit);

//            Menu menuRemove = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "菜单删除",
//                PermissionCode = "system:menu:remove",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = menu.Id,
//                IsDeleted = false
//            };
//            entities.Add(menuRemove);

//            //部门管理
//            Menu dept = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "部门管理",
//                PermissionCode = "system:dept:list",
//                MenuType = MenuType.Menu,
//                Router = "dept",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "system/dept/index",
//                MenuIcon = "tree",
//                OrderNum = 97,
//                ParentId = system.Id,
//                IsDeleted = false
//            };
//            entities.Add(dept);

//            Menu deptQuery = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "部门查询",
//                PermissionCode = "system:dept:query",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = dept.Id,
//                IsDeleted = false
//            };
//            entities.Add(deptQuery);

//            Menu deptAdd = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "部门新增",
//                PermissionCode = "system:dept:add",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = dept.Id,
//                IsDeleted = false
//            };
//            entities.Add(deptAdd);

//            Menu deptEdit = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "部门修改",
//                PermissionCode = "system:dept:edit",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = dept.Id,
//                IsDeleted = false
//            };
//            entities.Add(deptEdit);

//            Menu deptRemove = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "部门删除",
//                PermissionCode = "system:dept:remove",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = dept.Id,
//                IsDeleted = false
//            };
//            entities.Add(deptRemove);



//            //岗位管理
//            Menu post = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "岗位管理",
//                PermissionCode = "system:post:list",
//                MenuType = MenuType.Menu,
//                Router = "post",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "system/post/index",
//                MenuIcon = "post",
//                OrderNum = 96,
//                ParentId = system.Id,
//                IsDeleted = false
//            };
//            entities.Add(post);

//            Menu postQuery = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "岗位查询",
//                PermissionCode = "system:post:query",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = post.Id,
//                IsDeleted = false
//            };
//            entities.Add(postQuery);

//            Menu postAdd = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "岗位新增",
//                PermissionCode = "system:post:add",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = post.Id,
//                IsDeleted = false
//            };
//            entities.Add(postAdd);

//            Menu postEdit = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "岗位修改",
//                PermissionCode = "system:post:edit",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = post.Id,
//                IsDeleted = false
//            };
//            entities.Add(postEdit);

//            Menu postRemove = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "岗位删除",
//                PermissionCode = "system:post:remove",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = post.Id,
//                IsDeleted = false
//            };
//            entities.Add(postRemove);

//            //字典管理
//            Menu dict = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "字典管理",
//                PermissionCode = "system:dict:list",
//                MenuType = MenuType.Menu,
//                Router = "dict",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "system/dict/index",
//                MenuIcon = "dict",
//                OrderNum = 95,
//                ParentId = system.Id,
//                IsDeleted = false
//            };
//            entities.Add(dict);

//            Menu dictQuery = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "字典查询",
//                PermissionCode = "system:dict:query",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = dict.Id,
//                IsDeleted = false
//            };
//            entities.Add(dictQuery);

//            Menu dictAdd = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "字典新增",
//                PermissionCode = "system:dict:add",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = dict.Id,
//                IsDeleted = false
//            };
//            entities.Add(dictAdd);

//            Menu dictEdit = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "字典修改",
//                PermissionCode = "system:dict:edit",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = dict.Id,
//                IsDeleted = false
//            };
//            entities.Add(dictEdit);

//            Menu dictRemove = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "字典删除",
//                PermissionCode = "system:dict:remove",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = dict.Id,
//                IsDeleted = false
//            };
//            entities.Add(dictRemove);


//            //参数设置
//            Menu config = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "参数设置",
//                PermissionCode = "system:config:list",
//                MenuType = MenuType.Menu,
//                Router = "config",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "system/config/index",
//                MenuIcon = "edit",
//                OrderNum = 94,
//                ParentId = system.Id,
//                IsDeleted = false
//            };
//            entities.Add(config);

//            Menu configQuery = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "参数查询",
//                PermissionCode = "system:config:query",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = config.Id,
//                IsDeleted = false
//            };
//            entities.Add(configQuery);

//            Menu configAdd = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "参数新增",
//                PermissionCode = "system:config:add",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = config.Id,
//                IsDeleted = false
//            };
//            entities.Add(configAdd);

//            Menu configEdit = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "参数修改",
//                PermissionCode = "system:config:edit",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = config.Id,
//                IsDeleted = false
//            };
//            entities.Add(configEdit);

//            Menu configRemove = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "参数删除",
//                PermissionCode = "system:config:remove",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = config.Id,
//                IsDeleted = false
//            };
//            entities.Add(configRemove);




//            //通知公告
//            Menu notice = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "通知公告",
//                PermissionCode = "system:notice:list",
//                MenuType = MenuType.Menu,
//                Router = "notice",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "system/notice/index",
//                MenuIcon = "message",
//                OrderNum = 93,
//                ParentId = system.Id,
//                IsDeleted = false
//            };
//            entities.Add(notice);

//            Menu noticeQuery = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "通知查询",
//                PermissionCode = "system:notice:query",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = notice.Id,
//                IsDeleted = false
//            };
//            entities.Add(noticeQuery);

//            Menu noticeAdd = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "通知新增",
//                PermissionCode = "system:notice:add",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = notice.Id,
//                IsDeleted = false
//            };
//            entities.Add(noticeAdd);

//            Menu noticeEdit = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "通知修改",
//                PermissionCode = "system:notice:edit",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = notice.Id,
//                IsDeleted = false
//            };
//            entities.Add(noticeEdit);

//            Menu noticeRemove = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "通知删除",
//                PermissionCode = "system:notice:remove",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = notice.Id,
//                IsDeleted = false
//            };
//            entities.Add(noticeRemove);



//            //日志管理
//            Menu log = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "日志管理",
//                MenuType = MenuType.Catalogue,
//                Router = "log",
//                IsShow = true,
//                IsLink = false,
//                MenuIcon = "log",
//                OrderNum = 92,
//                ParentId = system.Id,
//                IsDeleted = false
//            };
//            entities.Add(log);

//            //操作日志
//            Menu operationLog = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "操作日志",
//                PermissionCode = "monitor:operlog:list",
//                MenuType = MenuType.Menu,
//                Router = "operlog",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "monitor/operlog/index",
//                MenuIcon = "form",
//                OrderNum = 100,
//                ParentId = log.Id,
//                IsDeleted = false
//            };
//            entities.Add(operationLog);

//            Menu operationLogQuery = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "操作查询",
//                PermissionCode = "monitor:operlog:query",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = operationLog.Id,
//                IsDeleted = false
//            };
//            entities.Add(operationLogQuery);

//            Menu operationLogRemove = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "操作删除",
//                PermissionCode = "monitor:operlog:remove",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = operationLog.Id,
//                IsDeleted = false
//            };
//            entities.Add(operationLogRemove);


//            //登录日志
//            Menu loginLog = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "登录日志",
//                PermissionCode = "monitor:logininfor:list",
//                MenuType = MenuType.Menu,
//                Router = "logininfor",
//                IsShow = true,
//                IsLink = false,
//                IsCache = true,
//                Component = "monitor/logininfor/index",
//                MenuIcon = "logininfor",
//                OrderNum = 100,
//                ParentId = log.Id,
//                IsDeleted = false
//            };
//            entities.Add(loginLog);

//            Menu loginLogQuery = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "登录查询",
//                PermissionCode = "monitor:logininfor:query",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = loginLog.Id,
//                IsDeleted = false
//            };
//            entities.Add(loginLogQuery);

//            Menu loginLogRemove = new Menu(_guidGenerator.Create())
//            {

//                MenuName = "登录删除",
//                PermissionCode = "monitor:logininfor:remove",
//                MenuType = MenuType.Component,
//                OrderNum = 100,
//                ParentId = loginLog.Id,
//                IsDeleted = false
//            };
//            entities.Add(loginLogRemove);

//            //默认值
//            entities.ForEach(m =>
//            {
//                m.IsDeleted = false;
//                m.State = true;
//                m.MenuSource = MenuSource.Ruoyi;
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
