# SharpFort 项目路线图
## Project Roadmap for SharpFort

> 版本: 1.0
> 创建日期: 2025-11-16
> 状态: 规划阶段

---

## 1. 背景与愿景 (Background & Vision)

### 1.1 项目现状

**SharpFort** 源自 **YiFramework** —— 一个基于 .NET 8、ABP Framework v8.3 和 SqlSugar ORM 的 DDD 开源后端框架。原项目已停止维护，现由新维护者接手并计划长期发展。

**技术栈**:
- .NET 8
- ABP Framework v8.3
- SqlSugar (Code First ORM)
- FreeRedis + Hangfire + Mapster

**已完成模块**:
- RBAC (基于角色的访问控制)
- BBS (论坛系统)
- 审计日志
- 租户管理
- 设置管理
- 代码生成

### 1.2 接手意义

1. **延续开源项目生命力** - 防止优质代码资产流失
2. **社区需求** - 为 .NET 开发者提供 DDD + ABP + SqlSugar 的参考实现
3. **技术积累** - 在实战中深化 DDD 和 ABP 框架理解
4. **商业价值** - 为企业级应用提供可靠的技术基座

### 1.3 双重目标

#### 目标 1: 商业化
- 打造企业级可用的后端框架
- 提供技术支持和定制开发服务
- 建立付费模块生态

#### 目标 2: 开源社区建设
- 建立活跃的贡献者社区
- 定期发布版本和技术博客
- 参与相关开源项目（如 Masuit.Tools）
- 组织技术分享和培训

---

## 2. 总体目标 (Overall Goals)

通过执行此路线图，我们将实现：

### 2.1 代码质量提升
- **安全性**: 替换为行业标准密码加密方案
- **规范性**: 统一命名约定，消除技术债务
- **可维护性**: 减少冗余代码，拥抱成熟开源库
- **可读性**: 遵循 .NET 和 DDD 最佳实践

### 2.2 技术栈现代化
- **框架升级**: ABP v8.3 → v9.0+
- **API 更新**: 移除所有废弃 API 调用
- **依赖精简**: 用 Masuit.Tools 替代自定义 Helper

### 2.3 品牌重塑
- **新身份**: YiFramework → SharpFort
- **专业形象**: 规范的文档、清晰的架构
- **社区认可**: 通过贡献和分享建立声誉

---

## 3. 分阶段实施计划

### 阶段 1: 基础整固 (Foundation Stabilization)
**时间**: 第 1-4 周
**优先级**: P0 - 最高

#### 1.1 安全加固
**任务**: 替换密码加密方案

**操作步骤**:
1. 添加 NuGet 包：
   ```bash
   dotnet add module/rbac/Yi.Framework.Rbac.Domain/Yi.Framework.Rbac.Domain.csproj package BCrypt.Net-Next
   ```

2. 创建新的密码服务：
   ```csharp
   // module/rbac/Yi.Framework.Rbac.Domain/Services/PasswordHasher.cs
   public class PasswordHasher : IPasswordHasher
   {
       private const int WorkFactor = 12;

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

3. 重构 `UserAggregateRoot`:
   ```csharp
   public User SetPassword(string hashedPassword)
   {
       ArgumentException.ThrowIfNullOrEmpty(hashedPassword);
       EncryPassword = new PasswordHash(hashedPassword);
       return this;
   }
   ```

4. 数据迁移策略：
   - 新用户直接使用 BCrypt
   - 旧用户登录时自动升级哈希

**风险**: 现有用户无法直接迁移密码
**缓解**: 实现双哈希验证，逐步迁移

---

#### 1.2 废弃 API 清理

**任务**: 移除 `#pragma warning disable` 并使用现代 API

**文件**: `framework/Yi.Framework.Core/Helper/MD5Hepler.cs`

**修复**:
```csharp
// 替换第 14-21 行
public static string GenerateSalt()
{
    byte[] buf = new byte[16];
    RandomNumberGenerator.Fill(buf);  // 新 API
    return Convert.ToBase64String(buf);
}

// 替换第 30-48 行
public static string SHA2Encode(string pass, string salt, int passwordFormat = 1)
{
    if (passwordFormat == 0) return pass;

    byte[] bIn = Encoding.Unicode.GetBytes(pass);
    byte[] bSalt = Convert.FromBase64String(salt);
    byte[] bAll = new byte[bSalt.Length + bIn.Length];

    Buffer.BlockCopy(bSalt, 0, bAll, 0, bSalt.Length);
    Buffer.BlockCopy(bIn, 0, bAll, bSalt.Length, bIn.Length);

    byte[] bRet = SHA512.HashData(bAll);  // 新 API
    return ConvertEx.ToUrlBase64String(bRet);
}
```

---

#### 1.3 拼写错误修复

