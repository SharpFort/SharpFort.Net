这是一个非常成熟且典型的**企业级通用权限管理系统**的设计思路。它不仅包含了基础的 RBAC（功能权限），还深度集成了 **数据权限（Data Scope）** 和 **字段级权限（Field Level Security）**。

这种系统的核心在于：**将“控制”的颗粒度从“接口”下沉到了“数据行”和“数据列”。**

为了清晰展示，我将其分为 **四大模块**：

---

### 第一模块：多租户与组织架构 (基础底座)

这部分决定了数据的物理隔离和逻辑层级。

| 表名 | 关键字段 | 表间关系 | 描述 |
| :--- | :--- | :--- | :--- |
| **SysTenant**<br>(租户表) | `Id`<br>`Name` (集团名称)<br>`TenantCode`<br>`DbConnection` (独立库链接串)<br>`ExpireTime`<br>`Status` | 1:N SysOrg<br>1:N SysUser | **SaaS 的顶层隔离**。<br>`DbConnection` 字段表明系统支持“一个租户一个数据库”或“共享库独立Schema”的混合模式。 |
| **SysOrg**<br>(组织/部门表) | `Id`<br>`ParentId` (树结构)<br>`TenantId`<br>`Name`<br>`Code`<br>`Sort`<br>`Leader` (负责人) | N:1 SysTenant<br>1:N SysUser | **数据权限的核心锚点**。<br>树形结构，用于计算“本部门及以下”的数据范围。 |
| **SysPosition**<br>(岗位表) | `Id`<br>`TenantId`<br>`Name` (如销售经理)<br>`Code`<br>`Sort` | N:1 SysTenant<br>N:N SysUser | **业务身份标签**。<br>用于工作流审批或业务逻辑判断，通常不直接挂钩权限，但可辅助角色分配。 |

---

### 第二模块：用户与档案 (主体)

将登录信息与详细档案分离，保持核心表轻量。

| 表名 | 关键字段 | 表间关系 | 描述 |
| :--- | :--- | :--- | :--- |
| **SysUser**<br>(用户核心表) | `Id`<br>`TenantId`<br>`OrgId` (归属部门)<br>`Username`<br>`Password`<br>`Mobile`<br>`Status` | N:1 SysOrg<br>1:1 SysUserProfile | **登录主体**。<br>仅存储鉴权和核心业务必须的字段。`OrgId` 决定了该用户默认的数据归属。 |
| **SysUserProfile**<br>(用户档案表) | `UserId`<br>`RealName`<br>`IdCard` (身份证)<br>`Education` (学历)<br>`Certificates` (证书)<br>`Birthday` | 1:1 SysUser | **扩展信息表**。<br>对应你提到的“档案信息”，与核心表分离，避免查询 User 时拖带大量非必要文本数据。 |
| **SysUserPost**<br>(用户岗位关联) | `UserId`<br>`PostId` | N:N | 处理一个用户身兼多职的情况。 |

---

### 第三模块：角色与高级权限 (核心大脑)

这是该系统最强大的地方，涵盖了功能、数据、字段、接口四维控制。

| 表名 | 关键字段 | 表间关系 | 描述 |
| :--- | :--- | :--- | :--- |
| **SysRole**<br>(角色表) | `Id`<br>`Name`<br>`Code`<br>`DataScope` (枚举: 全部/本部门/本人/自定义)<br>`TenantId` | N:N SysUser<br>1:N SysRoleMenu | **权限集合体**。<br>**`DataScope` 是核心**，决定了该角色能看到多少数据行（Row Level Security）。 |
| **SysUserRole**<br>(用户角色关联) | `UserId`<br>`RoleId` | N:N | 传统的用户-角色绑定。 |
| **SysRoleOrg**<br>(角色数据范围表) | `RoleId`<br>`OrgId` | N:N | **自定义数据权限专用**。<br>当 `DataScope` = "自定义"时，此表存储具体勾选了哪些部门。 |
| **SysRoleField**<br>(字段权限/黑名单) | `RoleId`<br>`MenuId` (所属菜单)<br>`TableName`<br>`FieldName`<br>`IsDeny` (是否禁止读取) | N:N | **列级权限 (Column Level)**。<br>对应你提到的“字段黑名单”。如果存在记录，则后端返回数据时需抹除该字段值。 |
| **SysRoleApi**<br>(接口权限/黑名单) | `RoleId`<br>`ApiCode` (接口路径)<br>`Type` (允许/禁止) | N:N | **接口级权限**。<br>对应“接口黑名单”。通常 Casbin 做白名单，这里可能用于特殊限制（如：超级管理员除个别接口外全开）。 |

