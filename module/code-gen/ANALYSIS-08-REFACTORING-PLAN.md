# 🔧 Code-Gen 模块改造方案 — 基于 DESIGN-FINAL v3 的深度分析

> **日期**: 2026-06-12  
> **参考文档**: `DESIGN-FINAL.md` (v2.0), `DESIGN-FINAL-v3.md` (v3.0)  
> **前置分析**: `ANALYSIS-00` ~ `ANALYSIS-07` 共 8 份源码分析  
> **核心结论**: 保留 Scriban 引擎优势，吸收 DESIGN-FINAL 架构精简思想，删除冗余链路

---

## 一、关键发现：DESIGN-FINAL 与当前项目的"世代差异"

### 1.1 根本性差异

| 维度 | DESIGN-FINAL v3 | 当前项目 | 谁更优 |
|------|:--:|:--:|:--:|
| **模板引擎** | 字符串替换 `@model/@field/@namespace` | **Scriban 7.2.3** 引擎 | ✅ 当前项目 |
| **模板语法** | `@model` 占位符 + HandlerChain | `{{Model}}` + 自定义函数 | ✅ 当前项目 |
| **代码保护** | 无（直接覆盖） | **IncrementalCodeMerger** + `sf-custom-code-start` | ✅ 当前项目 |
| **路径解析** | `{SolutionRoot}/{ProjectName}` | Scriban渲染BuildPath + SolutionRoot + InferSubPath | 各有优势 |
| **种子模板** | 8 个（无 Entity）+ 变量路径 | 9 个 Scriban 语法 + 本地覆盖机制 | ✅ 当前项目 |
| **DDL 安全** | 无（已删除DB链路） | DROP拦截 + dryRun + admin授权 | 设计如此 |

### 1.2 核心洞察

**DESIGN-FINAL 是为"字符串替换引擎"时代的 code-gen 写的改造方案。**  
当前项目**已经**完成了 Scriban 引擎的升级——这是 DESIGN-FINAL Phase 4 规划的远期目标。

因此本方案的核心原则是：

> **保留 Scriban 引擎的所有优势（模板、合并器、安全机制），吸收 DESIGN-FINAL 的架构精简思想。**

---

## 二、对比矩阵：逐项决策

### 2.1 删除项（共识 ✅）

| 项目 | DESIGN-FINAL | 理由 | 当前状态 |
|------|:--:|------|----------|
| `PostWebBuildDbAsync()` | ❌ 删除 | CodeFirst 自动建表，code-gen 不应管 DDL | 已实现但应删除 |
| `PostCodeBuildDbAsync()` | ❌ 删除 | 同上，ORM 基础设施职责 | 已实现但应删除 |
| `DataBaseManger.cs` | ❌ 删除 | 空壳，无任何实现 | 当前为空壳 |
| Entity 种子模板 | ❌ 删除 | Domain 层应手写，不自动生成 | 当前有 Entity 种子模板 |
| Entity.scriban | ❌ 删除 | 同上 | 当前存在 |

**结论：删除这 5 项，与 DESIGN-FINAL 完全一致。**

### 2.2 保留项（当前项目更优 ✅）

| 项目 | 决策 | 理由 |
|------|:--:|------|
| **Scriban 引擎** | ✅ 保留 | 远比字符串替换强大，DESIGN-FINAL 的 Phase 4 目标 |
| **IncrementalCodeMerger** | ✅ 保留 | 智能代码合并，比 DESIGN-FINAL 的"直接覆盖"更安全 |
| **ITemplateContextEnricher** | ✅ 保留 | 可扩展的模板上下文，优于硬编码 |
| **ScribanHelperFunctions** | ✅ 保留 | sugar_column/csharp_type/default_value 三个自定义函数 |
| **SolutionDirectoryDetector** | ✅ 保留 | 已实现三级探测，比 DESIGN-FINAL 的"约定推导"更健壮 |
| **Template 本地覆盖** | ✅ 保留 | `Templates/*.scriban` 开发期覆盖数据库模板 |
| **DDL 安全机制** | ✅ 保留 | 虽然删除 DB 链路，但安全机制设计模式值得保留给其他功能 |
| **多数据库适配** | ✅ 保留 | PostgreSQL/MySQL/SQL Server 的类型映射表 |

