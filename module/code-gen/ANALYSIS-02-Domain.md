# 📁 SharpFort.CodeGen.Domain/ 文件夹分析

> **分析日期**: 2026-06-12  
> **路径**: `module/code-gen/SharpFort.CodeGen.Domain/`  
> **文件数量**: 18 个文件（3 个子文件夹）

---

## 一、功能概述

**Domain 层**是整个代码生成模块的**核心**，包含领域实体、模板处理引擎和业务逻辑管理器。遵循 ABP DDD（领域驱动设计）架构。

---

## 二、子文件夹结构

```
Domain/
├── Entities/           ← 3 个实体（领域模型 + SqlSugar ORM 映射）
├── Handlers/           ← 11 个文件（模板处理管线，核心引擎）
└── Managers/           ← 5 个领域服务（代码生成、合并、探测等）
```

---

## 三、Entities/ — 领域实体 (3个文件)

### 3.1 Template.cs — 模板实体（聚合根）

```csharp
[SugarTable("gen_template")]
[SugarIndex("index_gen_template_name", nameof(Name), OrderByType.Asc, IsUnique = true)]
public class Template : FullAuditedAggregateRoot<Guid>
```

**数据库表**: `gen_template`

**属性**:
| 属性 | 数据库列 | 类型 | 约束 | 说明 |
|------|----------|------|------|------|
| Id | id | Guid | PK | 主键 |
| Name | name | string(64) | 必填,唯一 | 模板名称(如 Entity, Service) |
| BuildPath | build_path | string(256) | 必填 | 建议生成路径(支持 Scriban 占位符) |
| Content | content | text | 必填 | 模板内容(Scriban 脚本) |
| Remarks | remarks | string(512) | 可选 | 备注 |
| TemplateEngine | template_engine | string(20) | 必填,默认"Scriban" | 引擎类型(Scriban/Legacy) |

**领域行为**:
- `UpdateBasic()` — 更新名称+路径+备注
- `SetContent()` — 更新模板内容

---

### 3.2 Table.cs — 表定义实体（聚合根）

```csharp
[SugarTable("gen_table")]
[SugarIndex("index_gen_table_name", nameof(Name), OrderByType.Asc, IsUnique = true)]
public class Table : FullAuditedAggregateRoot<Guid>
```

**数据库表**: `gen_table`

**属性**:
| 属性 | 数据库列 | 类型 | 说明 |
|------|----------|------|------|
| Id | id | Guid PK | 主键 |
| Name | name | string(64) 必填唯一 | 表名(PascalCase) |
| Description | description | string(512) | 表描述 |
| ModuleName | module_name | string(128) | 目标模块名称 |
| RootNamespace | root_namespace | string(256) | 解决方案命名空间 |
| IsOverwrite | is_overwrite | bool | 是否覆盖已有文件 |
| TemplateEngine | template_engine | string(20) 默认"Scriban" | 引擎类型 |
| ExtraProperties | extra_properties | JSON | 扩展属性(ABP Feature) |
| Fields | — (导航) | List\<Field\> | 一对多导航属性 |

---

### 3.3 Field.cs — 字段定义实体

```csharp
[SugarTable("gen_field")]
[SugarIndex("index_gen_field_table_name", nameof(TableId), nameof(Name), IsUnique = true)]
[SugarIndex("index_gen_field_table_sort", nameof(TableId), nameof(OrderNum))]
public class Field : FullAuditedEntity<Guid>
```

**数据库表**: `gen_field`

**属性** (11 个业务属性):
| 属性 | 数据库列 | 类型 | 说明 |
|------|----------|------|------|
| Id | id | Guid PK | 主键 |
| TableId | table_id | Guid 必填 | 所属表 ID (外键) |
| Name | name | string(64) 必填 | 字段名 |
| Description | description | string(512) | 字段描述 |
| FieldType | field_type | enum FieldType | 字段类型枚举 |
| Length | length | int | 数据长度 |
| OrderNum | order_num | int | 排序权重 |
| IsRequired | is_required | bool | 是否必填 |
| IsKey | is_key | bool | 是否主键 |
| IsAutoAdd | is_auto_add | bool | 是否自增 |
| IsPublic | is_public | bool | 是否公共字段(如CreationTime) |
| IsQueryField | is_query_field | bool | 是否为查询字段 |
| IsListDisplay | is_list_display | bool | 是否列表显示 |
| IsFormItem | is_form_item | bool | 是否表单项 |
| HtmlType | html_type | string(32) | HTML展示类型 |

