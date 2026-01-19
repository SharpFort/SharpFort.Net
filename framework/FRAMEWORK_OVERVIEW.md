# Yi.Framework 模块概览分析

该目录包含项目的基础设施和通用框架层代码，基于 ABP Framework 进行扩展和封装。以下是各模块的初步功能分析：

## 1. 核心基础层 (Core Infrastructure)

*   **Yi.Framework.Core**
    *   **作用**: 整个框架的核心库，提供最基础的工具类、扩展方法、通用枚举和辅助函数。
    *   **主要内容**: 
        *   `Helper/`: 加密 (RSA, MD5)、HTTP 请求、系统信息、反射等工具。
        *   `Extensions/`: .NET 类型扩展。
        *   `Json/`: JSON 序列化转换器。
        *   `Modularity/`: 模块化管理相关。
    *   **依赖**: 无（或极少第三方依赖）。

*   **Yi.Framework.Mapster**
    *   **作用**: 对象映射模块，封装 Mapster 库。
    *   **主要内容**: 提供 `MapsterObjectMapper`，用于 DTO 与实体之间的自动转换，替代 ABP 默认的 AutoMapper（如果项目配置如此）。

## 2. 领域驱动设计 (DDD) 扩展层

*   **Yi.Framework.Ddd.Application.Contracts**
    *   **作用**: 应用层契约的基类定义。
    *   **主要内容**: 定义了分页查询参数 (`PagedAllResultRequestDto`)、CRUD 接口定义 (`IYiCrudAppService`) 等标准输入输出规范。

*   **Yi.Framework.Ddd.Application**
    *   **作用**: 应用服务的基类实现。
    *   **主要内容**: `YiCrudAppService`，封装了标准的增删改查逻辑，结合 SqlSugar 或其他仓储进行快速开发。

## 3. Web 与 API 层

*   **Yi.Framework.AspNetCore**
    *   **作用**: ASP.NET Core Web 层的扩展和统一处理。
    *   **主要内容**: 
        *   `UnifyResult/`: 统一返回结果封装 (`RESTfulResult`)，规范 API 响应格式。
        *   `Filters/`: 全局异常拦截 (`FriendlyExceptionFilter`)、结果过滤器。
        *   `Mvc/`: 路由约定、API 描述信息。

*   **Yi.Framework.AspNetCore.Authentication.OAuth**
    *   **作用**: 第三方 OAuth 认证集成。
    *   **主要内容**: 实现了 Gitee、QQ 等平台的 OAuth 登录处理器和扩展。

## 4. 数据访问与 ORM 层

*   **Yi.Framework.SqlSugarCore.Abstractions**
    *   **作用**: SqlSugar ORM 的抽象层接口。
    *   **主要内容**: 定义 `ISqlSugarRepository`、`ISqlSugarDbContext` 等接口，解耦具体实现。

*   **Yi.Framework.SqlSugarCore**
    *   **作用**: SqlSugar ORM 的具体实现与 ABP 集成。
    *   **主要内容**: 
        *   `Repositories/`: 泛型仓储实现 (`SqlSugarRepository`)。
        *   `Uow/`: 工作单元 (Unit of Work) 的 SqlSugar 适配，保证事务一致性。
        *   `DbContext/`: 数据库上下文管理。

*   **Yi.Framework.Caching.FreeRedis**
    *   **作用**: 分布式缓存实现，基于 FreeRedis。
    *   **主要内容**: 封装 Redis 操作，提供高性能缓存支持，可能用于替换或增强 ABP 默认的分布式缓存。

## 5. 基础设施与集成层

*   **Yi.Framework.BackgroundWorkers.Hangfire**
    *   **作用**: 后台任务与定时任务管理。
    *   **主要内容**: 集成 Hangfire，提供任务过滤器 (`UnitOfWorkHangfireFilter`) 和权限控制，用于处理异步任务和周期性作业。

*   **Yi.Framework.WeChat.MiniProgram**
    *   **作用**: 微信小程序功能集成。
    *   **主要内容**: 封装微信 API 调用（登录凭证校验 `Code2Session`、Token 管理等），支持小程序相关业务。

---

**总结**: 
Yi.Framework 构建了一套完整的 .NET 开发脚手架，从底层的工具库 (Core) 到 数据访问 (SqlSugar)，再到 Web 表现层 (AspNetCore) 和 基础设施 (Redis, Hangfire)，实现了对 ABP 框架的本地化增强（特别是替换 EF Core 为 SqlSugar，以及统一 API 响应格式）。