### 2.3 吸收项（从 DESIGN-FINAL 借鉴 🔄）

| 项目 | DESIGN-FINAL 方案 | 在当前 Scriban 体系下的适配 |
|------|-------------------|--------------------------|
| **删除 Legacy 引擎** | 无（DESIGN-FINAL 本身是Legacy） | ⚡ 当前更彻底：删除整个 Handler Chain + Legacy 代码路径 |
| **YiTable 扩展字段** | `ProjectName`, `LastSyncTime`, `LastBuildTime` | 直接加到 Table 实体 |
| **Truncate→Merge** | upsert by Name | 改造 `PostCodeBuildWebAsync()` |
| **ProjectName 提取** | 从 Entity 命名空间解析 | 改造 `WebTemplateManager` |
| **路径变量系统** | `{SolutionRoot}/{ProjectName}` | 融入 Scriban 上下文（作为内置变量注入） |
| **手动刷新端点** | `PostRefreshAsync()` | 新增到 `CodeGenService` |
| **种子模板修正** | 命名空间变量化 + 删除 Entity | 修改 `TemplateDataSeed.GetSeedData()` |

---

## 三、"Legacy 引擎"的去留决策

### 3.1 当前双引擎架构

```
CodeFileManager.BuildWebToCodeAsync()
  ├─ templateEngine == "Legacy" ?
  │   ├─ ModelTemplateHandler    (@model/@Model → 表名)
  │   ├─ FieldTemplateHandler    (@field → C#属性代码)
  │   └─ NameSpaceTemplateHandler(@namespace → "")
  └─ templateEngine == "Scriban" ?
      ├─ ITemplateContextEnricher.Enrich()
      ├─ ScribanHelperFunctions 注册
      └─ Scriban.Template.RenderAsync()
```

### 3.2 决策：删除 Legacy 引擎

**理由**:
1. DESIGN-FINAL v3 的所有"修复"（@namespace 动态替换、路径变量化）在 Scriban 体系下**不是修复，是降级**
2. Legacy Handler Chain 是**字符串替换实现**，而 Scriban 模板中这些占位符**已经以 `{{变量}}` 形式**存在
3. Scriban 模板已覆盖所有 8 种文件类型，Legacy 没有任何模板使用它
4. 代码中有 `_logger.LogWarning("建议尽快迁移至 Scriban！")`——说明 Legacy 本就是兼容模式

**删除内容**:
```
删除文件:
  ✗ ITemplateHandler.cs
  ✗ TemplateHandlerBase.cs
  ✗ ModelTemplateHandler.cs
  ✗ FieldTemplateHandler.cs
  ✗ NameSpaceTemplateHandler.cs
  ✗ HandledTemplate.cs

修改文件:
  ↻ CodeFileManager.cs — 删除 Legacy 分支（约30行）
  ↻ Template.cs — 删除 TemplateEngine 字段（或标记为 Obsolete）
```

---

## 四、改造方案详细设计

### Phase 1: 删除冗余链路（P0，不改不能用）

#### 1.1 删除 DB 相关 API

**文件**: `CodeGenService.cs`

```csharp
// ✗ 删除整个方法
public async Task<string> PostWebBuildDbAsync(List<Guid> ids, bool dryRun = false) { ... }

// ✗ 删除整个方法
public async Task PostCodeBuildDbAsync() { ... }
```

**文件**: `ICodeGenService.cs`

```csharp
// ✗ 删除这两行声明
Task<string> PostWebBuildDbAsync(List<Guid> ids, bool dryRun = false);
Task PostCodeBuildDbAsync();
```

**文件**: `CodeGenService.cs` — 删除对应的私有辅助方法（仅在 DB 操作中使用）:
- `GetColumnSqlDefinition()`
- `GetAlterColumnSql()`
- `MapFieldTypeToSqlType()` （注意：`GetDbTypeName()` 也可能仅被这些方法使用）
- `IsCommonField()`

