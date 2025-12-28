# 实体继承策略与导航属性使用指南

> 创建日期: 2025-11-18
> 目的: 为 SharpFort 项目提供实体设计的最佳实践指南

---

## 一、实体继承策略

### 1.1 ABP 框架提供的基类层次

```
Entity<TKey>
    └── AggregateRoot<TKey>
            └── AuditedAggregateRoot<TKey>         (创建 + 修改审计)
                    └── FullAuditedAggregateRoot<TKey>  (创建 + 修改 + 软删除审计)
```

### 1.2 选择基类的决策树

```
实体是否为聚合根？
├── 是 → 需要审计吗？
│       ├── 不需要 → AggregateRoot<TKey>
│       ├── 需要创建+修改 → AuditedAggregateRoot<TKey>
│       └── 需要完整审计（含软删除） → FullAuditedAggregateRoot<TKey>
│
└── 否 → 是否为关联表实体？
        ├── 是（如 UserRole） → Entity<TKey>
        └── 否（如子实体） → 考虑值对象或简单实体
```

### 1.3 实体类型分类与推荐基类

| 实体类型 | 业务特征 | 推荐基类 | 实现接口 | 示例 |
|---------|---------|---------|---------|------|
| **核心业务实体** | 高业务价值、需要完整审计追踪 | `FullAuditedAggregateRoot` | 无需额外接口 | User, Role, Department |
| **配置/元数据实体** | 需要审计但不需软删除 | `AuditedAggregateRoot` | `IOrderNum`, `IState` | Menu, Config, DictionaryType |
| **日志/记录实体** | 仅需创建时间、不修改 | `AggregateRoot` | `ICreationAuditedObject` | LoginLog, AuditLog, AccessLog |
| **关联表实体** | 纯关系表、无业务逻辑 | `Entity` | 无 | UserRole, RoleMenu, RoleDept |
| **临时/过程实体** | 短期存在、可物理删除 | `Entity` | 按需 | SignIn (签到), Assignment |

### 1.4 当前项目实体改进建议

#### RBAC 模块

| 实体 | 当前实现 | 问题 | 建议改进 |
|------|---------|------|---------|
| `UserAggregateRoot` | 手动实现 ISoftDelete, IAuditedObject | 缺少 DeleterId, DeletionTime | 改用 `FullAuditedAggregateRoot` |
| `RoleAggregateRoot` | 同上 | 同上 | 改用 `FullAuditedAggregateRoot` |
| `MenuAggregateRoot` | 同上 | 同上 | 改用 `FullAuditedAggregateRoot` |
| `DeptAggregateRoot` | 同上 | 同上 | 改用 `FullAuditedAggregateRoot` |
| `LoginLogAggregateRoot` | 需确认 | 日志不需修改审计 | 使用 `AggregateRoot` + `ICreationAuditedObject` |
| `UserRoleEntity` | `Entity` | 正确 | 保持不变 |
| `ConfigAggregateRoot` | 需确认 | 配置不需软删除 | 考虑 `AuditedAggregateRoot` |

#### BBS 模块

| 实体 | 当前实现 | 建议改进 |
|------|---------|---------|
| `DiscussAggregateRoot` | ISoftDelete, IAuditedObject | 改用 `FullAuditedAggregateRoot` |
| `CommentAggregateRoot` | 需确认 | 改用 `FullAuditedAggregateRoot` |
| `SignInAggregateRoot` | 需确认 | 使用 `AggregateRoot` + `ICreationAuditedObject` |
| `AccessLogAggregateRoot` | 需确认 | 使用 `AggregateRoot` + `ICreationAuditedObject` |

### 1.5 改进后的代码示例

**改进前（手动实现）:**
```csharp
public class UserAggregateRoot : AggregateRoot<Guid>, ISoftDelete, IAuditedObject, IOrderNum, IState
{
    public bool IsDeleted { get; set; }
    public DateTime CreationTime { get; set; } = DateTime.Now;  // 问题：手动初始化
    public Guid? CreatorId { get; set; }
    public Guid? LastModifierId { get; set; }
    public DateTime? LastModificationTime { get; set; }
    // 缺少 DeleterId 和 DeletionTime
}
```

**改进后（使用框架基类）:**
```csharp
[SugarTable("User")]
public class User : FullAuditedAggregateRoot<Guid>, IOrderNum, IState
{
    // 自动包含：
    // - CreationTime, CreatorId (ICreationAuditedObject)
    // - LastModificationTime, LastModifierId (IModificationAuditedObject)
    // - IsDeleted, DeleterId, DeletionTime (IDeletionAuditedObject)

    public int OrderNum { get; set; }
    public bool State { get; set; } = true;

    // ... 业务属性
}
```

