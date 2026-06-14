# Code-Gen 模块终极改造方案

> **日期**: 2026-06-13  
> **基于**: ANALYSIS-00~08 全量源码分析 + 逐文件代码审查  
> **目标**: 精简冗余代码、修复已知缺陷、完善现有功能  
> **原则**: 保留 Scriban 引擎优势，不新增功能方向，仅做减法和修复

---

## 一、现状总结与核心问题

### 1.1 模块概况

| 项目 | 数量 |
|------|:--:|
| 源文件（不含 obj） | 38 个 .cs + 8 个 .scriban + 5 个 .csproj |
| 实体 | 3 个（Template, Table, Field） |
| 转换方向 | 5 个（Web→Code, Web→DB, Code→Web, Code→DB, DB→Web） |
| 模板引擎 | 2 套（Scriban + Legacy），Scriban 为实际使用 |
| 种子模板 | 8 个（含 1 个 Entity 种子模板） |

### 1.2 核心问题清单

| # | 问题 | 严重度 | 当前状态 |
|---|------|:--:|------|
| P1 | `DataBaseManger.cs` 空壳（仅含类声明） | 🔴 高 | 死代码 |
| P2 | Legacy 引擎 6 个文件从未被使用 | 🔴 高 | 死代码，所有种子/本地模板均为 Scriban |
| P3 | `PostCodeBuildWebAsync` 使用 TRUNCATE 全覆盖 | 🔴 高 | 丢失手动 Web 配置 |
| P4 | Entity 种子模板（Domain 实体不应自动生成） | 🟡 中 | 与 DDD 手写实体原则冲突 |
| P5 | `Table.TemplateEngine` + `Template.TemplateEngine` 字段冗余 | 🟡 中 | Scriban 是唯一引擎 |
| P6 | `WebTemplateManager` 未提取 SugarColumn.Description | 🟡 中 | 同步后字段描述丢失 |
| P7 | `WebTemplateManager` 未提取 ModuleName/RootNamespace | 🟡 中 | 同步后模块信息丢失 |
| P8 | `FieldService.GetFieldType()` 拼写错误 `lable` | 🟡 中 | 前端可能适配此错误 |
| P9 | `TemplateService.GetListAsync` 精确匹配 Name | 🟡 中 | 模板多时难以搜索 |
| P10 | `PostDir` 仅支持 Windows | 🟢 低 | 跨平台开发受限 |
| P11 | FieldType 仅 7 种类型 | 🟢 低 | 缺少 Float/Double |
| P12 | `Code→Web` 和 `Code→DB` 存在重复的实体扫描逻辑 | 🟡 中 | 违反 DRY |

---

## 二、改造方案

### Phase 1: 删除死代码（P0）

> **目标**: 清除所有从未被使用的代码，降低维护成本  
> **预估**: 净删除约 120 行，2-3 小时

---

#### Task 1.1: 删除 DataBaseManger.cs

**理由**: 空壳类，无任何实现。DB 相关逻辑已在 `CodeGenService` 中直接实现。

**操作**:
```
删除文件:
  ✗ SharpFort.CodeGen.Domain/Managers/DataBaseManger.cs
```

**影响**: 无。该类无注入、无引用。

---

#### Task 1.2: 删除 Legacy 引擎（6 个文件）

**理由**: 
- 所有 8 个种子模板和 8 个本地 .scriban 模板均使用 `TemplateEngine = "Scriban"`
- 数据库中无 Legacy 模板记录（种子数据全部是 Scriban）
- `CodeFileManager.cs:99` 的 Legacy 分支是纯死代码路径
- `NameSpaceTemplateHandler` 仅替换为空字符串，无实际功能

**操作**:
```
删除文件（6 个）:
  ✗ Handlers/ITemplateHandler.cs          (12 行，接口定义)
  ✗ Handlers/TemplateHandlerBase.cs        (15 行，抽象基类)
  ✗ Handlers/ModelTemplateHandler.cs       (16 行，@model 替换)
  ✗ Handlers/FieldTemplateHandler.cs       (74 行，@field 替换)
  ✗ Handlers/NameSpaceTemplateHandler.cs   (16 行，@namespace 清空)
  ✗ Handlers/HandledTemplate.cs            (10 行，结果 DTO)

修改文件（1 个）:
  ↻ Managers/CodeFileManager.cs
```