**领域行为**:
- `UpdateBasic()` — 更新名称+类型+长度+排序
- `SetFlags()` — 设置 IsRequired/IsKey/IsAutoAdd/IsPublic 标记（含业务规则保护：主键自动设必填）

---

## 四、Handlers/ — 模板处理引擎 (11个文件)

### 核心类图

```
ITemplateHandler (接口)
    ↑
TemplateHandlerBase (抽象基类)
    ↑        ↑           ↑
ModelTemplateHandler   FieldTemplateHandler   NameSpaceTemplateHandler
    (替换@model)       (替换@field)           (替换@namespace)


ITemplateContextEnricher (接口)
    ↑
DefaultTemplateContextEnricher (实现)
    - 将 Table/Field 实体转换为 Scriban 模板上下文
    - 提供 Model/ModelCamel/ModelPlural 等派生属性
    - 包含极简英语复数规则
```

---

### 4.1 模板处理管线 (Legacy 引擎)

#### ITemplateHandler.cs
```csharp
public interface ITemplateHandler : ISingletonDependency
{
    void SetTable(Table table);
    HandledTemplate Invoker(string str, string path);
}
```
**注册**: 单例依赖注入  
**职责**: 接收模板字符串 + 路径，输出处理后的内容

#### TemplateHandlerBase.cs
```csharp
public class TemplateHandlerBase
{
    protected Table Table { get; set; }
    public void SetTable(Table table) { Table = table; }
}
```
抽象基类，持有当前处理的 Table 引用。

#### ModelTemplateHandler.cs
**替换规则**:
- `@model` → 首字母小写的表名（camelCase）
- `@Model` → 首字母大写的表名（PascalCase）
- 同时作用于模板内容和生成路径

#### FieldTemplateHandler.cs
**核心方法 `BuildFields()`**:
1. 遍历 `Table.Fields`
2. 通过反射从 `FieldType` 枚举获取类型字符串（如 "string", "int"）
3. 为每个字段生成：
   - `/// <summary>` XML 注释
   - `[SugarColumn(Length=N)]` 特性（如有长度）
   - `public {type}? {name} { get; set; }` 属性声明
4. 非必填字段自动添加可空标记 `?`

#### NameSpaceTemplateHandler.cs
简单地移除 `@namespace` 占位符（替换为空字符串）

#### HandledTemplate.cs
```csharp
public class HandledTemplate
{
    public required string TemplateStr { get; set; }
    public required string BuildPath { get; set; }
}
```
结果 DTO，包含处理后的模板内容和生成路径。

---

### 4.2 Scriban 引擎支持

#### ITemplateContextEnricher.cs
```csharp
public interface ITemplateContextEnricher : ISingletonDependency
{
    void Enrich(TemplateContext context, Table table);
    int Priority { get; }
}
```
**扩展点**: 可通过实现此接口来注入额外的模板变量。优先级数值小的先执行。

#### DefaultTemplateContextEnricher.cs
将 Table + Field 实体映射为 `TemplateContext` 对象：

| 映射 | 说明 |
|------|------|
| `context.Model` | = Table.Name (PascalCase) |
| `context.ModelCamel` | = 首字母小写 (camelCase) |
| `context.ModelPlural` | = 英语复数形式 |
| `context.RootNamespace` | = Table.RootNamespace 或 "Sf.Abp" |
| `context.Module` | = Table.ModuleName 或 "Rbac" |
| `context.Fields[i].CsharpType` | = FieldType → C# 类型映射（含可空 `?`） |

**英语复数规则** (极简版):
- 以辅音+y结尾 → `ies` (如 Entity → Entities)
- 以 s/x/ch/sh 结尾 → `es`
- 其他 → `s`

