# Code-Gen 模板管理页面空白问题分析与修复方案

> **日期**: 2026-06-13  
> **问题现象**: 前端模板管理页面显示空白，无模板数据  
> **分析范围**: Template 实体、TemplateDto、种子数据、模板加载链路

---

## 一、根因分析 — 发现两个关键问题

### 1.1 根因 #1：DTO 属性命名不匹配（映射断裂）

**这是一个 Bug**，很可能是页面空白的直接原因。

| 层级 | 属性名 | 说明 |
|------|--------|------|
| `Template` 实体 | `Content` | Scriban 模板脚本内容 |
| `TemplateDto` DTO | `TemplateStr` | 映射目标属性 |

**问题**：ABP 框架默认使用 AutoMapper 按属性名约定映射。`Content` ≠ `TemplateStr`，导致：
- `MapToGetOutputDtoAsync(entity)` → `TemplateDto.TemplateStr` 始终为空字符串
- `MapToEntityAsync(input)` → `Template.Content` 始终为 null（创建/更新时内容丢失）
- 前端收到的模板内容永远为空

**影响范围**：所有 Template CRUD 操作（GetList、Get、Create、Update）的 Content 字段均无法正常映射。

### 1.2 根因 #2：种子数据条件过于严格

`TemplateDataSeed.SeedAsync` 逻辑：

```csharp
if (!await _repository.IsAnyAsync(x => true))  // 仅当表完全为空时才播种
{
    await _repository.InsertManyAsync(GetSeedData());
}
```

**问题**：
- 如果数据库已存在任何数据（即使是旧版本遗留的），种子**永远不会执行**
- 如果用户误删了部分模板，种子不会补回缺失的模板
- 种子数据内容是**旧版本**（使用 `field.Name != "Id"` 硬编码排除），与本地 `.scriban` 文件（已改用标志过滤）不同步

### 1.3 补充发现：种子数据已过时

| 模板 | 种子数据中的逻辑 | 本地 .scriban 文件中的逻辑 |
|------|-----------------|--------------------------|
| CreateInput | `field.Name != "Id" && field.Name != "CreationTime"` | `field.IsFormItem && !field.IsPublic` |
| UpdateInput | `field.Name != "Id" && field.Name != "CreationTime"` | `field.IsFormItem && !field.IsPublic` |
| GetListOutputDto | `field.Name != "Id"` | `field.IsListDisplay && !field.IsPublic` |
| GetOutputDto | `field.Name != "Id"` | `!field.IsPublic` |
| GetListInput | 不遍历 Fields | 遍历 `field.IsQueryField && !field.IsPublic` |

> **注意**：这不影响代码生成功能，因为 `CodeFileManager` 优先从本地 `.scriban` 文件加载（双层架构），仅在本地文件不存在时才用 DB 种子内容。

---

## 二、方案对比

### 方案 A：仅修复 DTO 映射 + 手动维护

**做法**：
1. 将 `TemplateDto.TemplateStr` 重命名为 `TemplateDto.Content`
2. 清空 `gen_template` 表让种子重新执行（或手动 INSERT）

| 优势 | 劣势 |
|------|------|
| 改动最小 | 种子数据是旧版本，需要手动同步 |
| 不需要新接口 | 模板变更后无法从文件系统同步回 DB |
| | 用户需要手动操作数据库 |

### 方案 B：修复 DTO + 添加 Sync 接口（推荐）

**做法**：
1. 将 `TemplateDto.TemplateStr` 重命名为 `TemplateDto.Content`
2. 在 CodeGenService 中新增 `POST /api/app/code-gen/template-sync` 接口
3. 该接口扫描本地 `Templates/*.scriban` 文件，增量同步到 DB（Upsert by Name）
4. 更新种子数据为最新版本

| 优势 | 劣势 |
|------|------|
| 一键同步，用户体验好 | 需新增 1 个接口 |
| 本地 .scriban 文件是模板的"真相源"，保持一致 | — |
| DB 模板列表可作为管理视图（查看/比较） | — |
| 种子数据更新后新部署也能获得最新模板 | — |
| 与 Table/Field 的 Code→Web 同步理念一致 | — |

