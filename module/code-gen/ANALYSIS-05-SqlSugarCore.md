# 📁 SharpFort.CodeGen.SqlSugarCore/ 文件夹分析

> **分析日期**: 2026-06-12  
> **路径**: `module/code-gen/SharpFort.CodeGen.SqlSugarCore/`  
> **文件数量**: 3 个文件

---

## 一、功能概述

**SqlSugarCore 层**是 ABP 框架中的基础设施层变体，负责 SqlSugar ORM 的集成和数据初始化（种子数据）。

---

## 二、文件逐一分析

### 2.1 TemplateDataSeed.cs — 默认模板种子数据 (251行)

**实现**: `IDataSeedContributor, ITransientDependency`

**职责**: 在系统首次启动时，向数据库插入 8 个默认的 Scriban 模板。

**触发条件**: 仅当 `gen_template` 表为空时执行（`!await _repository.IsAnyAsync(x => true)`）

---

#### 8 个种子模板清单

| # | Name | 固定 GUID | 生成路径模板 | 说明 |
|---|------|-----------|-------------|------|
| 1 | Entity | `673752e5-...e3a` | `module/{{Module}}/SharpFort.{{Module}}.Domain/Entities/{{Model}}Entity.cs` | 实体类 |
| 2 | GetListInput | `7fa2b98e-...7a1` | `module/{{Module}}/.../Dtos/{{Model}}/{{Model}}GetListInput.cs` | 列表查询输入 |
| 3 | GetListOutputDto | `8a4de9c2-...5a1` | `module/{{Module}}/.../Dtos/{{Model}}/{{Model}}GetListOutputDto.cs` | 列表项输出 |
| 4 | GetOutputDto | `96a5b9e0-...6d2` | `module/{{Module}}/.../Dtos/{{Model}}/{{Model}}GetOutputDto.cs` | 详情输出 |
| 5 | CreateInput | `a5e9b98e-...7a2` | `module/{{Module}}/.../Dtos/{{Model}}/{{Model}}CreateInput.cs` | 新增输入 |
| 6 | UpdateInput | `b6fa9c8e-...7a3` | `module/{{Module}}/.../Dtos/{{Model}}/{{Model}}UpdateInput.cs` | 编辑输入 |
| 7 | IServices | `c7fa2b9e-...7a4` | `module/{{Module}}/.../IServices/I{{Model}}Service.cs` | 服务接口 |
| 8 | Service | `d8fa2b9e-...7a5` | `module/{{Module}}/.../Services/{{Model}}Service.cs` | 服务实现 |

---

#### 模板内容分析（以 Entity 为例）

种子数据中的模板内容是**硬编码在 C# 字符串**中的完整 Scriban 模板：

```scriban
using SqlSugar;
using Volo.Abp.Domain.Entities;

namespace {{RootNamespace}}.{{Module}}.Domain.Entities
{
    [SugarTable("{{Table.Name}}")]
    public class {{Model}}Entity : Entity<Guid>
    {
        {{~ for field in Fields ~}}
        {{~ if field.Name != "Id" && field.Name != "IsDeleted" ... ~}}
        {{ sugar_column field }}
        public {{ field.CsharpType }} {{ field.Name }} { get; set; } = {{ default_value field }};
        {{~ end ~}}
        {{~ end ~}}
    }
}
```

**关键观察**:
- 种子数据中的模板与 `Templates/` 文件夹中的 `.scriban` 文件**内容独立**，可作为两套模板源
- 所有种子模板使用 `TemplateEngine = "Scriban"`
- GUID 是硬编码的固定值 ── 意味着跨环境部署时模板 ID 一致
- `Service.scriban` 种子模板包含 `<sf-custom-code-start>` 标记块

---

### 2.2 SharpFortCodeGenSqlSugarCoreModule.cs

```csharp
[DependsOn(typeof(SharpFortSqlSugarCoreModule))]
public class SharpFortCodeGenSqlSugarCoreModule : AbpModule { }
```

**职责**: ABP 模块入口，依赖框架的 SqlSugarCore 基础设施模块。

---

### 2.3 SharpFort.CodeGen.SqlSugarCore.csproj

```
TargetFramework: net10.0.0
依赖:
  - SharpFort.SqlSugarCore (框架层 - 完整 SqlSugar 实现)
  - SharpFort.CodeGen.Domain (领域层)
```

---

## 三、种子数据的作用与覆盖机制

### 模板优先级链路

```
① 本地工作区模板 (最高优先级)
  路径: {SolutionRoot}/module/code-gen/Templates/{Name}.scriban
  作用: CodeFileManager 运行时优先加载
  
② 数据库模板 (中等优先级)
  表: gen_template
  来源: TemplateDataSeed 首次启动插入
  
③ 种子数据模板 (最低优先级)
  硬编码在 TemplateDataSeed.GetSeedData()
  触发: 仅 gen_template 为空时
```

### 修改模板的正确姿势

| 场景 | 操作 |
|------|------|
| 永久修改默认模板 | 修改 `TemplateDataSeed.GetSeedData()` 中的硬编码内容 |
| 当前项目覆盖模板 | 修改 `Templates/*.scriban` 文件 |
| 运行时动态调整 | 通过 API 修改 `gen_template` 表的记录 |

---

## 四、配置项

| 配置项 | 位置 | 说明 |
|--------|------|------|
| 种子数据触发条件 | TemplateDataSeed.SeedAsync() | `gen_template` 表为空时触发 |
| 默认模板 GUID | GetSeedData() 硬编码 | 固定的 Guid 值，用于跨环境识别 |

---

## 五、扩展/改写建议

| 优先级 | 建议 | 说明 |
|--------|------|------|
| 🔴 高 | 增加 Vue 前端模板种子 | 新增 .vue 模板的种子数据 |
| 🟡 中 | 种子数据版本化 | 支持模板升级时自动插入新版本 |
| 🟡 中 | 种子数据外部化 | 改为从 JSON 文件加载，便于维护 |
| 🟢 低 | 环境差异化种子 | 不同环境使用不同模板集 |
| 🟢 低 | 模板分类/标签 | 为种子模板增加分类和标签信息 |