**任务**: 重命名 `MD5Hepler` 为 `MD5Helper`

**步骤**:
1. 全局搜索替换: `MD5Hepler` → `MD5Helper`
2. 重命名文件: `MD5Hepler.cs` → `MD5Helper.cs`
3. 更新所有引用

**影响范围**:
- `UserAggregateRoot.cs:190, 191, 206, 207`
- 潜在的其他调用点

---

### 阶段 2: 代码规范化 (Code Standardization)
**时间**: 第 5-8 周
**优先级**: P1 - 高

#### 2.1 实体重命名

**策略**: 使用 IDE 重构工具 + SqlSugar 特性保持数据库兼容

**示例 - User 实体**:
```csharp
// 重命名前
[SugarTable("User")]
public class UserAggregateRoot : AggregateRoot<Guid>

// 重命名后
[SugarTable("User")]  // 保持表名不变
public class User : AggregateRoot<Guid>
```

**执行脚本**:
```bash
# PowerShell 重命名脚本
$replacements = @{
    "UserAggregateRoot" = "User"
    "RoleAggregateRoot" = "Role"
    "MenuAggregateRoot" = "Menu"
    "DeptAggregateRoot" = "Department"
    # ... 更多映射
}

Get-ChildItem -Recurse -Include *.cs | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    foreach ($key in $replacements.Keys) {
        $content = $content -replace $key, $replacements[$key]
    }
    Set-Content $_.FullName $content
}
```

---

#### 2.2 枚举重命名

**性别枚举修复**:
```csharp
// 修改前
public enum SexEnum
{
    Male = 0,
    Woman = 1,  // 错误
    Unknown = 2
}

// 修改后
public enum Gender
{
    Male = 0,
    Female = 1,  // 正确
    Unknown = 2
}
```

**全局替换**:
- `SexEnum` → `Gender`
- `MenuTypeEnum` → `MenuType`
- `DataScopeEnum` → `DataScope`
- ... (20+ 枚举)

---

#### 2.3 审计接口统一

**重构目标**: 使用 ABP 提供的基类

```csharp
// 修改前
public class UserAggregateRoot : AggregateRoot<Guid>, ISoftDelete, IAuditedObject
{
    public bool IsDeleted { get; set; }
    public DateTime CreationTime { get; set; } = DateTime.Now;  // 手动初始化
    public Guid? CreatorId { get; set; }
    public Guid? LastModifierId { get; set; }
    public DateTime? LastModificationTime { get; set; }
}

// 修改后
public class User : FullAuditedAggregateRoot<Guid>
{
    // 自动包含所有审计字段
}
```

---

### 阶段 3: 依赖项精简 (Dependency Streamlining)
**时间**: 第 9-12 周
**优先级**: P1 - 高

#### 3.1 引入 Masuit.Tools

```bash
dotnet add framework/Yi.Framework.Core/Yi.Framework.Core.csproj package Masuit.Tools.Core
```

#### 3.2 替换策略

| 自定义 Helper | Masuit.Tools 替代 | 行动 |
|--------------|------------------|------|
| StringHelper | `string.UrlEncode()`, `string.ToBase64()` | 删除 |
| DateTimeHelper | `DateTime.GetUnixTimestamp()` | 删除 |
| JsonHelper | `obj.ToJsonString()`, `string.ToObject<T>()` | 删除 |
| EnumHelper | `Enum.GetDescription()` | 删除 |
| MD5Helper | `string.MDString()` | 部分替换 |
| FileHelper | `File.ReadToString()`, `Directory.GetTree()` | 大部分删除 |
| RandomHelper | `Random.StrictNext()` | 删除 |

#### 3.3 代码迁移示例

**之前**:
```csharp
var json = JsonHelper.ObjToStr(obj);
var date = DateTimeHelper.ToUnixTimestampBySeconds(dt);
var guid = StringHelper.GetGUID("N");
```

**之后**:
```csharp
using Masuit.Tools;

var json = obj.ToJsonString();
var date = dt.GetUnixTimestamp();
var guid = Guid.NewGuid().ToString("N");
```

---

### 阶段 4: 框架升级 (Framework Upgrade)
**时间**: 第 13-20 周
**优先级**: P2 - 中

#### 4.1 ABP v8.3 → v9.0 升级

