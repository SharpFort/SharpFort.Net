# SharpFort 代码质量与最佳实践蓝图

> 版本: 1.0
> 创建日期: 2025-11-16
> 适用于: SharpFort (原 YiFramework) 项目开发

---

## 引言

本蓝图总结了当前代码库中发现的模式，并为未来的开发和代码审查提供了指导原则。这是一份"活文档"，会随着项目的发展而更新。

---

## 1. DDD (领域驱动设计) 规范

### 1.1 实体命名约定

#### 原则
> "实体类名应纯粹反映业务领域概念，技术类型信息应由基类继承体现。"

#### 正确示例
```csharp
// 聚合根
public class User : AggregateRoot<Guid>
public class Order : AggregateRoot<Guid>
public class Product : AggregateRoot<Guid>

// 实体
public class OrderItem : Entity<Guid>
public class Address : Entity<Guid>

// 值对象
public class Money : ValueObject
public class PasswordHash : ValueObject
```

#### 错误示例 (需重构)
```csharp
// 冗余的技术后缀
public class UserAggregateRoot : AggregateRoot<Guid>  // ❌
public class OrderItemEntity : Entity<Guid>           // ❌
public class MoneyValueObject : ValueObject           // ❌
```

#### 命名清单
| 类型 | 后缀 | 示例 |
|------|------|------|
| 聚合根 | 无 | `User`, `Order`, `Product` |
| 实体 | 无 | `OrderItem`, `Address` |
| 值对象 | 无 | `Money`, `DateRange`, `PasswordHash` |
| 领域事件 | `Event` | `OrderCreatedEvent` |
| 领域服务 | `Manager` 或 `Service` | `OrderManager`, `PricingService` |
| 仓储接口 | `I...Repository` | `IUserRepository` |

---

### 1.2 值对象设计

#### 原则
> "值对象必须是不可变的，通过值相等性比较。"

#### 正确示例
```csharp
public class PasswordHash : ValueObject
{
    public PasswordHash(string hashedValue)
    {
        HashedValue = hashedValue ?? throw new ArgumentNullException(nameof(hashedValue));
    }

    public string HashedValue { get; init; }  // init = 不可变

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return HashedValue;
    }
}

public class Money : ValueObject
{
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }

    public decimal Amount { get; init; }
    public string Currency { get; init; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Amount;
        yield return Currency;
    }

    // 值对象的行为方法应返回新实例
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Currency mismatch");
        return new Money(Amount + other.Amount, Currency);
    }
}
```

#### 错误示例
```csharp
public class PasswordHash : ValueObject
{
    public string Password { get; set; } = string.Empty;  // ❌ 可变
    public string Salt { get; set; } = string.Empty;      // ❌ 可变
}
```

---

### 1.3 聚合根设计

#### 原则
> "聚合根是一致性边界，应保护内部不变量，通过聚合根访问子实体。"

#### 正确示例
```csharp
public class Order : FullAuditedAggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();  // 只读集合
    public Money TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

    protected Order() { } // ORM 用

    public Order(Guid id, Guid customerId)
    {
        Id = id;
        CustomerId = customerId;
        Status = OrderStatus.Draft;
        TotalAmount = Money.Zero;
    }

    // 聚合根方法保护不变量
    public void AddItem(Guid productId, int quantity, Money unitPrice)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Cannot add items to confirmed order");

        var item = new OrderItem(Guid.NewGuid(), productId, quantity, unitPrice);
        _items.Add(item);
        RecalculateTotal();
    }

    public void Confirm()
    {
        if (_items.Count == 0)
            throw new InvalidOperationException("Cannot confirm empty order");

        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id));  // 领域事件
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(x => x.SubTotal);
    }
}
```

---

## 2. C# 命名规范

### 2.1 枚举命名

#### 原则
> ".NET 枚举类型名称不应有 'Enum' 后缀，枚举值应语义一致。"

#### 正确示例
```csharp
public enum Gender
{
    Male = 0,
    Female = 1,      // 与 Male 语义配对
    Unknown = 2,
    PreferNotToSay = 3
}

public enum OrderStatus
{
    Draft = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

public enum MenuType
{
    Directory = 0,
    Menu = 1,
    Button = 2
}
```

#### 错误示例
```csharp
public enum SexEnum  // ❌ 有 Enum 后缀，且 Sex 不如 Gender 清晰
{
    Male = 0,
    Woman = 1,       // ❌ 应为 Female
    Unknown = 2
}

public enum MenuTypeEnum  // ❌ 有 Enum 后缀
{
    Dir = 0,          // ❌ 缩写不清晰
    M = 1,            // ❌ 单字母不清晰
    Btn = 2           // ❌ 缩写不清晰
}
```

---

### 2.2 方法命名

#### 原则
> "方法名应使用动词开头，清晰表达行为意图。"

#### 正确示例
```csharp
// 布尔判断方法
public bool VerifyPassword(string password)
public bool IsValid()
public bool CanConfirm()
public bool HasItems()

// CRUD 操作
public void Create()
public Task<User> GetByIdAsync(Guid id)
public Task UpdateAsync()
public Task DeleteAsync()

// 业务操作
public void Confirm()
public void Cancel()
public void Ship()
public Money CalculateTotal()
```

