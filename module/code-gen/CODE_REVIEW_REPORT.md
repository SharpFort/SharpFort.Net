# Code-Gen 模块代码审查报告

审查范围：`module/code-gen/` 全部代码文件 + `test/Sf.Abp.Test/CodeGen_Tests.cs`
审查基准：`CODE_GEN_ENHANCEMENT_PLAN.md` + `CODE_GEN_REVIEW_SUPPLEMENT.md`

---

## 一、功能完成度对照

| 设计文档要求 | 完成情况 | 备注 |
|--------------|:---------|------|
| SolutionDirectoryDetector 三级 Fallback | ✅ | `.sln` → `.csproj` 密度 → 环境变量/配置 |
| Scriban 模板引擎迁移 | ✅ | Scriban 渲染 + 自定义辅助函数已集成 |
| `ITemplateHandler` 保留 + `ITemplateContextEnricher` 新增 | ✅ | Legacy 管道和新 Enricher 共存 |
| `TemplateEngine` 列区分 Legacy/Scriban | ✅ | Template.cs 和 Table.cs 均有此列 |
| IncrementalCodeMerger（含边界处理） | ✅ | 标记校验 + null 返回 + WarningHeader |
| 新增实体列（非 ExtraProperties） | ✅ | Table 和 Field 均为实际列 |
| `PostWebBuildDbAsync`（DDL 同步） | ✅ | CREATE/ALTER + dryRun 参数 |
| `PostDbToWebAsync`（DB-First 逆向） | ✅ | SqlSugar DbMaintenance API |
| `PostCodeBuildDbAsync`（Code-First 物理表同步） | ✅ | 反射扫描 + CodeFirst.InitTables |
| TemplateDataSeed Scriban 格式 | ✅ | 8 个模板种子数据已解锁 |
| TemplateContext 完整契约 | ✅ | TableInfo / FieldInfo 定义完整 |
| **Templates 目录 + .scriban 文件** | ❌ | **完全缺失，混合存储优先级 1 失效** |
| **FieldDto / TableDto 新属性同步** | ❌ | **DTO 未更新，API 无法管理新字段** |

---

## 二、发现的问题

### 🔴 严重问题

#### 问题 1：Templates 目录和 .scriban 文件缺失 — 混合存储优先级 1 失效

`CodeFileManager.cs:60-65` 有查找本地模板的逻辑：

```csharp
string localTemplateFolder = Path.Combine(solutionRoot, "module", "code-gen", "Templates");
string localTemplatePath = Path.Combine(localTemplateFolder, $"{dbTemplate.Name}.scriban");
if (!File.Exists(localTemplatePath))
{
    localTemplatePath = Path.Combine(localTemplateFolder, dbTemplate.Name!);
}
```

但 `module/code-gen/Templates/` 目录及其 `.scriban` 文件不存在。三层查找机制的最高优先级路径永远走不到，模板只能从数据库加载，无法享受 Git 版本化管理。

**修复要求**：创建 `module/code-gen/Templates/` 目录，将 `TemplateDataSeed` 中的 8 个模板内容抽取为独立 `.scriban` 文件：
- `Entity.scriban`
- `GetListInput.scriban`
- `GetListOutputDto.scriban`
- `GetOutputDto.scriban`
- `CreateInput.scriban`
- `UpdateInput.scriban`
- `IServices.scriban`
- `Service.scriban`

---

#### 问题 2：FieldDto / TableDto 缺少新增属性 — API 无法管理新字段

Field 实体新增了 `IsQueryField`、`IsListDisplay`、`IsFormItem`、`HtmlType` 列，但 `FieldDto.cs` 未同步。

Table 实体新增了 `ModuleName`、`RootNamespace`、`IsOverwrite`、`TemplateEngine`，但 `TableDto.cs` 未同步。

通过 ABP Auto API 暴露的 CRUD 接口无法读写这些新属性。

**修复要求**：在 FieldDto 和 TableDto 中补充对应属性。

---

### 🟠 重要问题

#### 问题 3：DDL 审计日志未记录真实用户身份

`CodeGenService.cs:137`：

```csharp
_logger.LogInformation($"[CodeGen DDL Audit] 审计日志：用户 {_logger.GetType().Name} 触发表结构同步。");
```

`_logger.GetType().Name` 始终输出 `Logger<CodeGenService>`，而非触发操作的实际用户。

**修复要求**：注入 `ICurrentUser` 并使用 `CurrentUser.UserName` 替代 `_logger.GetType().Name`。

---

