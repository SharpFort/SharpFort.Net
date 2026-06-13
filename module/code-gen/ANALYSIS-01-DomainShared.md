# 📁 SharpFort.CodeGen.Domain.Shared/ 文件夹分析

> **分析日期**: 2026-06-12  
> **路径**: `module/code-gen/SharpFort.CodeGen.Domain.Shared/`  
> **文件数量**: 3 个文件

---

## 一、功能概述

这是 ABP 框架分层架构中的 **Domain.Shared 层**，负责存放跨层共享的类型定义。按照 ABP DDD 最佳实践，Domain.Shared 层可被所有其他层引用（Application、Domain、Infrastructure 均依赖它）。

---

## 二、文件逐一分析

### 2.1 Enums/FieldType.cs — 字段类型枚举

```csharp
public enum FieldType
{
    String   = 1,  // 字符串
    Int      = 2,  // 32位整数
    Long     = 3,  // 64位整数
    Bool     = 4,  // 布尔值
    Decimal  = 5,  // 十进制数
    DateTime = 6,  // 日期时间
    Guid     = 7   // GUID/UUID
}
```

**功能**: 定义代码生成器支持的 7 种字段数据类型。

**映射关系**:

| FieldType | C# 类型 | SQL Server | PostgreSQL | MySQL |
|-----------|---------|------------|------------|-------|
| String | `string` | VARCHAR / NVARCHAR(MAX) | VARCHAR / TEXT | VARCHAR / LONGTEXT |
| Int | `int` | INT | INT | INT |
| Long | `long` | BIGINT | BIGINT | BIGINT |
| Bool | `bool` | BIT | BOOLEAN | TINYINT(1) |
| Decimal | `decimal` | DECIMAL(18,2) | DECIMAL(18,2) | DECIMAL(18,2) |
| DateTime | `DateTime` | DATETIME | TIMESTAMP | DATETIME |
| Guid | `Guid` | UNIQUEIDENTIFIER | UUID | VARCHAR(36) |

**设计说明**:
- 每个枚举值使用 `[Display]` 特性标注 Name 和 Description
- `Name` 属性用于 `FieldTemplateHandler` 获取类型字符串
- `Description` 属性用于 `WebTemplateManager` 从 C# PropertyType 反向匹配

---

### 2.2 SharpFortCodeGenDomainSharedModule.cs — ABP 模块定义

```csharp
[DependsOn(typeof(AbpDddDomainSharedModule))]
public class SharpFortCodeGenDomainSharedModule : AbpModule { }
```

**功能**: ABP 模块入口，声明依赖 `AbpDddDomainSharedModule`。

**依赖**: 仅依赖 ABP 框架的 Domain.Shared 基础模块

---

### 2.3 SharpFort.CodeGen.Domain.Shared.csproj — 项目配置

**关键配置**:
```
TargetFramework: net10.0.0          ← 目标为 .NET 10
Nullable: enable                     ← 启用可空引用类型
PackageReference:
  - Volo.Abp.Ddd.Domain.Shared 10.4.1  ← 唯一外部依赖
```

**项目依赖关系**: 本项目是最底层，不依赖任何模块内其他项目

---

## 三、配置项

| 配置项 | 位置 | 说明 |
|--------|------|------|
| TargetFramework | .csproj | `net10.0.0` — 需要 .NET 10 运行时 |
| Nullable | .csproj | `enable` — 全项目启用可空引用类型检查 |
| ImplicitUsings | .csproj | `enable` — 自动导入常用命名空间 |

---

## 四、扩展/改写建议

| 优先级 | 建议 | 说明 |
|--------|------|------|
| 高 | 增加字段类型 | `Enum`、`Json`、`Float`/`Double`、`byte[]` 等 |
| 中 | 类型元数据增强 | 为每个 FieldType 增加默认长度、HTML 控件映射等 |
| 中 | 多语言支持 | 枚举 Display 属性的 Description 目前仅英文 |
| 低 | 验证规则共享 | 增加 DataAnnotation 验证规则配置 |

---

## 五、在整个模块中的位置

```
SharpFort.CodeGen.Domain.Shared (本层)
    ↑ 被以下层依赖:
    ├── SharpFort.CodeGen.Application.Contracts
    ├── SharpFort.CodeGen.Domain
    └── (所有其他模块层间接依赖)
```