### 方案 C：去掉 DB 层，纯文件驱动

**做法**：
1. 去掉 `gen_template` 表和 Template CRUD
2. `CodeFileManager` 直接从本地 `.scriban` 文件读取
3. 前端模板页面改为读取文件系统

| 优势 | 劣势 |
|------|------|
| 架构最简，无数据同步问题 | 无法通过 UI 管理模板（查看/编辑） |
| | 丢失 DB 层的 CRUD 审计能力 |
| | 前端需要新 API 读取文件系统 |
| | 违背"双层架构"设计初衷 |

---

## 三、推荐方案：B（修复 DTO + Sync 接口 + 更新种子）

### 3.1 理由

1. **双层架构的核心价值**：DB 提供管理视图 + CRUD 审计，本地文件提供版本控制 + 快速迭代。两者互补而非替代。
2. **与现有模式一致**：Table/Field 有 Code→Web 同步（`PostRefreshAsync`），Template 也应有 File→DB 同步，形成完整的同步范式。
3. **用户场景覆盖**：
   - 新部署 → 种子自动初始化（更新后的种子）
   - 模板变更后 → 用户点击"同步模板"一键更新 DB
   - 日常查看/编辑 → 通过 Template CRUD 管理

### 3.2 实施清单

| # | 任务 | 文件 | 说明 |
|---|------|------|------|
| 1 | **修复 DTO 属性名** | `TemplateDto.cs` | `TemplateStr` → `Content`，修复 AutoMapper 映射 |
| 2 | **新增 Sync 接口** | `ICodeGenService.cs` | 声明 `PostTemplateSyncAsync()` |
| 3 | **实现 Sync 逻辑** | `CodeGenService.cs` | 扫描 `Templates/*.scriban` 文件，Upsert 到 `gen_template` 表 |
| 4 | **更新种子数据** | `TemplateDataSeed.cs` | 种子内容与本地 .scriban 文件保持一致 |

### 3.3 Sync 接口详细设计

```
POST /api/app/code-gen/template-sync

功能：扫描本地 Templates/*.scriban 文件，增量同步到 gen_template 表
策略：按 Name 匹配（Upsert）
  - 本地文件存在 + DB 不存在 → INSERT
  - 本地文件存在 + DB 已存在 → UPDATE Content（保留用户修改的 Remarks）
  - 本地文件不存在 + DB 存在 → 不删除（用户可能自定义了模板）

Swagger 描述：
  同步本地 Scriban 模板文件到数据库：扫描 Templates/*.scriban 目录，
  将文件内容增量合并到 gen_template 表（按名称匹配，新增或更新）
```

### 3.4 双层模板加载优先级（不变）

```
代码生成时模板加载顺序（CodeFileManager 已有逻辑）：
  1. 从 gen_template 表读取基线版本
  2. 检查本地 Templates/{Name}.scriban 文件是否存在
  3. 如果本地文件存在 → 使用本地文件内容（覆写）
  4. 如果本地文件不存在 → 使用 DB 种子内容
```

### 3.5 工作流示意

```
开发者修改 Templates/CreateInput.scriban
        │
        ├─→ 代码生成：自动使用最新本地文件（无需同步）
        │
        └─→ 前端管理页面：调用 POST /template-sync → DB 更新为最新内容
                │
                └─→ 前端可查看所有模板的当前内容、备注、生成路径
```

---

## 四、关于 TemplateDto 属性名的补充

当前 `TemplateDto` 的属性与 `Template` 实体的对应关系：

| Template 实体 | TemplateDto (当前) | TemplateDto (修复后) | 说明 |
|---------------|-------------------|---------------------|------|
| `Name` | `Name` ✅ | `Name` | 模板名称 |
| `BuildPath` | `BuildPath` ✅ | `BuildPath` | 生成路径 |
| **`Content`** | **`TemplateStr`** ❌ | **`Content`** ✅ | 模板脚本内容 |
| `Remarks` | `Remarks` ✅ | `Remarks` | 备注 |

