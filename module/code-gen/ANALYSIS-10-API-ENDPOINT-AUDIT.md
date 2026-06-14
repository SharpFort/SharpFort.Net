# Code-Gen 模块 API 端点审计分析

> **日期**: 2026-06-13  
> **前置依赖**: ANALYSIS-09 (二次精简分析，已删除 PostDbToWebAsync)  
> **分析范围**: Table / Field / Template 三大服务的全部 API 端点  
> **核心问题**: Table 和 Field 数据由反射 (Code→Web) 自动填充，哪些 CRUD 端点已无存在必要？

---

## 一、数据生命周期与端点评估原则

### 1.1 三大实体的数据来源

| 实体 | 数据来源 | 填充方式 | 用户可编辑范围 |
|------|----------|----------|---------------|
| **Table** | C# Entity 类反射 | `PostRefreshAsync` → `WebTemplateManager.BuildCodeToWebAsync()` | ModuleName, RootNamespace, IsOverwrite, Description 等**生成配置**字段 |
| **Field** | C# Entity 属性反射 | 随 Table 同步填充，保留用户 UI 配置 | IsQueryField, IsListDisplay, IsFormItem, HtmlType, OrderNum 等 **UI 配置**字段 |
| **Template** | DB 种子数据 + 用户手动维护 | 种子初始化 + 用户 CRUD | **全部内容**：Name, Content (Scriban), BuildPath, Description |

### 1.2 端点评估三原则

1. **反射可替代性**：如果端点功能可由反射同步完全替代，则该端点**不必要**
2. **数据一致性风险**：如果端点操作会导致与反射源不一致，则该端点**有害**
3. **业务场景需求**：如果端点服务于真实的用户操作场景，则该端点**必要**

---

## 二、Table 端点逐一分析

> 基类 `SfCrudAppService` 提供 8 个继承端点

### 2.1 端点总览

| # | HTTP | 路由 | 方法 | 判定 | 理由摘要 |
|---|------|------|------|:----:|----------|
| 1 | GET | `/api/app/table` | `GetListAsync` | **保留** | 浏览注册表，选择生成目标，核心入口 |
| 2 | POST | `/api/app/table` | `CreateAsync` | **移除** | Table 由反射填充，手动创建缺少 C# 实体支撑 |
| 3 | DELETE | `/api/app/table` | `DeleteAsync(ids)` | **移除** | 级联删除 Field，下次同步会重建，数据不一致 |
| 4 | PUT | `/api/app/table/{id}` | `UpdateAsync` | **保留** | 编辑生成配置 (ModuleName/RootNamespace/IsOverwrite) |
| 5 | GET | `/api/app/table/{id}` | `GetAsync` | **保留** | 查看实体详情，辅助配置 |
| 6 | GET | `/api/app/table/select-data-list` | `GetSelectDataListAsync` | **移除** | Table 是顶级实体，无下拉引用场景 |
| 7 | GET | `/api/app/table/export-excel` | `GetExportExcelAsync` | **移除** | 系统元数据，非用户业务数据 |
| 8 | POST | `/api/app/table/import-excel` | `PostImportExcelAsync` | **移除** | 基类直接 throw NotImplementedException |

### 2.2 详细分析

#### #1 GET `/api/app/table` — 分页查询实体注册表列表 ✅ 保留

**功能**：列出所有通过反射扫描注册的实体表，支持按名称模糊筛选。  
**场景**：用户在代码生成页面浏览实体列表 → 勾选目标实体 → 点击"生成代码"。  
**不可替代**：这是代码生成流程的**核心入口**，必须保留。

#### #2 POST `/api/app/table` — 手动创建实体 ❌ 移除

**功能**：允许用户手动创建一条 Table 记录。  
**移除理由**：
- Table 的标准填充路径是 `PostRefreshAsync` → 反射扫描所有带 `[SugarTable]` 的 C# 实体类
- 手动创建的 Table **没有对应的 C# 实体类**，导致：
  - 无 Field 数据（Field 由反射属性填充）
  - Web→Code 生成时找不到实体类，生成失败
  - 下次 `PostRefreshAsync` 不会同步此条目（因为 Name 匹配不到任何实体类）
- 如果用户需要注册新实体，正确流程是：**先写 C# Entity 类 → 再调 PostRefreshAsync**

#### #3 DELETE `/api/app/table` — 批量删除实体 ❌ 移除

**功能**：批量删除 Table 记录（级联删除关联 Field）。  
**移除理由**：
- Table 删除会**级联删除所有 Field** 记录（包括用户配置的 UI 设置）
- 如果被删实体类仍存在，下次 `PostRefreshAsync` 会**重新创建**该 Table，但**用户配置的 UI 设置丢失**
- 这构成一个**数据陷阱**：用户以为删除只是"取消注册"，实际上丢失了所有字段 UI 配置
- 如果需要"排除某实体不参与代码生成"，应添加 `IsEnabled` 字段或用 `[IgnoreCodeFirst]` 特性

