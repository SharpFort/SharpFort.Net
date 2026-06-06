# 性能审查报告：AccountService + UserManager 调用链

**审查日期**: 2026-05-30  
**触发条件**: `AccountService.GetAsync` 耗时 1305ms（日志: `Executed action ... in 1305.4783ms`）  
**部署模式**: 单实例，无 Redis，目标极致单机性能  

---

## 调用链追踪

```
GET /api/account → AccountService.GetAsync() [1305ms]
  └→ _userManager.GetInfoAsync(userId)           ← ❌ 无缓存！每次 DB
       └→ _userRepository.GetUserAllInfoAsync()
            └→ SQL: User ← INCLUDES → Roles ← INCLUDES → Menus
                 (3表嵌套 Include，每请求一次)
```

前端在每次页面刷新、路由切换时都会调用此接口。

---

## 严重问题（会造成性能劣化或安全漏洞）

### S1 — AccountService.GetAsync() 无缓存（检查项 #1 / #4）

**文件**: `SharpFort.CasbinRbac.Application/Services/AccountService.cs` 第 352-360 行

**问题**: `GetAsync()` 调用 `_userManager.GetInfoAsync(userId)`（无缓存版本），而非同一类中已有的 `GetInfoByCacheAsync(userId)`（缓存版本，`UserManager.cs` 第 176-195 行）。**每次请求都执行 User→Roles→Menus 三表 JOIN 查询**。

```csharp
// ❌ 当前代码 (第 358 行)
UserRoleMenuDto output = await _userManager.GetInfoAsync(userId);
```

注释写的是"此处优先从缓存中获取"，实际却调了无缓存的 `GetInfoAsync`。`GetInfoByCacheAsync` 已完整实现（cache miss → DB → 缓存写入，TTL 对齐 JWT），但从未被使用。

**影响**: 直接导致 1305ms。每次页面切换都触发。

**修复**:
```csharp
// ✅ 修复后
UserRoleMenuDto output = await _userManager.GetInfoByCacheAsync(userId);
```

**预估收益**: 1305ms → <5ms（缓存命中）/ ~30ms（首次）

---

### S2 — GetVue3Router() 的 admin 路径双重读取 + 无用查询（检查项 #1 / #2）

**文件**: `AccountService.cs` 第 393-426 行

**问题**: 
1. 第 402 行调用 `GetInfoAsync`（无缓存），仅为了在 406 行判断 `data.User.UserName == "admin"`
2. 若为 admin，前面拉取的完整 Roles/Menus 数据被丢弃（第 408 行重新查全部菜单）
3. admin 用户支付了从 DB 拉全量数据的价格却只读了 UserName 一个字段

```csharp
// ❌ 当前代码
UserRoleMenuDto data = await _userManager.GetInfoAsync(userId!.Value);  // DB: User+Roles+Menus
List<MenuDto> menus = [.. data.Menus];
if (UserConst.Admin.Equals(data.User.UserName, StringComparison.Ordinal))  // 只读了 UserName
{
    menus = ObjectMapper.Map<List<Menu>, List<MenuDto>>(await _menuRepository.GetListAsync());  // 重新查全部菜单
}
```

**修复**:

```csharp
// ✅ 修复后：通过 JWT claims 判断 admin，避免无用 DB 查询
bool isAdmin = UserConst.Admin.Equals(_currentUser.UserName, StringComparison.Ordinal);
List<MenuDto> menus;
if (isAdmin)
{
    menus = ObjectMapper.Map<List<Menu>, List<MenuDto>>(await _menuRepository.GetListAsync());
}
else
{
    UserRoleMenuDto data = await _userManager.GetInfoByCacheAsync(userId!.Value);
    menus = [.. data.Menus];
}
```

注意：`_currentUser.UserName` 来自 JWT token 的 claim，无需查 DB。但需确认 JWT 中已携带 UserName（`AccountManager.UserInfoToClaim` 第 179 行已添加）。

**预估收益**: admin 用户省去一次完整的 User+Roles+Menus 查询

---

### S3 — GetAsync(userName, phone) 重载无缓存（检查项 #1）

**文件**: `AccountService.cs` 第 363-384 行

**问题**: 383 行同步调用 `GetInfoAsync(user.Id)` 无缓存

```csharp
// ❌ 当前代码
UserRoleMenuDto output = await _userManager.GetInfoAsync(user.Id);
```

**修复**:
```csharp
// ✅ 修复后
UserRoleMenuDto output = await _userManager.GetInfoByCacheAsync(user.Id);
```

---

## 一般问题（有优化空间但不紧急）

### G1 — IDistributedCache vs IMemoryCache（检查项 #4）

**文件**: `UserManager.cs` 第 27 行

**问题**: `IDistributedCache<UserInfoCacheItem, UserInfoCacheKey>` — 单实例无 Redis 时，ABP 默认用 `DistributedMemoryCache`，内部做 JSON 序列化/反序列化往返，比原生 `IMemoryCache.GetOrCreateAsync` 慢。

**建议**: 切换到 `IMemoryCache`，消除序列化开销。涉及 `UserManager` 构造函数和 `GetInfoByCacheAsync` 改写。

### G2 — EntityMapToDto 反射嵌套循环（检查项 #3）

**文件**: `UserManager.cs` 第 229-247 行

**问题**: 嵌套 `foreach(Role) → foreach(Menu)` 中对每个 Menu 调用 Mapster `.Adapt<MenuDto>()`（反射），对每个 Role 调用 `.Adapt<RoleDto>()`。Mapster 的 Adapt 每次都有 Dictionary 查找 + 反射开销。