---

### 第四模块：系统资源 (客体)

| 表名 | 关键字段 | 表间关系 | 描述 |
| :--- | :--- | :--- | :--- |
| **SysMenu**<br>(菜单权限表) | `Id`<br>`ParentId`<br>`Type` (目录/菜单/按钮)<br>`Name`<br>`PermCode` (user:list)<br>`RoutePath`<br>`Component` | 1:N SysRoleMenu | **前端路由与按钮标识**。<br>构建左侧菜单树和页面内的按钮显隐。 |
| **SysRoleMenu**<br>(角色菜单关联) | `RoleId`<br>`MenuId` | N:N | 经典 RBAC 关联，决定用户能看到哪些页面和按钮。 |
| **SysTableConfig**<br>(表字段元数据) | `TableName`<br>`FieldName`<br>`FieldDesc` (如: 手机号) | 被 SysRoleField 引用 | **辅助表**。<br>用于在“字段黑名单”配置界面，把数据库字段名 `phone` 翻译成中文 “手机号” 给管理员选。 |
| **SysLog**<br>(审计日志) | `Id`<br>`UserId`<br>`Module`<br>`Action`<br>`Ip`<br>`CreateTime` | - | **审计追踪**。<br>记录谁在什么时间修改了什么权限。 |

---

### 深度解析：这个强大的系统是如何运作的？

你参考的这个系统之所以强大，是因为它在 **Casbin (功能拦截)** 之外，多做了两层逻辑处理。以下是它的核心运作机制，也是你在 .NET 中需要实现的逻辑：

#### 1. 数据权限 (Data Scope) - 解决“能看哪些行”
当用户查询“订单列表”时，后端不会直接 `SELECT * FROM Orders`，而是会执行一个 **AOP 切面或过滤器**：
1.  获取当前用户的所有角色。
2.  读取角色的 `DataScope` 字段。
    *   如果是 **“全部”**：不加过滤条件。
    *   如果是 **“本部门”**：自动拼接 SQL `WHERE OrgId = '用户当前OrgId'`。
    *   如果是 **“本部门及以下”**：查找 `SysOrg` 树，拼接 `WHERE OrgId IN (所有子部门ID)`。
    *   如果是 **“本人”**：拼接 `WHERE CreatorId = '用户ID'`。
    *   如果是 **“自定义”**：查 `SysRoleOrg` 表，拼接 `WHERE OrgId IN (勾选的ID列表)`。
3.  **结论：** 这部分 **不归 Casbin 管**，通常由你的 ORM (如 EF Core Global Query Filters) 或 业务层逻辑处理。

#### 2. 字段权限 (Field Blacklist) - 解决“能看哪些列”
当后端准备返回 JSON 数据给前端时：
1.  检查用户角色的 `SysRoleField` 表。
2.  如果发现用户属于“销售员”角色，且 `SysRoleField` 中配置了 `User` 表的 `Mobile` 字段为 **Deny (黑名单)**。
3.  在 JSON 序列化阶段（如 Newtonsoft.Json 或 System.Text.Json 的 Converter 中），动态将 `Mobile` 字段设为 `null` 或直接移除该 Key。
4.  **结论：** 这部分由 **JSON 序列化器** 或 **DTO 映射层 (AutoMapper)** 处理。

#### 3. 接口权限与 Casbin
这部分就是我们之前讨论的：
*   管理员在界面勾选“菜单/按钮”。
*   后端保存 `SysRoleMenu`。
*   **同步** 将对应的 API 规则写入 Casbin。
*   **接口黑名单**：这是一个特殊需求。Casbin 默认是“白名单”（没配策略就禁止）。如果系统实现了“接口黑名单”，通常意味着该系统有一个默认策略是 `Allow All`，然后通过 Casbin 的 `Deny` 策略来实现黑名单（Priority 机制，Deny 优先于 Allow）。