#### 1.2 删除 DataBaseManger.cs

直接删除文件 `SharpFort.CodeGen.Domain/Managers/DataBaseManger.cs`。

#### 1.3 删除 Legacy 引擎

**删除 6 个文件**:
```
SharpFort.CodeGen.Domain/Handlers/ITemplateHandler.cs
SharpFort.CodeGen.Domain/Handlers/TemplateHandlerBase.cs
SharpFort.CodeGen.Domain/Handlers/ModelTemplateHandler.cs
SharpFort.CodeGen.Domain/Handlers/FieldTemplateHandler.cs
SharpFort.CodeGen.Domain/Handlers/NameSpaceTemplateHandler.cs
SharpFort.CodeGen.Domain/Handlers/HandledTemplate.cs
```

**修改 `CodeFileManager.cs`** — 删除 Legacy 分支（约第99-115行）:

```csharp
// ✗ 删除整个 if (templateEngine == "Legacy") 分支
// ✗ 删除 _legacyHandlers 字段和构造函数注入
```

修改后 `CodeFileManager` 构造函数简化为:
```csharp
public CodeFileManager(
    IEnumerable<ITemplateContextEnricher> enrichers,  // 保留
    ISqlSugarRepository<DbTemplate> templateRepository,  // 保留
    IConfiguration configuration,  // 保留
    ILogger<CodeFileManager> logger)  // 保留
```

#### 1.4 删除 Entity 种子模板

**文件**: `TemplateDataSeed.cs`

删除 `GetSeedData()` 中 GUID 为 `673752e5-3ba5-48fa-bb6d-978d46a81e3a` 的 Entity 模板条目。种子模板从 9 个 → 8 个。

同时删除 `Templates/Entity.scriban` 文件。

#### 1.5 Table 实体删除 TemplateEngine 字段

**文件**: `Table.cs`

```csharp
// ✗ 删除
[SugarColumn(ColumnName = "template_engine", Length = 20, IsNullable = false)]
public string TemplateEngine { get; set; } = "Scriban";
```

**同步修改**:
- `TableDto.cs` — 删除 `TemplateEngine` 属性
- `DefaultTemplateContextEnricher.cs` — 删除 `TemplateEngine` 赋值
- 所有引用 `table.TemplateEngine` 的代码

---

### Phase 2: 统一实体注册表（P1，核心能力）

#### 2.1 Table 实体扩展新字段

```csharp
// Table.cs — 新增字段
[SugarColumn(ColumnName = "project_name", Length = 128, IsNullable = true)]
public string? ProjectName { get; set; }

[SugarColumn(ColumnName = "last_sync_time", IsNullable = true)]
public DateTime? LastSyncTime { get; set; }

[SugarColumn(ColumnName = "last_build_time", IsNullable = true)]
public DateTime? LastBuildTime { get; set; }
```

保留已有字段: `ModuleName`（用于筛选）和原有的 `BuildOutputPath` 相关字段。

#### 2.2 DTO 同步

```csharp
// TableDto.cs — 新增映射
public string? ProjectName { get; set; }
public DateTime? LastSyncTime { get; set; }
public DateTime? LastBuildTime { get; set; }
```

#### 2.3 WebTemplateManager — Code→Web 改为 Merge

**当前行为**: TRUNCATE + INSERT（全覆盖）
**改造后**: Upsert by Name