**CodeFileManager.cs 修改详情**:

构造函数删除 `_legacyHandlers` 参数:
```csharp
// 修改前
public CodeFileManager(
    IEnumerable<ITemplateContextEnricher> enrichers,
    IEnumerable<ITemplateHandler> legacyHandlers,  // ← 删除
    ISqlSugarRepository<DbTemplate> templateRepository,
    IConfiguration configuration,
    ILogger<CodeFileManager> logger)

// 修改后
public CodeFileManager(
    IEnumerable<ITemplateContextEnricher> enrichers,
    ISqlSugarRepository<DbTemplate> templateRepository,
    IConfiguration configuration,
    ILogger<CodeFileManager> logger)
```

删除字段声明:
```csharp
// ✗ 删除
private readonly IEnumerable<ITemplateHandler> _legacyHandlers;
```

删除 `BuildWebToCodeAsync()` 中的 Legacy 分支（约第 99-115 行）:
```csharp
// ✗ 删除整个 if (Legacy) 分支
if (string.Equals(templateEngine, "Legacy", StringComparison.OrdinalIgnoreCase))
{
    _logger.LogWarning(...);
    HandledTemplate handledTemplate = ...;
    foreach (ITemplateHandler handler in _legacyHandlers) { ... }
    renderedContent = handledTemplate.TemplateStr;
    relativeBuildPath = handledTemplate.BuildPath;
}
else
{
    // Scriban 逻辑保留（去掉 else，直接执行）
}
```

同时删除 `templateEngine` 变量的读取（第 88 行），改为直接走 Scriban:
```csharp
// ✗ 删除
string templateEngine = dbTemplate.TemplateEngine ?? "Scriban";

// 原 else 块中的 Scriban 渲染逻辑直接平铺
```

---

#### Task 1.3: 删除 Entity 种子模板

**理由**: 
- Domain 实体是 DDD 的核心，应手写而非自动生成
- ANALYSIS-00 明确指出"Domain 层应手写，不自动生成"
- 保留 Entity 种子模板会误导用户使用 Code-Gen 生成实体

**操作**:
```
修改文件:
  ↻ SharpFort.CodeGen.SqlSugarCore/TemplateDataSeed.cs
    - 删除 GUID "673752e5-3ba5-48fa-bb6d-978d46a81e3a" 的 Entity 条目
    - 种子模板从 8 个 → 7 个

删除文件:
  ✗ Templates/Entity.scriban (23 行)
```

---

#### Task 1.4: 删除 TemplateEngine 字段

**理由**: 删除 Legacy 引擎后，Scriban 是唯一引擎。`TemplateEngine` 字段不再有意义。

**操作**:
```
修改文件（5 个）:

1. Entities/Table.cs
   ✗ 删除 TemplateEngine 属性 (第 56-57 行)

2. Entities/Template.cs
   ✗ 删除 TemplateEngine 属性 (第 53-54 行)

3. Dtos/Table/TableDto.cs
   ✗ 删除 TemplateEngine 属性 (第 36-37 行)

4. Handlers/DefaultTemplateContextEnricher.cs
   ✗ 删除 TableInfo.TemplateEngine 赋值 (第 31 行)

5. Handlers/TemplateContext.cs
   ✗ 删除 TableInfo.TemplateEngine 属性 (第 24 行)
```

**数据库迁移**: 需要执行 `ALTER TABLE gen_template DROP COLUMN template_engine;` 和 `ALTER TABLE gen_table DROP COLUMN template_engine;`

**风险**: 🟡 中。需确保数据库中无 Legacy 模板记录。执行前检查:
```sql
SELECT COUNT(*) FROM gen_template WHERE template_engine = 'Legacy';
SELECT COUNT(*) FROM gen_table WHERE template_engine = 'Legacy';
```

---