### 疑问 1：独立数据库/Schema 模式下，还需要冗余 `TenantId` 吗？

**直接结论：强烈建议保留 `TenantId`，即使是独立数据库模式。**

虽然从物理隔离的角度看，如果数据库里只有 A 公司的数据，查询时确实不需要 `WHERE TenantId = 'A'` 来过滤。但是，从**架构灵活性**和**运维成本**角度考虑，保留该字段是“黄金准则”。

**理由如下：**

1.  **代码的一致性 (Code Consistency)：**
    *   你的后端代码（C# .NET）只有一套。如果你保留 `TenantId`，你可以使用 EF Core 的 **Global Query Filters** 统一处理所有查询。
    *   如果不保留，你需要写两套逻辑：一套给共享库模式（加过滤），一套给独立库模式（不加过滤）。这会极大地增加代码维护复杂度。
2.  **架构的演进与迁移 (Migration)：**
    *   **场景：** 某天你想把 10 个超小的租户从独立库合并到一个共享库以节省成本；或者某天一个共享库里的租户做大了，想独立出去。
    *   如果表里有 `TenantId`，数据迁移就是简单的 `SELECT INTO` 或数据泵导出导入。
    *   如果表里没 `TenantId`，你在合并数据时需要给每一行数据“补打标签”，这在海量数据下是一场灾难。
3.  **数据仓库与 BI 分析 (Analytics)：**
    *   SaaS 平台通常需要一个总的运营后台查看“所有租户的总订单量”。
    *   ETL 工具将各个独立库的数据抽取到数据仓库（Data Warehouse）时，必须依靠 `TenantId` 来区分数据来源。如果源表里没有，ETL 过程就会非常痛苦。
4.  **防呆设计 (Safety Net)：**
    *   万一配置错误，代码连错了数据库（比如租户 A 连到了租户 B 的库），虽然这是运维事故，但如果代码层面还有 `TenantId` 的过滤校验，可能会在应用层拦截住数据泄露，或者至少让查询返回空（因为 A 的 TenantId 匹配不上 B 库里的数据）。

**总结：** 存储成本几乎可以忽略不计，但带来的架构灵活性是巨大的。**请务必加上。**

---

### 疑问 2：数据范围（全部/本部门/本人等）如何定义和存储？

**核心误区纠正：**
你可能认为每一种数据范围都需要一张表来存。其实不是的。
*   **“自定义数据”**：需要 `SysRoleOrg` 表来存储具体勾选了哪些部门。
*   **“全部 / 本部门及以下 / 本部门 / 仅本人”**：这 4 种情况**不需要额外的关联表**，它们只需要在 `SysRole` 表里存一个**枚举值 (Enum)**。

#### 1. 存储方式

在 `SysRole` 表中，设计一个字段 `DataScope` (int 或 string)。

