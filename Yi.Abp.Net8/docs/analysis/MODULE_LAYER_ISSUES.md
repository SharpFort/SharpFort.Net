# SharpFort 模块层问题分析报告

> 分析日期: 2025-11-16
> 分析范围: `Yi.Abp.Net8/module/` 目录下的 6 个业务模块

---

## 一、模块概览

| 模块名称 | 功能描述 | 项目数量 | 问题等级 |
|---------|---------|---------|---------|
| **rbac** | 基于角色的访问控制 | 5 | **严重** |
| **bbs** | 论坛系统 | 5 | 中等 |
| **audit-logging** | 审计日志 | 3 | 轻微 |
| **tenant-management** | 多租户管理 | 4 | 轻微 |
| **setting-management** | 设置管理 | 3 | 轻微 |
| **code-gen** | 代码生成 | 5 | 中等 |

---

## 二、严重问题 (Critical Issues)

### 2.1 密码加密方案不安全

**位置**: `module/rbac/Yi.Framework.Rbac.Domain/Entities/UserAggregateRoot.cs`

**当前实现** (第 179-193 行):
```csharp
public UserAggregateRoot BuildPassword(string password = null)
{
    if (password == null)
    {
        password = EncryPassword.Password;
    }
    EncryPassword.Salt = MD5Helper.GenerateSalt();
    EncryPassword.Password = MD5Helper.SHA2Encode(password, EncryPassword.Salt);
    return this;
}
```

**问题分析**:
1. **使用 SHA512 而非专用密码哈希算法**
   - SHA512 是通用哈希函数，设计目标是快速
   - 密码哈希需要慢速算法以防暴力破解

2. **固定迭代次数**
   - 无工作因子(work factor)配置
   - 随硬件性能提升，安全性下降

3. **自定义盐值生成**
   - 使用废弃的 `RNGCryptoServiceProvider`
   - BCrypt 已内置安全盐值生成

**安全风险**:
- 密码数据库泄露后，攻击者可快速破解密码
- 不符合 OWASP 和 NIST 安全标准
- 潜在的合规性问题（GDPR、等保）

**推荐修复**:
```csharp
// 安装: dotnet add package BCrypt.Net-Next
public UserAggregateRoot BuildPassword(string password)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(password);
    // BCrypt 自动生成盐值并包含在哈希中
    EncryPassword.Password = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    EncryPassword.Salt = string.Empty; // BCrypt 不需要单独的盐值字段
    return this;
}

public bool JudgePassword(string password)
{
    return BCrypt.Net.BCrypt.Verify(password, EncryPassword.Password);
}
```

---

### 2.2 值对象设计问题

**位置**: `module/rbac/Yi.Framework.Rbac.Domain/Entities/ValueObjects/EncryPasswordValueObject.cs`

**当前实现**:
```csharp
public class EncryPasswordValueObject : ValueObject
{
    public string Password { get; set; } = string.Empty;  // 可变属性
    public string Salt { get; set; } = string.Empty;      // 可变属性
}
```

**DDD 违规**:
- **值对象应该是不可变的** (Immutable)
- 使用 `set` 而非 `init` 或私有 setter
- 违反值对象的基本原则

**推荐修复**:
```csharp
public class PasswordHash : ValueObject
{
    public PasswordHash(string hashedPassword)
    {
        HashedValue = hashedPassword ?? throw new ArgumentNullException(nameof(hashedPassword));
    }

    public string HashedValue { get; init; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return HashedValue;
    }
}
```

---

## 三、DDD 实现问题

### 3.1 实体命名冗余

**问题**: 所有聚合根和实体都带有类型后缀

**RBAC 模块示例**:
| 当前命名 | 建议命名 |
|---------|---------|
| `UserAggregateRoot` | `User` |
| `RoleAggregateRoot` | `Role` |
| `MenuAggregateRoot` | `Menu` |
| `DeptAggregateRoot` | `Department` |
| `PostAggregateRoot` | `Position` |
| `UserRoleEntity` | `UserRole` |
| `RoleMenuEntity` | `RoleMenu` |
| `DictionaryEntity` | `DictionaryItem` |

