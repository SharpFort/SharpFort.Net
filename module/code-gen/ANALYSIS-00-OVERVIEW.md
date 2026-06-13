# 📋 SharpFort.Net Code-Gen 模块 — 总体分析报告

> **分析日期**: 2026-06-12  
> **项目**: SharpFort.Net (ABP + SqlSugar 框架)  
> **模块路径**: `module/code-gen/`  
> **总文件数**: 50 个源文件 + 5 个已有分析文档  
> **技术栈**: .NET 10, ABP 10.4.1, SqlSugar, Scriban 7.2.3

---

## 📑 文档索引

本分析报告由 **8 份独立文档** 组成，按架构层次自底向上编排：

| 编号 | 文档 | 内容概要 |
|------|------|----------|
| **00** | 🏠 **[ANALYSIS-00-OVERVIEW.md](./ANALYSIS-00-OVERVIEW.md)** | **本文档** — 总体架构、数据模型、六大方向转换、缺陷与路线图 |
| **01** | 🔢 **[ANALYSIS-01-DomainShared.md](./ANALYSIS-01-DomainShared.md)** | **Domain.Shared 层** — `FieldType` 枚举（7种类型 × 3种数据库映射）、模块入口 |
| **02** | 💎 **[ANALYSIS-02-Domain.md](./ANALYSIS-02-Domain.md)** | **Domain 核心层** — 3个实体 + 11个Handler + 5个Manager（最详细） |
| **03** | 📋 **[ANALYSIS-03-ApplicationContracts.md](./ANALYSIS-03-ApplicationContracts.md)** | **Application.Contracts 层** — 5大API契约、4个服务接口、6个DTO |
| **04** | ⚡ **[ANALYSIS-04-Application.md](./ANALYSIS-04-Application.md)** | **Application 层** — `CodeGenService`（469行核心）、DDL安全机制、三大数据库适配 |
| **05** | 🌱 **[ANALYSIS-05-SqlSugarCore.md](./ANALYSIS-05-SqlSugarCore.md)** | **SqlSugarCore 层** — 8个种子模板、三级模板优先级机制 |
| **06** | 📝 **[ANALYSIS-06-Templates.md](./ANALYSIS-06-Templates.md)** | **Templates 工作区** — 8个 Scriban 模板逐一分析、变量上下文 |
| **07** | 🧠 **[ANALYSIS-07-SkillRecommendations.md](./ANALYSIS-07-SkillRecommendations.md)** | **Hermes Skill 推荐** — 改写开发所需的 Skill 矩阵与工作流建议 |
| **08** | 🔧 **[ANALYSIS-08-REFACTORING-PLAN.md](./ANALYSIS-08-REFACTORING-PLAN.md)** | **改造方案** — 基于 DESIGN-FINAL v3 的深度对比与分阶段实施计划 |

> **阅读建议**：点击上方链接直接跳转；先浏览本文档（00）了解全貌，再按编号深入各层；需要改造项目时直接看 **08 号文档**；评估开发工具链时直接跳至 07。

---

## 一、项目定位

这是一个 **数据库驱动的代码生成器模块**，专为 SharpFort.Net 框架设计。它通过 Web UI 配置表结构，自动生成符合 ABP 分层架构的完整 CRUD 代码（Entity、DTO、Service、IService）。

### 核心能力：六大方向转换

```
                    ┌──────────┐
         ③ Code→Web │          │ ① Web→Code
        ┌──────────►│   Web    ├───────────┐
        │           │  (UI配置) │           │
        │           └────┬─────┘           │
        │                │                 │
   ┌────┴─────┐   ② Web→DB          ┌─────┴────┐
   │   Code   │                     │    DB    │
   │(C#文件)  │◄───────────────────►│(物理库)  │
   └────┬─────┘   ④ Code→DB         └────┬─────┘
        │         ⑤ DB→Web              │
        └────────────────────────────────┘
```

| # | 方向 | API | 核心方法 |
|---|------|-----|----------|
| ① | **Web→Code** | `PostWebBuildCodeAsync` | CodeFileManager.BuildWebToCodeAsync() |
| ② | **Web→DB** | `PostWebBuildDbAsync` | DDL 生成 + 安全执行 |
| ③ | **Code→Web** | `PostCodeBuildWebAsync` | WebTemplateManager.BuildCodeToWebAsync() |
| ④ | **Code→DB** | `PostCodeBuildDbAsync` | CodeFirst.InitTables() |
| ⑤ | **DB→Web** | `PostDbToWebAsync` | 物理表逆向工程 |

---

## 二、分层架构

```
┌──────────────────────────────────────────────────┐
│            SharpFort.CodeGen.Application          │  ← 应用服务层
│  CodeGenService / FieldService / TableService     │     编排领域服务
│  / TemplateService                                │
├──────────────────────────────────────────────────┤
│        SharpFort.CodeGen.Application.Contracts    │  ← 接口契约层
│  ICodeGenService / DTOs (Field/Table/Template)    │     API 契约定义
├──────────────────────────────────────────────────┤
│           SharpFort.CodeGen.Domain               │  ← 核心领域层
│  Entities(Template/Table/Field)                   │     实体 + 处理器
│  Handlers(模板引擎管线: Legacy + Scriban)          │     + 管理器
│  Managers(CodeFileMgr/WebTemplateMgr/Merger/...)  │
├──────────────────────────────────────────────────┤
│        SharpFort.CodeGen.Domain.Shared            │  ← 共享层
│  Enums/FieldType (7种字段类型)                     │     跨层类型
├──────────────────────────────────────────────────┤
│       SharpFort.CodeGen.SqlSugarCore              │  ← 基础设施层
│  TemplateDataSeed (8个默认模板种子)                 │     ORM集成
├──────────────────────────────────────────────────┤
│                   Templates/                      │  ← 工作区模板
│  8个 .scriban 模板文件 (本地覆盖)                   │     开发期模板
└──────────────────────────────────────────────────┘
```