### Phase 2: 修复缺陷（P1）

> **目标**: 修复已知 Bug 和功能缺陷  
> **预估**: 4-5 小时

---

#### Task 2.1: Code→Web 改为增量合并（解决 P3）

**理由**: 当前 `PostCodeBuildWebAsync` 使用 `TRUNCATE + INSERT`，每次同步都会丢失在 Web UI 上手动修改的字段配置（IsQueryField, IsListDisplay, IsFormItem, HtmlType 等）。

**操作**:
```
修改文件（2 个）:
  ↻ Managers/WebTemplateManager.cs — BuildCodeToWebAsync 改为返回扫描结果
  ↻ Services/CodeGenService.cs — PostCodeBuildWebAsync 改为 Upsert 逻辑
```

**WebTemplateManager.cs 新增辅助方法**:
```csharp
/// <summary>
/// 从实体命名空间提取模块名
/// "SharpFort.Rbac.Domain.Entities" → "Rbac"
/// </summary>
private static string? ExtractModuleName(string? namespaceStr)
{
    if (string.IsNullOrEmpty(namespaceStr)) return null;
    var parts = namespaceStr.Split('.');
    return parts.Length >= 2 ? parts[1] : parts[0];
}
```

在 `EntityTypeMapperToTable` 中补充提取逻辑:
```csharp
table.ModuleName = ExtractModuleName(entityType.Namespace);
table.RootNamespace = ExtractRootNamespace(entityType.Namespace);
```

**CodeGenService.cs — PostCodeBuildWebAsync 改造**:
```csharp
// 修改前：
_tableRepository._Db.DbMaintenance.TruncateTable<Table>();
_tableRepository._Db.DbMaintenance.TruncateTable<Field>();
await _tableRepository._Db.InsertNav(tables).Include(x => x.Fields).ExecuteCommandAsync();

// 修改后（Upsert by Name）：
foreach (var scanned in tables)
{
    var existing = await _tableRepository._DbQueryable
        .Where(x => x.Name == scanned.Name).FirstAsync();

    if (existing != null)
    {
        // 更新结构信息，保留手动 UI 配置
        existing.Description = scanned.Description;
        existing.ModuleName = scanned.ModuleName ?? existing.ModuleName;
        existing.RootNamespace = scanned.RootNamespace ?? existing.RootNamespace;
        
        // 同步字段（Upsert）
        foreach (var scannedField in scanned.Fields)
        {
            var existingField = existing.Fields?.FirstOrDefault(f => f.Name == scannedField.Name);
            if (existingField != null)
            {
                // 更新类型/长度等结构信息，保留 UI 标记
                existingField.FieldType = scannedField.FieldType;
                existingField.Length = scannedField.Length;
                existingField.IsRequired = scannedField.IsRequired;
                existingField.IsKey = scannedField.IsKey;
                existingField.IsAutoAdd = scannedField.IsAutoAdd;
            }
            else
            {
                // 新增字段：设置默认 UI 标记
                scannedField.IsQueryField = true;
                scannedField.IsListDisplay = true;
                scannedField.IsFormItem = true;
                scannedField.HtmlType = "Input";
                scannedField.TableId = existing.Id;
                existing.Fields ??= [];
                existing.Fields.Add(scannedField);
            }
        }
        
        await _tableRepository.UpdateAsync(existing);
    }
    else
    {
        // 新表：设置默认 UI 标记并插入
        scanned.Fields.ForEach(x =>
        {
            x.IsQueryField = true;
            x.IsListDisplay = true;
            x.IsFormItem = true;
            x.HtmlType = "Input";
        });
        await _tableRepository._Db.InsertNav(scanned).Include(x => x.Fields).ExecuteCommandAsync();
    }
}
```

---

#### Task 2.2: 修复 WebTemplateManager 信息提取不完整（解决 P6, P7）

**理由**: 当前 `EntityTypeMapperToTable` 和 `PropertyMapperToFiled` 遗漏了多项有用信息。

