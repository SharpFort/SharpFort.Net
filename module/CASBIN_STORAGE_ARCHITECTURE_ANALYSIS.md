# Casbin 存储架构分析报告：为何需要“双重存储”？

## 1. 核心问题回顾
**现状**：系统目前同时维护着两套权限数据：
1.  **传统 RBAC 表**：`SysUserRole`, `SysRoleMenu`, `SysUserPosition` 等。
2.  **Casbin 规则表**：`CasbinRule` (存储 `p` 策略和 `g` 策略)。

**疑问**：既然 Casbin 能够管理权限，能否将所有关系完全迁移到 `CasbinRule` 中，废弃传统中间表？这些中间表是否仅仅是备份？

## 2. 深度分析：Casbin vs 传统表

### 2.1 数据结构的根本差异

| 特性 | **Casbin (`CasbinRule`)** | **传统表 (`SysMenu`/`SysRoleMenu`)** |
| :--- | :--- | :--- |
| **设计目的** | **运行时鉴权 (Runtime Enforcement)** | **UI 呈现与配置管理 (Management)** |
| **数据结构** | 扁平化字符串 (`sub, obj, act`) | 关系型、树形结构 (`ParentId`, `Sort`) |
| **存储内容** | `p, role_admin, tenant_1, /api/user, GET` | 菜单名称="用户管理", 图标="user", 排序=1 |
| **查询能力** | "User A 能否访问 URL B?" (极快) | "User A 拥有哪些菜单树?" (极快) |
| **缺失能力** | 无法高效存储元数据（名称、图标、父子关系） | 无法高效处理复杂的正则匹配和通配符鉴权 |

### 2.2 如果只用 Casbin 会发生什么？ (灾难推演)

假设我们删除了 `SysRoleMenu` 和 `SysMenu` 的层级关系，试图只用 Casbin 存储一切：

1.  **渲染菜单树变为“不可能的任务”**:
    *   前端请求“我的菜单”。
    *   后端从 Casbin 查出该角色拥有的 API 列表：`['/api/user', '/api/dept', ...]`。
    *   **死胡同**：后端如何知道 `/api/user` 对应前端的哪个页面？叫什么名字？图标是什么？父级菜单是谁？
    *   Casbin 的 `p` 策略中没有位置存放 `Icon`, `Title`, `ComponentPath`, `ParentId`, `SortOrder` 等 UI 字段。即便强行塞入 JSON 到扩展字段，查询和解析性能也会极其低下。

2.  **管理界面难以实现**:
    *   在“角色管理”中分配权限时，通常展示的是一棵**树形复选框**。
    *   如果只有扁平的 Casbin 规则，反向构建这棵树需要极复杂的算法，且难以维护。

3.  **业务语义丢失**:
    *   传统表承载了业务语义（如：这是一个“目录”，那是一个“外链”）。Casbin 只关心“拦截”或“放行”。

## 3. 架构结论

**结论：绝对不能删除中间表。**

它们不是“备份”，而是系统的 **UI/配置主数据 (Source of Truth for Configuration)**。
Casbin 表则是系统的 **运行时鉴权索引 (Compiled Index for Runtime)**。

这是一种经典的 **CQRS (命令查询职责分离)** 变体模式：

*   **配置侧 (Write/UI Read)**:
    *   **存储**: `SysUser`, `SysRole`, `SysMenu`, `SysRoleMenu`。
    *   **职责**: 负责后台管理的增删改查，以及前端菜单树的渲染。
    *   **特点**: 富含元数据，结构化强。

*   **鉴权侧 (Runtime Check)**:
    *   **存储**: `CasbinRule`。
    *   **职责**: 负责 API 请求的毫秒级拦截。
    *   **特点**: 数据扁平，读取极快，专为匹配优化。

## 4. 操作改造列表 (Refactoring Actions)

既然确定了“双写”模式，我们需要确保两套数据的一致性。

### 4.1 确保“同步机制”的原子性
目前在 `RoleManager` 和 `UserManager` 中已经实现了同步调用。建议进一步检查：
- [ ] **事务一致性**: 确保 SQL 表更新和 Casbin 更新在同一个逻辑事务中（或者容忍短暂的最终一致性）。如果 Casbin 更新失败，应该回滚 SQL 操作或抛出异常。

### 4.2 增加“灾难恢复”机制 (推荐)
为了防止因程序 Bug 或手动操作数据库导致两边数据不一致，建议实现一个 **"重建策略 (Rebuild Policies)"** 功能。

*   **新增方法**: `ICasbinPolicyManager.RebuildAllPoliciesAsync()`
*   **逻辑**:
    1.  清空 `CasbinRule` 表。
    2.  读取 `SysUserRole`，批量生成 `g` 策略。
    3.  读取 `SysRoleMenu` (关联 `SysMenu` 获取 ApiUrl)，批量生成 `p` 策略。
    4.  批量插入 Casbin。
*   **触发方式**: 在系统启动时 (`OnApplicationInitialization`) 自动检查，或在后台提供一个“刷新缓存”按钮手动触发。

### 4.3 前端对接规范
*   **菜单配置**: 务必在前端“菜单管理”页面，为每个需要鉴权的菜单/按钮配置正确的 **API 路径** 和 **请求方法**。这是连接两套数据的唯一纽带。

## 5. 总结
**不要试图用 Casbin 替代传统 RBAC 表来管理 UI 结构。**
保持现在的架构：
*   **UI 看传统表**。
*   **API 守门员看 Casbin 表**。
*   **Domain Service 负责同步两者**。

这就是企业级高性能权限系统的标准做法。