#### #4 PUT `/api/app/table/{id}` — 更新实体配置 ✅ 保留

**功能**：编辑 Table 的生成配置属性。  
**场景**：用户需要设置/修改：
- `ModuleName` (如 "Rbac") — 控制生成路径
- `RootNamespace` (如 "Sf.Abp") — 控制 namespace 前缀
- `IsOverwrite` — 是否覆盖已有文件
- `Description` — 实体备注

这些数据**无法由反射自动推断**，必须用户手动配置，是 Table 表存在的核心价值。

#### #5 GET `/api/app/table/{id}` — 获取单个实体详情 ✅ 保留

**功能**：获取单个 Table 的完整信息。  
**场景**：查看实体详细配置（命名空间、物理表名、最后同步时间等），辅助配置决策。

#### #6 GET `/api/app/table/select-data-list` — 动态下拉框 ❌ 移除

**功能**：提供 Table 的下拉框数据源。  
**移除理由**：Table 是顶级管理实体，不存在"其他表单引用 Table 做下拉选择"的场景。Field 通过 TableId 关联，但那是子表查询，不需要 Table 的下拉接口。

#### #7 GET `/api/app/table/export-excel` — 导出 Excel ❌ 移除

**功能**：将 Table 列表导出为 Excel 文件。  
**移除理由**：Table 是系统元数据，不是用户业务数据。实体配置没有"批量导出-修改-导入"的使用场景。

#### #8 POST `/api/app/table/import-excel` — 导入 Excel ❌ 移除

**功能**：从 Excel 批量导入 Table 记录。  
**移除理由**：基类默认实现直接 `throw NotImplementedException`，从未被实现。Table 由反射填充，Excel 导入无意义。

### 2.3 Table 结论

| 判定 | 端点 | 数量 |
|------|------|:----:|
| **保留** | GetList, Get, Update | **3** |
| **移除** | Create, Delete, SelectDataList, ExportExcel, ImportExcel | **5** |

---

## 三、Field 端点逐一分析

> 基类提供 8 个继承端点 + 1 个自定义端点 (`GetFieldType`)

### 3.1 端点总览

| # | HTTP | 路由 | 方法 | 判定 | 理由摘要 |
|---|------|------|------|:----:|----------|
| 1 | GET | `/api/app/field` | `GetListAsync` | **保留** | 查看/管理实体的字段列表，核心功能 |
| 2 | POST | `/api/app/field` | `CreateAsync` | **移除** | Field 由反射填充，手动创建会在下次同步被覆盖 |
| 3 | DELETE | `/api/app/field` | `DeleteAsync(ids)` | **移除** | 同步时会全量删除重建，手动删除无持久意义 |
| 4 | PUT | `/api/app/field/{id}` | `UpdateAsync` | **保留** | 编辑 UI 配置 (IsQueryField/IsFormItem/HtmlType 等) |
| 5 | GET | `/api/app/field/{id}` | `GetAsync` | **保留** | 查看单个字段详情 |
| 6 | GET | `/api/app/field/select-data-list` | `GetSelectDataListAsync` | **移除** | Field 始终在 Table 上下文中引用，无独立下拉场景 |
| 7 | GET | `/api/app/field/export-excel` | `GetExportExcelAsync` | **移除** | 系统元数据，非业务数据 |
| 8 | POST | `/api/app/field/import-excel` | `PostImportExcelAsync` | **移除** | 基类直接 throw NotImplementedException |
| 9 | GET | `/api/app/field/type` | `GetFieldType` | **保留** | 返回 FieldType 枚举列表，前端 UI 必需 |

### 3.2 详细分析

#### #1 GET `/api/app/field` — 分页查询字段列表 ✅ 保留

**功能**：按 TableId 查询某个实体下的所有字段，支持按名称筛选。  
**场景**：用户在实体详情页面查看所有字段 → 修改 UI 配置（是否查询/是否显示/是否表单/控件类型）。  
**不可替代**：这是字段 UI 配置管理的**唯一入口**。

#### #2 POST `/api/app/field` — 手动创建字段 ❌ 移除

**功能**：手动添加一条 Field 记录。  
**移除理由**：
- Field 由 `PropertyMapperToFiled()` 反射 C# 属性自动填充
- `BuildCodeToWebAsync` 同步时**先全量删除旧字段再重新插入**（第 71-74 行）
- 手动创建的字段会在下次 `PostRefreshAsync` 时被**静默清除**
- 如果需要"额外的非实体字段"，应在 C# Entity 类中添加属性，然后同步