#### ScribanHelperFunctions.cs — Scriban 自定义函数
在 Scriban 渲染上下文中注册的 3 个全局函数：

| 函数 | 输入 | 输出示例 |
|------|------|----------|
| `sugar_column(field)` | FieldInfo | `[SugarColumn(ColumnName = "user_name", IsNullable = false, Length = 64)]` |
| `csharp_type(field)` | FieldInfo | `string?` 或 `int` |
| `default_value(field)` | FieldInfo | `string.Empty`, `0`, `false`, `Guid.Empty`, `null` 等 |

---

## 五、Managers/ — 领域服务 (5个文件)

### 5.1 CodeFileManager.cs — 代码文件生成器（核心引擎）

**职责**: 将 Table 元数据 + 模板 → 渲染生成 C# 代码文件

**核心流程 `BuildWebToCodeAsync(Table tableEntity)`**:

```
① 自动探测解决方案根目录
    └→ SolutionDirectoryDetector.Detect()
② 从数据库加载所有模板
    └→ _templateRepository.GetListAsync()
③ 对每个模板：
   ├─ 检查本地工作区是否有覆盖模板 (Templates/{Name}.scriban)
   ├─ 判断模板引擎类型
   │  ├─ Legacy: 走 ITemplateHandler 管线处理
   │  └─ Scriban: 走 TemplateContext + Scriban.Template.RenderAsync()
   ├─ 渲染 BuildPath (路径也支持 Scriban 占位符)
   ├─ 路径规范化 (处理旧模板硬编码的绝对路径)
   ├─ 增量安全合并 (IncrementalCodeMerger)
   └─ 写入磁盘文件
```

**关键代码 ① — 本地模板覆盖机制**:
```csharp
string localTemplatePath = Path.Combine(solutionRoot, "module", "code-gen", "Templates", $"{dbTemplate.Name}.scriban");
if (File.Exists(localTemplatePath))
{
    templateContent = await File.ReadAllTextAsync(localTemplatePath);
    // 优先使用本地工作区的模板文件，覆盖数据库中的模板
}
```

**关键代码 ② — Scriban 渲染管线**:
```csharp
// 1. 构建上下文
var contextModel = new TemplateContext();
foreach (var enricher in _enrichers.OrderBy(x => x.Priority))
    enricher.Enrich(contextModel, tableEntity);

// 2. 注册自定义函数
var scriptObject = new ScriptObject();
scriptObject.Import(contextModel);
scriptObject.Import("sugar_column", new Func<FieldInfo, string>(...));
scriptObject.Import("csharp_type", new Func<FieldInfo, string>(...));
scriptObject.Import("default_value", new Func<FieldInfo, string>(...));

// 3. 渲染
var scribanTemplate = Scriban.Template.Parse(templateContent);
renderedContent = await scribanTemplate.RenderAsync(renderContext);
```

**配置项**:
| 配置 | 位置 | 说明 |
|------|------|------|
| `CodeGen:SolutionRoot` | IConfiguration | 解决方案根目录 |
| 本地模板路径 | 固定 `{solutionRoot}/module/code-gen/Templates/` | 工作区模板优先级高于数据库 |

---

### 5.2 IncrementalCodeMerger.cs — 增量安全合并器

**核心问题**: 代码生成器重新生成时，如何保护开发者手写的业务代码？

**解决方案**: 使用标记块（Marker Block）机制

**标记语法**:
```csharp
// <sf-custom-code-start id="CustomLogic">
// 这里的手写代码会被保护
// </sf-custom-code-end>
```

**合并逻辑 `Merge(existingFilePath, newContent)`**:
```
如果文件不存在:
  → 直接写入新内容（加警告头）
如果文件存在:
  ├─ 检查旧文件标记成对性 (ValidateMarkers)
  ├─ 检查新内容标记成对性
  ├─ 提取旧文件中的所有自定义代码块 (ExtractCustomBlocks)
  ├─ 如果旧文件无任何标记 → 返回 null (跳过覆盖，警告)
  └─ 将旧内容的代码块注入新内容对应标记中 (ApplyCustomBlocks)
      └→ 跳过新模板中标记块的内置默认体
```

