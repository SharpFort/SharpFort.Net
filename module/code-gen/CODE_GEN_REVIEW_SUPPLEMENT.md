# Code-Gen 增强方案 — 独立审查补充意见

本文是对 `CODE_GEN_ENHANCEMENT_PLAN.md` 的独立审查，整合了并行专家审查的 6 项补充建议及原 AI 分析报告中遗留的结构性建议。阅读时请对照原方案。

**本次增强版本定位**：聚焦后端代码生成（C# / ABP / SqlSugar）。前端代码生成（Vue / JS / TS）纳入下一版本。Code-Gen Skill 将独立创建，不混入本模块。

---

## 补充 1：SolutionDirectoryDetector — 增加部署环境 Fallback（采纳）

原方案用向上递归查找 `.sln` 定位根目录。但 `dotnet publish` 不会输出 `.sln` 文件，生产环境将失效。

**补充三级 Fallback**：

| 优先级 | 策略 | 适用场景 |
|--------|------|----------|
| 1 | 向上递归查找 `.sln` 文件 | 开发环境 |
| 2 | 查找 `.csproj` 文件最多的目录 | 开发环境兜底（monorepo 多项目场景） |
| 3 | 读取 `appsettings.json` 或环境变量 `SF_SOLUTION_ROOT` | 生产环境显式配置 |

三级全部失败时抛出明确异常，包含搜索起点路径和各层级查找结果。

---

## 补充 2：PostWebBuildDbAsync — 增加 DDL 安全护栏（采纳）

从 Web API 直接执行 `ALTER TABLE` / `CREATE TABLE` 是高风险操作，原方案未提及安全措施。

**补充安全措施**：

- **权限**：接口仅限超级管理员（`[Authorize(Roles = "admin")]` 或对应 Casbin 策略）
- **审计**：执行前将完整 DDL SQL 写入操作日志（如 AuditLog 模块）
- **DDL 白名单**：禁止自动执行 `DROP COLUMN` / `DROP TABLE` / `TRUNCATE TABLE`。仅允许 `ADD COLUMN` / `ALTER COLUMN` / `CREATE TABLE`。删除操作需人工手动执行
- **Dry-run**：`POST /api/app/code-gen/web-build-db?dryRun=true` 仅返回将执行的 SQL，不实际执行
- **事务**：所有 DDL 操作包裹在事务中，失败回滚

---

## 补充 3：IncrementalCodeMerger — 正则解析的边界情况（采纳）

受保护区域标记 `<sf-custom-code-start>...</sf-custom-code-end>` 用正则匹配合并，需明确边界处理规则。

**补充规则**：

| 场景 | 行为 |
|------|------|
| 标记独占一行（行级标记） | 正常提取并重新插入 |
| 不支持行内标记 | 设计如此，降低正则复杂度 |
| 标记不成对（仅 start 无 end，或反之） | **报错并跳过该文件**，记录错误日志。禁止静默丢失代码 |
| 文件已存在但不含任何标记 | **不覆盖**，输出警告引导开发者手动迁移 |
| 空标记（start 后紧跟 end） | 正常处理，保留空区域 |

每个生成文件头部自动插入：

```csharp
// <sf-generated-warning>此文件由代码生成器自动生成，请勿手动修改。</sf-generated-warning>
```

---

## 补充 4：Scriban 迁移 — 旧模板兼容策略（采纳）

数据库 `gen_template` 表中可能已有使用旧占位符（`@model` / `@Model` / `@field`）的模板数据。直接切换 Scriban 会导致现有模板渲染失败。

**补充迁移方案**：

1. `gen_template` 表新增列 `TemplateEngine`（`varchar(20)`，默认值 `"Scriban"`）
2. 数据库中已存在的旧模板 → `TemplateEngine = "Legacy"`，标记为只读兼容
3. Legacy 模板仍走旧渲染管线（保留旧 Handler 代码），仅读取不修改
4. 首次运行时检测：若存在 Legacy 模板，输出警告日志列出模板名称，引导开发者迁移
5. `TemplateDataSeed` 提供完整 Scriban 格式默认模板
6. 至少一个版本过渡期后废弃 Legacy 引擎

---

## 补充 5：Field.cs 新增属性 — 前端配套变更记录（部分采纳，延后）

原方案为 Field 增加了 `IsQueryField`、`IsListDisplay`、`IsFormItem`、`HtmlType`。这些字段存入数据库列没有问题，但 Scaffolder 管理界面需同步更新编辑控件才能使用。

**处理方式**：

- 数据列在本版本中添加并存储
- 前端 Scaffolder UI 的管理界面变更列入下一版本待办清单
- 后端生成逻辑中 `IsQueryField` 可用于控制 DTO 查询字段的生成

---

## 补充 6：测试策略 — 优先覆盖最高风险场景（采纳）

原方案测试写得较概括，建议优先覆盖以下高风险场景：

**Scriban 模板 — C# 类型映射矩阵**：