修复 `TemplateStr` → `Content` 后，ABP 默认 AutoMapper 按属性名约定即可正确映射，无需额外配置。

---

## 五、用户方案评审 — DB 优先 + 双向同步架构

> 用户提出改进方案：放弃种子数据，DB 作为运行时唯一数据源，本地文件仅用于版本控制和初始导入

### 5.1 用户方案概述

**核心理念**：
- DB 是运行时"唯一真相源"，代码生成直接从 DB 读取模板
- 本地 `.scriban` 文件 = Git 版本控制载体（不参与代码生成运行时）
- 双向同步：本地 → DB（初始导入）、DB → 本地（Web 编辑后回写）

**三个工作流**：
```
新部署 → 点击"同步模板" → 读取本地 .scriban → 写入 DB
Web 编辑 → 保存到 DB → 同时回写本地 .scriban（Git 版本控制）
日常操作 → Template CRUD 管理（查看/编辑/新增/删除）
```

### 5.2 与原方案 B 的对比

| 维度 | 原方案 B | 用户方案 | 评判 |
|------|---------|---------|------|
| **运行时数据源** | DB 基线 + 本地文件覆写 | **仅 DB** | ✅ 用户方案更简洁，消除了"DB vs 本地文件不一致"的歧义 |
| **种子数据** | 保留（但需更新） | **放弃** | ✅ 减少维护成本，种子数据与本地文件重复是冗余 |
| **同步方向** | 单向（本地 → DB） | **双向**（本地 ↔ DB） | ✅ Web 编辑自动回写本地，保证 Git 版本控制不断裂 |
| **CodeFileManager** | 需检查本地文件覆写 | **仅读 DB** | ✅ 消除文件探测逻辑，运行时更确定 |
| **首次部署** | 种子自动初始化 | 用户手动点"同步" | ⚠️ 多一步操作，但可接受（与 PostRefreshAsync 一致） |
| **DB 为空时生成** | 回退到本地文件 | 生成失败（DB 无数据） | ⚠️ 需添加空检查提示 |

### 5.3 合理性分析

**用户方案比原方案 B 更优**，理由：

1. **消除双层歧义**：原架构"DB 基线 + 本地覆写"让开发者困惑——到底哪个是"真相"？用户方案明确 DB 是运行时真相，本地文件是 Git 备份。

2. **与 Table/Field 范式一致**：
   - Table/Field：C# 实体类 → PostRefreshAsync → DB → 代码生成读 DB
   - Template：本地 .scriban → 同步模板 → DB → 代码生成读 DB
   - 两者都是"外部源 → 同步到 DB → 运行时读 DB"，架构一致

3. **Web 编辑即时生效**：用户在 Web 上编辑模板后，下次代码生成直接使用新内容。不需要"先同步本地文件再刷新"的额外步骤。

4. **版本控制不断裂**：Web 编辑后自动回写本地文件，Git 始终能追踪模板变更历史。

### 5.4 注意事项

| 风险 | 缓解措施 |
|------|----------|
| 首次部署未点"同步"→ DB 为空 → 生成失败 | CodeFileManager 中添加空检查，抛出明确错误提示 |
| Web 回写本地文件失败（权限/路径问题） | 回写失败时仅记录 Warning 日志，不阻断 DB 保存 |
| 放弃种子数据后 TemplateDataSeed 类如何处理 | 删除该类，或改为"检测到 DB 为空时自动提示用户同步" |

### 5.5 最终架构（用户方案）

```
┌─────────────┐     同步模板      ┌──────────────┐     代码生成      ┌─────────────┐
│ 本地 .scriban │ ──────────────→ │  gen_template  │ ──────────────→ │  生成的代码文件  │
│ (Git 版本控制) │ ←────────────── │    (DB 表)     │                 │  (DTO/Service) │
└─────────────┘     Web编辑回写    └──────────────┘                   └─────────────┘
                                         ↑
                                    Template CRUD
                                    (Web 页面管理)
```

### 5.6 修订后的实施清单