### 1.6 自定义接口使用场景

| 接口 | 用途 | 使用场景 |
|------|------|---------|
| `IOrderNum` | 排序字段 | 树形结构、列表排序 |
| `IState` | 启用/禁用状态 | 需要激活/停用功能的实体 |
| `IMultiTenant` | 多租户隔离 | SaaS 应用的租户数据隔离 |

---

## 二、导航属性使用策略

### 2.1 SqlSugar Navigate 特性概述

SqlSugar 提供了 `[Navigate]` 特性来定义实体间的关系，主要类型：

| 关系类型 | 语法 | 用途 |
|---------|------|------|
| 一对一 | `[Navigate(NavigateType.OneToOne, nameof(ForeignKey))]` | 如 User -> Dept |
| 一对多 | `[Navigate(NavigateType.OneToMany, nameof(ForeignKey))]` | 如 Dept -> Users |
| 多对多 | `[Navigate(typeof(JoinTable), nameof(JoinTable.AId), nameof(JoinTable.BId))]` | 如 User <-> Role |

### 2.2 导航属性设计原则

#### 原则一：明确聚合边界

```csharp
// 良好：User 是聚合根，拥有对 Role 的引用
public class User : FullAuditedAggregateRoot<Guid>
{
    // 跨聚合引用使用 ID
    public Guid? DeptId { get; set; }

    // 导航属性用于查询便利，不用于持久化
    [Navigate(NavigateType.OneToOne, nameof(DeptId))]
    public Department? Dept { get; set; }
}

// 不建议：在实体内部直接操作其他聚合
public class User
{
    public void AssignToDepartment(Department dept)
    {
        this.Dept = dept;  // 不好：跨聚合直接赋值
        dept.Users.Add(this);  // 不好：修改其他聚合
    }
}

// 建议：通过领域服务处理跨聚合操作
public class UserDomainService
{
    public async Task AssignToDepartmentAsync(User user, Guid deptId)
    {
        user.DeptId = deptId;
        await _userRepository.UpdateAsync(user);
    }
}
```

#### 原则二：集合属性初始化

```csharp
// 问题：未初始化可能导致 NullReferenceException
public List<Role> Roles { get; set; }

// 正确：始终初始化集合属性
public List<Role> Roles { get; set; } = new();

// 或使用延迟初始化
private List<Role>? _roles;
public List<Role> Roles => _roles ??= new();
```

#### 原则三：可空性标注

```csharp
// 一对一可选关系：使用可空类型
[Navigate(NavigateType.OneToOne, nameof(DeptId))]
public Department? Dept { get; set; }  // ? 表示可能为空

// 一对多/多对多：集合始终非空但可能为空集合
[Navigate(typeof(UserRole), nameof(UserRole.UserId), nameof(UserRole.RoleId))]
public List<Role> Roles { get; set; } = new();  // 初始化为空集合
```

### 2.3 关系表实体设计

#### 简单关联表（无额外属性）

```csharp
[SugarTable("UserRole")]
public class UserRole : Entity<Guid>
{
    [SugarColumn(IsPrimaryKey = true)]
    public override Guid Id { get; protected set; }

    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}
```

#### 带额外属性的关联表

```csharp
[SugarTable("UserRole")]
public class UserRole : Entity<Guid>, ICreationAuditedObject
{
    [SugarColumn(IsPrimaryKey = true)]
    public override Guid Id { get; protected set; }

    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    // 额外业务属性
    public DateTime AssignedAt { get; set; }
    public Guid? AssignedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }  // 角色分配过期时间

    // 审计属性
    public DateTime CreationTime { get; set; }
    public Guid? CreatorId { get; set; }
}
```

### 2.4 查询优化策略

#### 延迟加载 vs 预加载

```csharp
// 延迟加载（默认）：需要时才查询
var user = await _userRepository.GetAsync(userId);
// 此时 user.Roles 为 null 或空

// 预加载：一次性加载关联数据
var user = await _userRepository
    .AsQueryable()
    .Includes(u => u.Roles)
    .Includes(u => u.Dept)
    .FirstAsync(u => u.Id == userId);
```

#### N+1 问题避免

