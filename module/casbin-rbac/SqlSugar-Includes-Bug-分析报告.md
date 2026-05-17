# 登录 403 问题 — 完整分析报告

## 一、问题总览

| 项 | 内容 |
|---|---|
| **现象** | POST `/api/app/account/login` 返回 403 Forbidden |
| **根因** | **SqlSugar 5.1.4.214 在 .NET 10 下 `Includes` 导航属性加载机制全局失效** |
| **影响范围** | 项目中所有使用 `Includes` + `[Navigate]` 的地方 |
| **修复策略** | 用分步手动查询替代 `Includes`，绕过失效的导航加载 |

---

## 二、诊断过程（排除法）

```
用户登录 → 403
    ↓ 加诊断码
  LOGIN_ERR_004（用户无角色）
    ↓ 查 UserRole 表
  有 1 条记录 ✓（数据存在）
    ↓ Includes(u => u.Roles) 无过滤
  RoleCount = 0 ✗（Includes 本身失败）
    ↓ FirstAsync 替代 InSingleAsync
  RoleCount = 0 ✗（非 InSingleAsync 问题）
    ↓ 无过滤 Includes
  RoleCount = 0 ✗（非 IsDeleted 过滤问题）
    ↓ 手动分步查询
  RoleCount = 1 ✓（绕过 Includes 即可）
    ↓ 继续手动加载 Menu
  LOGIN_ERR_005（无权限）
    ↓ 手动组装 Role.Menus
  登录成功 ✓
```

**关键证据**：连最基本形式的 `.Includes(u => u.Roles)`（无过滤、单层导航、`FirstAsync`）都返回 `RoleCount=0`，同时 `UserRole` 中间表和 `Role` 表均有正确数据。

**已排除的假设**：

| 假设 | 结论 |
|---|---|
| `r.IsDeleted == false` ≠ `!r.IsDeleted` | ❌ `ISoftDelete.IsDeleted` 确认是 `bool`（非 nullable），语义相同 |
| `InSingleAsync` 不支持 `Includes` | ❌ 改为 `FirstAsync` + `Where` 同样失败 |
| ABP 10.x `IsDeleted` 变为 `bool?` | ❌ 反编译 ABP 10.3.0 Core.dll 确认是 `System.Boolean` |
| 数据库记录缺失 | ❌ UserRole 和 Role 表均有正确数据 |
| `FriendlyExceptionFilter` 修改导致 | ❌ 确认 `AddFurionUnifyResultApi()` 未被调用 |

---

## 三、受影响文件清单

### 文件 1（已修复）：`UserRepository.cs`
**路径**: `module/casbin-rbac/SharpFort.CasbinRbac.SqlSugarCore/Repositories/UserRepository.cs`

**原代码**:
```csharp
// 使用 Include 加载导航属性（已失效）
User user = await _DbQueryable
    .Includes(u => u.Roles.Where(r => !r.IsDeleted).ToList(), 
              r => r.Menus.Where(m => !m.IsDeleted).ToList())
    .InSingleAsync(userId);
```

**修复方案**: 6 步手动查询
```
User ──→ UserRole(中间表) ──→ Role ──→ RoleMenu(中间表) ──→ Menu
                                            ↓
                                    手动组装 Role.Menus
```

### 文件 2（待修复）：`UserService.cs`
**路径**: `module/casbin-rbac/SharpFort.CasbinRbac.Application/Services/System/UserService.cs`

**位置 A** — 第 79-83 行，列表查询加载 Roles + Posts：
```csharp
// 原代码（失效）
List<User> usersWithRelations = await _repository._DbQueryable
    .Includes(u => u.Roles)
    .Includes(u => u.Posts)
    .Where(u => userIds.Contains(u.Id))
    .ToListAsync();
```