**定义枚举 (C# Enum):**
```csharp
public enum DataScopeType
{
    All = 1,             // 全部数据
    DeptAndChild = 2,    // 本部门及以下
    DeptOnly = 3,        // 本部门
    Self = 4,            // 仅本人
    Custom = 5           // 自定义
}
```

**数据库存储示例 (`SysRole` 表):**

| Id | RoleName | DataScope | 描述 |
| :--- | :--- | :--- | :--- |
| 1 | 超级管理员 | 1 | 拥有全部权限，无需查关联表 |
| 2 | 销售经理 | 2 | 只能看本部门及下级，无需查关联表 |
| 3 | 销售专员 | 4 | 只能看自己的，无需查关联表 |
| 4 | 华北区督导 | 5 | 自定义，**只有这种情况才去查 SysRoleOrg 表** |

#### 2. 授权逻辑与实现原理

这几种范围的“授权”不需要复杂的存储，而是体现在**运行时的 SQL 拼接逻辑**上。

假设当前登录用户是 `UserA`，归属部门 `Org_Sales` (ID: 100)，ID 为 `User_999`。

**场景：查询订单列表**

*   **情况 A：DataScope = 1 (全部)**
    *   **逻辑：** 代码检测到枚举是 `All`。
    *   **生成的 SQL：** `SELECT * FROM Orders WHERE TenantId = 'T1'` (不做额外限制)

*   **情况 B：DataScope = 3 (本部门)**
    *   **逻辑：** 代码检测到枚举是 `DeptOnly`，取出当前用户的 `OrgId`。
    *   **生成的 SQL：** `SELECT * FROM Orders WHERE TenantId = 'T1' AND OrgId = 100`

*   **情况 C：DataScope = 4 (仅本人)**
    *   **逻辑：** 代码检测到枚举是 `Self`，取出当前用户的 `UserId`。
    *   **生成的 SQL：** `SELECT * FROM Orders WHERE TenantId = 'T1' AND CreatorId = 'User_999'`

*   **情况 D：DataScope = 5 (自定义)**
    *   **逻辑：** 代码检测到枚举是 `Custom`。**此时才去查询 `SysRoleOrg` 表**，获取该角色绑定的部门 ID 列表 (比如: 100, 101, 102)。
    *   **生成的 SQL：** `SELECT * FROM Orders WHERE TenantId = 'T1' AND OrgId IN (100, 101, 102)`

*   **情况 E：DataScope = 2 (本部门及以下) —— 这是个技术难点**
    *   **逻辑：** 需要找到 ID=100 的部门以及它所有的子子孙孙部门。
    *   **低效做法：** 递归查询数据库（性能差）。
    *   **高效做法 (推荐)：** 在 `SysOrg` 表设计时加入 `Ancestors` (祖籍/路径) 字段。
        *   *SysOrg 表数据示例：*
            *   总部 (Id:1, Ancestors: "0")
            *   分公司 (Id:10, Ancestors: "0,1")
            *   销售部 (Id:100, Ancestors: "0,1,10")
            *   销售一组 (Id:1001, Ancestors: "0,1,10,100")
    *   **生成的 SQL：**
        *   先查用户部门的 Ancestors 规则。
        *   `SELECT * FROM Orders WHERE OrgId IN (SELECT Id FROM SysOrg WHERE Ancestors LIKE '0,1,10,100%')`
        *   或者直接利用业务代码算出所有子 ID 列表，再用 `IN` 查询。

#### 3. 总结

*   **如何定义？** 代码里的 Enum。
*   **如何存储？** `SysRole` 表里的一个字段 (Int)。
*   **如何实现？** 后端的一个通用过滤器（Filter/AOP），根据这个 Enum 值，动态生成不同的 SQL `WHERE` 条件。
*   **SysRoleOrg 表的作用？** 它是**专款专用**的，只有当 `DataScope == Custom` 时，这张表才有意义，其他时候它是空的或者被忽略的。

你的理解非常精准。

**是的，Casbin 的核心职责是“功能访问控制”（Functional Access Control）。**
*   **Casbin 管：** 谁（User/Role）在哪个租户（Tenant）下，能访问哪个 API（URL + Method）。
*   **Casbin 不管：** SQL 查出来是 10 条数据还是 100 条数据（数据权限），返回的 JSON 里有没有“手机号”字段（字段权限）。

针对你的需求（RBAC + 多租户），我们需要使用 Casbin 的 **RBAC with Domains** 模型。下面我为你详细设计 Casbin 的策略模型和落地实操方案。

---

### 一、 Casbin 模型设计 (Model.conf)

在 .NET 项目中，你需要创建一个 `rbac_with_domains_model.conf` 文件。

```ini
[request_definition]
r = sub, dom, obj, act
# 解释：请求 = [谁, 哪个租户, 访问什么资源, 什么动作]

[policy_definition]
p = sub, dom, obj, act
# 解释：策略 = [角色/用户, 租户, 资源URL, 动作]

[role_definition]
g = _, _, _
# 解释：角色组 = [用户, 角色, 租户] 
# 含义：用户在某个租户下拥有某个角色

[policy_effect]
e = some(where (p.eft == allow))
# 解释：只要有一条策略允许，就放行

[matchers]
m = g(r.sub, p.sub, r.dom) && r.dom == p.dom && keyMatch2(r.obj, p.obj) && regexMatch(r.act, p.act)
# 解释：
# 1. g(...) : 检查用户是否拥有该角色（且必须是在当前租户 dom 下）。
# 2. r.dom == p.dom : 请求的租户必须匹配策略的租户（防止 A 租户的角色访问 B 租户的资源）。
# 3. keyMatch2 : 支持 URL 路径参数通配符 (如 /api/users/:id)。
# 4. regexMatch : 支持动作正则 (如 GET|POST)。
```

---

### 二、 策略数据设计 (Policy Strategy)

这是最关键的一步。我们需要制定一套**命名规范**，将你的数据库实体（SysUser, SysRole）映射到 Casbin 的字符串规则中。

#### 1. 映射规范 (Mapping)

| Casbin 字段 | 来源实体 | 建议格式 | 示例 |
| :--- | :--- | :--- | :--- |
| **sub (Subject)** | `SysUser.Id` 或 `SysRole.Code` | 用户加前缀，角色用 Code | 用户: `u_1001`<br>角色: `admin`, `sales_manager` |
| **dom (Domain)** | `SysTenant.Id` | 租户 ID | `tenant_alibaba`, `tenant_tencent` |
| **obj (Object)** | `SysApi.Path` | 接口 URL | `/api/sys/user/list` |
| **act (Action)** | `SysApi.Method` | HTTP 方法 | `GET`, `POST`, `PUT`, `DELETE` |

#### 2. 数据库中的策略示例 (CasbinRule 表)

Casbin 会在数据库生成一张 `CasbinRule` 表，里面的数据大概是这样的：

**A. 角色继承策略 (g policy) - 解决“谁是谁”**
*对应操作：管理员在后台给“张三”分配了“销售经理”角色。*

| PType | V0 (User) | V1 (Role) | V2 (Tenant) | 含义 |
| :--- | :--- | :--- | :--- | :--- |
| **g** | `u_1001` | `sales_manager` | `tenant_A` | 张三在租户A是销售经理 |
| **g** | `u_1002` | `admin` | `tenant_A` | 李四在租户A是管理员 |

**B. 资源访问策略 (p policy) - 解决“角色能干什么”**
*对应操作：管理员在后台给“销售经理”角色勾选了“查询订单”权限（对应 API）。*

| PType | V0 (Role) | V1 (Tenant) | V2 (API URL) | V3 (Method) | 含义 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **p** | `sales_manager` | `tenant_A` | `/api/orders` | `GET` | 销售经理可查询订单 |
| **p** | `sales_manager` | `tenant_A` | `/api/orders` | `POST` | 销售经理可创建订单 |
| **p** | `admin` | `tenant_A` | `/api/*` | `*` | **管理员通配所有接口** |

---

### 三、 具体的落地实施步骤

在 .NET Core Web API 中，如何把这些串起来？

#### 1. 超级管理员的“上帝模式” (Wildcard)

你肯定不希望给“超级管理员”分配权限时，要往数据库插几百条 API 规则。
**策略：** 使用通配符。

```csharp
// 当创建租户并初始化 "admin" 角色时，插入一条无敌规则：
await _enforcer.AddPolicyAsync("admin", "tenant_new", "/api/*", "(GET)|(POST)|(PUT)|(DELETE)");
// 或者更简单，如果你的 keyMatch2 支持前缀
await _enforcer.AddPolicyAsync("admin", "tenant_new", "*", "*"); 
```

#### 2. 日常权限分配 (Role-Menu-API 联动)

这是你之前问的重点：**实体表与 Casbin 的同步。**

**场景：** 管理员修改了“销售经理”角色的权限，勾选了 5 个菜单，这 5 个菜单背后对应 8 个 API。

**后端代码逻辑 (伪代码):**

```csharp
public async Task UpdateRolePermissions(string roleCode, string tenantId, List<long> menuIds)
{
    // 1. 查业务表：找出这些菜单对应的 API 列表
    // SysMenu 表里存了 ApiUrl (e.g., "GET:/api/orders")
    var apis = _db.SysMenus
                  .Where(m => menuIds.Contains(m.Id) && !string.IsNullOrEmpty(m.ApiUrl))
                  .Select(m => m.ApiUrl)
                  .ToList();

    // 2. 清理 Casbin 旧策略
    // 删除该角色在当前租户下的所有 p 策略
    // RemoveFilteredPolicy(0, roleCode, tenantId) -> 对应 p 的 V0, V1
    await _enforcer.RemoveFilteredPolicyAsync(0, roleCode, tenantId);

    // 3. 插入 Casbin 新策略
    foreach (var api in apis)
    {
        // 假设 api 字符串格式是 "GET:/api/orders"
        var parts = api.Split(':'); 
        var method = parts[0];
        var path = parts[1];

        // 添加策略: p, sales_manager, tenant_A, /api/orders, GET
        await _enforcer.AddPolicyAsync(roleCode, tenantId, path, method);
    }
    
    // 4. 保存业务表关系 (SysRoleMenu) 用于前端回显
    // ... Save SysRoleMenu ...
}
```

#### 3. 运行时鉴权 (Middleware)

当请求进来时，中间件如何工作？

```csharp
public async Task InvokeAsync(HttpContext context)
{
    // 1. 获取当前用户信息
    var userId = "u_" + context.User.FindFirst("sub")?.Value; // u_1001
    var tenantId = context.User.FindFirst("tenant")?.Value;   // tenant_A
    var path = context.Request.Path;                          // /api/orders
    var method = context.Request.Method;                      // GET

    // 2. 调用 Casbin 检查
    // Casbin 会自动查找 g 策略，发现 u_1001 是 sales_manager
    // 然后查找 p 策略，发现 sales_manager 允许 GET /api/orders
    bool isAllowed = await _enforcer.EnforceAsync(userId, tenantId, path, method);

    if (!isAllowed)
    {
        context.Response.StatusCode = 403; // Forbidden
        return;
    }

    await _next(context);
}
```

---

### 四、 几个高级设计技巧

为了让你的系统更强大，建议考虑以下几点：

1.  **接口与按钮的 N:N 关系处理：**
    *   有些按钮可能不调用 API（纯前端跳转），Casbin 不需要存。
    *   有些按钮调用多个 API（"保存" -> Create + Upload）。
    *   **建议：** 在 `SysMenu` 表中，如果类型是按钮，增加一个 `ApiIds` 字段（JSON数组），关联到 `SysApi` 表。同步 Casbin 时，遍历这个数组。

2.  **公共接口 (Public API)：**
    *   登录接口、获取验证码接口、健康检查接口。
    *   **不要** 放进 Casbin。
    *   **做法：** 在中间件里做一个 `WhiteList` (List<string>)，如果是白名单路径，直接 `await _next(context)` 跳过 Casbin 检查。

3.  **性能优化：**
    *   Casbin 默认是从内存读取策略（非常快），但如果策略有几十万条（超大型系统），加载会慢。
    *   **SaaS 优化：** 你的 `model.conf` 里有 `r.dom == p.dom`。这意味着 Casbin 在匹配时，其实只需要加载当前租户的策略。如果使用支持分片加载的 Adapter，可以极大提升性能。不过对于一般企业级应用（几万条规则），全量加载到内存完全没问题。

### 总结

*   **Model:** 使用 `RBAC with Domains`。
*   **Subject:** `u_ID` (用户) 和 `RoleCode` (角色)。
*   **Domain:** `TenantId`。
*   **Object:** `API URL`。
*   **同步机制:** 修改角色权限时，先删后加 (`RemoveFilteredPolicy` -> `AddPolicy`)。

这样设计，Casbin 就完美地充当了你系统的**“守门员”**，而复杂的业务逻辑（数据范围、字段过滤）则在进门之后由你的业务代码处理。


### 给你的建议

你现在正处于架构设计的关键期。如果要对标这个强大的系统，你的开发顺序应该是：
1.  **先建表**：按照上面的表格建立实体。
2.  **搞定 Casbin**：实现最基础的“菜单-接口”拦截（白名单模式）。
3.  **搞定数据权限**：这是难点，建议在 .NET 中利用 EF Core 的 `IQueryable` 扩展方法来实现动态的数据范围过滤。
4.  **最后做字段权限**：这是一个锦上添花的功能，前期可以先不做，等系统稳定了再加。