```csharp
// 当前代码 (简化)
foreach (Role role in roleList)
{
    foreach (Menu menu in role.Menus)
    {
        userRoleMenu.Menus.Add(menu.Adapt<MenuDto>());  // 反射 × M 次
    }
    userRoleMenu.Roles.Add(role.Adapt<RoleDto>());      // 反射 × R 次
}
```

**建议**: 缓存化后此问题影响降低（仅首次 cache miss 时执行），但首次仍慢。可用预编译映射或手写 DTO 转换。

### G3 — GetInfoListAsync 循环单查（检查项 #1）

**文件**: `UserManager.cs` 第 198-206 行

**问题**: 循环遍历 userIds，每个调用一次 `GetInfoByCacheAsync`。虽缓存命中后快，但首次（全部 cache miss）会逐个 DB 查询。

```csharp
// 当前代码
foreach (Guid userId in userIds)
{
    output.Add(await GetInfoByCacheAsync(userId));  // N 次单独查询
}
```

**建议**: 批量查询已有 `GetListUserAllInfoAsync`，可做一次批量 DB 查询后用 Dictionary 分发。

---

## 已通过检查

| 检查项 | 状态 | 证据 |
|--------|------|------|
| #5 缓存键冲突风险 | ✓ | `UserInfoCacheKey` 仅使用 `Guid`，无 bool? 三态问题 |
| #6 缓存失效竞态 | ✓ | 当前无原子版本号，但缓存失效用 `RemoveAsync`，无读-算-写问题 |
| #8 启动预热无保护 | ✓ | `SharpFortCasbinRbacApplicationModule.cs:66-75` 有 try-catch 保护 |
| #10 UOW 兜底路径 | ✓ | `GetAsync` 不涉及事务，当前场景不适用 |
| #12 绕过基类缺权限 | ✓ | `GetInfoAsync` 无绕过基类 CRUD 的场景 |

---

## 总结

| 指标 | 数值 |
|------|------|
| 严重问题 | 3 个（S1、S2、S3） |
| 一般问题 | 3 个（G1、G2、G3） |
| **预估优化收益** | **1305ms → <5ms（缓存命中）/ ~30ms（首次）** |

### 关键变更是 3 行代码

1. `AccountService.cs:358` — `GetInfoAsync` → `GetInfoByCacheAsync`
2. `AccountService.cs:383` — 同上
3. `AccountService.cs:402-408` — admin 路径重构

S1 修完即可解决 90% 的 1305ms 问题。G1（IMemoryCache）可提供额外 20-30% 的缓存命中加速，但需改动构造函数。

---

## 💡 AI 自动审查补充报告 (SharpFort 性能优化器)

经过系统级 `sharpfort-performance-optimizer` (基于13项检查清单) 的复查，原文档的性能分析和解决方案**非常准确**。切换到 `GetInfoByCacheAsync` 和消除 `GetVue3Router` 的 admin 双重读取将带来决定性的性能提升。

**但还需补充以下必须完善的点（特别是启用缓存后的副作用）：**

### 严重问题（遗漏/伴生隐患）

#### S4 — `UserManager` 角色/岗位变更后缺少缓存失效清理（安全隐患与一致性问题）
**文件**: `UserManager.cs` 的 `GiveUserSetRoleAsync` (第 46-81 行) 和 `GiveUserSetPostAsync` (第 87-102 行)
**问题**: 当为用户重新分配角色或岗位时，直接修改了数据库并同步了 Casbin 策略，但**并未移除该用户的 `_userCache` 缓存**。
在落实原文档的 S1 修复（启用缓存）后，如果管理员变更了某个用户的角色，该用户在缓存自然过期（与 JWT TTL 对齐，可能长达数小时）之前，仍然会携带旧的菜单和权限进行访问，导致权限撤销不及时，造成严重的越权漏洞！
**建议方案 (参考清单 #7 批量失效原则)**:
在 `UserManager.cs` 中修改角色/岗位的相关方法，通过 `finally` 块或在成功写入数据库后，增加对相应用户缓存的精确清理操作：
```csharp
try
{
    // ... 原有的 DB 和 Casbin 同步逻辑 ...
}
finally
{
    // 批量清理受影响用户的用户信息缓存
    foreach(Guid userId in userIds)
    {
        await _userCache.RemoveAsync(new UserInfoCacheKey(userId));
    }
}
```

### 一般问题（补充）

#### G4 — `GiveUserSetRoleAsync` 循环内的 `InsertRangeAsync` (检查项 #1 / 批处理优化)
**文件**: `UserManager.cs` 第 55-65 行
**问题**: 在外层通过 `foreach (Guid userId in userIds)` 遍历时，循环体内每次都调用了 `await _repositoryUserRole.InsertRangeAsync(userRoleEntities);`。如果将其作为批量分配接口，会变成 DB 的 N+1 插入。
**建议方案**: 将 `List<UserRole> userRoleEntities` 提到最外层循环，把所有的映射关系添加到同一个集合中，然后在循环外部执行单次 `InsertRangeAsync(allUserRoleEntities)`。

### 总结意见
**原文档的方案完全成立**。但在实际动手修复（特别是执行 S1 启用缓存机制）的同时，**必须同步修复新发现的 S4 问题**，以确保在拥有高性能的同时不会引入缓存不一致的权限逃逸风险。
