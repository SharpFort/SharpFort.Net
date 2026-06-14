# AUDIT: Menu → casbin_rule 增量同步链路深度审查报告

> **创建时间**: 2026-06-14
> **关联模块**: `casbin-rbac` (MenuService / CasbinPolicyManager / RoleManager)
> **审查类型**: 权限同步链路完整性审计
> **严重程度**: **CRITICAL** — 涉及超管权限丢失风险 + 新建菜单权限黑洞

---

## 一、问题概述

### 1.1 触发场景

用户在"菜单管理"中新增了两个 API 菜单：

| 菜单名 | ApiUrl | ApiMethod |
|--------|--------|-----------|
| 导入模板 | `/api/app/template/import-templates` | POST |
| 导出模板 | `/api/app/template/export-templates` | POST |

**预期**: 菜单创建后（或后续编辑保存时），`casbin_rule` 表中应自动生成对应的 `p` 规则行。
**实际**: `casbin_rule` 表无任何新增行，导致非超管角色调用这两个接口时 Casbin 鉴权失败。

### 1.2 审查范围

本报告审计以下完整链路：
- Menu CRUD 操作 → casbin_rule 写入路径
- Role CRUD 操作 → casbin_rule 写入路径
- 系统启动时的策略加载机制
- 超管 `*,*` 通配符的生命周期保护

---

## 二、系统架构：casbin_rule 写入链路全景

### 2.1 核心组件关系

```
┌─────────────────────────────────────────────────────────────────────┐
│                        casbin_rule 写入入口                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────────┐    ┌──────────────────┐                      │
│  │   MenuService     │    │   RoleService     │                     │
│  │  (菜单 CRUD)      │    │  (角色 CRUD)      │                     │
│  └───────┬──────────┘    └───────┬──────────┘                      │
│          │                       │                                  │
│          │  CreateAsync          │  CreateAsync / UpdateAsync       │
│          │  UpdateAsync          │                                  │
│          │  DeleteAsync          │                                  │
│          ▼                       ▼                                  │
│  ┌───────────────┐    ┌──────────────────────┐                     │
│  │ 直接调用       │    │   RoleManager         │                    │
│  │ CasbinPolicy   │    │  .GiveRoleSetMenuAsync│                   │
│  │ Manager        │    └──────────┬───────────┘                    │
│  └───────┬───────┘               │                                  │
│          │                       │                                  │
│          ▼                       ▼                                  │
│  ┌──────────────────────────────────────────────────────────┐      │
│  │              CasbinPolicyManager                          │      │
│  │  SetRolePermissionsAsync(role, menus)   ← p 规则写入     │      │
│  │  InitAdminPermissionAsync(adminRole)    ← *,* 初始化     │      │
│  │  AddRoleForUserAsync / RemoveRoleForUserAsync ← g 规则   │      │
│  └────────────────────────┬─────────────────────────────────┘      │
│                           │                                         │
│                           ▼                                         │
│  ┌──────────────────────────────────────────────────────────┐      │
│  │                    casbin_rule 表                          │      │
│  │  p 规则: roleCode, domain, apiUrl, httpMethod             │      │
│  │  g 规则: userId, roleCode, domain                         │      │
│  └──────────────────────────────────────────────────────────┘      │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────┐      │
│  │  CasbinSeedService.MigrateAllAsync() ← 全量重建（手动）    │      │
│  │  CasbinMigrationService → API: POST /api/app/casbin-      │      │
│  │    migration/migrate-all                                  │      │
│  └──────────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 Casbin 鉴权模型（rbac_with_domains_model.conf）

```ini
[matchers]
m = (g(r.sub, p.sub, r.dom) && r.dom == p.dom && keyMatch2(r.obj, p.obj) && (r.act == p.act || p.act == "*"))
```

**关键含义**:
- `g(r.sub, p.sub, r.dom)`: 用户 → 角色 → 域 三级分组
- `keyMatch2(r.obj, p.obj)`: URL 路径匹配（支持 `:param` 动态路由）
- `r.act == p.act || p.act == "*"`: HTTP 方法匹配，`*` 为通配
- **超管权限依赖 `InitAdminPermissionAsync` 写入的 `p, admin, domain, *, *` 规则**

### 2.3 启动时加载流程

```
应用启动
  │
  ├─ [1] SharpFortCasbinRbacSqlSugarCoreModule.ConfigureServices
  │      └─ Enforcer(modelPath, adapter)  ← 创建 Singleton Enforcer
  │      └─ enforcer.LoadPolicy()          ← 从 casbin_rule 表加载所有规则
  │      └─ AutoSave = false               ← 禁用自动保存，所有写操作手动双写
  │
  ├─ [2] OnPostApplicationInitializationAsync
  │      └─ db.CodeFirst.InitTables<CasbinRule>()  ← 确保表结构存在
  │
  └─ [3] SharpFortCasbinRbacApplicationModule.OnApplicationInitializationAsync
         └─ menuService.WarmupCacheAsync()  ← 仅预热缓存，不涉及 casbin_rule
