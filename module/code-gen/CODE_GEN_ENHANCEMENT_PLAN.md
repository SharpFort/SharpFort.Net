# SharpFort.Net Code-Gen 模块完美化增强方案与实施计划

基于对项目中 `code-gen` 模块的底层代码分析，以及对 `CODE_GEN_AI_SKILL_ANALYSIS.md` 观点的独立评估，该模块的设计思想（即**通过结构化元数据保障分层架构一致性**，与 **AI 代理处理边界逻辑的创造性**相辅相成）是非常前瞻且具有极高实用价值的。

为了让此模块力争完美、功能强大且完全具备工业级实用性，本文在原有分析的基础上，进行**深度批驳、设计重构与功能补充**，提出以下完整的增强方案。

---

## 1. 独立批驳与核心设计重构

### a) 彻底废除绝对路径，引入解决方案自适应定位 (Solution Root Auto-detection)
* **设计意图**：原有 `TemplateDataSeed.cs` 中硬编码的路径为 `D:\code\Entities\...`，这在团队协作或 CI/CD 环境中是不可用的。
* **重构方案**：
  * 引入 `SolutionDirectoryDetector` 机制。在运行时，从程序运行目录（`AppDomain.CurrentDomain.BaseDirectory`）向上递归查找 `.sln` 文件，锁定当前解决方案的绝对根路径 `SolutionRoot`。
  * `gen_template` 中的 `BuildPath` 全面使用**方案相对路径**（例如：`module/{{Module}}/SharpFort.{{Module}}.Domain/Entities/{{Model}}Entity.cs`），通过引擎动态拼接 `SolutionRoot` 完成写入。

### b) 解决命名空间与模块名称的硬编码问题
* **设计意图**：模板中硬编码了命名空间（如 `SharpFort.Rbac...`）。当在其他业务模块中使用时，会导致代码编译错误，或需要繁琐的手动全局替换。
* **重构方案**：
  * 在代码生成上下文（Context）中，不仅包含表名，还应包含**目标模块名称（Module）**与**解决方案命名空间（RootNamespace，如 `Sf.Abp` 或 `SharpFort`）**。
  * 自动通过文件路径推断命名空间。例如，若生成路径为 `module/tenant-management/.../Entities/Tenant.cs`，则命名空间自动推断为 `SharpFort.TenantManagement.Domain.Entities`。

### c) 增量生成设计： partial class 与 AST/正则表达式受保护区域双轨制
* **设计意图**：在非 C# 文件（如前端 `Index.vue`、`api.js` 等）和部分 C# 特殊类（如 Dto、Mapping 配置文件）中，`partial` 是无法使用的。
* **重构方案**：
  * **C# 后端服务与实体**：推行 `partial class`。将生成代码写入 `[Model]Service.Generated.cs`（每次均覆盖），而手动扩展的业务逻辑写在 `[Model]Service.cs`（仅首次生成，之后永不覆盖）。
  * **非 C# 与配置文件（Vue/JS/TS/JSON）**：引入 **受保护区域（Protected Region）标记解析器**。在模板中定义：
    ```javascript
    // <sf-custom-code-start>
    // 在此处编写的手动代码，在重新生成时将予以保留
    // </sf-custom-code-end>
    ```
    *(此设计已确认符合项目的开发规范与审美习惯)*
    在写入目标文件前，若检测到文件已存在，解析器将使用高效的正则表达式提取已有文件中的受保护内容，合并写入到新生成的代码中。

---

## 2. 功能补充与完美化蓝图

为了让该模块实现"工业级 scaffolding"体验，我们需要补充三个原本缺失的核心维度：

### 维度 A：全面增强的双向工作流 (Db-First ⇄ Code-First ⇄ Web-First)

目前仅实现了 `Code-First ➔ Web 配置 (BuildCodeToWebAsync)`，缺乏真正的数据库逆向和双向流转：