| # | 任务 | 文件 | 说明 |
|---|------|------|------|
| 1 | **修复 DTO 属性名** | `TemplateDto.cs` | `TemplateStr` → `Content`，修复 AutoMapper 映射 |
| 2 | **新增 Sync 接口** | `ICodeGenService.cs` + `CodeGenService.cs` | `PostTemplateSyncAsync()`：双向同步（本地 ↔ DB） |
| 3 | **简化 CodeFileManager** | `CodeFileManager.cs` | 移除本地文件覆写逻辑，仅从 DB 读取模板 |
| 4 | **删除种子数据** | `TemplateDataSeed.cs` | 删除该类，模板来源统一为本地文件同步 |
| 5 | **Web 编辑回写本地** | `TemplateService.cs` | override UpdateAsync，保存 DB 后同步写回本地 .scriban 文件 |

**Sync 接口双向逻辑**：

```
POST /api/app/code-gen/template-sync

参数: direction (可选，默认 "import")
  - "import"：本地 .scriban → DB（首次部署 / 手动导入）
  - "export"：DB → 本地 .scriban（回写备份，通常由 UpdateAsync 自动触发）

import 模式（Upsert by Name）：
  - 本地文件存在 + DB 不存在 → INSERT
  - 本地文件存在 + DB 已存在 → UPDATE Content（保留用户 Remarks）
  - 本地文件不存在 + DB 存在 → 不删除（用户自定义模板）

export 模式：
  - 遍历 DB 所有模板 → 写入 Templates/{Name}.scriban 文件
```

**Swagger 描述**：
```
同步 Scriban 模板（双向）：
  import 模式 — 从本地 Templates/*.scriban 文件导入到数据库（首次部署或手动同步）
  export 模式 — 从数据库导出到本地 Templates/*.scriban 文件（备份/版本控制）
  默认方向为 import
```

### 5.7 补充说明 — import/export 职责分离与模板唯一标识

> 用户确认：import 和 export 两个操作分别解决不同方向的同步，模板以 Name 作为唯一标识

#### import/export 职责严格分离

| 操作 | 方向 | 职责 | 触发场景 |
|------|------|------|----------|
| **import** | 本地 → DB | 仅读取本地 `.scriban` 文件并写入 DB | 首次部署、手动导入 |
| **export** | DB → 本地 | 仅读取 DB 模板并写入本地 `.scriban` 文件 | Web 编辑后回写、备份 |

**设计原则**：每个操作只做单向同步，不做“既读又写”的混合操作。用户需要完全同步时，依次点击 import + export 即可。

#### import 模式边界情况处理

| 本地文件 | DB 记录 | import 行为 | 理由 |
|:--------:|:------:|:----------:|------|
| ✅ 存在 | ❌ 不存在 | **INSERT** | 新模板入库 |
| ✅ 存在 | ✅ 存在 | **UPDATE** Content（保留 Remarks） | 更新内容，保留用户备注 |
| ❌ 不存在 | ✅ 存在 | **跳过**（不删除、不回写） | import 职责仅为 local→DB，回写由 export 负责 |

#### export 模式边界情况处理

| DB 记录 | 本地文件 | export 行为 | 理由 |
|:------:|:--------:|:----------:|------|
| ✅ 存在 | ❌ 不存在 | **写入**本地文件 | 补全本地缺失的模板文件 |
| ✅ 存在 | ✅ 存在 | **覆写**本地文件 | 本地始终与 DB 保持一致 |

#### 模板唯一标识

```
Template.Name = 本地文件名（不含扩展名）

示例：
  DB Name = "CreateInput"  ←→  本地文件 = "Templates/CreateInput.scriban"
  DB Name = "Service"       ←→  本地文件 = "Templates/Service.scriban"

约束：
  - DB 层：唯一索引 index_gen_template_name (Name ASC, IsUnique = true)
  - 文件层：文件名天然唯一
  - import/export 均按 Name 匹配，一对一关系
```

#### 完整同步流程图

```
首次部署：
  本地 Templates/*.scriban → [import] → gen_template 表

日常 Web 编辑：
  Web 页面编辑 → [UpdateAsync] → gen_template 表 → [自动 export] → 本地 .scriban

手动全量同步：
  [import] → [export] → 两侧完全一致
```