```

**结论**: 启动时 **不会** 自动执行 `CasbinSeedService.MigrateAllAsync()`，全量迁移仅通过手动 API 调用触发。

---

## 三、casbin_rule 写入路径全矩阵

### 3.1 写入路径清单

| # | 触发操作 | 文件:方法 | 写入类型 | 是否有超管保护 | 审查状态 |
|---|---------|----------|---------|:---:|:---:|
| P1 | 创建菜单 | `MenuService.CreateInternalAsync` | **无写入** | N/A | **BUG-C1** |
| P2 | 批量导入菜单 | `MenuService.PostImportExcelAsync` | **无写入** | N/A | **BUG-C1** |
| P3 | 修改菜单 | `MenuService.UpdateAsync` | p 规则（有条件） | **无** | **BUG-C2 + BUG-C3** |
| P4 | 删除菜单 | `MenuService.DeleteAsync` | p 规则（有条件） | **无** | **BUG-C2** |
| P5 | 创建/修改角色 | `RoleService → RoleManager.GiveRoleSetMenuAsync` | p 规则 | **无** | **BUG-C2** |
| P6 | 初始化超管 | `CasbinPolicyManager.InitAdminPermissionAsync` | `*,*` | 是（专属方法） | OK |
| P7 | 全量迁移 | `CasbinSeedService.MigrateAllAsync` | 全部 | 仅在全量重建时 | OK（但为手动触发） |

### 3.2 各路径详细代码追踪

---

#### P1: CreateInternalAsync — 菜单创建

**文件**: `MenuService.cs` 第 171-214 行

```csharp
private async Task<MenuGetOutputDto> CreateInternalAsync(
    MenuCreateInputVo input, bool invalidateCache, bool associateAdminRole = true)
{
    // ... 参数校验 ...
    var result = await base.CreateAsync(input);

    if (associateAdminRole)
    {
        Role? adminRole = await _roleRepository.GetFirstAsync(
            r => r.RoleCode == UserConst.AdminRolesCode);
        if (adminRole != null)
        {
            await _roleMenuRepository.InsertAsync(
                new RoleMenu { RoleId = adminRole.Id, MenuId = result.Id });
            // 注意：不调用 SetRolePermissionsAsync — 超管已有 *,* 通配符
        }
    }
    // ...
}
```

**问题**: 创建带 ApiUrl 的菜单时，仅建立 `RoleMenu` 关联（超管），**不写入任何 casbin_rule 行**。注释说明"超管已有 `*,*`"——这本身正确，但意味着 **非超管角色永远不会自动获得新菜单的权限**。

**影响**: 新建 API 菜单在 casbin_rule 中完全不存在，直到用户手动通过"角色管理"分配菜单。

---

#### P3: UpdateAsync — 菜单修改

**文件**: `MenuService.cs` 第 250-322 行

```csharp
public override async Task<MenuGetOutputDto> UpdateAsync(Guid id, MenuUpdateInputVo input)
{
    // ... 参数校验 ...

    Menu oldMenu = await _repository.GetByIdAsync(id);
    bool isApiChanged = (oldMenu.ApiUrl != input.ApiUrl
        || (oldMenu.ApiMethod?.ToUpper(...) ?? "") != (input.ApiMethod?.ToUpper(...) ?? ""));

    // ... 保存变更 ...

    if (isApiChanged)  // ← 条件门控
    {
        List<Guid> roleIds = await _roleMenuRepository._DbQueryable
            .Where(x => x.MenuId == id).Select(x => x.RoleId).Distinct().ToListAsync();

        if (roleIds.Count > 0)
        {
            List<Role> roles = await _roleRepository.GetListAsync(x => roleIds.Contains(x.Id));
            // ← 未过滤超管角色！

            foreach (Role role in roles)
            {
                await _casbinPolicyManager.SetRolePermissionsAsync(role, menus!);
            }
        }
    }
    return result;
}
```

**问题 1（BUG-C2）**: `roles` 列表 **包含超管角色**。`SetRolePermissionsAsync` 会先 `DELETE` 超管的所有 `p` 规则（包括 `*,*`），再逐条写入具体规则——**超管通配符被永久覆盖**。

**问题 2（BUG-C3）**: `isApiChanged` 条件过于严格。如果用户仅编辑菜单名称、图标、状态等非 API 字段，`isApiChanged == false`，整个同步块被跳过。用户"重启后编辑菜单想触发同步"失败的原因正在于此。

---

#### P5: RoleManager.GiveRoleSetMenuAsync — 角色分配菜单

**文件**: `RoleManager.cs` 第 24-54 行

```csharp
public async Task GiveRoleSetMenuAsync(List<Guid> roleIds, List<Guid> menuIds)
{
    await _roleMenuRepository.DeleteAsync(u => roleIds.Contains(u.RoleId));
    // ... 批量插入 RoleMenu ...

    List<Role> roles = await _repository.GetListAsync(r => roleIds.Contains(r.Id));
    List<Menu> menus = await _menuRepository.GetListAsync(m => menuIds.Contains(m.Id));

    foreach (Role role in roles)
    {
        await _casbinPolicyManager.SetRolePermissionsAsync(role, menus);
        // ← 同样未过滤超管角色
    }
}
```

**调用入口**:
- `RoleService.CreateAsync` 第 136 行: `GiveRoleSetMenuAsync([entity.Id], input.MenuIds ?? [])`
- `RoleService.UpdateAsync` 第 185 行: `GiveRoleSetMenuAsync([id], input.MenuIds ?? [])`

**问题（BUG-C2）**: 与 UpdateAsync 相同，如果通过角色管理页面对超管角色执行编辑操作，`SetRolePermissionsAsync` 会覆盖超管的 `*,*` 通配符。

---

#### SetRolePermissionsAsync — 核心写入方法

**文件**: `CasbinPolicyManager.cs` 第 159-204 行

```csharp
public async Task SetRolePermissionsAsync(Role role, List<Menu> menus)
{
    string roleSub = GetRoleSubject(role.RoleCode!);
    string domain = GetTenantDomain(role.TenantId);

    // 第一步：DELETE 该角色在指定域下的所有 p 规则
    await _roleRepository._Db.Deleteable<CasbinRule>()
        .Where(x => x.PType == "p" && x.V0 == roleSub && x.V1 == domain)
        .ExecuteCommandAsync();

    // 第二步：根据菜单列表构建新规则
    List<CasbinRule> newRules = new();
    foreach (Menu menu in menus)
    {
        if (string.IsNullOrWhiteSpace(menu.ApiUrl)) continue;
        string methods = string.IsNullOrWhiteSpace(menu.ApiMethod) ? "*" : menu.ApiMethod;
        newRules.Add(new CasbinRule
            { PType = "p", V0 = roleSub, V1 = domain, V2 = menu.ApiUrl, V3 = methods });
    }

    if (newRules.Count > 0)
        await _roleRepository._Db.Insertable(newRules).ExecuteCommandAsync();

    // 第三步：同步 Enforcer 内存
    Func<Task> syncAction = async () =>
    {
        await _writeLock.WaitAsync();
        try
        {
            await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);
            if (menus.Count > 0)
            {
                List<List<string>> policies = new();
                foreach (Menu menu in menus) { /* ... */ }
                await _enforcer.AddPoliciesAsync(policies);
            }
        }
        finally { _writeLock.Release(); }
    };
    // ... UOW / SyncOrFallback ...
}
```

**关键缺陷**: 此方法 **不检查角色是否为超管**。如果 `role.RoleCode == "admin"`:
1. 第 164 行: `DELETE` 删除 `admin` 的所有 `p` 规则，**包括 `*,*`**
2. 第 168-175 行: 仅写入 `menus` 中有 ApiUrl 的具体规则
3. **结果**: 超管从"拥有所有接口访问权"退化为"仅拥有 RoleMenu 中已分配菜单的访问权"

---

## 四、发现的 Bug 清单

### BUG-C1: 菜单创建路径零 casbin_rule 写入（严重度: HIGH）

| 维度 | 说明 |
|------|------|
| **位置** | `MenuService.CreateInternalAsync` 第 193-206 行 |
| **现象** | 新建带 ApiUrl 的菜单后，`casbin_rule` 表无任何对应行 |
| **根因** | 创建时仅关联超管 RoleMenu，注释明确说明"不调用 SetRolePermissionsAsync" |
| **影响** | 非超管角色无法通过 Casbin 鉴权访问新建菜单的 API |
| **触发条件** | 任何通过菜单管理创建的 API 类型菜单 |
| **为什么其他接口正常** | 其他接口的菜单记录在初始化数据中已存在，且通过角色管理分配给了相关角色，触发了 `GiveRoleSetMenuAsync` 路径写入 casbin_rule |

**复现步骤**:
1. 菜单管理 → 新建菜单，填入 ApiUrl = `/api/app/template/import-templates`, ApiMethod = `POST`
2. 查询 `casbin_rule` 表: `SELECT * FROM casbin_rule WHERE v2 LIKE '%import-templates%'`
3. **预期**: 至少有 1 条 `p` 规则行
4. **实际**: 0 条

---

### BUG-C2: SetRolePermissionsAsync 未保护超管 `*,*` 通配符（严重度: CRITICAL）

| 维度 | 说明 |
|------|------|
| **位置** | `CasbinPolicyManager.SetRolePermissionsAsync` 第 159-204 行 |
| **现象** | 超管的 `p, admin, domain, *, *` 规则被删除，替换为具体菜单规则 |
| **根因** | 方法不检查 `role.RoleCode`，对所有角色执行相同的"删除全部 → 重建"逻辑 |
| **影响** | 超管丢失全局通配符权限，退化为仅拥有已分配菜单的权限 |
| **触发条件** | 以下任意操作会触发: (1) `MenuService.UpdateAsync` 且 `isApiChanged` 且超管在 roleIds 中; (2) `MenuService.DeleteAsync` 且超管在 affectedRoleIds 中; (3) `RoleService.UpdateAsync` 对超管角色执行编辑 |

**复现步骤**:
1. 确认 `casbin_rule` 中存在超管通配符: `SELECT * FROM casbin_rule WHERE p_type='p' AND v0='admin' AND v2='*'` → 1 条
2. 进入菜单管理 → 找到任意一个已关联超管（几乎所有菜单都关联了超管）的 API 菜单
3. 修改其 ApiUrl（如末尾加 `/test`）→ 保存
4. 再次查询: `SELECT * FROM casbin_rule WHERE p_type='p' AND v0='admin' AND v2='*'`
5. **预期**: 仍然 1 条（`*,*` 应被保留）
6. **实际**: 0 条（`*,*` 已被删除，超管权限丢失）

**波及范围**:
- `MenuService.UpdateAsync` 第 293 行: `roles` 未过滤超管
- `MenuService.DeleteAsync` 第 339 行: `roles` 未过滤超管
- `RoleManager.GiveRoleSetMenuAsync` 第 49 行: `roles` 未过滤超管

---

### BUG-C3: UpdateAsync 同步条件过于严格（严重度: MEDIUM）

| 维度 | 说明 |
|------|------|
| **位置** | `MenuService.UpdateAsync` 第 272、283 行 |
| **现象** | 编辑菜单保存后，casbin_rule 未同步更新 |
| **根因** | `isApiChanged` 仅在 ApiUrl 或 ApiMethod 变化时为 true；编辑名称/图标/状态等不会触发同步 |
| **影响** | 用户无法通过"编辑保存"操作触发权限同步 |
| **触发条件** | 编辑菜单的非 API 字段后保存 |

**复现步骤**:
1. 在 BUG-C1 的基础上，打开刚创建的菜单
2. 修改菜单名称（不改 ApiUrl 和 ApiMethod）→ 保存
3. 查询 `casbin_rule`
4. **预期**: 有对应规则行
5. **实际**: 仍然 0 条（`isApiChanged == false`，同步块被跳过）

---

## 五、import-templates / export-templates 具体 Bug 链路

```
用户操作                              系统行为                           casbin_rule 状态
═══════════                          ═══════════                         ═══════════════