#### #3 DELETE `/api/app/field` — 批量删除字段 ❌ 移除

**功能**：批量删除 Field 记录。  
**移除理由**：
- 同步时采用**全量替换策略**：`Deleteable<Field>().Where(f => f.TableId == existing.Id)` → `Insertable(existing.Fields)`
- 手动删除的字段会在下次同步时被**重新创建**
- 如果用户想"排除某字段不参与代码生成"，应使用 `IsPublic = true`（Scriban 模板会跳过公共字段）或调整 `IsQueryField/IsListDisplay/IsFormItem` 配置

#### #4 PUT `/api/app/field/{id}` — 更新字段 UI 配置 ✅ 保留

**功能**：编辑字段的 UI 配置属性。  
**场景**：用户配置代码生成行为：
- `IsQueryField` — 是否生成查询条件
- `IsListDisplay` — 是否在列表 DTO 中生成
- `IsFormItem` — 是否在表单 DTO 中生成
- `HtmlType` — 前端控件类型 (Input/Select/DatePicker)
- `OrderNum` — 字段排序
- `Description` — 字段备注

这些配置在同步时**被保留**（`MergeFields` 方法会将已有 UI 配置复制到新字段），是 Field 表的核心价值。

#### #5 GET `/api/app/field/{id}` — 获取单个字段详情 ✅ 保留

**功能**：获取单个 Field 的完整信息。  
**场景**：查看字段详细属性（类型、长度、是否主键、UI 配置等）。

#### #6 GET `/api/app/field/select-data-list` — 动态下拉框 ❌ 移除

**功能**：提供 Field 的下拉框数据源。  
**移除理由**：Field 始终在 Table 上下文中通过 TableId 查询，不存在"独立引用 Field 做下拉选择"的场景。

#### #7 GET `/api/app/field/export-excel` — 导出 Excel ❌ 移除

**移除理由**：系统元数据，非用户业务数据，无导出需求。

#### #8 POST `/api/app/field/import-excel` — 导入 Excel ❌ 移除

**移除理由**：基类 `throw NotImplementedException`，从未实现。Field 由反射填充。

#### #9 GET `/api/app/field/type` — 字段类型枚举 ✅ 保留

**功能**：返回所有 FieldType 枚举值 (String/Int/Long/Bool/Decimal/DateTime/Guid/Float/Double)。  
**场景**：前端字段编辑表单中，FieldType 下拉框的数据源。代码生成必需。

### 3.3 Field 结论

| 判定 | 端点 | 数量 |
|------|------|:----:|
| **保留** | GetList, Get, Update, GetFieldType | **4** |
| **移除** | Create, Delete, SelectDataList, ExportExcel, ImportExcel | **5** |

---

## 四、Template 端点逐一分析

> 基类提供 8 个继承端点  
> **重要区别**：Template 是**用户主动维护**的实体，不由反射填充

### 4.1 端点总览

| # | HTTP | 路由 | 方法 | 判定 | 理由摘要 |
|---|------|------|------|:----:|----------|
| 1 | GET | `/api/app/template` | `GetListAsync` | **保留** | 浏览模板列表，核心功能 |
| 2 | POST | `/api/app/template` | `CreateAsync` | **保留** | 用户自定义新 Scriban 模板 |
| 3 | DELETE | `/api/app/template` | `DeleteAsync(ids)` | **保留** | 删除不需要的模板（需前端确认） |
| 4 | PUT | `/api/app/template/{id}` | `UpdateAsync` | **保留** | 编辑 Scriban 内容/生成路径，核心功能 |
| 5 | GET | `/api/app/template/{id}` | `GetAsync` | **保留** | 查看/预览模板内容 |
| 6 | GET | `/api/app/template/select-data-list` | `GetSelectDataListAsync` | **移除** | 无其他表单引用 Template 做下拉 |
| 7 | GET | `/api/app/template/export-excel` | `GetExportExcelAsync` | **移除** | Scriban 模板内容是代码，Excel 不适合 |
| 8 | POST | `/api/app/template/import-excel` | `PostImportExcelAsync` | **移除** | 基类直接 throw NotImplementedException |

### 4.2 详细分析

Template 与 Table/Field **本质不同**：

- **Table/Field** 是反射自动填充的系统元数据 → CRUD 大部分无意义
- **Template** 是用户主动维护的代码资产 → 完整 CRUD 合理

#### #1-5 全部保留 ✅

| 端点 | 用户场景 |
|------|----------|
| GetList | 浏览所有 Scriban 模板 (DTO模板×5 + IService + Service) |
| Create | 新增自定义模板（如新增 `xxxApi.js` 前端 API 模板） |
| Update | 编辑模板 Scriban 内容、调整 BuildPath 生成路径 |
| Get | 查看模板完整内容（Scriban 脚本 + 生成路径规则） |
| Delete | 删除不再使用的模板 |