**BBS 模块示例**:
| 当前命名 | 建议命名 |
|---------|---------|
| `DiscussAggregateRoot` | `Discussion` |
| `CommentAggregateRoot` | `Comment` |
| `ArticleAggregateRoot` | `Article` |
| `BannerAggregateRoot` | `Banner` |
| `PlateAggregateRoot` | `Forum` 或 `Plate` |
| `AssignmentAggregateRoot` | `Assignment` |

**DDD 最佳实践**:
> "实体类名应反映业务概念，技术类型信息由基类提供"

```csharp
// 错误: 技术细节泄露到类名
public class UserAggregateRoot : AggregateRoot<Guid>

// 正确: 纯业务命名
public class User : AggregateRoot<Guid>
```

**总计**: 33+ 个实体需要重命名

---

### 3.2 枚举命名不规范

**位置**: `module/*/Domain.Shared/Enums/`

**发现的问题枚举** (共 20+ 个):

#### RBAC 模块
```csharp
// 问题1: Enum 后缀冗余
public enum SexEnum { ... }          // 应为 Gender 或 Sex
public enum MenuTypeEnum { ... }     // 应为 MenuType
public enum DataScopeEnum { ... }    // 应为 DataScope
public enum JobTypeEnum { ... }      // 应为 JobType

// 问题2: 枚举值命名不一致
public enum SexEnum
{
    Male = 0,
    Woman = 1,   // 应为 Female (与 Male 配对)
    Unknown = 2
}
```

#### BBS 模块
```csharp
public enum DiscussTypeEnum { ... }           // 应为 DiscussionType
public enum AssignmentStateEnum { ... }       // 应为 AssignmentState
public enum GoodsTypeEnum { ... }             // 应为 GoodsType
public enum BankCardStateEnum { ... }         // 应为 BankCardState
```

**.NET 命名约定**:
> "枚举类型名称不应有 'Enum' 后缀"
> "枚举值应使用 PascalCase，且语义一致"

---

### 3.3 审计接口使用不完整

**位置**: `module/rbac/Yi.Framework.Rbac.Domain/Entities/UserAggregateRoot.cs:16`

**当前实现**:
```csharp
public class UserAggregateRoot : AggregateRoot<Guid>, ISoftDelete, IAuditedObject, IOrderNum, IState
{
    public bool IsDeleted { get; set; }
    public DateTime CreationTime { get; set; } = DateTime.Now;
    public Guid? CreatorId { get; set; }
    public Guid? LastModifierId { get; set; }
    public DateTime? LastModificationTime { get; set; }
    // 缺少 DeleterId 和 DeletionTime
}
```

**问题**:
1. 实现 `ISoftDelete` 但没有 `DeleterId` 和 `DeletionTime`
2. 审计信息不完整
3. 手动初始化 `DateTime.Now`（应由框架处理）

**推荐**: 使用 ABP 提供的组合接口
```csharp
public class User : FullAuditedAggregateRoot<Guid>
{
    // 自动包含:
    // - CreationTime, CreatorId
    // - LastModificationTime, LastModifierId
    // - IsDeleted, DeleterId, DeletionTime
}
```

---

## 四、中等问题 (Medium Priority)

### 4.1 业务逻辑泄露到实体

**位置**: `UserAggregateRoot.cs:27`

```csharp
public UserAggregateRoot(string userName, string password, long? phone, string? nick = null)
{
    UserName = userName;
    EncryPassword.Password = password;
    Phone = phone;
    Nick = string.IsNullOrWhiteSpace(nick) ? "萌新-" + userName : nick.Trim();
    BuildPassword();
}
```

**问题**:
- 硬编码默认昵称 "萌新-"
- 业务规则混入构造函数
- 不利于国际化

**推荐**:
```csharp
public User(string userName, string nick)
{
    UserName = userName ?? throw new ArgumentNullException(nameof(userName));
    Nick = nick ?? throw new ArgumentNullException(nameof(nick));
}

// 在领域服务或工厂中处理默认值
public class UserFactory
{
    public User Create(string userName, string? nick = null)
    {
        var defaultNick = _nickNameGenerator.Generate(userName);
        return new User(userName, nick ?? defaultNick);
    }
}
```

---

### 4.2 导航属性未初始化