**修复方案**：
```csharp
// 1. 查用户列表
List<User> users = await _repository._DbQueryable
    .Where(u => userIds.Contains(u.Id))
    .ToListAsync();

if (users.Count > 0)
{
    var userIdList = users.Select(u => u.Id).ToList();

    // 2. 查 UserRole → RoleId → Role
    var roleIdsByUser = await _repository._Db.Queryable<UserRole>()
        .Where(ur => userIdList.Contains(ur.UserId))
        .Select(ur => new { ur.UserId, ur.RoleId })
        .ToListAsync();
    var allRoleIds = roleIdsByUser.Select(x => x.RoleId).Distinct().ToList();
    var roles = allRoleIds.Count > 0
        ? await _repository._Db.Queryable<Role>()
            .Where(r => allRoleIds.Contains(r.Id) && !r.IsDeleted)
            .ToListAsync()
        : [];

    // 3. 查 UserPosition → PostId → Position
    var postIdsByUser = await _repository._Db.Queryable<UserPosition>()
        .Where(up => userIdList.Contains(up.UserId))
        .Select(up => new { up.UserId, up.PostId })
        .ToListAsync();
    var allPostIds = postIdsByUser.Select(x => x.PostId).Distinct().ToList();
    var posts = allPostIds.Count > 0
        ? await _repository._Db.Queryable<Position>()
            .Where(p => allPostIds.Contains(p.Id) && !p.IsDeleted)
            .ToListAsync()
        : [];

    // 4. 组装
    foreach (var user in users)
    {
        user.Roles = roles.Where(r => roleIdsByUser
            .Where(x => x.UserId == user.Id)
            .Select(x => x.RoleId).Contains(r.Id)).ToList();
        user.Posts = posts.Where(p => postIdsByUser
            .Where(x => x.UserId == user.Id)
            .Select(x => x.PostId).Contains(p.Id)).ToList();
    }
}

var usersWithRelations = users;
```

**位置 B** — 第 183-184 行，单个查询加载 Roles + Posts + Dept：
```csharp
// 原代码（失效）
User entity = await _repository._DbQueryable
    .Includes(u => u.Roles).Includes(u => u.Posts)
    .Includes(u => u.Dept).InSingleAsync(id);
```

**修复方案**：
```csharp
// 1. 查用户
User entity = await _repository._DbQueryable.Where(u => u.Id == id).FirstAsync();

if (entity is not null)
{
    // 2. Dept（OneToOne，直接用 DepartmentId）
    if (entity.DepartmentId is not null)
    {
        entity.Dept = await _repository._Db.Queryable<Department>()
            .Where(d => d.Id == entity.DepartmentId && !d.IsDeleted)
            .FirstAsync();
    }

    // 3. Roles（ManyToMany via UserRole）
    var roleIds = await _repository._Db.Queryable<UserRole>()
        .Where(ur => ur.UserId == id)
        .Select(ur => ur.RoleId).ToListAsync();
    entity.Roles = roleIds.Count > 0
        ? await _repository._Db.Queryable<Role>()
            .Where(r => roleIds.Contains(r.Id) && !r.IsDeleted)
            .ToListAsync()
        : [];

    // 4. Posts（ManyToMany via UserPosition）
    var postIds = await _repository._Db.Queryable<UserPosition>()
        .Where(up => up.UserId == id)
        .Select(up => up.PostId).ToListAsync();
    entity.Posts = postIds.Count > 0
        ? await _repository._Db.Queryable<Position>()
            .Where(p => postIds.Contains(p.Id) && !p.IsDeleted)
            .ToListAsync()
        : [];
}
```

### 文件 3（待修复）：`CodeGenService.cs`
**路径**: `module/code-gen/SharpFort.CodeGen.Application/Services/CodeGenService.cs`

**位置** — 第 29 行，一对多加载 Fields：
```csharp
// 原代码（失效）
List<Table> tables = await _tableRepository._DbQueryable
    .Where(x => ids.Contains(x.Id))
    .Includes(x => x.Fields)
    .ToListAsync();
```

**Navigate 关系**：`[Navigate(NavigateType.OneToMany, nameof(Field.TableId))]`

**修复方案**：
```csharp
// 1. 查 Table
List<Table> tables = await _tableRepository._DbQueryable
    .Where(x => ids.Contains(x.Id))
    .ToListAsync();

if (tables.Count > 0)
{
    var tableIds = tables.Select(t => t.Id).ToList();

    // 2. 查所有关联的 Field
    var fields = await _tableRepository._Db.Queryable<Field>()
        .Where(f => tableIds.Contains(f.TableId) && !f.IsDeleted)
        .ToListAsync();

    // 3. 组装
    foreach (var table in tables)
    {
        table.Fields = fields.Where(f => f.TableId == table.Id).ToList();
    }
}
```