#### #6 GET `/api/app/template/select-data-list` — 动态下拉框 ❌ 移除

**移除理由**：当前没有其他表单需要引用 Template 做下拉选择。代码生成时模板选择由系统内部逻辑控制，不走 API。

#### #7 GET `/api/app/template/export-excel` — 导出 Excel ❌ 移除

**移除理由**：Template.Content 是 Scriban 脚本代码，用 Excel 展示/编辑代码内容体验极差。如需备份，应使用文件覆写层的 `.scriban` 文件。

#### #8 POST `/api/app/template/import-excel` — 导入 Excel ❌ 移除

**移除理由**：基类 `throw NotImplementedException`，且 Scriban 代码不适合通过 Excel 导入。

### 4.3 Template 结论

| 判定 | 端点 | 数量 |
|------|------|:----:|
| **保留** | GetList, Create, Update, Get, Delete | **5** |
| **移除** | SelectDataList, ExportExcel, ImportExcel | **3** |

---

## 五、综合统计与实施方案

### 5.1 总览

| 服务 | 当前端点数 | 保留 | 移除 | 精简率 |
|------|:---------:|:----:|:----:|:------:|
| Table | 8 | 3 | 5 | 62.5% |
| Field | 9 | 4 | 5 | 55.6% |
| Template | 8 | 5 | 3 | 37.5% |
| **合计** | **25** | **12** | **13** | **52.0%** |

### 5.2 保留端点 — 需完善的 Swagger 描述

以下为每个保留端点提供准确的 Swagger XML summary，确保在 Swagger UI 中显示清晰的功能描述。

#### Table 保留端点 (3 个)

```csharp
/// <summary>
/// 分页查询实体注册表列表：列出所有通过反射扫描注册的 C# Entity 类，支持按实体名称模糊筛选
/// 场景：代码生成页面的实体列表数据源，用户在此选择目标实体后执行代码生成
/// </summary>
public override async Task<PagedResultDto<TableDto>> GetListAsync(...)

/// <summary>
/// 更新实体注册表配置：修改实体的代码生成参数（所属模块、命名空间、是否覆盖等）
/// 注意：实体类名(Name)和物理表名(PhysicalTableName)由反射同步维护，不建议手动修改
/// </summary>
public override async Task<TableDto> UpdateAsync(...)

/// <summary>
/// 获取实体注册表详情：查看单个实体的完整配置信息，包括物理表名、命名空间、同步时间等
/// </summary>
public override async Task<TableDto> GetAsync(...)
```

#### Field 保留端点 (4 个)

```csharp
/// <summary>
/// 分页查询字段列表：获取指定实体下的所有字段定义，支持按字段名称筛选
/// 场景：查看某实体的字段结构及 UI 配置（查询/列表/表单/控件类型）
/// </summary>
public override async Task<PagedResultDto<FieldDto>> GetListAsync(...)

/// <summary>
/// 更新字段 UI 配置：修改字段在代码生成时的行为参数
/// 可配置项：IsQueryField(查询条件)、IsListDisplay(列表显示)、IsFormItem(表单字段)、HtmlType(控件类型)、OrderNum(排序)
/// 注意：字段名/类型/长度等结构性属性由反射同步维护，修改后下次同步可能覆盖
/// </summary>
public override async Task<FieldDto> UpdateAsync(...)

/// <summary>
/// 获取字段详情：查看单个字段的完整信息，包括结构属性（类型/长度/是否主键）和 UI 配置
/// </summary>
public override async Task<FieldDto> GetAsync(...)

/// <summary>
/// 获取字段类型枚举列表：返回所有可用的 FieldType 枚举值
/// 枚举值：String/Int/Long/Bool/Decimal/DateTime/Guid/Float/Double
/// 用途：前端字段编辑表单中 FieldType 下拉框的数据源
/// </summary>
public object GetFieldType()
```

#### Template 保留端点 (5 个)

