# ABP 常规控制器端点生成规则 — 踩坑记录

> 创建时间：2026-06-13
> 关联模块：code-gen（TemplateService / CodeGenService）
> 严重程度：高 — 导致新增端点不注册、Casbin 权限无法同步

---

## 一、问题现象

在 `TemplateService` 中新增了两个 public 方法：

```csharp
public async Task PostImportTemplatesAsync() { ... }
public async Task PostExportTemplatesAsync() { ... }
```

**现象**：
1. Swagger 中能看到这两个端点（ABP 仍然生成了路由）
2. 但在前端"菜单管理"中将它们添加到 `menu` 表后，`casbin_rule` 表**没有自动增量同步**
3. 导致 Casbin 权限校验时找不到对应路由规则，接口无法通过权限认证

**对比**：同一模块中其他接口（如 `POST /api/app/code-gen/refresh`）在菜单管理中维护时能正常同步到 `casbin_rule`。

---

## 二、根因分析

### ABP 常规控制器的端点生成规则

ABP Framework 的 Conventional Controller（常规控制器）按以下规则自动生成 API 端点：

```
接口方法 (IXxxService) → ABP 自动注册为 API 端点
实现类方法 (XxxService) → ABP 不会自动注册
```

**关键规则**：ABP 只扫描**接口（Interface）**中声明的方法来生成 API 端点。实现类中的普通 public 方法**不会**被 ABP 的常规控制器机制识别。

### 三种让端点生效的方式

| 方式 | 机制 | ABP 端点注册 | Casbin 菜单同步 | 示例 |
|------|------|:-----------:|:-------------:|------|
| **① 接口声明** | 方法在 `IXxxService` 中声明 | ✅ 自动 | ✅ 正常 | `ISfCrudAppService` 基类的所有 CRUD 方法 |
| **② 显式路由属性** | `[HttpPost("xxx")]` / `[HttpGet("xxx")]` | ⚠️ ASP.NET Core 层 | ❌ 可能异常 | `CodeGenService.PostDir` 使用 `[HttpPost("code-gen/dir/{**path}")]` |
| **③ 仅 public 方法** | 实现类上的普通 public 方法 | ⚠️ 不确定 | ❌ 异常 | `PostImportTemplatesAsync` / `PostExportTemplatesAsync` |

### 本案例的具体问题

```csharp
// ITemplateService.cs — 接口中为空，只继承了基类
public interface ITemplateService : ISfCrudAppService<TemplateDto, Guid, TemplateGetListInput>
{
    // ❌ 没有声明 PostImportTemplatesAsync
    // ❌ 没有声明 PostExportTemplatesAsync
}

// TemplateService.cs — 实现类中新增了方法
public class TemplateService : SfCrudAppService<...>, ITemplateService
{
    public async Task PostImportTemplatesAsync() { ... }  // ❌ 不在接口中
    public async Task PostExportTemplatesAsync() { ... }  // ❌ 不在接口中
}
```

**对比正常工作的端点**：

```csharp
// ICodeGenService.cs — 接口中明确声明
public interface ICodeGenService : IApplicationService
{
    Task PostRefreshAsync();           // ✅ 在接口中 → 正常同步
    Task PostWebBuildCodeAsync(...);   // ✅ 在接口中 → 正常同步
    Task PostCodeBuildWebAsync();      // ✅ 在接口中 → 正常同步
}
```

### PostDir 的特殊情况

`CodeGenService.PostDir` 不在接口中，但通过显式路由属性 `[HttpPost("code-gen/dir/{**path}")]` 工作：

```csharp
// 不在 ICodeGenService 中，但有显式路由 → ASP.NET Core 层面可用
[HttpPost("code-gen/dir/{**path}")]
public Task PostDir([FromRoute] string path) { ... }
```

这种方式在 ASP.NET Core 路由层面可用，但**不经过 ABP 常规控制器注册流程**，可能导致：
- Casbin 菜单同步时无法发现该端点
- ABP 的 API 描述提供程序不包含该端点

---

## 三、修复方案

### 方式 ① 接口声明（推荐）

在 `ITemplateService` 中声明方法，ABP 自动注册端点并参与菜单/Casbin 同步：

```csharp
public interface ITemplateService : ISfCrudAppService<TemplateDto, Guid, TemplateGetListInput>
{
    /// <summary>
    /// 导入模板（本地 → DB）
    /// </summary>
    Task PostImportTemplatesAsync();

    /// <summary>
    /// 导出模板（DB → 本地）
    /// </summary>
    Task PostExportTemplatesAsync();
}
```

### 方式 ② 显式路由属性（备选）

在实现类方法上添加显式路由，适用于不想暴露在接口中的方法：

```csharp
[HttpPost("template/import-templates")]
public async Task PostImportTemplatesAsync() { ... }
```

**注意**：此方式绕过了 ABP 常规控制器机制，可能仍然无法被菜单/Casbin 同步发现。

---

## 四、规则总结 — 新增端点检查清单

在 ABP 模块中新增 API 端点时，务必检查以下清单：

| # | 检查项 | 必须 |
|---|--------|:----:|
| 1 | 方法是否在 `IXxxService` 接口中声明？ | ✅ |
| 2 | 方法命名是否符合 ABP 路由约定？（`GetXxx` → GET, `PostXxx` → POST 等） | ✅ |
| 3 | 自定义参数是否添加了 `[FromQuery]` / `[FromBody]` 标注？ | ✅ |
| 4 | 如果不需要某些基类端点，是否用 `[RemoteService(isEnabled: false)]` 禁用？ | ✅ |
| 5 | 新增端点后是否在"菜单管理"中维护并同步到 `casbin_rule`？ | ✅ |

### 口诀

> **新增端点先写接口，再写实现；不写接口等于不注册。**

---

## 五、ABP 方法名 → HTTP 方法映射规则

| 方法前缀 | HTTP 方法 | 示例 |
|---------|----------|------|
| `Get` | GET | `GetAsync(id)` → `GET /api/app/xxx/{id}` |
| `GetList` | GET | `GetListAsync(input)` → `GET /api/app/xxx` |
| `Post` | POST | `PostImportTemplatesAsync()` → `POST /api/app/xxx/import-templates` |
| `Put` | PUT | `PutXxxAsync()` → `PUT /api/app/xxx` |
| `Delete` | DELETE | `DeleteAsync(ids)` → `DELETE /api/app/xxx` |
| 其他 | POST（默认） | `CustomMethodAsync()` → `POST /api/app/xxx/custom-method` |

ABP 会自动将 PascalCase 方法名转为 kebab-case 路由段。

---

## 六、本案例修复记录

**修复文件**：`ITemplateService.cs`
**修复内容**：在接口中添加 `PostImportTemplatesAsync()` 和 `PostExportTemplatesAsync()` 声明
**修复日期**：2026-06-13