**修改 `EntityTypeMapperToTable`**:
```csharp
// 新增：提取表描述
table.Description = sugarTable?.TableDescription ?? string.Empty;

// 新增：提取模块名和命名空间
table.ModuleName = ExtractModuleName(entityType.Namespace);
table.RootNamespace = ExtractRootNamespace(entityType.Namespace);
```

新增辅助方法:
```csharp
private static string? ExtractRootNamespace(string? namespaceStr)
{
    if (string.IsNullOrEmpty(namespaceStr)) return null;
    var parts = namespaceStr.Split('.');
    return parts.Length >= 1 ? parts[0] : null;
}
```

**修改 `PropertyMapperToFiled`**:
```csharp
// 新增：提取字段描述
SugarColumn? sugarCol = propertyInfo.GetCustomAttribute<SugarColumn>();
if (sugarCol is not null && !string.IsNullOrEmpty(sugarCol.ColumnDescription))
{
    fieldEntity.Description = sugarCol.ColumnDescription;
}
```

---

#### Task 2.3: 修复 FieldService 拼写错误（解决 P8）

**文件**: `SharpFort.CodeGen.Application/Services/FieldService.cs` 第 44 行

```csharp
// 修改前
return typeof(FieldType).GetFields(...).Select(x => new { lable = x.Name, ... })

// 修改后
return typeof(FieldType).GetFields(...).Select(x => new { label = x.Name, ... })
```

**⚠️ 注意**: 需同步修改前端代码中引用 `lable` 的地方。搜索前端代码中的 `.lable` 并改为 `.label`。

---

#### Task 2.4: TemplateService 改为模糊搜索（解决 P9）

**文件**: `SharpFort.CodeGen.Application/Services/TemplateService.cs` 第 19 行

```csharp
// 修改前（精确匹配）
.WhereIF(input.Name is not null, x => x.Name == input.Name)

// 修改后（模糊匹配）
.WhereIF(input.Name is not null, x => x.Name!.Contains(input.Name!))
```

---

#### Task 2.5: PostDir 跨平台支持（解决 P10）

**文件**: `SharpFort.CodeGen.Application/Services/CodeGenService.cs` 第 286-302 行

```csharp
// 修改前（仅 Windows）
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    path = Uri.UnescapeDataString(path);
    path = string.Join('\\', path.Split('\\').Where(x => !x.Contains('@')));
    Process.Start("explorer.exe", path);
}
else
{
    throw new UserFriendlyException("当前操作系统不支持打开目录");
}

// 修改后（跨平台）
path = Uri.UnescapeDataString(path);
path = string.Join(Path.DirectorySeparatorChar, 
    path.Split(Path.DirectorySeparatorChar, '/').Where(x => !x.Contains('@')));

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    Process.Start("explorer.exe", path);
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    Process.Start("xdg-open", path);
else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    Process.Start("open", path);
else
    throw new UserFriendlyException("当前操作系统不支持打开目录");
```

---

### Phase 3: 功能增强（P2）

> **目标**: 在现有框架上增强功能  
> **预估**: 3-4 小时

---

#### Task 3.1: FieldType 枚举扩展（解决 P11）

**文件**: `SharpFort.CodeGen.Domain.Shared/Enums/FieldType.cs`

```csharp
// 新增
[Display(Name = "float", Description = "Single")]
Float = 8,

[Display(Name = "double", Description = "Double")]
Double = 9,
```

**同步修改**:

1. `DefaultTemplateContextEnricher.GetCsharpType()`:
```csharp
FieldType.Float => "float",
FieldType.Double => "double",
```

2. `ScribanHelperFunctions.DefaultValue()`:
```csharp
"float" => "0F",
"double" => "0D",
```

3. `CodeGenService.MapFieldTypeToSqlType()`:
```csharp
// PostgreSQL
FieldType.Float => "REAL",
FieldType.Double => "DOUBLE PRECISION",
// MySQL
FieldType.Float => "FLOAT",
FieldType.Double => "DOUBLE",
// SQL Server
FieldType.Float => "REAL",
FieldType.Double => "FLOAT",
```