#### 错误示例
```csharp
public bool JudgePassword(string password)  // ❌ Judge 语义不清
public bool CheckValid()                     // ❌ 应为 IsValid
public void DoCreate()                       // ❌ Do 前缀多余
public User UserGet(Guid id)                 // ❌ 名词在前
```

---

### 2.3 参数命名

#### 原则
> "参数名使用 camelCase，避免缩写，语义清晰。"

#### 正确示例
```csharp
public static void WriteFile(string filePath, string content, Encoding encoding)
public static string ReadFile(string filePath, Encoding encoding)
public static void CopyFile(string sourcePath, string destinationPath)
public void SetPassword(string plainPassword)
```

#### 错误示例
```csharp
public static void WriteFile(string Path, string Strings)  // ❌ PascalCase
public static void FileCoppy(string orignFile, string NewFile)  // ❌ 拼写错误 + 大小写混用
public void SetPwd(string p)  // ❌ 缩写
```

---

## 3. 安全最佳实践

### 3.1 密码存储

#### 原则
> "使用专用密码哈希算法（BCrypt/Argon2），而非通用哈希函数（SHA/MD5）。"

#### 正确示例
```csharp
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;  // 可配置

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

#### 错误示例
```csharp
// ❌ SHA512 过快，易受暴力破解
public string HashPassword(string password, string salt)
{
    return SHA512.HashData(Encoding.UTF8.GetBytes(salt + password));
}

// ❌ MD5 已被破解
public string HashPassword(string password)
{
    return MD5.HashData(Encoding.UTF8.GetBytes(password));
}
```

---

### 3.2 输入验证

#### 原则
> "永不信任用户输入，在边界处验证。"

#### 正确示例
```csharp
public class UserCreateDto
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9_]+$")]
    public string UserName { get; set; }

    [Required]
    [MinLength(8)]
    public string Password { get; set; }

    [EmailAddress]
    public string? Email { get; set; }
}

// 应用服务层验证
public async Task<UserDto> CreateUserAsync(UserCreateDto input)
{
    // 业务规则验证
    if (await _userRepository.ExistsAsync(x => x.UserName == input.UserName))
        throw new BusinessException("UserNameAlreadyExists");

    // 创建实体
    var user = new User(input.UserName);
    user.SetPassword(_passwordHasher.HashPassword(input.Password));

    await _userRepository.InsertAsync(user);
    return ObjectMapper.Map<User, UserDto>(user);
}
```

---

## 4. 依赖项管理

### 4.1 优先使用成熟库

#### 原则
> "不重复造轮子。优先使用成熟、活跃维护的开源库。"

#### 推荐库
| 功能 | 推荐库 | 避免 |
|------|-------|------|
| JSON 处理 | `System.Text.Json` 或 `Newtonsoft.Json` | 自定义 JsonHelper |
| 字符串工具 | `Masuit.Tools` | 自定义 StringHelper |
| 日期处理 | `Masuit.Tools` | 自定义 DateTimeHelper |
| 密码哈希 | `BCrypt.Net-Next` | 自定义 MD5/SHA 实现 |
| 对象映射 | `Mapster` 或 `AutoMapper` | 手动映射 |
| 验证 | `FluentValidation` | 手动 if 检查 |

#### 决策流程
```
需要新工具功能?
    ↓
检查 Masuit.Tools 是否已有
    ↓ 是               ↓ 否
  直接使用      检查其他成熟库
                    ↓
              是否具有通用性?
                ↓ 是          ↓ 否
           向 Masuit.Tools    项目内实现
              提交 PR         (最后选择)
```

---

### 4.2 工具类使用示例

#### Masuit.Tools 常用功能
```csharp
using Masuit.Tools;
using Masuit.Tools.DateTimeExt;
using Masuit.Tools.Security;

// 字符串操作
var encoded = "Hello World".ToBase64String();
var decoded = encoded.FromBase64String();
var isEmail = "test@example.com".MatchEmail();

// 日期操作
var timestamp = DateTime.Now.GetUnixTimestamp();
var date = timestamp.ToDateTime();
var isWeekend = DateTime.Now.IsWeekend();

// JSON 操作
var json = myObject.ToJsonString();
var obj = json.FromJson<MyClass>();

// 安全操作
var md5 = "password".MDString();  // 仅用于非安全场景
var sha = "data".SHA256();
```

---

## 5. 审计与追溯

### 5.1 审计接口使用

#### 原则
> "审计信息应保持完整性。优先使用 ABP 提供的组合接口。"

#### 审计接口层次
```csharp
// 基本创建审计
public interface IHasCreationTime { DateTime CreationTime { get; } }
public interface ICreationAudited : IHasCreationTime { Guid? CreatorId { get; } }

// 修改审计
public interface IHasModificationTime { DateTime? LastModificationTime { get; } }
public interface IModificationAudited : IHasModificationTime { Guid? LastModifierId { get; } }

// 软删除审计
public interface ISoftDelete { bool IsDeleted { get; } }
public interface IDeletionAudited : ISoftDelete
{
    Guid? DeleterId { get; }
    DateTime? DeletionTime { get; }
}