```csharp
/// <summary>
/// 分页查询模板列表：列出所有 Scriban 代码生成模板，支持按名称模糊筛选
/// 模板类型：DTO 模板（GetListInput/GetListOutput/CreateInput/UpdateInput/GetOutput）+ IService + Service
/// </summary>
public override async Task<PagedResultDto<TemplateDto>> GetListAsync(...)

/// <summary>
/// 创建新模板：添加自定义 Scriban 代码生成模板
/// 场景：新增前端 API 模板 (xxxApi.js)、额外的 DTO 模板等
/// </summary>
public override async Task<TemplateDto> CreateAsync(...)

/// <summary>
/// 更新模板：修改 Scriban 模板内容或生成路径 (BuildPath)
/// 架构：DB 中存储基线版本，本地 Templates/*.scriban 文件可覆写（双层模板系统）
/// </summary>
public override async Task<TemplateDto> UpdateAsync(...)

/// <summary>
/// 获取模板详情：查看单个 Scriban 模板的完整内容和生成路径规则
/// </summary>
public override async Task<TemplateDto> GetAsync(...)

/// <summary>
/// 批量删除模板：移除不再使用的 Scriban 模板
/// 警告：种子模板（默认 7 个）删除后需重新初始化种子数据
/// </summary>
public async Task DeleteAsync(IEnumerable<Guid> ids)
```

### 5.3 移除端点 — 实施方案

**方式：在 Service 子类中 override 并标记 `[RemoteService(isEnabled: false)]`**

这是 ABP 框架禁用远程端点的标准做法。项目中已有先例：
- `SfCrudAppService.DeleteAsync(TKey id)` — 单个删除已禁用
- `AccountService` 中 5 处禁用

```csharp
// 示例：TableService 中禁用不需要的端点
[RemoteService(isEnabled: false)]
public override Task<TableDto> CreateAsync(TableDto input) => throw new NotImplementedException();

[RemoteService(isEnabled: false)]
public override Task DeleteAsync(IEnumerable<Guid> ids) => throw new NotImplementedException();

[RemoteService(isEnabled: false)]
public override Task<PagedResultDto<TableDto>> GetSelectDataListAsync(string? keywords = null) 
    => throw new NotImplementedException();

[RemoteService(isEnabled: false)]
public override Task<IActionResult> GetExportExcelAsync(TableGetListInput input) 
    => throw new NotImplementedException();

[RemoteService(isEnabled: false)]
public override Task PostImportExcelAsync(List<TableDto> input) 
    => throw new NotImplementedException();
```

**注意事项**：
- `[RemoteService(isEnabled: false)]` 仅禁用 HTTP 端点暴露，C# 层面方法仍可用
- 方法体 `throw new NotImplementedException()` 是防御性编程，防止内部误调用
- 不需要修改接口 (ITableService/IFieldService/ITemplateService)，接口保持基类定义即可

### 5.4 Swagger UI 预期效果

修改后 Swagger UI 中：
- **Table** 分组下仅显示 3 个端点（GET列表、PUT更新、GET详情）
- **Field** 分组下仅显示 4 个端点（GET列表、PUT更新、GET详情、GET枚举）
- **Template** 分组下仅显示 5 个端点（GET列表、POST创建、PUT更新、GET详情、DELETE删除）
- 每个端点展开后可见详细的功能描述、使用场景和注意事项

---

## 六、关于 DELETE 端点的补充讨论

### 6.1 Table DELETE 是否应保留？

**反方观点**（保留）：
- 用户可能需要清理已经废弃的实体注册表条目
- 框架提供了功能，不应该主动限制

**正方观点**（移除，本分析推荐）：
- 级联删除风险高（Table 删除 → 所有 Field UI 配置丢失）
- 下次同步会重建 Table，但用户精心配置的 Field UI 设置**不可恢复**
- 如果需要"排除实体"，更安全的做法是在 C# Entity 类上标记 `[IgnoreCodeFirst]`

### 6.2 Field DELETE 是否应保留？

**不保留**：
- `BuildCodeToWebAsync` 中同步策略为**全量删除重建**（第 71 行）
- 任何手动删除操作在下次 `PostRefreshAsync` 后都会失效
- 这不是"权限控制"问题，而是"操作无持久效果"的设计现实

### 6.3 Template DELETE 是否需要保护？

**建议保留但前端加确认弹窗**：
- 种子模板（默认 7 个）是代码生成的基础，误删会导致生成功能失效
- 可在前端层面添加"确认删除"弹窗，提示"种子模板删除后需重新初始化"

---

## 七、用户反馈分析与需求完善

> 基于用户对初版端点审计的 5 点反馈，逐一分析合理性并给出完善方案  
> **约束前提**：不修改 `SfCrudAppService` / `ISfCrudAppService` 基类代码，仅在子类中 override

### 7.1 Table GetList 添加模块/项目筛选

**用户诉求**：在分页查询中添加按模块 (ModuleName)、项目 (ProjectName) 筛选的功能。

**判定：✅ 合理，推荐实施**

**当前状态**：`TableGetListInput` 仅有 `Name` 一个筛选字段：

```csharp
// 当前
public class TableGetListInput : PagedAndSortedResultRequestDto
{
    public string? Name { get; set; }  // 仅支持表名模糊筛选
}
```

**完善方案**：在 `TableGetListInput` 中增加两个筛选字段：

