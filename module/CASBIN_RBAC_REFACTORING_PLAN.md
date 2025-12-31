# Casbin-RBAC 权限系统重构计划

## 1. 核心决策：绞杀者模式 (Strangler Fig Pattern)

为了保证现有系统的稳定性，同时引入强大的 Casbin 权限引擎，我们采用**模块复制与独立演进**的策略。

*   **当前状态**: 系统使用 `Yi.Framework.Rbac` 模块，基于传统表结构管理权限。
*   **目标状态**: 系统使用集成了 Casbin 的增强版 RBAC 模块，支持更细粒度的数据和字段权限。
*   **实施路径**:
    1.  **复制 (Clone)**: 将 `module/rbac` 完整复制并重命名为 `module/casbin-rbac`。
    2.  **隔离 (Isolate)**: 新模块命名空间修改为 `Yi.Framework.CasbinRbac`，与旧模块共存但不冲突。
    3.  **演进 (Evolve)**: 在新模块中安装 Casbin 依赖，按照设计文档进行开发和测试。
    4.  **替换 (Replace)**: 功能完善后，移除旧模块，将新模块名称改回 `Yi.Framework.Rbac`。

## 2. 目标架构设计分析

参考企业级通用权限管理系统设计，新系统将包含以下核心能力：

### 四大核心模块

1.  **多租户与组织架构 (基础)**
    *   **SysTenant**: 租户隔离（SaaS 顶层）。
    *   **SysOrg**: 组织树，数据权限的核心锚点（用于计算“本部门及以下”）。
    *   **SysPosition**: 岗位。

2.  **用户与档案 (主体)**
    *   **SysUser**: 登录主体，关联 `TenantId` 和 `OrgId`。
    *   **SysUserProfile**: 扩展档案信息。

3.  **角色与高级权限 (核心)**
    *   **SysRole**: 包含 `DataScope` (数据范围枚举)。
    *   **SysRoleOrg**: 自定义数据权限的具体部门列表。
    *   **SysRoleApi**: (新) 接口级权限控制，与 Casbin 策略同步。
    *   **SysRoleField**: (新) 字段级权限黑名单。

4.  **系统资源 (客体)**
    *   **SysMenu**: 菜单/按钮。需增加与 API 的关联配置。

### 权限控制分层逻辑

*   **功能权限 (Functional)**: **由 Casbin 接管**。
    *   控制谁 (User/Role) 能访问哪个 API (URL + Method)。
    *   中间件拦截：`Enforcer.EnforceAsync(sub, dom, obj, act)`。
*   **数据权限 (Data Scope)**: **由 ORM (SqlSugar/EF) 过滤器接管**。
    *   控制 SQL 查询的 `WHERE` 条件（如：只能查本部门数据）。
    *   基于 `SysRole.DataScope` 枚举动态拼接。
*   **字段权限 (Field Level)**: **由 JSON 序列化器接管**。
    *   控制返回 JSON 中是否包含敏感字段（如：手机号）。
    *   基于 `SysRoleField` 黑名单配置。

## 3. Casbin 模型设计

采用 **RBAC with Domains** 模型。

**配置文件 (`rbac_with_domains_model.conf`)**:
```ini
[request_definition]
r = sub, dom, obj, act

[policy_definition]
p = sub, dom, obj, act

[role_definition]
g = _, _, _

[policy_effect]
e = some(where (p.eft == allow))

[matchers]
m = g(r.sub, p.sub, r.dom) && r.dom == p.dom && keyMatch2(r.obj, p.obj) && regexMatch(r.act, p.act)
```

**策略映射规范**:
*   `sub`: 用户 (`u_{UserId}`) 或 角色 (`{RoleCode}`)。
*   `dom`: 租户ID (`{TenantId}`)。
*   `obj`: API 路径 (`/api/user/list`)。
*   `act`: HTTP 方法 (`GET`, `POST`)。

## 4. 详细开发计划与进度 (Progress)

### 第一阶段：模块初始化与基础建设 [已完成]
- [x] **执行克隆脚本**: 创建 `Yi.Framework.CasbinRbac`。
- [x] **依赖安装**: 手动修改 `.csproj` 添加了 `Casbin.NET` 和 `Adapter`。
- [x] **配置注入**: 创建了 `rbac_with_domains_model.conf`。
- [x] **项目注册**: 将 5 个新项目添加到了 `Yi.Abp.sln`。

### 第二阶段：实体增强 (Entity Refactoring) [已完成]
- [x] **租户支持**: `User`, `Role`, `Department`, `Position` 实体已实现 `IMultiTenant`，添加了 `TenantId`。
- [x] **菜单-接口关联**: `Menu` 实体已添加 `ApiUrl` 和 `ApiMethod`。
- [x] **组织架构增强**: `Department` 已添加 `Ancestors` 字段支持高效树形查询。
- [x] **新表创建**: 已创建 `RoleField` (字段权限) 和 `TableConfig` (元数据配置) 实体。

### 第三阶段：同步逻辑实现 (The "Brain") [已完成]
- [x] **策略管理器**: 实现了 `CasbinPolicyManager` 领域服务，封装了 `g` 和 `p` 策略的同步逻辑。
- [x] **事件驱动同步**:
    *   `UserManager.GiveUserSetRoleAsync` 中注入了 Casbin 同步逻辑。
    *   `RoleManager.GiveRoleSetMenuAsync` 中注入了 Casbin 同步逻辑。
- [x] **初始化种子数据**: 创建了 `CasbinSeedService`，用于初始化 admin 的通配符权限。

### 第四阶段：运行时拦截 (Runtime Enforcement) [已完成]
- [x] **中间件开发**: 实现了 `CasbinAuthorizationMiddleware`，支持 URL 级鉴权和 `[AllowAnonymous]` 白名单。
- [x] **上下文解析**: 从 HttpContext 解析 User, Tenant, Path, Method。
- [x] **鉴权调用**: 调用 Casbin Enforcer 进行判断，失败返回 403。
- [x] **便捷扩展**: 实现了 `app.UseCasbinRbac()` 扩展方法。

> **⚠️ 手动操作提醒**:
> 由于无法自动修改 Web 层代码，请手动在 Web 项目（如 `Yi.Abp.Web`）的 `Program.cs` 或 `Startup.cs` 中，在 `UseAuthentication()` 之后、`UseAuthorization()` 之前调用 `app.UseCasbinRbac()`。

### 第五阶段：高级权限 (Data & Field) [进行中]
1.  **数据过滤器**: 实现 SqlSugar 的全局过滤器，注入数据范围逻辑。[进行中]
2.  **字段脱敏**: 实现 JSON 序列化转换器，处理字段黑名单。