// 组合接口 (推荐)
public interface IAuditedObject : ICreationAudited, IModificationAudited { }
public interface IFullAuditedObject : IAuditedObject, IDeletionAudited { }
```

#### 推荐用法
```csharp
// 简单实体 - 只需创建时间
public class Log : Entity<Guid>, IHasCreationTime
{
    public DateTime CreationTime { get; set; }
}

// 标准业务实体 - 创建+修改审计
public class Product : AuditedAggregateRoot<Guid>  // 基类自动实现
{
    // 无需手动声明审计字段
}

// 需要软删除的核心实体
public class User : FullAuditedAggregateRoot<Guid>  // 推荐
{
    // 自动包含:
    // - CreationTime, CreatorId
    // - LastModificationTime, LastModifierId
    // - IsDeleted, DeleterId, DeletionTime
}
```

#### 错误示例
```csharp
// ❌ 审计信息不完整
public class User : AggregateRoot<Guid>, ISoftDelete, IAuditedObject
{
    public bool IsDeleted { get; set; }  // 没有 DeleterId 和 DeletionTime
    public DateTime CreationTime { get; set; } = DateTime.Now;  // 手动初始化
    // ...
}
```

---

## 6. 错误处理

### 6.1 异常处理原则

#### 正确示例
```csharp
public async Task<UserDto> GetUserAsync(Guid id)
{
    var user = await _userRepository.GetAsync(id);
    if (user == null)
        throw new EntityNotFoundException(typeof(User), id);

    return ObjectMapper.Map<User, UserDto>(user);
}

public void ProcessFile(string filePath)
{
    if (!File.Exists(filePath))
        throw new FileNotFoundException("File not found", filePath);

    try
    {
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        // 处理内容
    }
    catch (IOException ex)
    {
        _logger.LogError(ex, "Failed to read file: {FilePath}", filePath);
        throw;  // 记录后重新抛出
    }
}
```

#### 错误示例
```csharp
// ❌ 吞掉异常
public string ReadFile(string path)
{
    try
    {
        return File.ReadAllText(path);
    }
    catch (Exception)
    {
        return "";  // 隐藏错误
    }
}

// ❌ 空 catch
public void Process()
{
    try
    {
        // ...
    }
    catch (Exception)
    {
        throw;  // 无意义的 catch-throw
    }
}
```

---

## 7. 资源管理

### 7.1 IDisposable 模式

#### 原则
> "使用 using 语句或声明确保资源正确释放。"

#### 正确示例
```csharp
// using 声明 (C# 8+)
public string ReadFile(string path)
{
    using var reader = new StreamReader(path, Encoding.UTF8);
    return reader.ReadToEnd();
}

// using 语句
public async Task WriteFileAsync(string path, string content)
{
    await using var writer = new StreamWriter(path, false, Encoding.UTF8);
    await writer.WriteAsync(content);
}

// 现代简洁写法
public string ReadAllText(string path)
{
    return File.ReadAllText(path, Encoding.UTF8);
}
```

#### 错误示例
```csharp
// ❌ 手动 Close/Dispose
public void WriteFile(string path, string content)
{
    StreamWriter writer = new StreamWriter(path);
    writer.Write(content);
    writer.Close();  // 如果前面抛异常，不会执行
    writer.Dispose(); // 冗余
}
```

---

## 8. 代码审查清单

### 提交前自检
- [ ] 类名无技术后缀 (AggregateRoot, Entity, ValueObject, Enum)
- [ ] 枚举值语义一致 (Male/Female, 非 Male/Woman)
- [ ] 值对象不可变 (使用 init 而非 set)
- [ ] 使用 ABP 审计基类 (FullAuditedAggregateRoot)
- [ ] 密码使用 BCrypt
- [ ] 无废弃 API 调用
- [ ] 优先使用 Masuit.Tools
- [ ] 文件编码为 UTF-8
- [ ] 参数使用 camelCase
- [ ] 方法名动词开头
- [ ] 使用 using 管理资源
- [ ] 异常被正确处理
- [ ] 添加 XML 文档注释
- [ ] 无硬编码业务规则

---

## 9. 工具与资源

### 代码分析工具
- **ReSharper** / **Rider** - 重构和代码分析
- **SonarQube** - 代码质量检测
- **StyleCop** - 代码风格检查
- **Roslyn Analyzers** - 编译时检查

### 参考资源
- [ABP Framework 最佳实践](https://docs.abp.io/en/abp/latest/Best-Practices)
- [.NET 设计指南](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [Clean Code (C#)](https://github.com/thangchung/clean-code-dotnet)
- [DDD Reference](https://www.domainlanguage.com/ddd/reference/)

---

## 10. 持续改进

本文档是活文档，应随项目发展更新：

1. **发现新问题** → 添加到本指南
2. **采用新技术** → 更新最佳实践
3. **代码审查反馈** → 完善清单
4. **社区建议** → 整合最佳方案

**责任**: 每位贡献者都有责任遵循和改进这些实践。

---

*SharpFort - 构建坚固、优雅、可维护的 .NET 后端*