**位置**: `UserAggregateRoot.cs:159-160`

```csharp
[Navigate(...)]
public List<RoleAggregateRoot> Roles { get; set; }  // 未初始化，可能 NullReferenceException

[Navigate(...)]
public List<PostAggregateRoot> Posts { get; set; }  // 同上
```

**推荐**:
```csharp
public List<Role> Roles { get; set; } = new();
public List<Position> Posts { get; set; } = new();
```

---

### 4.3 布尔判断方法命名

**位置**: `UserAggregateRoot.cs:200`

```csharp
public bool JudgePassword(string password)  // "Judge" 不够清晰
```

**推荐**:
```csharp
public bool VerifyPassword(string password)
// 或
public bool ValidatePassword(string password)
```

---

## 五、跨模块问题

### 5.1 重复的通知枚举

**发现**:
- `module/rbac/Yi.Framework.Rbac.Domain.Shared/Enums/NoticeTypeEnum.cs`
- `module/bbs/Yi.Framework.Bbs.Domain.Shared/Enums/NoticeTypeEnum.cs`

**问题**: 相同名称的枚举在不同模块中重复定义

**建议**:
1. 如果语义相同，提取到共享包
2. 如果语义不同，使用更明确的命名（如 `BbsNoticeType`）

---

### 5.2 实体/聚合根后缀统计

| 模块 | AggregateRoot 后缀 | Entity 后缀 | 总计 |
|------|-------------------|-------------|------|
| rbac | 11 | 4 | 15 |
| bbs | 18 | 1 | 19 |
| audit-logging | 1 | 3 | 4 |
| code-gen | 2 | 1 | 3 |
| tenant-management | 1 | 0 | 1 |
| setting-management | 1 | 0 | 1 |
| **总计** | **34** | **9** | **43** |

---

## 六、模块级审查清单

### P0 - 安全优先
- [ ] 替换 RBAC 模块的密码哈希为 BCrypt
- [ ] 重构 `EncryPasswordValueObject` 为不可变
- [ ] 审计所有用户输入的安全性

### P1 - DDD 规范化
- [ ] 移除所有 `AggregateRoot` 后缀 (34 个文件)
- [ ] 移除所有 `Entity` 后缀 (9 个文件)
- [ ] 移除所有 `Enum` 后缀 (20+ 个文件)
- [ ] 修正 `SexEnum` 为 `Gender`，`Woman` 为 `Female`

### P2 - 代码质量
- [ ] 使用 `IFullAuditedObject` 替代手动实现
- [ ] 初始化所有集合导航属性
- [ ] 提取硬编码业务规则到领域服务

### P3 - 长期优化
- [ ] 统一命名约定文档
- [ ] 添加领域事件
- [ ] 完善聚合边界定义

---

## 七、重构影响评估

### 命名重构影响范围

重命名实体/枚举会影响：
1. **数据库表名** - 如果使用 SqlSugar 的默认映射
2. **API 响应** - DTO 可能暴露实体类型
3. **序列化** - JSON/XML 序列化结果
4. **日志** - 类型名出现在日志中
5. **异常消息** - 类型名出现在错误信息中

### 迁移策略建议

1. **表名兼容**:
```csharp
[SugarTable("User")]  // 保持原表名
public class User : AggregateRoot<Guid>  // 类名改变
```

2. **渐进式重构**:
   - 创建别名/废弃标记
   - 分阶段迁移
   - 保持向后兼容

3. **自动化脚本**:
   - 批量重命名工具
   - 更新所有引用
   - 验证编译

---

## 八、下一步行动

1. **创建密码迁移方案** - 计划现有用户密码如何迁移到 BCrypt
2. **命名规范文档** - 发布团队遵循的命名约定
3. **重构脚本** - 编写自动化重命名脚本
4. **测试覆盖** - 为关键领域逻辑添加单元测试
5. **代码审查流程** - 建立 PR 审查清单

---

## 九、参考资源

- [ABP 实体最佳实践](https://docs.abp.io/en/abp/latest/Domain-Entities)
- [DDD 聚合设计指南](https://martinfowler.com/bliki/DDD_Aggregate.html)
- [.NET 命名指南](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)
- [OWASP 密码存储](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
