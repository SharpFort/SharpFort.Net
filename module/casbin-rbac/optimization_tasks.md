# SharpFort Casbin RBAC 性能优化实施与审查任务清单

> **状态**：✅ 全部完成（2026-06-06）
> **最后更新**：步骤 0-4 已实施

本清单旨在指导开发人员对 SharpFort 系统进行高并发性能优化与 Bug 修复，开发人员完成以下修复后，将由 AI 助手进行代码审查。

---

### ✅ 步骤 0：新建共享缓存扩展工具
*   **新建文件**：`module/casbin-rbac/SharpFort.CasbinRbac.Application/Extensions/DistributedCacheExtensions.cs`
*   **状态**：✅ 已完成

### ✅ 步骤 1：修复 P0 登录接口瓶颈与 Browser 字段映射
*   **目标文件**：`framework/SharpFort.Core/Helper/ClientInfoHelper.cs`
*   **状态**：✅ 已完成
    *   `_uaParser` 静态单例化
    *   `Device.Family` → `UA.Family` 修正

### ✅ 步骤 2：实现 `UserService` 缓存与多触发点失效机制
*   **目标文件**：`module/casbin-rbac/SharpFort.CasbinRbac.Application/Services/System/UserService.cs`
*   **状态**：✅ 已完成
    *   6 处 `InvalidateUserCache()`：Create / Update / UpdateProfile / UpdateState / Delete(Guid) / Delete(IEnumerable)
    *   `GetSelectDataListAsync` Redis 缓存重写
    *   `DeleteAsync(IEnumerable<Guid>)` 批量删除重写（含 Casbin 清理）

### ✅ 步骤 3：实现 `RoleService` 缓存与多触发点失效机制
*   **目标文件**：`module/casbin-rbac/SharpFort.CasbinRbac.Application/Services/System/RoleService.cs`
*   **状态**：✅ 已完成
    *   5 处 `InvalidateRoleCache()`：Create / Update / UpdateState / UpdateDataScope / Delete(IEnumerable)
    *   `GetSelectDataListAsync` Redis 缓存重写

### ✅ 步骤 4：优化 `MenuService` 菜单创建与批量导入性能
*   **目标文件**：`module/casbin-rbac/SharpFort.CasbinRbac.Application/Services/System/MenuService.cs`
*   **状态**：✅ 已完成
    *   `CreateInternalAsync` 增加 `associateAdminRole` 参数
    *   `PostImportExcelAsync` 批量 RoleMenu 插入
    *   未调用 `SetRolePermissionsAsync`（保护超管 *,* 通配符）

---

### 提测与审查要求
1.  **编译验证**：修改完成后，须保证 `.sln` 能正常编译，且 RBAC/Auth 相关的单元测试全部通过。
2.  **代码审查**：提交修复代码（PR 或 Git Diff）后，我将对其进行深度审查，确认无“锁竞争、二次查询、事务逃逸以及死锁”等隐患。