- `string` → `string`
- `int` / `int?` → `int` / `int?`
- `long` / `long?` → `long` / `long?`
- `bool` / `bool?` → `bool` / `bool?`
- `decimal` / `decimal?` → `decimal` / `decimal?`
- `DateTime` / `DateTime?` → `DateTime` / `DateTime?`
- `Guid` / `Guid?` → `Guid` / `Guid?`
- 每种类型校验 Entity / DTO / Input 三层渲染结果

**IncrementalCodeMerger — 畸形标记处理**：

- 缺 end → 报错 + 跳过
- 嵌套标记 → 按最外层识别
- 空标记 → 正常处理
- 无标记的已有文件 → 不覆盖 + 警告

**SolutionDirectoryDetector — CI 环境行为**：

- CI 环境（无 `.sln`）→ Fallback 到优先级 2 或 3
- 环境变量生效验证
- 三级全失败 → 异常信息完整性

---

## 补充 7：ITemplateHandler 管道应保留并转型（原 AI 分析遗留建议）

原方案说"替换当前的责任链替换机制"，但当前 Handler 管道通过 DI 注册的特性具有架构价值——不同业务模块可独立注入 Handler 扩展生成逻辑（如 Ai 模块添加自定义模板变量）。

**建议**：保留管道，但转变其职责。Handler 不再做字符串替换，而是向 Scriban 渲染上下文贡献数据：

```csharp
public interface ITemplateContextEnricher : ISingletonDependency
{
    void Enrich(TemplateContext context, Table table);
    int Priority { get; }
}
```

Scriban 负责渲染，Enricher 管道负责准备上下文。各业务模块通过 DI 注入自己的 Enricher 扩展能力。

---

## 补充 8：Table/Field 新增属性应作为实际列而非 ExtraProperties（原 AI 分析遗留建议）

原方案将 `ModuleName`、`RootNamespace`、`IsOverwrite` 等放入 `ExtraProperties` 字典。但 ExtraProperties 是 JSON 列，不可查询、不可索引、无类型安全。这些属性是代码生成的核心控制参数，应作为实际数据库列：

```csharp
// Table.cs — 实际列
[SugarColumn(Length = 128)]
public string? ModuleName { get; set; }

[SugarColumn(Length = 256)]
public string? RootNamespace { get; set; }

public bool IsOverwrite { get; set; }

[SugarColumn(Length = 20)]
public string? TemplateEngine { get; set; } = "Scriban";

// Field.cs — 实际列
public bool IsQueryField { get; set; }
public bool IsListDisplay { get; set; }     // 前端 UI 待下一版本
public bool IsFormItem { get; set; }         // 前端 UI 待下一版本
[SugarColumn(Length = 32)]
public string? HtmlType { get; set; }        // 后续扩展可考虑 ExtraProperties
```

---

## 补充 9：Scriban 模板上下文需完整契约定义（原 AI 分析遗留建议）

原方案仅展示了最简 context 匿名对象，工业级实现需要完整的类型定义作为模板编写的契约：

```csharp
public class TemplateContext
{
    public TableInfo Table { get; set; }
    public List<FieldInfo> Fields { get; set; }
    public string Module { get; set; }
    public string RootNamespace { get; set; }
    public string Model { get; set; }           // PascalCase
    public string ModelCamel { get; set; }      // camelCase
    public string ModelPlural { get; set; }     // 复数形式
}

public class TableInfo
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string ModuleName { get; set; }
    public string RootNamespace { get; set; }
    public bool IsOverwrite { get; set; }
}

public class FieldInfo
{
    public string Name { get; set; }
    public string Type { get; set; }
    public int MaxLength { get; set; }
    public bool IsRequired { get; set; }
    public bool IsPrimaryKey { get; set; }
    public string Description { get; set; }
    public bool IsQueryField { get; set; }
    public int OrderNum { get; set; }
}
```

Scriban 自定义函数（注入为模板可调用的全局函数）：

- `sugar_column(field)` → 生成完整 `[SugarColumn(...)]` 特性代码行
- `csharp_type(field)` → 数据库类型到 C# 类型映射
- `default_value(field)` → 字段默认值的 C# 表达式

---

## 实施优先级汇总

| 优先级 | 内容 | 来源 |
|--------|------|------|
| P0 | SolutionDirectoryDetector 三级 Fallback | 补充 1 |
| P0 | Scriban 引擎迁移 + 旧模板兼容 | 原方案 + 补充 4 |
| P0 | IncrementalCodeMerger（含边界处理） | 原方案 + 补充 3 |
| P0 | Table/Field 实体列变更（非 ExtraProperties） | 补充 8 |
| P1 | PostWebBuildDbAsync（含 DDL 安全护栏） | 原方案 + 补充 2 |
| P1 | PostDbToWebAsync（DB-First 逆向） | 原方案 |
| P1 | ITemplateContextEnricher 管道 | 补充 7 |
| P1 | TemplateContext 完整契约 | 补充 9 |
| P1 | 模板文件系统存储 + 混合查找 | 原方案 |
| P2 | TemplateDataSeed Scriban 格式重写 | 原方案 |
| P2 | 单元测试（类型映射 + Merger + Fallback） | 补充 6 |
| 下一版本 | 前端代码生成、Scaffolder UI、Legacy 引擎移除、Code-Gen Skill | 补充 5 + 其他 |