```csharp
public class TableGetListInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// 实体名称模糊筛选 (如: System)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 所属模块精确筛选 (如: Rbac)
    /// </summary>
    public string? ModuleName { get; set; }

    /// <summary>
    /// 所属项目精确筛选 (如: Rbac)
    /// </summary>
    public string? ProjectName { get; set; }
}
```

`TableService.GetListAsync` 对应增加 `WhereIF` 条件：

```csharp
.WhereIF(input.Name is not null, x => x.Name!.Contains(input.Name!))
.WhereIF(input.ModuleName is not null, x => x.ModuleName == input.ModuleName)
.WhereIF(input.ProjectName is not null, x => x.ProjectName == input.ProjectName)
```

> 注：ModuleName / ProjectName 用精确匹配（值来自下拉框），Name 保持模糊匹配（用户手输关键字）。

---

### 7.2 Table Get 关联 Field 实体

**用户诉求**：获取单个 Table 详情时，是否能同时返回其 Field 列表。

**判定：✅ 合理，推荐实施**

**当前状态**：
- `TableDto` 已有 `List<FieldDto>? Fields` 导航属性（第 62 行），**DTO 已就绪**
- 但基类 `GetAsync` 内部的 `GetEntityByIdAsync(id)` 不会自动 Include Fields
- 前端需要两次请求才能获取完整数据：`GET /table/{id}` + `GET /field?TableId={id}`

**完善方案**：在 `TableService` 中 override `GetAsync`，显式 Include Fields：

```csharp
/// <summary>
/// 获取实体注册表详情：查看单个实体的完整配置信息，包含关联的字段列表
/// </summary>
public override async Task<TableDto> GetAsync(Guid id)
{
    var entity = await _repository._DbQueryable
        .Includes(x => x.Fields)
        .Where(x => x.Id == id)
        .FirstAsync() ?? throw new EntityNotFoundException(typeof(Table), id);

    return await MapToGetOutputDtoAsync(entity);
}
```

**收益**：前端一次请求即可获得 Table 配置 + 全部 Field 列表，适合实体详情页面。

---

### 7.3 SelectDataList 用于搜索栏下拉框

**用户诉求**：能否将 `select-data-list` 用于搜索栏，返回去重后的模块、项目、表名列表？

**判定：✅ 合理，推荐改造此端点**

**分析**：原审计建议移除此端点，理由是"Table 是顶级实体，无下拉引用场景"。但用户提出了一个更优的使用场景——**搜索栏快速筛选下拉框**，这确实是一个真实需求。

**改造方案**：在 `TableService` 中 override `GetSelectDataListAsync`，返回去重后的筛选项：

```csharp
/// <summary>
/// 获取搜索栏下拉框数据：返回去重后的模块列表、项目列表、实体列表，
/// 供前端搜索栏的快速筛选下拉框使用
/// </summary>
public override async Task<PagedResultDto<TableDto>> GetSelectDataListAsync(string? keywords = null)
{
    var query = _repository._DbQueryable;
    if (!string.IsNullOrEmpty(keywords))
    {
        query = query.Where(x => x.Name!.Contains(keywords)
            || (x.ModuleName != null && x.ModuleName.Contains(keywords))
            || (x.ProjectName != null && x.ProjectName.Contains(keywords)));
    }

    var items = await query
        .Select(x => new TableDto
        {
            Id = x.Id,
            Name = x.Name!,
            ModuleName = x.ModuleName,
            ProjectName = x.ProjectName
        })
        .ToListAsync();

    return new PagedResultDto<TableDto>(items.Count, items);
}
```

> 前端使用方式：调用此接口获取全部 Table 条目（仅含 Id/Name/ModuleName/ProjectName），
> 在前端按 ModuleName/ProjectName 分组去重，填充搜索栏下拉框。

**更新端点判定**：`select-data-list` 从"移除"改为"**保留**"。

| 服务 | 修订后保留 | 修订后移除 |
|------|:---------:|:---------:|
| Table | **4** (GetList + Get + Update + **SelectDataList**) | 4 |
| Field | 4 | 5 |
| Template | 5 | 3 |
| **合计** | **13** | **12** |

---

### 7.4 Field UI 配置默认值

**用户诉求**：字段的"代码生成行为"配置是否应预设合理默认值？少部分需要额外调整？

**判定：✅ 合理，当前已部分实现，有一处可改进**

**当前默认值（在 `WebTemplateManager` 中）**：

| 属性 | 默认值 | 评估 |
|------|--------|------|
| IsQueryField | `true` | ✅ 合理：新字段默认参与查询，用户按需关闭 |
| IsListDisplay | `true` | ✅ 合理：新字段默认在列表显示 |
| IsFormItem | `true` | ✅ 合理：新字段默认出现在表单中 |
| HtmlType | `"Input"` | ✅ 合理：Input 是最通用的控件类型 |
| OrderNum | `0` | ❌ **应改为属性声明顺序** |

