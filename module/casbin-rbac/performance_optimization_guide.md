# SharpFort 项目性能优化指南

从 Casbin-RBAC 模块深度优化中提炼的通用原则，适用于全项目推广。

---

## 一、数据库访问

### 1.1 N+1 查询消除

**问题**：循环内逐个查询数据库，N 个实体触发 2N+ 次 DB 交互。

```csharp
// ❌ N+1：每个角色独立查询 RoleMenu 和 Menu
foreach (var role in roles)
{
    var menuIds = await _repo.Where(x => x.RoleId == role.Id).ToListAsync();  // N 次
    var menus = await _menuRepo.GetListAsync(x => menuIds.Contains(x.Id));    // N 次
}
```

```csharp
// ✅ 批量：固定 4 次查询搞定
var roleIds = ...;                                            // 1 次
var roles = await _roleRepo.GetListAsync(x => roleIds.Contains(x.Id));  // 2 次
var mappings = await _repo.Where(x => roleIds.Contains(x.RoleId))...;  // 3 次
var menus = await _menuRepo.GetListAsync(x => allMenuIds.Contains(x.Id)); // 4 次
// 内存归类分发
```

**识别方法**：在循环体内看到 `await ...Repository` 或 `await ..._DbQueryable` 即为 N+1 信号。

---

### 1.2 消除框架基类导致的重复 DB 读取

**问题**：ABP 的 `base.UpdateAsync(id, input)` 内部会 `GetEntityByIdAsync(id)` + `MapToEntityAsync` + `UpdateAsync`。如果你在调用前已经加载了实体，基类会做第二次读取。

```csharp
// ❌ 双重读取
var entity = await _repository.GetByIdAsync(id);  // 第 1 次读
var result = await base.UpdateAsync(id, input);    // base 内部第 2 次读
```

```csharp
// ✅ 绕过基类，直接在已加载实体上操作
var entity = await _repository.GetByIdAsync(id);        // 唯一 1 次读
await CheckUpdatePolicyAsync();                          // 权限校验（不能省）
await CheckUpdateInputDtoAsync(entity, input);          // 输入校验（不能省）
await MapToEntityAsync(input, entity);                  // 内存映射
await _repository.UpdateAsync(entity, autoSave: true);  // 直接写
```

**关键原则**：绕过基类时，必须手动补齐基类中的权限和校验调用，否则引入安全漏洞。

---

### 1.3 内存字典加速

**问题**：`list.First(x => x.Id == key)` 在循环内是 O(n) 查找。

```csharp
// ❌ 每次 O(n)
g.Select(x => allMenus.First(m => m.Id == x.MenuId)).ToList()
```

```csharp
// ✅ 先建 Dictionary，O(1) 查找
var menuDict = allMenus.ToDictionary(m => m.Id);
g.Select(x => menuDict.TryGetValue(x.MenuId, out var m) ? m : null)
 .Where(m => m != null).ToList()
```

---

## 二、缓存设计

### 2.1 缓存技术选型：单实例优先用本地内存

**问题**：`IDistributedCache<T>` 在有 Redis 的 ABP 模块中走网络调用（序列化 + 往返 2-5ms），无法达到 <0.1ms 目标。

| 方案 | 延迟 | 适用场景 |
|------|------|----------|
| `IMemoryCache` | <0.1ms | 单实例/数据一致性要求不高 |
| `IDistributedCache` (Redis) | 2-5ms | 多实例/需要跨节点共享 |
| `ConcurrentDictionary` | <0.01ms | 极简场景/手动控制生命周期 |

```csharp
// ✅ 单实例场景首选
public class MyService(IMemoryCache memoryCache) : ...
{
    return await _memoryCache.GetOrCreateAsync(key, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
        return await QueryFromDbAsync();
    });
}
```

**注意**：ABP 的 `AbpCachingModule` 已自动注册 `IMemoryCache`，无需手动 `AddMemoryCache()`。

---

### 2.2 缓存键设计：警惕可空类型的隐式合并

**问题**：`Nullable<T>` 的 `??` 操作可能让 `null` 和某个合法值合并成同一个键。

```csharp
// ❌ null 和 false 都变成 "False"——严重的数据泄露风险
string state = input.State ?? false;  // bool? → bool 丢失了一个状态
string key = $"{prefix}:{state}";
```

```csharp
// ✅ 显式三态映射
string stateKey = input.State?.ToString() ?? "all";
// State=true → "True", State=false → "False", State=null → "all"
```

**检查清单**：所有用于缓存键的 `bool?`、`int?`、`Enum?` 等字段，都需要显式处理 `null` 分支。

---

### 2.3 原子版本号：缓存失效的正确姿势

**问题**：分布式缓存的"读-算-写"不是原子操作，存在竞态。