4. `CodeGenService.GetDbTypeName()`:
```csharp
// PostgreSQL
FieldType.Float => "float4",
FieldType.Double => "float8",
// MySQL
FieldType.Float => "float",
FieldType.Double => "double",
// SQL Server
FieldType.Float => "real",
FieldType.Double => "float",
```

---

#### Task 3.2: Table 实体新增跟踪字段

**文件**: `SharpFort.CodeGen.Domain/Entities/Table.cs`

```csharp
// 新增字段
[SugarColumn(ColumnName = "last_sync_time", IsNullable = true)]
public DateTime? LastSyncTime { get; set; }

[SugarColumn(ColumnName = "last_build_time", IsNullable = true)]
public DateTime? LastBuildTime { get; set; }
```

**同步修改**:
- `Dtos/Table/TableDto.cs` — 新增对应属性
- `WebTemplateManager.BuildCodeToWebAsync` — 设置 `LastSyncTime = DateTime.UtcNow`
- `CodeFileManager.BuildWebToCodeAsync` — 生成完毕后更新 `LastBuildTime`

---

#### Task 3.3: Scriban 上下文注入路径变量

**文件**: `SharpFort.CodeGen.Domain/Managers/CodeFileManager.cs`

在 Scriban 渲染上下文中注入额外变量:
```csharp
scriptObject.Import("solution_root", solutionRoot);
scriptObject.Import("project_name", tableEntity.ModuleName ?? "Rbac");
```

这使得种子模板的 BuildPath 可以直接使用 `{{solution_root}}` 和 `{{project_name}}` 变量，增强路径灵活性。

---

#### Task 3.4: 提取公共实体扫描逻辑（解决 P12）

**理由**: `PostCodeBuildDbAsync` 和 `WebTemplateManager.BuildCodeToWebAsync` 包含相同的实体扫描过滤逻辑。

**操作**: 在 `WebTemplateManager` 中提取为静态方法:
```csharp
public static List<Type> ScanEntityTypes(IModuleContainer moduleContainer)
{
    List<Type> entityTypes = [];
    foreach (IAbpModuleDescriptor module in moduleContainer.Modules)
    {
        entityTypes.AddRange(module.Assembly.GetTypes()
            .Where(x => x.GetCustomAttribute<IgnoreCodeFirstAttribute>() == null)
            .Where(x => x.GetCustomAttribute<SugarTable>() != null)
            .Where(x => x.GetCustomAttribute<SplitTableAttribute>() is null));
    }
    return entityTypes;
}
```

`CodeGenService.PostCodeBuildDbAsync` 改为调用此方法:
```csharp
var entityTypes = WebTemplateManager.ScanEntityTypes(_moduleContainer);
```

---

## 三、不推荐的改动（与 ANALYSIS-08 的分歧）

以下改动在 ANALYSIS-08 中建议执行，但经过代码审查后**不推荐**：

| ANALYSIS-08 建议 | 不推荐原因 |
|---|---|
| 删除 `PostWebBuildDbAsync` (Web→DB) | 该功能提供完整的 DDL 生成 + 安全护栏（DROP 拦截、dryRun 预览、admin 授权），是 CodeFirst 之外有价值的补充。用户可通过 Web UI 定义表结构后直接建表，无需手写 Entity。**建议保留**。 |
| 删除 `PostCodeBuildDbAsync` (Code→DB) | 仅 16 行代码，调用 `CodeFirst.InitTables()` 同步实体到物理数据库，是开发期的便捷功能。**建议保留**。 |
| 新增 `PostRefreshAsync` 端点 | 新增 API 端点属于功能扩展，与"精简"目标不符。Phase 2 的 Merge 改造已解决核心问题。 |
| 种子模板 BuildPath 全面变量化 | 当前 `{{Module}}` + `{{Model}}` 变量体系已经足够灵活。引入 `{{solution_root}}` 作为路径前缀反而增加模板复杂度。Task 3.3 仅注入变量供可选使用，不修改现有种子模板。 |

---

## 四、文件变更总览