```csharp
public async Task<List<Table>> BuildCodeToWebAsync()
{
    // 扫描所有 Entity Type（逻辑不变）
    var entityTypes = ...;
    var scannedTables = new List<Table>();
    foreach (Type entityType in entityTypes)
    {
        var table = EntityTypeMapperToTable(entityType);
        // 🆕 从命名空间提取 ProjectName
        table.ProjectName = ExtractProjectName(entityType.Namespace);
        table.LastSyncTime = DateTime.UtcNow;
        scannedTables.Add(table);
    }

    // 🆕 Merge 而非 Truncate+Insert
    foreach (var scanned in scannedTables)
    {
        var existing = await _repository._DbQueryable
            .Where(x => x.Name == scanned.Name).FirstAsync();
        
        if (existing != null)
        {
            // 更新已有记录
            existing.Description = scanned.Description;
            existing.ProjectName = scanned.ProjectName;
            existing.LastSyncTime = scanned.LastSyncTime;
            // ... 更新 Fields
            await _repository.UpdateAsync(existing);
        }
        else
        {
            // 插入新记录
            await _repository.InsertAsync(scanned);
        }
    }
    
    // 🆕 清理 Entity 中已删除的表
    // (可选，取决于业务需求)
    
    return scannedTables;
}

// 🆕 新增辅助方法
private static string? ExtractProjectName(string? namespaceStr)
{
    // "SharpFort.Rbac.Domain.Entities" → "Rbac"
    if (string.IsNullOrEmpty(namespaceStr)) return null;
    var parts = namespaceStr.Split('.');
    return parts.Length >= 2 ? parts[1] : parts[0];
}
```

#### 2.4 CodeGenService — Truncate→Merge

```csharp
// PostCodeBuildWebAsync() — 改造
public async Task PostCodeBuildWebAsync()
{
    List<Table> tables = await _webTemplateManager.BuildCodeToWebAsync();
    
    // ✗ 删除这两行
    // _tableRepository._Db.DbMaintenance.TruncateTable<Table>();
    // _tableRepository._Db.DbMaintenance.TruncateTable<Field>();
    
    // ✅ Merge 逻辑已在 WebTemplateManager 中处理
    // ✅ 插入/更新已在上一步完成
}
```

#### 2.5 新增 PostRefreshAsync 端点

```csharp
// CodeGenService.cs
public async Task PostRefreshAsync()
{
    _logger.LogInformation("[CodeGen] 手动刷新实体注册表...");
    await PostCodeBuildWebAsync(); // 内部已改为 merge
    _logger.LogInformation("[CodeGen] 刷新完成！");
}

// ICodeGenService.cs
Task PostRefreshAsync();
```

#### 2.6 CodeFileManager — 记录生成时间

在 `BuildWebToCodeAsync()` 末尾添加:
```csharp
// 记录最后生成时间
tableEntity.LastBuildTime = DateTime.UtcNow;
// 通过仓储更新（需注入 ISqlSugarRepository<Table>）
await _tableRepository.UpdateAsync(tableEntity);
```

---

### Phase 3: 路径变量系统（P1，配合 Scriban）

#### 3.1 当前路径生成机制

当前 `CodeFileManager` 的路径流程：
```
dbTemplate.BuildPath (如 "module/{{Module}}/.../{{Model}}Entity.cs")
  → Scriban 渲染 BuildPath
  → InferSubPath() 推断子路径
  → 加上 SolutionRoot
  → 绝对路径写入
```

#### 3.2 改造方案

在 Scriban 上下文中注入路径变量，使种子模板可以直接使用:

**ScriptObject 注册**:
```csharp
// CodeFileManager — 在渲染上下文中注入
scriptObject.Import("solution_root", solutionRoot);
scriptObject.Import("project_name", tableEntity.ProjectName ?? tableEntity.ModuleName ?? "Rbac");
```

**种子模板 BuildPath 改为**:
```
// 之前:
"module/{{Module}}/SharpFort.{{Module}}.Application.Contracts/Dtos/{{Model}}/{{Model}}GetListInput.cs"

// 之后 (更灵活):
"{{solution_root}}/src/SharpFort.{{project_name}}.Application.Contracts/Dtos/{{Model}}/{{Model}}GetListInput.cs"
```

#### 3.3 TemplateDataSeed 路径去硬编码