[1] 新建菜单                          CreateInternalAsync                 无变化
    "导入模板"                         ├─ Menu 表: 插入行 ✅
    ApiUrl=/api/app/template/         ├─ RoleMenu: 仅超管 ✅
    import-templates                  └─ casbin_rule: 无任何写入 ❌       ← BUG-C1
    ApiMethod=POST

[2] 打开菜单编辑                      UpdateAsync                         无变化
    修改名称或描述                     ├─ isApiChanged = false ✅
    → 保存                            └─ 同步块被跳过 ❌                  ← BUG-C3

[3] 打开菜单编辑                      UpdateAsync                         仍然无变化
    修改 ApiUrl                       ├─ isApiChanged = true ✅
    → 保存                            ├─ 查 RoleMenu → 仅超管
                                      ├─ roles = [超管]
                                      └─ SetRolePermissionsAsync(超管, menus)
                                         ├─ DELETE 超管所有 p 规则        ← BUG-C2 触发!
                                         ├─ 写入具体菜单规则
                                         └─ *,* 通配符永久丢失 ❌❌❌

    即使用户确实触发了同步，           超管权限被降级为仅已分配菜单
    结果更糟：超管权限丢失！           其他非超管角色: 仍无对应规则
```

---

## 六、风险矩阵

| Bug | 严重度 | 触发概率 | 影响范围 | 数据可恢复性 |
|-----|:------:|:-------:|---------|:----------:|
| **BUG-C1** 菜单创建零写入 | HIGH | 100%（每次新建 API 菜单） | 新建 API 菜单对非超管角色不可访问 | 需手动角色分配 |
| **BUG-C2** 超管 `*,*` 被覆盖 | **CRITICAL** | 中高（任何修改超管关联菜单的 API 字段时） | **超管丢失全局权限，可能锁死系统** | 需全量迁移或手动恢复 |
| **BUG-C3** 编辑不同步 | MEDIUM | 100%（每次编辑非 API 字段） | 菜单信息变更后权限不更新 | 改 API 字段可间接触发 |

---

## 七、超管 `*,*` 通配符生命周期分析

```
创建阶段                    运行阶段                              危险操作
═══════                   ════════                             ════════

