# 📁 SharpFort.CodeGen.Application.Contracts/ 文件夹分析

> **分析日期**: 2026-06-12  
> **路径**: `module/code-gen/SharpFort.CodeGen.Application.Contracts/`  
> **文件数量**: 12 个文件

---

## 一、功能概述

ABP 框架中的 **Application.Contracts 层**，定义应用服务接口和 DTO（数据传输对象）。这一层是前端和 API 消费者与后端交互的契约/接口定义。

---

## 二、文件逐一分析

### 2.1 IServices/ — 服务接口定义 (4个文件)

#### 2.1.1 ICodeGenService.cs

```csharp
public interface ICodeGenService : IApplicationService
{
    Task PostWebBuildCodeAsync(List<Guid> ids);                              // Web→Code
    Task<string> PostWebBuildDbAsync(List<Guid> ids, bool dryRun = false);   // Web→DB
    Task PostCodeBuildWebAsync();                                            // Code→Web
    Task PostCodeBuildDbAsync();                                             // Code→DB
    Task PostDbToWebAsync(string tableName, string? moduleName, string? rootNamespace); // DB→Web
}
```

**核心 5 大 API**:
| API | 方向 | 说明 |
|-----|------|------|
| `PostWebBuildCodeAsync` | Web → Code | 从 Web 端配置的表结构生成 C# 代码文件 |
| `PostWebBuildDbAsync` | Web → DB | 从 Web 端配置生成/修改物理数据库表 |
| `PostCodeBuildWebAsync` | Code → Web | 扫描现有 C# Entity 类同步到 Web 配置 |
| `PostCodeBuildDbAsync` | Code → DB | CodeFirst 同步实体类到物理数据库 |
| `PostDbToWebAsync` | DB → Web | 逆向物理表结构生成 Web 配置元数据 |

#### 2.1.2 IFieldService.cs
```csharp
// 继承标准 CRUD 接口，无额外方法
public interface IFieldService : ISfCrudAppService<FieldDto, Guid, FieldGetListInput>
```

#### 2.1.3 ITableService.cs
```csharp
// 继承标准 CRUD 接口，无额外方法
public interface ITableService : ISfCrudAppService<TableDto, Guid, TableGetListInput>
```

#### 2.1.4 ITemplateService.cs
```csharp
// 继承标准 CRUD 接口，无额外方法
public interface ITemplateService : ISfCrudAppService<TemplateDto, Guid, TemplateGetListInput>
```

---

### 2.2 Dtos/ — 数据传输对象 (6个文件)

#### 2.2.1 FieldDto.cs — 字段 DTO
```
属性:
  - Name, Description          : 基础信息
  - OrderNum, Length           : 排序和长度
  - FieldType                  : 字段类型枚举
  - TableId                    : 所属表 ID
  - IsRequired, IsKey          : 约束标记
  - IsAutoAdd, IsPublic        : 特性标记
  - IsQueryField               : 查询字段标记
  - IsListDisplay              : 列表显示标记
  - IsFormItem                 : 表单项标记
  - HtmlType                   : HTML 控件类型
```

#### 2.2.2 FieldGetListInput.cs — 字段查询参数
```csharp
Name?: string    // 按名称过滤
TableId?: Guid   // 按所属表过滤
```

#### 2.2.3 TableDto.cs — 表 DTO
```
属性:
  - Name, Description          : 基础信息
  - ModuleName, RootNamespace  : 模块和命名空间
  - IsOverwrite                : 是否覆盖
  - TemplateEngine             : 模板引擎类型
  - Fields: List<FieldDto>?    : 嵌套的字段列表
```

#### 2.2.4 TableGetListInput.cs — 表查询参数
```csharp
// 空类，继承 PagedAndSortedResultRequestDto 即支持分页排序
// 无额外过滤条件
```

#### 2.2.5 TemplateDto.cs — 模板 DTO
```csharp
属性:
  - Id           : Guid
  - TemplateStr  : 模板字符串内容
  - BuildPath    : 生成路径
  - Name         : 模板名称
  - Remarks      : 备注
```

#### 2.2.6 TemplateGetListInput.cs — 模板查询参数
```csharp
Name?: string  // 精确匹配模板名称
```

---

### 2.3 模块和项目文件

#### SharpFortCodeGenApplicationContractsModule.cs
```csharp
[DependsOn(typeof(SharpFortCodeGenDomainSharedModule), typeof(SharpFortDddApplicationContractsModule))]
```
依赖 Domain.Shared 层和框架的 Application.Contracts 基础。

#### SharpFort.CodeGen.Application.Contracts.csproj
```
TargetFramework: net10.0.0
依赖:
  - SharpFort.Ddd.Application.Contracts (框架层)
  - SharpFort.CodeGen.Domain.Shared
```

---

## 三、API 契约总结

```
ICodeGenService:
  ① Web→Code  : POST /api/code-gen/web-build-code    → 渲染模板生成代码文件
  ② Web→DB    : POST /api/code-gen/web-build-db      → 生成 DDL 建表 SQL
  ③ Code→Web  : POST /api/code-gen/code-build-web    → 扫描 C# Entity→Web
  ④ Code→DB   : POST /api/code-gen/code-build-db     → CodeFirst 同步物理表
  ⑤ DB→Web    : POST /api/code-gen/db-to-web         → 物理表→Web 元数据

ITableService   : CRUD  /api/code-gen/table
IFieldService   : CRUD  /api/code-gen/field
ITemplateService: CRUD  /api/code-gen/template
```

---

## 四、配置项

| 配置项 | 位置 | 说明 |
|--------|------|------|
| 分页参数 | PagedAndSortedResultRequestDto | SkipCount, MaxResultCount, Sorting |
| 查询过滤 | 各 GetListInput 中的可选字段 | Name 模糊/精确匹配 |

---

## 五、扩展/改写建议

| 优先级 | 建议 | 说明 |
|--------|------|------|
| 高 | DTO 增加创建/更新方法 | FieldDto 缺少 Create/Update 专用 DTO |
| 高 | ICodeGenService 增加预览接口 | 生成前预览代码内容 |
| 中 | 批量操作接口 | 批量删除字段、批量导入表 |
| 中 | 导出/导入接口 | 模板配置的导出导入 |
| 低 | 历史版本接口 | 字段变更历史的 CRUD |