```csharp
// ❌ 竞态窗口：两个写操作可能读到同一版本号
var v = await cache.GetAsync(versionKey);      // 读
await cache.SetAsync(versionKey, v + 1);        // 算+写
```

```csharp
// ✅ CPU 级原子操作，零开销无竞态
private static long _schemaVersion = 1;
private void InvalidateCache() => Interlocked.Increment(ref _schemaVersion);
private string GetCacheKey() => $"v{Interlocked.Read(ref _schemaVersion)}:{...}";
```

**适用前提**：单实例部署。多实例需配合消息广播机制。

---

### 2.4 批量操作的缓存失效：try-finally 保证最终一致

**问题**：批量导入中途异常时，已写入 DB 的数据不会被缓存感知。

```csharp
// ❌ 第 5 条抛异常，Invalidate 永远不执行
foreach (var item in items)
    await CreateInternalAsync(item, invalidateCache: false);
InvalidateMenuCache();
```

```csharp
// ✅ try-finally 保证无论如何都失效一次
try
{
    foreach (var item in items)
        await CreateInternalAsync(item, invalidateCache: false);
}
finally
{
    InvalidateCache();  // 必定执行
}
```

---

### 2.5 缓存预热：尽力而为，不阻断启动

**问题**：启动时预热缓存，DB 未就绪则应用崩溃。

```csharp
// ❌ 启动失败
await menuService.WarmupCacheAsync();
```

```csharp
// ✅ 尽力而为
try
{
    await menuService.WarmupCacheAsync();
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "缓存预热失败，将在首次请求时自动加载");
}
```

---

## 三、锁与并发

### 3.1 锁范围最小化：I/O 在锁外，内存操作在锁内

**问题**：锁住整个 DB 写入过程，并发写吞吐极低。

```csharp
// ❌ 锁持有整个 DB 操作（10-100ms）
await _writeLock.WaitAsync();
try
{
    await db.Deleteable<Rule>().ExecuteCommandAsync();   // DB I/O 在锁内
    await db.Insertable(rules).ExecuteCommandAsync();    // DB I/O 在锁内
}
finally { _writeLock.Release(); }
```

```csharp
// ✅ DB 写入在锁外（利用 DB 事务隔离），锁仅保护纯内存操作（<1μs）
// 第一步：DB 写入（无锁）
await db.Deleteable<Rule>().Where(...).ExecuteCommandAsync();
await db.Insertable(rules).ExecuteCommandAsync();

// 第二步：事务提交后，内存增量操作（加锁）
uow.OnCompleted(async () =>
{
    await _writeLock.WaitAsync();
    try { await enforcer.AddPoliciesAsync(...); }  // 纯内存，微秒级
    finally { _writeLock.Release(); }
});
```

**原则**：锁内**不应有任何 I/O 操作**（网络、磁盘、DB）。锁只保护非线程安全的内存数据结构。

---

### 3.2 UOW 感知的回退路径

**问题**：`?.OnCompleted` 在无事务环境静默跳过，导致内存不同步。

```csharp
// ❌ 无 UOW 时内存同步被跳过
_unitOfWorkManager.Current?.OnCompleted(syncAction);
```

```csharp
// ✅ 始终有兜底
IUnitOfWork? uow = _unitOfWorkManager.Current;
if (uow != null)
    uow.OnCompleted(syncAction);
else
    await SyncOrFallback(syncAction);  // 直接执行，失败则全量兜底
```

---

## 四、第三方 API 的语义陷阱

### 4.1 Casbin：空字符串是字面量，不是通配符

**问题**：认为 `""` 等于"匹配任意"。

```csharp
// ❌ "" 是字面量匹配——永远删不掉任何规则（roleCode 不可能为空串）
await enforcer.RemoveFilteredGroupingPolicyAsync(0, userId, "", domain);
// 匹配 V0=userId AND V1="" AND V2=domain —— V1 永远不为空！
```

```csharp
// ✅ 方案1：枚举旧规则，过滤后逐个删除（精确，适合少量规则）
var oldRules = enforcer.GetFilteredGroupingPolicy(0, userId)
    .Where(r => r[2] == domain)
    .ToList();  // 先物化快照，避免枚举中修改集合
foreach (var rule in oldRules)
    await enforcer.RemoveGroupingPolicyAsync(rule[0], rule[1], rule[2]);

// ✅ 方案2：单 tenant 下匹配 V0 即可
await enforcer.RemoveFilteredGroupingPolicyAsync(0, userId);
```

**教训**：使用任何第三方 API 前，必须确认其参数匹配语义（字面量 vs 通配符 vs 正则）。

---

### 4.2 Casbin：枚举策略集合时不能边遍历边修改

**问题**：`GetFilteredGroupingPolicy` 返回的是 Enforcer 内部集合的引用。在遍历过程中调用 `RemoveGroupingPolicyAsync` 修改底层集合，触发运行时异常。