**警告头部**:
```csharp
// <sf-generated-warning>此文件由代码生成器自动生成，请勿手动修改。</sf-generated-warning>
```

**标记支持**:
- **带ID标记**: `<sf-custom-code-start id="CustomLogic">` — 确保跨版本匹配
- **匿名标记**: `<sf-custom-code-start>` — 按出现顺序匹配

**安全保护**:
- 标记不成对 → 抛出 `InvalidOperationException`
- 无标记的旧文件 → 不覆盖（返回 null，触发 LogWarning）
- DROP 语句拦截（在 CodeGenService 中额外保护）

---

### 5.3 SolutionDirectoryDetector.cs — 解决方案目录探测器

**三级探测策略**:

| 优先级 | 策略 | 实现 |
|--------|------|------|
| Level 1 | 向上递归查找 `.sln` 文件 | `FindSolutionBySln()` — 从当前目录向上遍历 |
| Level 2 | 查找 `.csproj` 密度最高的目录 | `FindSolutionByCsprojDensity()` — 向上最多4层 |
| Level 3 | 环境变量 / 配置文件 | `SF_SOLUTION_ROOT` 环境变量 或 `CodeGen:SolutionRoot` 配置 |

**边界条件**: 三级全部失败时抛出详细异常，提示用户配置环境变量

---

### 5.4 WebTemplateManager.cs — Code→Web 同步器

**职责**: 从现有的 C# Entity 类自动生成 Web 端的表/字段配置

**核心流程 `BuildCodeToWebAsync()`**:
```
① 扫描所有 ABP 模块中的 Type
   ├─ 过滤: IgnoreCodeFirst 不为 null → 跳过
   ├─ 过滤: SugarTable 特性为 null → 跳过
   └─ 过滤: SplitTable 分表 → 跳过
② 对每个 Entity Type:
   ├─ 反射提取 SugarTable 获取表名
   └─ 反射提取每个 Property:
       ├─ 从 PropertyType 判断 FieldType 枚举 (通过 Display.Description 匹配)
       ├─ 判断是否可空 (IsGenericType + Nullable<>)
       ├─ 提取 [SugarColumn] 特性获取主键、长度信息
       └─ 设置默认 UI 标记: IsQueryField/IsListDisplay/IsFormItem/HtmlType="Input"
③ 返回 List<Table>
```

---

### 5.5 DataBaseManger.cs — 数据库管理器（空壳）

```csharp
public class DataBaseManger : DomainService { }
```
**状态**: ⚠️ 空实现，作为占位符预留

---

## 六、项目依赖

```
SharpFort.CodeGen.Domain.csproj:
  TargetFramework: net10.0.0
  Package:
    - Scriban 7.2.3                          ← 模板引擎
    - Volo.Abp.Ddd.Domain 10.4.1             ← ABP 框架
  Project:
    - SharpFort.SqlSugarCore.Abstractions    ← SqlSugar 仓储抽象
    - SharpFort.CodeGen.Domain.Shared        ← 领域共享层
```

---

## 七、扩展/改写建议

| 优先级 | 建议 | 说明 |
|--------|------|------|
| 🔴 高 | 实现 DataBaseManger | 当前是空壳，需实现 DB→Web 的具体逻辑 |
| 🔴 高 | 增加 Vue 前端模板生成 | 新增 VueRenderer / FrontendTemplateHandler |
| 🟡 中 | 多引擎支持 | 除 Scriban 外增加 Razor、Handlebars 引擎 |
| 🟡 中 | 模板上下文扩展点 | ITemplateContextEnricher 插件化为多注册模式 |
| 🟡 中 | 复数规则完善 | 当前仅极简英语规则，需 Humanizer 库支持 |
| 🟢 低 | 模板预览 | 生成前预览渲染结果 |
| 🟢 低 | 字段分组模板 | 支持字段分组（如：基本信息、审计信息） |