所有 8 个种子模板的 `BuildPath` 改为使用 `{{solution_root}}` 和 `{{project_name}}` 变量:
```
GetListInput:     "{{solution_root}}/src/.../Dtos/{{Model}}/{{Model}}GetListInput.cs"
GetListOutputDto: "{{solution_root}}/src/.../Dtos/{{Model}}/{{Model}}GetListOutputDto.cs"
GetOutputDto:     "{{solution_root}}/src/.../Dtos/{{Model}}/{{Model}}GetOutputDto.cs"
CreateInput:      "{{solution_root}}/src/.../Dtos/{{Model}}/{{Model}}CreateInput.cs"
UpdateInput:      "{{solution_root}}/src/.../Dtos/{{Model}}/{{Model}}UpdateInput.cs"
IServices:        "{{solution_root}}/src/.../IServices/I{{Model}}Service.cs"
Service:          "{{solution_root}}/src/.../Services/{{Model}}Service.cs"
Api:              "{{solution_root}}/src/.../Web/.../{{model_camel}}Api.js"
```

---

### Phase 4: 体验优化（P2，锦上添花）

#### 4.1 FieldType 枚举扩展

```csharp
// FieldType.cs — 新增
Float = 8,   // [Display(Name = "float", Description = "Single")]
Double = 9,  // [Display(Name = "double", Description = "Double")]
```

同步更新:
- `DefaultTemplateContextEnricher.GetCsharpType()` — 添加 float/double 映射
- `ScribanHelperFunctions.DefaultValue()` — 添加 `"float" => "0F"`, `"double" => "0D"`

#### 4.2 TableService — 增加 Name 筛选

```csharp
// TableGetListInput.cs
public string? Name { get; set; }

// TableService.cs
public override Task<PagedResultDto<TableDto>> GetListAsync(TableGetListInput input)
{
    // 添加 WhereIF(x => x.Name!.Contains(input.Name!))
}
```

#### 4.3 TemplateService — 模糊搜索

```csharp
// 当前: x.Name == input.Name (精确)
// 改为: x.Name!.Contains(input.Name!) (模糊)
```

#### 4.4 FieldService — 修正拼写

```csharp
// GetFieldType() 中:
// "lable" → "label"
new { label = x.Name, value = (int)Enum.Parse<FieldType>(x.Name) }
```

⚠️ 需同步前端代码。

#### 4.5 PostDir 跨平台

```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    Process.Start("explorer.exe", path);
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    Process.Start("xdg-open", path);
else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    Process.Start("open", path);
```

#### 4.6 种子模板内容修正

- `using lo.Abp` → `using Volo.Abp`（如果存在）
- Vue API 模板：`.vue` → `.js`（如果实际是 JS 文件）

---

## 五、改造后的最终架构

```
                     ┌──────────────────────┐
                     │   SqlSugar Db First   │
                     │  (数据库 → 实体代码)   │
                     └──────────┬───────────┘
                                │
                                ▼
                ┌───────────────────────────────┐
                │  手写 Entity 类                 │
                │  [SugarTable("User")]          │
                │  namespace ...Rbac.Domain      │
                └───────┬───────────────────────┘
                        │
          ┌─────────────┼─────────────┐
          │             │             │
          ▼             ▼             ▼
   ┌───────────┐ ┌────────────┐ ┌────────────┐
   │ CodeFirst │ │PostRefresh│ │PostWebBuild│
   │ 自动建表   │ │ 扫描实体    │ │ Code(ids)  │
   │ (ORM职责)  │ │ → Merge    │ │ → 生成代码  │
   │           │ │ → YiTable  │ │ (Scriban)  │
   └───────────┘ └─────┬──────┘ └─────┬──────┘
                       │               │
                       ▼               ▼
                ┌──────────────┐ ┌──────────────┐
                │  YiTable     │ │  生成文件      │
                │  (注册表)     │ │ ───────────  │
                │ ─────────── │ │  DTOs × 5    │
                │ ProjectName │ │  IService    │
                │ LastSyncTime│ │  Service     │
                │ LastBuildTm │ │  xxxApi.js   │
                │ Fields[N]   │ └──────────────┘
                └──────────────┘

    Scriban 引擎（唯一引擎）
    ├── TemplateContext (TableInfo + List<FieldInfo>)
    ├── IncrementalCodeMerger (sf-custom-code-start 标记保护)
    ├── ITemplateContextEnricher (可扩展)
    └── ScribanHelperFunctions (sugar_column/csharp_type/default_value)

    ❌ 已删除:
    ├── PostWebBuildDbAsync / PostCodeBuildDbAsync
    ├── DataBaseManger.cs
    ├── Legacy 引擎 (5 个 Handler)
    ├── Entity 种子模板
    └── HandledTemplate.cs
```