**唯一改进点 — OrderNum**：

当前所有字段的 `OrderNum` 默认为 0，导致生成代码中字段顺序不可控。
应在 `PropertyMapperToFiled` 中传入属性索引：

```csharp
// 当前：
foreach (PropertyInfo p in entityType.GetProperties())
    table.Fields.Add(PropertyMapperToFiled(p));

// 改进：传入索引
int order = 0;
foreach (PropertyInfo p in entityType.GetProperties())
    table.Fields.Add(PropertyMapperToFiled(p, order++));
```

**默认值策略总结**："全开 + 按需关闭"是正确策略。用户只需调整少数不需要参与查询/列表/表单的字段，而不是逐一开启每个字段。

---

### 7.5 IsPublic / IsEnabled / [IgnoreCodeFirst] 与 Scriban 模板的关系

**用户诉求**：这些参数和特性是否需要额外维护，才能让 Scriban 模板在生成时跳过/忽略？

**判定：✅ 极其重要的问题，揭示了当前系统的核心缺口**

#### 7.5.1 当前状态 — 严重脱节

**经过逐文件检查，发现当前 Scriban 模板完全不使用 IsQueryField / IsListDisplay / IsFormItem / IsPublic 标志！**

| Scriban 模板 | 当前过滤逻辑 | 问题 |
|-------------|------------|------|
| `CreateInput.scriban` | `field.Name != "Id" && field.Name != "CreationTime"` | 硬编码字段名排除 |
| `UpdateInput.scriban` | `field.Name != "Id" && field.Name != "CreationTime"` | 硬编码字段名排除 |
| `GetListOutputDto.scriban` | `field.Name != "Id"` | 硬编码字段名排除 |
| `GetOutputDto.scriban` | `field.Name != "Id"` | 硬编码字段名排除 |
| `GetListInput.scriban` | 不遍历 Fields（仅生成 `Filter` 属性） | 未使用 IsQueryField |

**后果**：用户在 UI 中精心配置的 `IsQueryField`/`IsListDisplay`/`IsFormItem` 对生成代码**完全无效**。

#### 7.5.2 当前 `FieldInfo`（传给 Scriban 的上下文对象）缺失关键属性

`DefaultTemplateContextEnricher` 构建的 `FieldInfo` 对象：

```
已传递：Name, Type, MaxLength, IsRequired, IsPrimaryKey, Description, IsQueryField, OrderNum, CsharpType
未传递：IsListDisplay, IsFormItem, IsPublic, HtmlType, IsAutoAdd
```

`IsQueryField` 虽然传了，但模板里没有使用。`IsListDisplay`/`IsFormItem`/`IsPublic` 既没传也没用。

#### 7.5.3 三个标志的职责澄清

| 标志 | 当前状态 | 应有职责 | 需要修改 |
|------|---------|---------|----------|
| **IsPublic** | Field 有此属性；Enricher 未传递；模板未使用 | 标记审计/基类字段，Scriban 跳过这些字段 | Enricher + 模板 + 自动检测 |
| **IsListDisplay** | Field 有此属性；Enricher 未传递；模板未使用 | 控制是否在 ListOutputDto 中生成 | Enricher + 模板 |
| **IsFormItem** | Field 有此属性；Enricher 未传递；模板未使用 | 控制是否在 CreateInput/UpdateInput 中生成 | Enricher + 模板 |
| **IsQueryField** | Field 有此属性；Enricher 已传递；模板**未使用** | 控制是否在 GetListInput 中生成查询条件 | 模板 |
| **IsEnabled** | ❌ **Table 和 Field 上均不存在此字段** | 用于整体禁用某实体/字段不参与生成 | 需新增字段 |
| **[IgnoreCodeFirst]** | Table 级别已生效；**Field 级别不存在** | 标记整个实体类跳过扫描 | 无需改动（已工作） |

#### 7.5.4 IsPublic 自动检测方案

当前 `IsPublic` 完全依赖手动标记，应在 `PropertyMapperToFiled` 中自动检测基类属性：