---

## 三、数据模型 (3张元数据表)

```
gen_template (模板)           gen_table (表定义)          gen_field (字段)
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│ Id         (PK) │         │ Id         (PK) │         │ Id         (PK) │
│ Name       (UQ) │         │ Name       (UQ) │         │ TableId    (FK) │
│ Content    TEXT │         │ Description     │         │ Name             │
│ BuildPath       │         │ ModuleName      │         │ Description      │
│ TemplateEngine  │    1───N│ RootNamespace   │    1───N│ FieldType(Enum)  │
│ Remarks         │         │ IsOverwrite     │         │ Length           │
└─────────────────┘         │ ExtraProps(JSON)│         │ OrderNum         │
                            │ Fields(Nav)     │────────►│ IsRequired       │
                            └─────────────────┘         │ IsKey            │
                                                        │ IsAutoAdd        │
                                                        │ IsPublic         │
                                                        │ IsQueryField     │
                                                        │ IsListDisplay    │
                                                        │ IsFormItem       │
                                                        │ HtmlType         │
                                                        └─────────────────┘
```

---

## 四、模板引擎双轨制

### 4.1 Scriban 引擎 (推荐，默认)

- 使用 NuGet 包 `Scriban 7.2.3`
- 上下文对象: `TemplateContext` (TableInfo + List\<FieldInfo\>)
- 自定义函数: `sugar_column()`, `csharp_type()`, `default_value()`
- 支持路径模板渲染（BuildPath 也支持占位符）
- 可扩展: `ITemplateContextEnricher` 接口

### 4.2 Legacy 引擎 (兼容模式)

- 使用字符串替换方式
- 处理器管线: `ModelTemplateHandler` → `FieldTemplateHandler` → `NameSpaceTemplateHandler`
- 占位符: `@model`, `@Model`, `@field`, `@namespace`
- 已标记为 deprecated，建议迁移至 Scriban

---

## 五、核心安全机制

### 5.1 增量安全合并 (`IncrementalCodeMerger`)

```
<sf-custom-code-start id="xxx">
  → 用户手写代码被保护，重新生成不丢失
</sf-custom-code-end>
```

### 5.2 DDL 安全检查
- `[Authorize(Roles = "admin")]` — 仅管理员可执行 Web→DB
- DROP 语句拦截 — 禁止任何包含 "DROP" 的 SQL
- dryRun 预览模式 — 执行前可先预览 DDL

### 5.3 文件覆盖保护
- 已存在但无标记的文件 → 跳过覆盖（LogWarning）
- 旧路径硬编码绝对路径 → 自动转换为相对路径

---

## 六、技术依赖链

```
SharpFort.CodeGen.Domain
  ├── Scriban 7.2.3               ← 模板引擎 (MIT License)
  ├── Volo.Abp.Ddd.Domain 10.4.1  ← ABP DDD框架
  ├── SharpFort.SqlSugarCore.Abstractions  ← SqlSugar仓储抽象
  └── SharpFort.CodeGen.Domain.Shared     ← 共享类型

目标框架: net10.0.0 (需要 .NET 10 运行时)
```

---

## 七、当前缺陷与待改进点

| 严重程度 | 问题 | 影响 |
|----------|------|------|
| 🔴 高 | DataBaseManger 是空壳 | DB→Web 方向缺少独立管理逻辑 |
| 🔴 高 | Code→Web 是全量覆盖(TRUNCATE) | 丢失手动配置的字段属性 |
| 🔴 高 | 无 Vue 前端代码生成 | 仅生成后端 C#，缺少前端配套 |
| 🟡 中 | 英语复数规则极简 | 使用简单规则而非 Humanizer 库 |
| 🟡 中 | 种子数据硬编码 | 模板内容写在 C# 字符串中 |
| 🟡 中 | 无模板预览功能 | 生成前无法预览结果 |
| 🟡 中 | DTO 不完整 | Field 缺少 Create/Update 专用 DTO |
| 🟡 中 | NameSpaceTemplateHandler 简单清空 | 未实现实际命名空间解析 |
| 🟢 低 | 仅支持 Windows 打开目录 | PostDir 依赖 explorer.exe |
| 🟢 低 | GetListInput 仅 Filter 字段 | 无法按具体字段过滤 |

---

## 八、改写/增加功能的建议路线图

### Phase 1: 补全现有缺陷 (优先级最高)

1. **实现 DataBaseManger** — 完成 DB→Web 逻辑从 CodeGenService 中抽取
2. **Code→Web 增量更新** — 替代 TRUNCATE + INSERT 策略
3. **增加生成预览 API** — 返回渲染结果但不写入文件

### Phase 2: 前端代码生成 (核心扩展)

4. **新增 Vue 模板** — 参考现有 .scriban 模式，增加 Vue SFC 模板
5. **新增 FrontendTemplateHandler** — 处理前端特有逻辑
6. **种子数据扩展** — 增加 Vue 模板的种子数据

### Phase 3: 体验优化

7. **模板版本管理** — 支持回滚和对比
8. **字段分组** — 按"基本信息/审计信息/业务信息"分组
9. **多种数据库扩展** — Oracle, SQLite 支持
10. **外部化配置** — 种子数据改为 JSON 文件
