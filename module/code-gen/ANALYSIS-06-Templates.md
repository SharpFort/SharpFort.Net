# 📁 Templates/ 文件夹分析

> **分析日期**: 2026-06-12  
> **路径**: `module/code-gen/Templates/`  
> **文件数量**: 8 个 Scriban 模板文件

---

## 一、功能概述

本文件夹包含 8 个 **Scriban 模板引擎**的模板文件，用于从 Table+Field 元数据自动生成 C# 代码文件。这些模板覆盖了 ABP（ASP.NET Boilerplate）框架标准分层架构的完整代码生成。

Scriban 是一种高性能模板引擎（通过 NuGet 包 `Scriban 7.2.3` 引入），模板中使用 `{{ }}` 语法访问上下文变量，使用 `{{~ }}` 语法进行流控制（如循环、条件）。

---

## 二、模板文件逐一分析

### 2.1 Entity.scriban - 实体类模板

**生成目标**: `{RootNamespace}.{Module}.Domain.Entities/{Model}Entity.cs`

**功能**: 生成 SqlSugar ORM 的实体类定义。

**Scriban 变量使用**:
| 变量 | 来源 | 说明 |
|------|------|------|
| `{{RootNamespace}}` | Table.RootNamespace → DefaultTemplateContextEnricher | 根命名空间，默认 "Sf.Abp" |
| `{{Module}}` | Table.ModuleName → DefaultTemplateContextEnricher | 模块名，默认 "Rbac" |
| `{{Model}}` | Table.Name → DefaultTemplateContextEnricher | 表名/PascalCase |
| `{{Table.Name}}` | Table.Name | 数据库原始表名 |
| `{{Table.Description}}` | Table.Description | 表描述 |
| `{{field.CsharpType}}` | FieldInfo.CsharpType | C#类型(含可空?) |
| `{{field.Name}}` | Field.Name | 字段名 |
| `{{field.Description}}` | Field.Description | 字段描述 |
| `{{sugar_column field}}` | ScribanHelperFunctions.SugarColumn() | 生成 `[SugarColumn(...)]` 特性 |
| `{{default_value field}}` | ScribanHelperFunctions.DefaultValue() | 字段默认值 |

**过滤逻辑**:
- 跳过 `Id`、`IsDeleted`、`CreationTime` 字段（这些由基类提供）

**基类**: `Entity<Guid>`（非 `FullAuditedAggregateRoot`，简化版实体）

---

### 2.2 CreateInput.scriban - 新增输入 DTO

**生成目标**: `{RootNamespace}.{Module}.Application.Contracts.Dtos/{Model}/{Model}CreateInput.cs`

**功能**: 生成新增操作的输入 DTO 类。

**过滤逻辑**: 跳过 `Id`（自增/自动生成）和 `CreationTime`（自动设置）

**特点**:
- 不继承 `EntityDto<Guid>`，因为新增时不需要 ID
- 作为普通类而不是 DTO 基类继承

---

### 2.3 UpdateInput.scriban - 编辑输入 DTO

**生成目标**: `{RootNamespace}.{Module}.Application.Contracts.Dtos/{Model}/{Model}UpdateInput.cs`

**功能**: 生成编辑/修改操作的输入 DTO 类。

**过滤逻辑**: 与 CreateInput 相同，跳过 `Id` 和 `CreationTime`

---

### 2.4 GetListInput.scriban - 列表查询输入 DTO

**生成目标**: `{RootNamespace}.{Module}.Application.Contracts.Dtos/{Model}/{Model}GetListInput.cs`

**功能**: 生成列表查询的输入参数 DTO。

**基类**: `PagedAllResultRequestDto`（来自 `SharpFort.Ddd.Application.Contracts`）

**固定字段**: 仅包含一个 `Filter` 属性用于模糊关键字查询

**特点**: 这是一个静态模板，不根据字段动态生成查询条件

---

### 2.5 GetListOutputDto.scriban - 列表项返回 DTO

**生成目标**: `{RootNamespace}.{Module}.Application.Contracts.Dtos/{Model}/{Model}GetListOutputDto.cs`

**功能**: 生成列表查询返回的单个项目 DTO。

**基类**: `EntityDto<Guid>`

**过滤逻辑**: 跳过 `Id` 字段（基类已提供）

---

### 2.6 GetOutputDto.scriban - 详情返回 DTO

**生成目标**: `{RootNamespace}.{Module}.Application.Contracts.Dtos/{Model}/{Model}GetOutputDto.cs`

**功能**: 生成单个实体详情的返回 DTO。

**基类**: `EntityDto<Guid>`

**过滤逻辑**: 跳过 `Id` 字段（基类已提供）

---

### 2.7 IServices.scriban - 服务接口

**生成目标**: `{RootNamespace}.{Module}.Application.Contracts.IServices/I{Model}Service.cs`

**功能**: 生成 ABP 应用服务接口。

**继承**: `ISfCrudAppService<GetOutputDto, GetListOutputDto, Guid, GetListInput, CreateInput, UpdateInput>`

**泛型参数**: 完整的 CRUD 泛型签名（5 个 DTO 类型 + 1 个 Key 类型）

---

### 2.8 Service.scriban - 服务实现

**生成目标**: `{RootNamespace}.{Module}.Application/Services/{Model}Service.cs`

**功能**: 生成 ABP 应用服务的具体实现类。

**继承**: `SfCrudAppService<Entity, GetOutputDto, GetListOutputDto, Guid, GetListInput, CreateInput, UpdateInput>`

**关键特性 - 增量安全合并**:
```csharp
// <sf-custom-code-start id="CustomLogic">
// 在此区域中添加手写业务逻辑，重新生成代码时不会丢失
// </sf-custom-code-end>
```
这是 `IncrementalCodeMerger` 的核心机制--保护手写代码不被覆盖。

---

## 三、模板变量上下文

所有模板共享 `TemplateContext` 对象的数据：

```
TemplateContext
├── Table (TableInfo)
│   ├── Name, Description, ModuleName
│   ├── RootNamespace, IsOverwrite, TemplateEngine
├── Fields (List<FieldInfo>)
│   ├── Name, Type, CsharpType
│   ├── MaxLength, IsRequired, IsPrimaryKey
│   ├── Description, IsQueryField, OrderNum
├── Module (string)
├── RootNamespace (string)
├── Model (string, PascalCase)
├── ModelCamel (string, camelCase)
├── ModelPlural (string, 复数形式)
```

## 四、扩展/改写建议

| 优先级 | 建议 | 说明 |
|--------|------|------|
| 高 | 新增 Vue 前端模板 | 目前只有后端 C# 模板，需要 Vue 页面模板 |
| 高 | 模板可配置化 | 允许用户自定义模板变量和过滤逻辑 |
| 中 | 模板版本管理 | 支持模板版本对比和回滚 |
| 中 | 批量字段模板 | 如枚举、常量等特殊字段类型的专用模板 |
| 低 | 模板校验 | 生成前预览、语法校验、冲突检测 |

---

## 五、配置项

模板本身不直接包含配置项，但依赖的上下文变量由以下配置控制：
- `CodeGen:SolutionRoot` — 解决方案根目录（appsettings.json 或环境变量）
- `SF_SOLUTION_ROOT` — 环境变量形式的解决方案根目录
- `Table.ModuleName` — 目标模块名称
- `Table.RootNamespace` — 命名空间前缀