---

## 六、文件变更统计

| 操作 | 数量 | 文件列表 |
|------|:--:|------|
| ✗ **删除文件** | 7 | `DataBaseManger.cs`, `ITemplateHandler.cs`, `TemplateHandlerBase.cs`, `ModelTemplateHandler.cs`, `FieldTemplateHandler.cs`, `NameSpaceTemplateHandler.cs`, `HandledTemplate.cs` |
| ✗ **删除模板** | 1 | `Templates/Entity.scriban` |
| ↻ **修改文件** | ~14 | `CodeGenService.cs`, `ICodeGenService.cs`, `CodeFileManager.cs`, `WebTemplateManager.cs`, `Table.cs`, `TableDto.cs`, `FieldType.cs`, `TemplateDataSeed.cs`, `TableService.cs`, `TableGetListInput.cs`, `TemplateService.cs`, `FieldService.cs`, `DefaultTemplateContextEnricher.cs`, `ScribanHelperFunctions.cs` |
| 🆕 **新增文件** | 0 | 所有改动在已有文件中完成 |

**预估改动量**: 约 **-400 行 + 200 行 = -200 行**（净减少，删除 > 新增）

---

## 七、风险与注意事项

| 风险 | 等级 | 缓解措施 |
|------|:--:|------|
| 删除 Legacy 引擎影响旧模板 | 🟡 中 | 检查数据库 `gen_template` 表中是否有 `TemplateEngine="Legacy"` 的记录；如有，需要先迁移 |
| Delete Entity 模板影响下游 | 🟡 中 | 确认没有其他模块依赖 Entity 自动生成 |
| Merge 逻辑可能丢数据 | 🟢 低 | 先在测试环境验证 upsert 行为 |
| 字段删除（TemplateEngine） | 🟢 低 | DB 迁移脚本需要 ALTER TABLE DROP COLUMN |
| 前端的 `lable→label` | 🟡 中 | 需要同步修改前端代码 |

---

## 八、实施路线图

```
Phase 1 (P0): 删除冗余链路
  ├── 1.1 删除 PostWebBuildDbAsync + PostCodeBuildDbAsync
  ├── 1.2 删除 DataBaseManger.cs
  ├── 1.3 删除 Legacy 引擎（6 文件）
  ├── 1.4 删除 Entity 种子模板
  └── 1.5 删除 Table.TemplateEngine 字段
  预估: 3-4 小时

Phase 2 (P1): 统一实体注册表
  ├── 2.1 扩展 Table 实体 (ProjectName, LastSyncTime, LastBuildTime)
  ├── 2.2 DTO 同步
  ├── 2.3 WebTemplateManager: Truncate→Merge + ProjectName提取
  ├── 2.4 CodeGenService: 删除 Truncate 调用
  ├── 2.5 新增 PostRefreshAsync 端点
  └── 2.6 CodeFileManager: 记录生成时间
  预估: 5-6 小时

Phase 3 (P1): 路径变量系统
  ├── 3.1 Scriban 上下文注入 solution_root + project_name
  └── 3.2 种子模板 BuildPath 变量化
  预估: 2-3 小时

Phase 4 (P2): 体验优化
  ├── 4.1 FieldType 加 Float/Double
  ├── 4.2 TableService Name 筛选
  ├── 4.3 TemplateService 模糊搜索
  ├── 4.4 lable→label 修正
  ├── 4.5 PostDir 跨平台
  └── 4.6 种子模板内容修正
  预估: 1-2 小时

────────────────────────────────
  总计: 约 2 天工作量
```

---

> **下一步**: 基于本文档，使用 `writing-plans` + `subagent-driven-development` 按 Phase 1→2→3→4 顺序实施。每个 Phase 建议一个独立的 PR。