```csharp
private static Field PropertyMapperToFiled(PropertyInfo propertyInfo, int order)
{
    Field fieldEntity = new() { OrderNum = order, /* ... */ };

    // 自动检测审计/基类字段：属性的 DeclaringType 不是当前实体类 → 继承自基类
    bool isInherited = propertyInfo.DeclaringType != propertyInfo.ReflectedType
                    || propertyInfo.DeclaringType?.Name == "FullAuditedAggregateRoot`1"
                    || propertyInfo.DeclaringType?.Name == "FullAuditedEntity`1"
                    || propertyInfo.DeclaringType?.Name == "Entity`1";
    fieldEntity.IsPublic = isInherited;

    // ... 其他现有逻辑
}
```

> 注意：`DeclaringType` 返回的是**最初声明该属性的类**，如果属性来自基类，
> `DeclaringType` 会是基类而非当前实体类。这是最可靠的检测方式。

#### 7.5.5 模板改造方案（Scriban 应使用标志而非硬编码名称）

**改造后的模板逻辑**：

```
CreateInput.scriban:
  {{~ for field in Fields ~}}
  {{~ if field.IsFormItem && !field.IsPublic ~}}   ← 替代硬编码名称排除
  public {{ field.CsharpType }} {{ field.Name }} { get; set; }
  {{~ end ~}}
  {{~ end ~}}

GetListOutputDto.scriban:
  {{~ for field in Fields ~}}
  {{~ if field.IsListDisplay && !field.IsPublic ~}} ← 替代 field.Name != "Id"
  public {{ field.CsharpType }} {{ field.Name }} { get; set; }
  {{~ end ~}}
  {{~ end ~}}

GetListInput.scriban:
  {{~ for field in Fields ~}}
  {{~ if field.IsQueryField && !field.IsPublic ~}}  ← 当前完全不使用 Fields
  public {{ field.CsharpType }} {{ field.Name }} { get; set; }
  {{~ end ~}}
  {{~ end ~}}
```

**需要改造的文件**：

| 文件 | 改造内容 |
|------|----------|
| `FieldInfo` (TemplateContext.cs) | 添加 `IsListDisplay`, `IsFormItem`, `IsPublic` 属性 |
| `DefaultTemplateContextEnricher.cs` | 传递 Field 的 IsListDisplay/IsFormItem/IsPublic 到 FieldInfo |
| `CreateInput.scriban` | `field.IsFormItem && !field.IsPublic` 替代硬编码 |
| `UpdateInput.scriban` | 同上 |
| `GetListOutputDto.scriban` | `field.IsListDisplay && !field.IsPublic` 替代硬编码 |
| `GetOutputDto.scriban` | `!field.IsPublic` (详情返回所有非公共字段) |
| `GetListInput.scriban` | 遍历 Fields，按 `IsQueryField` 生成查询属性 |
| `WebTemplateManager.PropertyMapperToFiled` | 自动检测 IsPublic |

#### 7.5.6 IsEnabled 字段是否需要？

**分析**：
- `IsEnabled` 不存在于当前任何实体上
- 在 ANALYSIS-10 第 2.2 节 #3 中提到：如果需要"排除某实体不参与代码生成"，
  可以用 `[IgnoreCodeFirst]` 特性（已生效）
- 但 `[IgnoreCodeFirst]` 是代码级标记，需要修改 C# 代码并重新编译
- 如果希望**运行时**控制是否参与代码生成（不改代码），需要在 Table 上添加 `IsEnabled` 字段

**建议**：
- **当前阶段不添加** `IsEnabled` 字段，`[IgnoreCodeFirst]` 已能满足需求
- 如果后续有"临时禁用某实体生成但不删代码"的需求，再在 Table 上加 `IsEnabled` 布尔字段

#### 7.5.7 总结 — 回答用户的原始问题

> "IsPublic = true，IsEnabled，[IgnoreCodeFirst] 这些参数和特性是否需要额外的维护，
> 才能告知 Scriban 模板在遇到这些参数时跳过或忽略？"

**回答**：

| 机制 | 是否需要额外维护 | 原因 |
|------|:---------------:|------|
| **[IgnoreCodeFirst]** | ❌ 不需要 | 已在 `BuildCodeToWebAsync` 中生效，标记的实体类会被扫描逻辑跳过 |
| **IsPublic** | ⚠️ **当前需要，改造后不需要** | 需要：① Enricher 传递该属性到 Scriban 上下文 ② 模板使用该属性做过滤 ③ 反射时自动检测基类属性 |
| **IsFormItem/IsListDisplay/IsQueryField** | ⚠️ **当前需要，改造后不需要** | 需要：① Enricher 传递到 Scriban 上下文 ② 模板使用对应属性做过滤 |
| **IsEnabled** | ❌ 暂不需要 | 当前不存在此字段，[IgnoreCodeFirst] 已满足需求 |

**核心结论**：当前存在一个**配置与生成脱节**的系统性缺口——用户在 UI 中配置的 UI 标志（IsQueryField/IsListDisplay/IsFormItem/IsPublic）对代码生成完全无效。需要在 Enricher + Scriban 模板两个层面补全，才能让整套 UI 配置真正生效。这应作为后续改造的优先任务。