#### 问题 4：PostWebBuildDbAsync 多条 DDL 拼接为单命令执行

`CodeGenService.cs:149`：

```csharp
await _tableRepository._Db.Ado.ExecuteCommandAsync(totalSql);
```

多个 DDL 语句拼成单一字符串。PostgreSQL 的 Npgsql 驱动默认不支持一条命令执行多条语句。

**修复要求**：按 `;` 拆分 SQL 语句逐条执行。

---

#### 问题 5：PostCodeBuildDbAsync 在 ASP.NET 中使用 Task.Run

`CodeGenService.cs:199`：

```csharp
await Task.Run(() =>
{
    _tableRepository._Db.CodeFirst.InitTables(entityTypes.ToArray());
});
```

`Task.Run` 在 ASP.NET 线程池中额外占用线程执行同步操作，可能引发线程饥饿。

**修复要求**：移除 `Task.Run` 包装，直接调用同步方法。

---

#### 问题 6：缺少显式 DDL DROP 拦截检查

代码只生成 `CREATE TABLE`、`ADD COLUMN`、`ALTER COLUMN`，逻辑上不会生成 DROP 语句，但缺乏显式安全网。

**修复要求**：在 `totalSql` 执行前添加显式校验：

```csharp
if (totalSql.Contains("DROP", StringComparison.OrdinalIgnoreCase))
    throw new UserFriendlyException("禁止的DDL操作：代码生成不允许执行DROP语句");
```

---

### 🟡 一般问题

#### 问题 7：WebTemplateManager 未填充 Field 新属性

`WebTemplateManager.PropertyMapperToFiled()` 在 Code-First 扫描时未初始化 `IsQueryField`、`IsListDisplay`、`IsFormItem`、`HtmlType`。当前依赖 `CodeGenService.PostCodeBuildWebAsync:169-176` 在写入前统一设置默认值，但逻辑是隐式的。

**修复要求**：在 `PropertyMapperToFiled` 中显式设置默认值，使逻辑内聚。

---

#### 问题 8：CodeFileManager 绝对路径 Fallback 硬编码了模块名

`CodeFileManager.cs:136-138`：

```csharp
relativeBuildPath = $"module/{tableEntity.ModuleName ?? "Rbac"}/SharpFort.{tableEntity.ModuleName ?? "Rbac"}.Domain/Entities/{fileName}";
```

Fallback 路径始终指向 `Domain/Entities/`，这对 DTO、Service 等模板会产生错误路径。

**修复要求**：根据模板名称推断正确的子目录层级，或至少记录警告日志。

---

#### 问题 9：测试覆盖可补充

审查补充文档要求的优先测试项中缺少：

- `SolutionDirectoryDetector` 在 CI 环境（无 `.sln`）下 Fallback 到 `.csproj` 密度检测的行为
- Scriban 模板对各种 C# 类型映射（`int`/`int?`/`DateTime`/`Guid` 等）的完整渲染测试

**修复要求**：补充上述测试用例。

---

## 三、问题汇总

| # | 严重度 | 问题 | 位置 |
|---|--------|------|------|
| 1 | 🔴 严重 | Templates 目录和 .scriban 文件缺失 | `module/code-gen/Templates/` 不存在 |
| 2 | 🔴 严重 | FieldDto / TableDto 未同步新属性 | `FieldDto.cs` / `TableDto.cs` |
| 3 | 🟠 重要 | DDL 审计日志未记录真实用户 | `CodeGenService.cs:137` |
| 4 | 🟠 重要 | 多条 DDL 拼接单命令执行 | `CodeGenService.cs:149` |
| 5 | 🟠 重要 | ASP.NET 中使用 Task.Run | `CodeGenService.cs:199` |
| 6 | 🟠 重要 | 缺少显式 DDL DROP 拦截 | `CodeGenService.cs:144` |
| 7 | 🟡 一般 | WebTemplateManager 未填充新属性默认值 | `WebTemplateManager.cs:63` |
| 8 | 🟡 一般 | 绝对路径 Fallback 硬编码模块名 | `CodeFileManager.cs:138` |
| 9 | 🟡 一般 | 测试覆盖可补充 | `CodeGen_Tests.cs` |

---

## 四、总体评价

模块核心功能实现完整度高：Scriban 引擎集成、双向工作流、增量合并、三级路径检测均正确实现。TemplateDataSeed 从注释状态成功解锁为生产就绪的 Scriban 格式。

**建议修复问题 1 和 2 后再合并代码，其余问题可在后续迭代中处理。**