```
                    ┌─────────────────────────┐
                    │     Physical Database   │
                    └───────────┬─────────────┘
                                │ ▲
     1. DB-First (逆向读取元数据) │ │ 2. Web-First to DB (在线设计物理表)
     PostDbToWebAsync()         │ │ PostWebBuildDbAsync()
                                ▼ │
                    ┌─────────────────────────┐
                    │  gen_table / gen_field  │
                    │   (Web Scaffolder UI)   │
                    └───────────┬─────────────┘
                                │ ▲
        3. Scaffolding (生成代码)│ │ 4. Code-First (扫描实体类同步)
        BuildWebToCodeAsync()   │ │ PostCodeBuildWebAsync()
                                ▼ │
                    ┌─────────────────────────┐
                    │      C# Source Code     │
                    └─────────────────────────┘
```

1. **DB-First 逆向同步（新增接口：`PostDbToWebAsync`）**：
   * 调用 SqlSugar 的 `Db.DbMaintenance.GetTableInfoList()` 和 `Db.DbMaintenance.GetColumnInfosByTableName()`，将现有的物理表结构逆向写入 `gen_table` 和 `gen_field` 中，随后进入 Web-First 生成流程。
2. **Web-First 写入数据库（实现接口：`PostWebBuildDbAsync`）**：
   * 让开发者能够在 Web 界面上增加新字段，点击"同步至数据库"，即可直接在开发数据库中执行 `ALTER TABLE` / `CREATE TABLE`。
   * 利用 SqlSugar 强大的 Code-First 特性，在内存中动态构建实体元数据，并调用 `Db.CodeFirst.InitTables` 实现表结构的动态创建与更新。

### 维度 B：模板系统的混合存储 (Hybrid Engine) 与 Git 版本化控制
* **设计意图**：模板保存在数据库 `gen_template` 中，导致开发者无法享受 IDE 的语法高亮，也不利于 Git 版本管理与团队同步。
* **重构方案**：
  * **混合模板位置**：**放置在 code-gen 模块的根目录中**（具体为 `module/code-gen/Templates/`），直接作为模块的一部分提交到 Git 仓库，便于团队协作与版本化管理。
  * **三层模板查找机制**：
    1. **本地模块级自定义模板（优先级 1）**：查找 `module/code-gen/Templates/[TemplateName].scriban` 文件。如果存在，优先使用。
    2. **数据库动态配置（优先级 2）**：读取 `gen_template` 表中的内容（支持生产环境下动态热调整）。
    3. **内嵌默认模板（优先级 3）**：在 `SharpFort.CodeGen.Domain` 程序集中，作为 `EmbeddedResource` 内嵌一套标准的、支持 ABP + SqlSugar 最佳实践的零配置开箱即用模板。

### 维度 C：引入高吞吐、零编译开箱的 Scriban 模板引擎
* 替换当前的责任链替换机制，直接将 `ITemplateHandler` 改造为提供模板上下文（ModelContext）的属性绑定器，并由 `Scriban` 引擎统一渲染：
  ```csharp
  // 注入到 Scriban 的上下文模型
  var context = new {
      Table = table,
      Fields = table.Fields.OrderBy(x => x.OrderNum),
      Module = moduleName,
      RootNamespace = rootNamespace,
      Model = table.Name,
      ModelCamel = table.Name.ToCamelCase()
  };
  ```

---

## 3. 拟修改与新增文件清单

为达成上述完美化方案，我们规划对 `code-gen` 模块进行如下重构和补充：

### [CodeGen.Domain]

#### [MODIFY] [Table.cs](file:///e:/Projects/SharpFort.Net/module/code-gen/SharpFort.CodeGen.Domain/Entities/Table.cs)
* 丰富 `ExtraProperties` 字典中的强类型映射属性，如 `ModuleName`（模块名）、`RootNamespace`（根命名空间）、`IsOverwrite`（是否默认覆盖非受保护文件）。

#### [MODIFY] [Field.cs](file:///e:/Projects/SharpFort.Net/module/code-gen/SharpFort.CodeGen.Domain/Entities/Field.cs)
* 添加控制字段在不同层展现的精细化特性开关：`IsQueryField`（是否是查询条件字段）、`IsListDisplay`（是否在表格列表展示）、`IsFormItem`（是否是表单输入项）、`HtmlType`（前端输入组件类型，如 Input, Select, DatePicker 等）。