**准备工作**:
1. 阅读 [ABP 9.0 迁移指南](https://docs.abp.io/en/abp/latest/Migration-Guides/Abp-9_0)
2. 列出所有破坏性变更
3. 创建兼容层（如需要）

**升级步骤**:
1. 更新 NuGet 包版本
2. 修复编译错误
3. 运行测试套件
4. 修复运行时问题

**预期变更**:
- 新的依赖注入模式
- 更新的权限系统
- 优化的审计日志

---

#### 4.2 .NET 10 长期规划

**时间线**: 2025年11月 .NET 10 发布后

**评估清单**:
- [ ] 新 C# 语言特性利用
- [ ] 性能改进评估
- [ ] 第三方库兼容性
- [ ] ABP for .NET 10 可用性

---

### 阶段 5: 品牌重塑 (Brand Rebranding)
**时间**: 第 21-24 周
**优先级**: P3 - 低

#### 5.1 命名空间迁移

**策略**: 渐进式迁移，保持向后兼容

```csharp
// 创建别名
namespace Yi.Framework.Core
{
    [Obsolete("Use SharpFort.Core instead", false)]
    public static class LegacyNamespace { }
}

namespace SharpFort.Core
{
    // 新代码
}
```

#### 5.2 程序集重命名

```xml
<!-- 更新 .csproj -->
<PropertyGroup>
  <RootNamespace>SharpFort.Framework.Core</RootNamespace>
  <AssemblyName>SharpFort.Framework.Core</AssemblyName>
</PropertyGroup>
```

#### 5.3 组织与仓库

1. 创建 GitHub 组织: `github.com/SharpFort`
2. Fork 或迁移仓库
3. 设置 CI/CD
4. 发布 NuGet 包

---

## 4. 风险评估与缓解策略

### 4.1 技术风险

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|---------|
| **重命名导致编译错误** | 高 | 高 | 使用 IDE 重构工具，逐文件提交 |
| **密码迁移失败** | 中 | 严重 | 双哈希支持，保留旧算法验证 |
| **ABP 升级破坏性变更** | 高 | 高 | 详细阅读迁移文档，创建分支测试 |
| **第三方库不兼容** | 中 | 中 | 锁定版本，逐步升级 |
| **性能回归** | 低 | 中 | 基准测试，性能监控 |

### 4.2 项目风险

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|---------|
| **社区不活跃** | 高 | 中 | 定期发布内容，参与相关社区 |
| **资源不足** | 中 | 高 | 优先级排序，自动化工具 |
| **需求变更** | 中 | 中 | 模块化设计，清晰的 API 边界 |
| **文档滞后** | 高 | 中 | 代码即文档，自动生成 API 文档 |

### 4.3 缓解策略详解

#### 密码迁移方案
```csharp
public bool VerifyPassword(string password)
{
    // 尝试新算法 (BCrypt)
    if (EncryPassword.Password.StartsWith("$2"))
    {
        return BCrypt.Net.BCrypt.Verify(password, EncryPassword.Password);
    }

    // 回退到旧算法 (SHA512)
    var oldHash = MD5Helper.SHA2Encode(password, EncryPassword.Salt);
    if (EncryPassword.Password == oldHash)
    {
        // 自动升级到新算法
        var newHash = BCrypt.Net.BCrypt.HashPassword(password);
        // 保存新哈希...
        return true;
    }

    return false;
}
```

#### 重构安全网
1. **单元测试** - 为关键业务逻辑添加测试
2. **集成测试** - 确保 API 行为不变
3. **代码审查** - 每个 PR 需要审查
4. **渐进发布** - 灰度发布，监控错误率

---

## 5. 成功指标 (Success Metrics)

### 5.1 代码质量
- [ ] 零编译警告
- [ ] 零废弃 API 调用
- [ ] 100% 命名规范遵循
- [ ] 测试覆盖率 > 60%

### 5.2 安全性
- [ ] 密码哈希符合 OWASP 标准
- [ ] 无已知安全漏洞
- [ ] 通过安全扫描工具检查

### 5.3 社区
- [ ] GitHub Stars > 500
- [ ] 月活跃贡献者 > 5
- [ ] 文档完整性 > 90%
- [ ] Issue 响应时间 < 48h

---

## 6. 资源与支持

### 6.1 工具与库
- **BCrypt.Net-Next**: 密码哈希
- **Masuit.Tools**: 通用工具库
- **ReSharper/Rider**: 代码重构
- **SonarQube**: 代码质量分析

### 6.2 参考资料
- [ABP Framework 文档](https://docs.abp.io/)
- [SqlSugar 文档](https://www.donet5.com/)
- [DDD 参考资料](https://www.domainlanguage.com/ddd/)
- [.NET 设计指南](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)

### 6.3 社区资源
- ABP 官方 Discord
- .NET 中国社区
- SqlSugar QQ 群
- GitHub Discussions

---

## 7. 下一步行动

1. **立即开始** - 安全加固（密码哈希替换）
2. **本周** - 修复废弃 API 警告
3. **本月** - 完成实体/枚举重命名规划
4. **下月** - 引入 Masuit.Tools，开始替换 Helper
5. **持续** - 文档更新和社区建设

---

**SharpFort - 构建坚固的 .NET 后端基础**

*此路线图将根据项目进展和社区反馈定期更新。*
