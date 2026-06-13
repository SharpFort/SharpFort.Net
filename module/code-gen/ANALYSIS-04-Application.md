# 📁 SharpFort.CodeGen.Application/ 文件夹分析

> **分析日期**: 2026-06-12  
> **路径**: `module/code-gen/SharpFort.CodeGen.Application/`  
> **文件数量**: 6 个文件

---

## 一、功能概述

ABP 框架中的 **Application 层**，实现 Application.Contracts 中定义的服务接口。负责编排领域服务（Domain Managers）完成业务用例。

---

## 二、文件逐一分析

### 2.1 CodeGenService.cs — 代码生成核心服务 (469行，最复杂的文件)

**继承**: `ApplicationService, ICodeGenService`

**注入依赖**:
```csharp
- ISqlSugarRepository<Table, Guid> _tableRepository    // 表仓储
- ISqlSugarRepository<Field, Guid> _fieldRepository    // 字段仓储
- CodeFileManager         _codeFileManager              // 代码文件生成器
- WebTemplateManager      _webTemplateManager           // Web模板管理器
- IModuleContainer        _moduleContainer              // 模块容器(扫描程序集)
- IGuidGenerator          _guidGenerator                // GUID生成器
- ICurrentUser            _currentUser                  // 当前用户
- ILogger<CodeGenService> _logger                       // 日志记录器
```

#### 五大核心方法

##### ① PostWebBuildCodeAsync — Web → Code

```csharp
public async Task PostWebBuildCodeAsync(List<Guid> ids)
```

**流程**:
1. 根据 ID 列表查询 Table 实体（Includes Fields）
2. 对每个 Table 调用 `_codeFileManager.BuildWebToCodeAsync(table)`
3. 生成所有对应的 C# 代码文件

**使用场景**: 用户在 Web UI 上配置好表结构后，一键生成所有 C# 代码

---

##### ② PostWebBuildDbAsync — Web → DB (DDL 生成)

```csharp
[Authorize(Roles = "admin")]     ← 仅管理员可执行
[UnitOfWork]
public async Task<string> PostWebBuildDbAsync(List<Guid> ids, bool dryRun = false)
```

**安全控制**:
- 角色限制: 仅 `admin` 角色
- DROP 语句拦截: 任何包含 "DROP" 的 SQL 将被拒绝
- dryRun 模式: 仅返回 SQL 预览，不实际执行

**流程**:
```
对每个 Table:
  ├─ 检查物理表是否存在
  │  ├─ 不存在 → 生成 CREATE TABLE 语句
  │  └─ 存在   → 生成 ALTER TABLE 语句
  │              ├─ 新增列: ADD COLUMN
  │              └─ 变更列: ALTER COLUMN (类型/可空性变更)
  ├─ 审计日志: 记录用户身份 + SQL 内容
  ├─ dryRun ? 返回SQL : 事务执行
  └─ 返回执行结果
```

**DDL 生成辅助方法**:
- `GetColumnSqlDefinition()` — 生成单列 DDL 定义
- `GetAlterColumnSql()` — 生成 ALTER COLUMN 语句（支持 PostgreSQL/MySQL/其他）
- `MapFieldTypeToSqlType()` — FieldType → 数据库类型映射

FieldType → SQL 类型映射:

| FieldType | PostgreSQL | MySQL | SQL Server |
|-----------|------------|-------|------------|
| String | VARCHAR(N) / TEXT | VARCHAR(N) / LONGTEXT | VARCHAR(N) / NVARCHAR(MAX) |
| Int | INT | INT | INT |
| Long | BIGINT | BIGINT | BIGINT |
| Bool | BOOLEAN | TINYINT(1) | BIT |
| Decimal | DECIMAL(18,2) | DECIMAL(18,2) | DECIMAL(18,2) |
| DateTime | TIMESTAMP | DATETIME | DATETIME |
| Guid | UUID | VARCHAR(36) | UNIQUEIDENTIFIER |

---

##### ③ PostCodeBuildWebAsync — Code → Web

```csharp
[UnitOfWork]
public async Task PostCodeBuildWebAsync()
```

**流程**:
1. 调用 `_webTemplateManager.BuildCodeToWebAsync()` 扫描所有 C# Entity
2. **覆盖式更新**: 先 TRUNCATE `gen_table` 和 `gen_field` 表
3. 对每个生成的 Table 设置默认 UI 标记
4. 批量插入（含导航属性 Fields）

**⚠️ 风险**: 全覆盖式更新会丢失手动在 Web 端做的字段配置修改

---

##### ④ PostCodeBuildDbAsync — Code → DB (CodeFirst)

```csharp
public async Task PostCodeBuildDbAsync()
```

**流程**:
1. 扫描所有模块中的 Entity Type（过滤条件同 WebTemplateManager）
2. 调用 `_tableRepository._Db.CodeFirst.InitTables()` 同步到物理数据库

**依赖**: SqlSugar CodeFirst 机制

---

##### ⑤ PostDbToWebAsync — DB → Web (逆向工程)

```csharp
[UnitOfWork]
public async Task PostDbToWebAsync(string tableName, string? moduleName, string? rootNamespace)
```