| 操作 | 数量 | 文件列表 |
|------|:--:|------|
| ✗ **删除文件** | 8 | `DataBaseManger.cs`, `ITemplateHandler.cs`, `TemplateHandlerBase.cs`, `ModelTemplateHandler.cs`, `FieldTemplateHandler.cs`, `NameSpaceTemplateHandler.cs`, `HandledTemplate.cs`, `Entity.scriban` |
| ↻ **修改文件** | 13 | `CodeFileManager.cs`, `CodeGenService.cs`, `WebTemplateManager.cs`, `Table.cs`, `Template.cs`, `TableDto.cs`, `FieldService.cs`, `TemplateService.cs`, `DefaultTemplateContextEnricher.cs`, `TemplateContext.cs`, `FieldType.cs`, `ScribanHelperFunctions.cs`, `TemplateDataSeed.cs` |
| 🆕 **新增文件** | 0 | — |

**预估净变动**: 约 **-200 行**（删除远大于新增）

---

## 五、风险评估

| 风险 | 等级 | 缓解措施 |
|------|:--:|------|
| 删除 Legacy 引擎后数据库可能有 Legacy 记录 | 🟡 中 | 执行前检查 `gen_template` 和 `gen_table` 表中 `template_engine` 列的值 |
| `TemplateEngine` 字段删除需 DB 迁移 | 🟡 中 | 编写 `ALTER TABLE DROP COLUMN` 迁移脚本，在低峰期执行 |
| `lable → label` 修正影响前端 | 🟡 中 | 搜索前端代码确认 `lable` 引用范围，同步修改 |
| Merge 逻辑可能遗漏字段更新 | 🟢 低 | 先在开发环境验证 Upsert 行为，确认结构更新和 UI 配置保留 |
| Entity.scriban 删除影响已有工作流 | 🟢 低 | 确认无模块依赖 Entity 自动生成后再删除 |

---

## 六、实施路线图

```
Phase 1 (P0): 删除死代码                    预估: 2-3 小时
  ├── Task 1.1 删除 DataBaseManger.cs
  ├── Task 1.2 删除 Legacy 引擎 (6 文件 + 修改 CodeFileManager)
  ├── Task 1.3 删除 Entity 种子模板
  └── Task 1.4 删除 TemplateEngine 字段 (5 文件 + DB 迁移)

Phase 2 (P1): 修复缺陷                      预估: 4-5 小时
  ├── Task 2.1 Code→Web 改为增量合并
  ├── Task 2.2 修复 WebTemplateManager 信息提取不完整
  ├── Task 2.3 修复 lable → label 拼写
  ├── Task 2.4 TemplateService 模糊搜索
  └── Task 2.5 PostDir 跨平台支持

Phase 3 (P2): 功能增强                      预估: 3-4 小时
  ├── Task 3.1 FieldType 扩展 Float/Double
  ├── Task 3.2 Table 新增跟踪字段
  ├── Task 3.3 Scriban 上下文注入路径变量
  └── Task 3.4 提取公共实体扫描逻辑

────────────────────────────────
总计: 约 1-2 天工作量
```

**建议**: 按 Phase 顺序实施，每个 Phase 完成后编译验证。Phase 1 和 Phase 2 优先级高，建议优先完成。

---

## 七、验收标准

- [ ] `dotnet build` 全模块编译通过，无 warning（Legacy 相关）
- [ ] `gen_template` 表中无 `template_engine` 列
- [ ] `gen_table` 表中无 `template_engine` 列
- [ ] Code→Web 同步后，手动修改的字段 UI 配置（IsQueryField/IsListDisplay/IsFormItem/HtmlType）被保留
- [ ] Web→Code 正常生成 7 种文件（无 Entity）
- [ ] FieldService `/field/type` 返回 `label`（非 `lable`）
- [ ] TemplateService 搜索支持模糊匹配
- [ ] PostDir 在 Windows/Linux/macOS 均可正常使用
- [ ] FieldType 枚举包含 Float(8) 和 Double(9)

---

> **下一步**: 审核此方案后，按 Phase 1 → 2 → 3 顺序逐步实施。每个 Phase 建议一个独立提交。
