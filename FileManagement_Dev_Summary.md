# Yi框架模块开发实战总结 - FileManagement模块

## 1. 模块开发流程概览

本次 `Yi.Framework.FileManagement` 模块的开发遵循 DDD（领域驱动设计）原则，并充分利用了 Yi 框架的基础能力。以下是将此次经验应用到未来模块开发的标准化流程。

### 1.1 模块脚手架与工具 (CLI & Tools)

**工具**: `yi-abp` CLI
**命令**: `yi-abp new YourModuleName`
*   该命令会自动为您生成标准的 DDD 5层架构项目（Domain.Shared, Domain, Application.Contracts, Application, SqlSugarCore）。
*   **注意**: 默认生成的目录结构可能嵌套较深（例如 `module/YourModule/YourModule/`），建议手动将被嵌套的文件夹移至外层，保持与其他模块平级。

### 1.2 关键架构分层

*   **Domain.Shared**: 存放枚举 (`Enum`)、常量 (`Consts`)、多语言资源。此层不依赖任何业务逻辑。
*   **Domain**: 核心业务逻辑层。包含实体 (`Entity`)、仓储接口 (`Repository Interface`)、领域服务 (`DomainManager`)。
*   **Application.Contracts**: 数据传输对象 (`DTOs`)、应用服务接口 (`IAppService`)。
*   **Application**: 业务逻辑编排层。包含应用服务实现 (`AppService`)、鉴权逻辑。
*   **SqlSugarCore**: 基础设施层。包含 `DbContext`、仓储实现。

---

## 2. 核心开发模式 (Core Patterns)

### 2.1 实体与仓储 (Domain Layer)

*   **实体继承**: 推荐继承 `FullAuditedAggregateRoot<Guid>`。
    *   自动获得审计字段（创建人、修改人、软删除等）。
    *   **关键点**: 实体必须包含一个 `public` 无参构造函数，否则 SqlSugar 在查询实例化时会报错 (`new()` 约束)。
*   **仓储接口**: 推荐使用 `ISqlSugarRepository<TEntity, TKey>` 而不是标准的 ABP `IRepository`。
    *   **原因**: Yi框架封装的 `ISqlSugarRepository` 提供了直接访问 `_DbQueryable` 的能力，能使用 SqlSugar 强大的查询语法（如 `WhereIF`, `ToPageListAsync`），同时兼容 ABP 的仓储接口。

### 2.2 应用服务 (Application Layer)

Yi 框架提供了强大的基类，极大地减少了 CRUD 代码量。

#### **A. YiCrudAppService<...>**
这是最常用的基类，适用于标准的增删改查业务。
*   **泛型参数**: `<TEntity, TGetOutputDto, TGetListOutputDto, TKey, TGetListInput, TCreateInput, TUpdateInput>`
*   **内置功能**:
    *   **GetListAsync**: 自动处理分页、排序。
    *   **Create/Update/Delete**: 标准实现。
    *   **GetExportExcelAsync**: 自动导出 Excel（需引入 MiniExcel）。
    *   **DeleteManyAsync**: 批量删除。
*   **扩展点**: 重写 `CheckCreateInputDtoAsync` / `CheckUpdateInputDtoAsync` 进行业务校验。

#### **B. YiCacheCrudAppService<...>**
适用于读多写少、需要缓存的数据（如字典、配置）。
*   **机制**: 自动在 `Get` 时缓存，在 `Update/Delete` 时清除缓存。
*   **注意**: 需要实现 `GetListFromDatabaseAsync` 和 `GetListFromCacheAsync` 来定义缓存策略。

### 2.3 数据传输对象 (DTOs)

*   **分页查询基类**:
    *   **PagedAndSortedResultRequestDto**: ABP 标准基类，仅包含 `SkipCount`, `MaxResultCount`, `Sorting`。
    *   **PagedAllResultRequestDto**: Yi 框架增强版，增加了 `StartTime`, `EndTime` 时间范围查询，以及更方便的 `OrderByColumn` / `IsAsc` 排序字段。**推荐后台管理列表查询使用此基类。**