**流程**:
```
① 验证物理表存在
② 获取物理表描述
③ 获取物理列信息 (GetColumnInfosByTableName)
④ 清理旧元数据（同名表）
⑤ 创建新 Table 实体:
    - Name: 转换为 PascalCase (去除 sys_/gen_/sf_ 前缀)
    - ModuleName: 使用参数或默认 "Rbac"
    - RootNamespace: 使用参数或默认 "Sf.Abp"
⑥ 对每个物理列创建 Field 实体:
    - 类型映射: MapDbTypeToFieldType() — SQL类型→FieldType枚举
    - 公共字段识别: IsCommonField() — ID/CreationTime/CreatorId等
    - UI标记智能默认:
      · 主键列 → 不查询/不表单
      · 公共列 → 不表单
      · 自增列 → 不表单
      · 其他 → 全标记
⑦ 插入数据库 (含导航属性)
```

**辅助方法**:
| 方法 | 功能 |
|------|------|
| `ToPascalCase(input)` | 蛇形/短横线 → PascalCase，去除 sys_/gen_/sf_ 前缀 |
| `MapDbTypeToFieldType(sqlType)` | SQL 数据类型 → FieldType 枚举 |
| `IsCommonField(columnName)` | 判断是否为 ABP 公共字段 |
| `GetDbTypeName(type, dbType)` | FieldType → 数据库类型简名 |
| `MapFieldTypeToSqlType(type, len, dbType)` | FieldType → 完整 SQL 类型 |

---

##### ⑥ PostDir — 打开本地目录（辅助功能）

```csharp
[HttpPost("code-gen/dir/{**path}")]
public Task PostDir([FromRoute] string path)
```

**功能**: 在 Windows 开发环境下，通过 `explorer.exe` 打开指定目录路径。
**限制**: 仅支持 Windows；路径中过滤 `@` 字符（防止路径遍历攻击）。

---

### 2.2 FieldService.cs — 字段管理服务

```csharp
public class FieldService(...) : SfCrudAppService<Field, FieldDto, Guid, FieldGetListInput>(...), IFieldService
```

**继承**: `SfCrudAppService<Field, FieldDto, Guid, FieldGetListInput>` — 即获得完整的 CRUD 能力

**重写**: `GetListAsync` — 增加条件过滤（按 TableId 和 Name 过滤）

**扩展方法**: `GetFieldType()` — 返回 FieldType 枚举的所有值 → 前端下拉框用

---

### 2.3 TableService.cs — 表管理服务

```csharp
public class TableService(...) : SfCrudAppService<Table, TableDto, Guid, TableGetListInput>(...), ITableService
```

**说明**: 纯继承，无自定义逻辑。完整的 CRUD 由基类 `SfCrudAppService` 提供。

---

### 2.4 TemplateService.cs — 模板管理服务

```csharp
public class TemplateService(...) : SfCrudAppService<Template, TemplateDto, Guid, TemplateGetListInput>(...), ITemplateService
```

**重写**: `GetListAsync` — 按 Name 精确过滤

---

### 2.5 SharpFortCodeGenApplicationModule.cs

```csharp
[DependsOn(
    typeof(SharpFortCodeGenApplicationContractsModule),
    typeof(SharpFortCodeGenDomainModule),
    typeof(SharpFortDddApplicationModule))]
public class SharpFortCodeGenApplicationModule : AbpModule { }
```

### 2.6 SharpFort.CodeGen.Application.csproj

```
TargetFramework: net10.0.0
依赖:
  - SharpFort.Ddd.Application (框架层)
  - SharpFort.CodeGen.Application.Contracts
  - SharpFort.CodeGen.Domain
```

---

## 三、数据库类型适配矩阵

CodeGenService 中对 PostgreSQL、MySQL、SQL Server 三种数据库实现了完整的 DDL 适配：

| 操作 | PostgreSQL | MySQL | 默认(SQL Server) |
|------|-----------|-------|-----------------|
| CREATE TABLE | `"Table"` | \`Table\` | [Table] |
| ALTER COLUMN | `ALTER COLUMN "col" TYPE type, ALTER COLUMN "col" SET/DROP NOT NULL` | `MODIFY COLUMN` | `ALTER COLUMN` |
| String 无长度 | TEXT | LONGTEXT | NVARCHAR(MAX) |
| Bool | BOOLEAN | TINYINT(1) | BIT |
| Guid | UUID | VARCHAR(36) | UNIQUEIDENTIFIER |

---

## 四、配置项

| 配置项 | 位置 | 说明 |
|--------|------|------|
| 角色授权 | `[Authorize(Roles = "admin")]` | Web→DB 需要 admin 角色 |
| dryRun | 方法参数 | 是否仅预览 DDL SQL |
| moduleName | PostDbToWebAsync 参数 | 默认 "Rbac" |
| rootNamespace | PostDbToWebAsync 参数 | 默认 "Sf.Abp" |

---

## 五、扩展/改写建议

| 优先级 | 建议 | 说明 |
|--------|------|------|
| 🔴 高 | Web→Code 生成前预览 | 先返回预览结果供用户确认 |
| 🔴 高 | Code→Web 增量更新 | 当前是全量 TRUNCATE 后重新插入 |
| 🟡 中 | 批量表处理进度 | 大数量表时提供进度反馈 |
| 🟡 中 | DDL 变更对比 | ALTER 时展示变更前后对比 |
| 🟢 低 | 多数据库支持扩展 | 增加 Oracle、SQLite 等 |
| 🟢 低 | 事务回滚提示 | DDL 执行失败时的事务回滚信息 |