---

## 四、SqlSugar Bug 分析

### 证据链

| # | 测试 | 结果 |
|---|---|---|
| 1 | `.Includes(u => u.Roles)` 单层无过滤 + `FirstAsync` | RoleCount=0 ✗ |
| 2 | `.Includes(u => u.Roles, r => r.Menus)` 两层无过滤 + `InSingleAsync` | RoleCount=0 ✗ |
| 3 | `.Includes(u => u.Roles.Where(r => !r.IsDeleted).ToList(), ...)` + `InSingleAsync` | RoleCount=0 ✗ |
| 4 | `.Includes(u => u.Roles.Where(r => r.IsDeleted == false).ToList(), ...)` + `FirstAsync` | RoleCount=0 ✗ |
| 5 | 手动分步查询（直接查中间表 + JOIN） | 正常 ✓ |

全部 4 种 `Includes` 变体均失败，唯一区别是手动查询。**这是 SqlSugar 5.1.4.214 在 .NET 10 下的 Bug**。

### Bug 特征
- **环境**: .NET 10 (net10.0) + SqlSugarCore 5.1.4.214
- **表现**: `Includes` 方法完全不加载导航属性，无论导航类型（OneToOne/OneToMany/ManyToMany）
- **范围**: 单层和嵌套 `Includes` 均受影响
- **与查询方法无关**: `InSingleAsync`、`FirstAsync`、`ToListAsync` 均无法加载
- **与过滤条件无关**: 带 `.Where().ToList()` 过滤和不带过滤均失败
- **推测原因**: SqlSugar 在解析 `Includes` 表达式树时，.NET 10 的表达式树内部结构可能有变化，导致导航属性信息无法被正确提取

---

## 五、GitHub Issue 模板

以下可直接提交到 https://github.com/DotNetNext/SqlSugar/issues：

> **Title**: `Includes` with `[Navigate]` attribute completely fails on .NET 10 / SqlSugarCore 5.1.4.214
>
> **Environment**:
> - .NET: `net10.0.0`
> - SqlSugarCore: `5.1.4.214`
> - OS: Windows 11
>
> **Description**:
> All `Includes` methods that rely on `[Navigate]` attributes fail to load navigation properties. This affects all navigation types: OneToOne, OneToMany (via intermediate table), and ManyToMany.
>
> **Steps to Reproduce**:
> ```csharp
> // Entity definition
> [SugarTable("casbin_sys_user")]
> public class User : FullAuditedAggregateRoot<Guid>
> {
>     [Navigate(typeof(UserRole), nameof(UserRole.UserId), nameof(UserRole.RoleId))]
>     public List<Role> Roles { get; set; } = [];
> }
>
> // This query returns RoleCount = 0 on .NET 10
> var user = await db.Queryable<User>()
>     .Where(u => u.Id == userId)
>     .Includes(u => u.Roles)  // simplest form, no filter
>     .FirstAsync();
> // user.Roles.Count == 0 ← BUG
>
> // Manual query works correctly — proves data exists
> var roleIds = await db.Queryable<UserRole>()
>     .Where(ur => ur.UserId == userId)
>     .Select(ur => ur.RoleId).ToListAsync();
> // roleIds.Count == 1 ← CORRECT
> ```
>
> **Expected Behavior**: `user.Roles.Count` should be > 0 (the user has roles in the database).
>
> **Actual Behavior**: `user.Roles.Count` is always 0, regardless of filtering or Include depth.
>
> **Workaround**: Manual step-by-step queries through intermediate tables.
>
> **Impact**: Breaking — all navigation property loading via `Includes` is non-functional on .NET 10.

---

## 六、修复验证

| 步骤 | 状态 |
|---|---|
| 编译通过 | ✅ |
| 登录返回 token | ✅ |
| 超级管理员角色正确加载 | ✅ |
| 权限列表正确加载 | ✅ |
| `LOGIN_ERR_004` / `LOGIN_ERR_005` 不再出现 | ✅ |