#### [NEW] [SolutionDirectoryDetector.cs](file:///e:/Projects/SharpFort.Net/module/code-gen/SharpFort.CodeGen.Domain/Managers/SolutionDirectoryDetector.cs)
* 独立工具类，通过向上回溯自动锁定 `.sln` 文件所在的解决方案绝对物理路径。

#### [NEW] [IncrementalCodeMerger.cs](file:///e:/Projects/SharpFort.Net/module/code-gen/SharpFort.CodeGen.Domain/Managers/IncrementalCodeMerger.cs)
* 实现基于受保护区域正则匹配的非 C# 代码流合并器，支持保留自定义前端组件或业务逻辑。

#### [MODIFY] [CodeFileManager.cs](file:///e:/Projects/SharpFort.Net/module/code-gen/SharpFort.CodeGen.Domain/Managers/CodeFileManager.cs)
* 引入 **Scriban** 引擎，合并 `SolutionRoot` 路径自适应，并在写入时使用 `IncrementalCodeMerger` 进行文件级安全写入合并。

#### [MODIFY] [WebTemplateManager.cs](file:///e:/Projects/SharpFort.Net/module/code-gen/SharpFort.CodeGen.Domain/Managers/WebTemplateManager.cs)
* 增强扫描逻辑，除了类名、属性之外，增加属性上的 `[Comment]`、`[Required]` 特性提取，精准生成 `gen_field` 配置。

### [CodeGen.Application]

#### [MODIFY] [CodeGenService.cs](file:///e:/Projects/SharpFort.Net/module/code-gen/SharpFort.CodeGen.Application/Services/CodeGenService.cs)
* 实现 `PostWebBuildDbAsync`：基于表和字段定义元数据，调用 SqlSugar 动态 CodeFirst 更新物理表结构。
* 新增接口 `PostDbToWebAsync`（DB-First）：根据指定的物理数据库表，逆向拉取字段元数据，覆盖或初始化写入 `gen_table`/`gen_field` 中。
* 实现 `PostCodeBuildDbAsync`：扫描所有的 Entity，并自动调用物理数据库的 Schema 同步。

### [CodeGen.SqlSugarCore]

#### [MODIFY] [TemplateDataSeed.cs](file:///e:/Projects/SharpFort.Net/module/code-gen/SharpFort.CodeGen.SqlSugarCore/TemplateDataSeed.cs)
* 解锁并全面升级该数据种子！
* 升级为 Scriban 格式的模板，将硬编码路径改造为 workspace 相对路径，命名空间全部适配 `{{RootNamespace}}.{{Module}}`。

---

## 4. 自动化与手动验证方案

### 1. 单元测试自动验证
* **模板转换测试**：编写集成测试用例，提供 Table 元数据，通过 `Scriban` 引擎渲染，校验生成出来的类文件语法正确性。
* **增量安全写入合并测试**：
  * 构建一份带有 `// <sf-custom-code-start> public void MyMethod(){} // <sf-custom-code-end>` 的伪文件。
  * 用新模板覆盖写入它，断言在重新写入后 `MyMethod` 仍然完好无损地存在于最终文件中。
* **解决方案根目录定位测试**：验证在单元测试的 `bin/Debug` 路径下，仍能精准回溯找到正确的项目根目录 `.sln` 文件。

### 2. 手动集成验证
* **第一步**：启用 `TemplateDataSeed` 并运行项目迁移，初始化数据库。
* **第二步**：创建新实体（如 `SfBook.cs`），加上 `[SugarTable("gen_book")]`。
* **第三步**：调用 `/api/app/code-gen/code-build-web` 同步至元数据。
* **第四步**：进入 Web Scaffolder UI 界面（或调用 `/api/app/code-gen/web-build-code`），传入生成的表 ID 列表。
* **第五步**：检查项目的 `src/` 与 `module/` 中是否在正确的相对路径下自动创建了 Dto、AppService、Entity 及其 Controller。
* **第六步**：再次修改 `gen_book` 实体，增加字段 `Price`，重新调用同步与生成，确保旧代码（如在 Dto 或 Service 中手工扩展的部分）不会丢失。