```csharp
// 问题：N+1 查询
var users = await _userRepository.GetListAsync();
foreach (var user in users)
{
    var roles = await _roleRepository.GetListByUserIdAsync(user.Id);  // N 次查询
}

// 解决：批量预加载
var users = await _userRepository
    .AsQueryable()
    .Includes(u => u.Roles)  // 单次 JOIN 查询
    .ToListAsync();
```

### 2.5 数据一致性保障

#### 外键约束（数据库层）

```csharp
// 在 DbContext 配置中定义外键
modelBuilder.Entity<User>()
    .HasOne(u => u.Dept)
    .WithMany(d => d.Users)
    .HasForeignKey(u => u.DeptId)
    .OnDelete(DeleteBehavior.SetNull);  // 级联行为
```

#### 领域事件（应用层）

```csharp
// 当部门被删除时，通过领域事件处理相关用户
public class DepartmentDeletedEventHandler : ILocalEventHandler<DepartmentDeletedEto>
{
    public async Task HandleEventAsync(DepartmentDeletedEto eventData)
    {
        // 将该部门下的用户设置为无部门
        await _userRepository.UpdateManyAsync(
            u => u.DeptId == eventData.DeptId,
            u => u.DeptId = null
        );
    }
}
```

### 2.6 当前项目导航属性问题

| 实体 | 问题 | 建议 |
|------|------|------|
| `UserAggregateRoot.Roles` | 未初始化 | 添加 `= new()` |
| `UserAggregateRoot.Posts` | 未初始化 | 添加 `= new()` |
| `RoleAggregateRoot.Menus` | 可空但无初始化 | 使用 `= new()` 或保持 `?` |
| `RoleAggregateRoot.Depts` | 同上 | 同上 |
| `MenuAggregateRoot.Children` | IsIgnore=true，OK | 保持不变 |

---

## 三、最佳实践总结

### 3.1 实体设计清单

- [ ] 选择正确的基类（根据审计需求）
- [ ] 移除冗余的类型后缀（AggregateRoot, Entity）
- [ ] 初始化所有集合属性
- [ ] 正确标注可空性（`?`）
- [ ] 使用 `[SugarTable]` 保持表名兼容
- [ ] 使用 `init` 或 `private set` 保护关键属性

### 3.2 导航属性设计清单

- [ ] 明确定义关系类型（1:1, 1:N, M:N）
- [ ] 跨聚合使用 ID 引用，不直接引用实体
- [ ] 初始化集合属性避免 NullReferenceException
- [ ] 考虑查询性能（预加载 vs 延迟加载）
- [ ] 通过领域服务处理跨聚合操作

### 3.3 命名约定

| 类型 | 约定 | 示例 |
|------|------|------|
| 实体类名 | PascalCase，无后缀 | `User`, `Role`, `Menu` |
| 外键属性 | `{关联实体}Id` | `DeptId`, `RoleId` |
| 导航属性 | 单数或复数根据关系 | `Dept`, `Roles` |
| 关联表 | `{A实体}{B实体}` | `UserRole`, `RoleMenu` |

---

## 四、迁移指南

### 4.1 从手动实现迁移到框架基类

```csharp
// 步骤 1: 修改基类
- public class UserAggregateRoot : AggregateRoot<Guid>, ISoftDelete, IAuditedObject
+ public class User : FullAuditedAggregateRoot<Guid>

// 步骤 2: 移除手动实现的属性
- public bool IsDeleted { get; set; }
- public DateTime CreationTime { get; set; } = DateTime.Now;
- public Guid? CreatorId { get; set; }
- public Guid? LastModifierId { get; set; }
- public DateTime? LastModificationTime { get; set; }

// 步骤 3: 保留 SugarTable 特性以兼容数据库
[SugarTable("User")]  // 保持原表名
public class User : FullAuditedAggregateRoot<Guid>
```

### 4.2 添加缺失的审计字段

如果当前数据库缺少 `DeleterId` 和 `DeletionTime` 字段，需要添加数据库迁移：

```sql
-- SQL Server
ALTER TABLE [User] ADD DeleterId UNIQUEIDENTIFIER NULL;
ALTER TABLE [User] ADD DeletionTime DATETIME2 NULL;

-- MySQL
ALTER TABLE `User` ADD COLUMN `DeleterId` CHAR(36) NULL;
ALTER TABLE `User` ADD COLUMN `DeletionTime` DATETIME(6) NULL;
```

---

**文档版本**: v1.0
**最后更新**: 2025-11-18
**负责人**: Claude AI Assistant