*   **实体 DTO**:
    *   **EntityDto<Guid>**: 输出 DTO 继承此基类，自动包含 `Id` 字段。
*   **输入 DTO**:
    *   CreateInput / UpdateInput: 通常是普通 `class` 或 `record`，无需特定继承。

---

## 3. 接口鉴权 (Authorization)

本次开发中，我们采用了 Casbin 策略鉴权，这是 Yi 框架的核心鉴权机制。

### 3.1 鉴权原理
*   **中间件**: `CasbinAuthorizationMiddleware` 拦截所有请求。
*   **四要素**:
    *   **Sub (Subject)**: 当前用户 ID。
    *   **Dom (Domain)**: 租户 ID（默认 "default"）。
    *   **Obj (Object)**: 请求路径 (Request Path)，例如 `/api/app/file-management/file-descriptor`。
    *   **Act (Action)**: 请求方法 (Method)，例如 `GET`, `POST`, `DELETE`。
*   **策略**: 必须在 `casbin_rule` 表中存在对应的策略 (`p, role_admin, default, /api/..., POST, allow`) 才能通过鉴权。

### 3.2 实施步骤
1.  **添加特性**: 在 Application Service 类上添加 `[Authorize]` 特性。
2.  **例外处理**: 如果某个接口（如文件上传/下载）需要公开访问，在具体方法上添加 `[AllowAnonymous]`。
3.  **Swagger 生成**: 系统启动后，Swagger 会自动扫描 API。管理员需要在“菜单管理”中配置这些接口的权限点，Casbin 策略会自动生成。

---

## 4. 关键踩坑与经验 (Lessons Learned)

本次开发中遇到的问题，是未来开发的重要警示：

1.  **DbContext 命名冲突**:
    *   **问题**: 模块内部定义了 `YiDbContext`，与框架核心 `Yi.Abp.SqlSugarCore.YiDbContext` 重名，导致 DI 注入时出现歧义。
    *   **解决**: 模块内的 DbContext 应具有唯一名称，如 `FileManagementDbContext`。
    *   **经验**: 永远不要使用通用的 `YiDbContext` 作为模块 DbContext 的名称。

2.  **实体构造函数**:
    *   **问题**: 定义实体时为了封装性将构造函数设为 `private`。
    *   **后果**: `ISqlSugarRepository` 依赖 `new()` 约束来实例化实体，导致编译错误。
    *   **解决**: 必须提供一个 `public` 无参构造函数。

3.  **模块依赖关系**:
    *   **Application 层**: 必须依赖 `YiFrameworkDddApplicationModule` 以使用 `YiCrudAppService`。
    *   **SqlSugarCore 层**: 尽量保持独立，不要依赖 RBAC 等其他业务模块，除非有直接的实体关联。本次开发中我们移除了不需要的 RBAC 依赖，保持了模块的纯净。

4.  **Web 层集成**:
    *   **Controller 注册**: 必须在 `YiAbpWebModule.cs` 中使用 `ConventionalControllers.Create` 注册模块的 API，否则 Swagger 中不会显示接口。
    *   **命名空间**: 记得在 Web 模块中 `using` 你新模块的 `Application` 命名空间。

---

## 5. 快速检查清单 (Quick Checklist)

- [ ] 使用 `yi-abp new` 创建模块
- [ ] 实体继承 `FullAuditedAggregateRoot<Guid>` 且有 `public` 无参构造
- [ ] 应用服务继承 `YiCrudAppService` (普通) 或 `YiCacheCrudAppService` (缓存)
- [ ] 分页 DTO 继承 `PagedAllResultRequestDto` (推荐)
- [ ] Service 类添加 `[Authorize]`
- [ ] DbContext 命名唯一 (e.g. `XxxModuleDbContext`)
- [ ] Web 模块注册 API Controller (`ConventionalControllers.Create`)
- [ ] 运行 `dotnet build` 验证 0 错误

此文档旨在帮助您在未来的模块开发中少走弯路，依此流程可快速构建高质量、符合 Yi 框架规范的业务模块。