```csharp
// ❌ Collection was modified
foreach (var rule in enforcer.GetFilteredGroupingPolicy(0, sub))
    await enforcer.RemoveGroupingPolicyAsync(...);
```

```csharp
// ✅ 先创建快照
var oldRules = enforcer.GetFilteredGroupingPolicy(0, sub)
    .Select(r => r.ToList()).ToList();  // 物化
foreach (var rule in oldRules)
    await enforcer.RemoveGroupingPolicyAsync(rule[0], rule[1], rule[2]);
```

---

## 五、架构与安全

### 5.1 接口完整性：跨层调用必须声明在接口中

**问题**：类上新增 public 方法，但接口未声明，通过接口注入的调用方编译失败。

```csharp
// ❌ 方法在类上，不在接口中
public class MyManager : IMyManager
{
    public async Task NewMethod() { ... }  // 类上有
}
public interface IMyManager
{
    // 接口没有 → 编译失败
}
```

```csharp
// ✅ 接口和实现同步
public interface IMyManager
{
    Task NewMethod();  // 先加接口
}
```

---

### 5.2 绕过基类必须补齐权限校验

**问题**：绕过了 `base.UpdateAsync`，但也绕过了其中的 `CheckUpdatePolicyAsync()`。

```csharp
// ❌ 直接操作——权限校验被跳过
await MapToEntityAsync(input, entity);
await _repository.UpdateAsync(entity);
```

```csharp
// ✅ 手动补齐
await CheckUpdatePolicyAsync();               // 权限
await CheckUpdateInputDtoAsync(entity, input); // 输入校验
await MapToEntityAsync(input, entity);
await _repository.UpdateAsync(entity, autoSave: true);
```

---

### 5.3 多租户：数据操作始终带 domain/tenant 过滤

**问题**：DB 操作过滤了租户，内存操作没有——双侧不一致。

```csharp
// ❌ DB 有 domain，内存没有
await db.Deleteable<Rule>().Where(x => x.V0 == sub && x.V2 == domain)...;  // 精准
await enforcer.RemoveFilteredGroupingPolicyAsync(0, sub);                   // 全局
```

```csharp
// ✅ DB 和内存双侧一致
await db.Deleteable<Rule>().Where(x => x.V0 == sub && x.V2 == domain)...;
// 内存侧：枚举 + domain 过滤 + 逐个精确删除
var oldRules = enforcer.GetFilteredGroupingPolicy(0, sub)
    .Where(r => r.Count >= 3 && r[2] == domain).ToList();
foreach (var rule in oldRules)
    await enforcer.RemoveGroupingPolicyAsync(rule[0], rule[1], rule[2]);
```

---

## 六、审查清单（推广用）

在对项目的其他模块做性能审查时，按此清单逐项检查：

| # | 检查项 | 信号 |
|---|--------|------|
| 1 | 循环内有 `await ...Repository` 或 `await ..._DbQueryable` | N+1 嫌疑 |
| 2 | 先 `GetByIdAsync` 后立即调 `base.UpdateAsync` | 双重读取 |
| 3 | `list.First(x => x.Id == ...)` 出现在循环或 GroupBy 的 Select 内 | O(n²) 嫌疑 |
| 4 | `IDistributedCache<T>` 注入，但部署是单实例 | 不必要的网络开销 |
| 5 | 缓存键拼接中有 `bool?.ToString()` / `int?.ToString()` / `??` | 键冲突风险 |
| 6 | 缓存失效是"读-算-写"三步操作 | 竞态风险 |
| 7 | 批量操作循环内每次调缓存失效 | 重复失效 |
| 8 | 启动预热无 try-catch 保护 | 启动脆弱 |
| 9 | `SemaphoreSlim` 的 `Wait` 范围覆盖了 `await db.xxx` | 锁持有 I/O |
| 10 | `?.OnCompleted` 无 else 分支 | 无事务场景静默跳过 |
| 11 | 类上有 public 方法但对应接口没有 | 编译将失败 |
| 12 | 绕过基类 CRUD 方法但没调 `CheckXxxPolicyAsync` | 安全漏洞 |
| 13 | DB 操作有 domain 过滤但内存操作没有（或反过来） | 多租户数据泄露 |

---

## 七、性能目标参考

| 操作 | 优化前 | 优化后 | 手段 |
|------|--------|--------|------|
| 菜单列表读取 | 10-20ms | <0.1ms | IMemoryCache + 版本号 |
| 菜单更新（无 API 变更） | ~320ms | 3-5ms | 消除双读 + 消除 N+1 |
| 菜单更新（API 变更+多角色） | ~320ms+ | <15ms | 批量 I/O + 增量内存同步 |
| Casbin 策略同步 | 全量 LoadPolicy(50-100ms) | 增量 API(<1μs) | 精准 Add/Remove |
| 写锁持有 | 10-100ms(含 DB) | <1μs(仅内存) | 锁范围收窄 |