InitAdminPermissionAsync   正常运行：*,* 匹配所有请求
  │                         ┌────────────────────────┐
  │ p, admin, domain, *, *  │ Enforcer 内存中         │
  │ 写入 casbin_rule        │ 每次鉴权都命中          │
  └──────────────────────►  │ 此规则                  │
                            └──────────┬─────────────┘
                                       │
                          ┌────────────┴──────────────────────────┐
                          │ SetRolePermissionsAsync(admin, menus)  │
                          │                                        │
                          │ 1. DELETE WHERE v0='admin' → 删除 *,*  │
                          │ 2. INSERT 具体菜单规则                  │
                          │ 3. Enforcer.RemoveFilteredPolicyAsync  │
                          │ 4. Enforcer.AddPoliciesAsync(具体规则)  │
                          │                                        │
                          │ 结果: *,* 永久丢失                      │
                          │ 超管退化为普通角色权限                    │
                          └────────────────────────────────────────┘

恢复方式:
  - 调用 CasbinMigrationService.MigrateAllAsync() 全量重建
    （但全量重建也不保护 *,*——它从 RoleMenu 重建，如果超管的 RoleMenu
    不包含所有菜单，重建后 *,* 仍然不存在）
  - 手动调用 InitAdminPermissionAsync() 恢复
```

---

## 八、修复方案建议（待审查）

### 方案 A: SetRolePermissionsAsync 增加超管保护（推荐，改动最小）

在 `CasbinPolicyManager.SetRolePermissionsAsync` 方法中：
- 检测 `role.RoleCode == UserConst.AdminRolesCode`
- 如果为超管，在 DELETE 后始终重新写入 `*,*` 规则
- 同时在 Enforcer 内存中追加 `*,*`

**优点**: 一处修改，覆盖所有调用路径（P3/P4/P5）
**风险**: 需确保 `*,*` 不会与具体规则冲突（Casbin matcher 中 `p.act == "*"` 已支持通配）

### 方案 B: 调用端过滤超管（纵深防御）

在 `MenuService.UpdateAsync`、`MenuService.DeleteAsync`、`RoleManager.GiveRoleSetMenuAsync` 中：
- 查询 roles 时排除 `RoleCode == "admin"` 的角色
- 超管不参与逐菜单同步

**优点**: 调用端显式控制，逻辑清晰
**缺点**: 需在多处修改，遗漏任一处仍有风险

### 方案 C: 菜单创建时为非超管角色触发同步

在 `MenuService.CreateInternalAsync` 中：
- 创建后查找所有 `State == true` 的非超管角色
- 为新菜单自动写入 casbin_rule

**优点**: 新建菜单立即对所有角色可用
**缺点**: 改变业务语义——不是所有新菜单都应该对所有角色开放
**更合理的替代**: 在角色管理页面分配菜单时自动触发（现有 `GiveRoleSetMenuAsync` 已覆盖，但需确保 BUG-C2 已修复）

### 推荐策略

1. **优先修复方案 A**（保护超管 `*,*`）— 消除 CRITICAL 风险
2. **补充修复方案 B**（调用端过滤）— 纵深防御
3. **BUG-C1 暂不自动修复** — 新菜单应通过角色管理手动分配，这是合理的权限管控流程
4. **BUG-C3 评估需求** — 是否需要在编辑非 API 字段时也触发同步？当前设计有其合理性（仅 API 变更才影响权限规则）

---

## 九、验证检查清单

修复完成后，按以下清单逐项验证：

| # | 验证项 | 预期结果 |
|---|--------|---------|
| V1 | 新建 API 菜单后查 casbin_rule | 超管 `*,*` 仍存在 |
| V2 | 修改已关联超管的 API 菜单的 ApiUrl | 超管 `*,*` 仍存在 |
| V3 | 修改已关联超管的 API 菜单的名称 | 超管 `*,*` 仍存在 |
| V4 | 通过角色管理为普通角色分配新菜单 | casbin_rule 有该角色 + 该菜单的 p 规则 |
| V5 | 编辑超管角色（角色管理页面） | 超管 `*,*` 仍存在 |
| V6 | 删除一个已关联超管的 API 菜单 | 超管 `*,*` 仍存在 |
| V7 | 全量迁移后 | 超管 `*,*` 仍存在 + 所有角色权限正确 |
| V8 | 非超管角色调用 import-templates | Casbin 鉴权通过（需先分配菜单给该角色） |

---

## 十、附录：关键文件索引

| 文件 | 路径 | 职责 |
|------|------|------|
| MenuService.cs | `module/casbin-rbac/SharpFort.CasbinRbac.Application/Services/System/MenuService.cs` | 菜单 CRUD + 缓存管理 |
| RoleService.cs | `module/casbin-rbac/SharpFort.CasbinRbac.Application/Services/System/RoleService.cs` | 角色 CRUD + 菜单分配 |
| RoleManager.cs | `module/casbin-rbac/SharpFort.CasbinRbac.Domain/Managers/RoleManager.cs` | 角色-菜单关联持久化 + Casbin 同步 |
| CasbinPolicyManager.cs | `module/casbin-rbac/SharpFort.CasbinRbac.Domain/Managers/CasbinPolicyManager.cs` | casbin_rule 双写（DB + Enforcer 内存） |
| CasbinSeedService.cs | `module/casbin-rbac/SharpFort.CasbinRbac.Domain/Managers/CasbinSeedService.cs` | 全量迁移（手动触发） |
| rbac_with_domains_model.conf | `module/casbin-rbac/SharpFort.CasbinRbac.Domain/rbac_with_domains_model.conf` | Casbin 鉴权模型定义 |
| UserConst.cs | `module/casbin-rbac/SharpFort.CasbinRbac.Domain.Shared/Consts/UserConst.cs` | 常量定义（AdminRolesCode = "admin"） |

---

*本报告仅包含审查分析结果，不涉及任何代码修改。所有修复方案需经人工审查确认后方可实施。*
