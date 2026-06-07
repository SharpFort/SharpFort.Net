# SharpFort Casbin RBAC 性能优化 — 最终实施方案

> 版本：v3.0（终版）
> 审查轮次：三轮交叉审查（原始方案 → V2 审查补充 → 高级专家审查 → 分歧再评估）
> 状态：已达成共识，可进入实施阶段

---

## 0. 已知限制（部署前提）

以下为项目现有的架构约束，本方案沿袭既有模式，**不在本次修改范围内**，但实施者需知晓：

| 编号 | 限制 | 影响 | 触发条件 |
|------|------|------|----------|
| **L-01** | 缓存 Key 不含 `TenantId`，`static _schemaVersion` 进程级全局 | 多租户场景下缓存跨租户污染 | `EnabledSaasMultiTenancy: true`（当前 `false`） |
| **L-02** | `static long _schemaVersion` 仅存于进程内存 | 多实例部署时各实例缓存版本不同步 | 水平扩容 ≥ 2 实例（当前单实例，`EnableRedisWatcher: false`） |
| **L-03** | `SfDistributedCacheKeyNormalizer` 多租户前缀已注释 | ABP 层面 `IDistributedCache<T>` 也不自动区分租户 | 同上 L-01 |

> L-03 证据文件：`framework/SharpFort.Caching.FreeRedis/SfDistributedCacheKeyNormalizer.cs` 第 35-39 行 — 多租户 Key 前缀已被 `//todo` 注释。

**当前 `appsettings.json` 配置确认**：`EnabledSaasMultiTenancy: false`，单实例部署，以上限制暂不触发。

---

## 1. P0 — 登录接口性能修复

### 1.1 `ClientInfoHelper` — `Parser.GetDefault()` 每次实例化

**目标文件**：`framework/SharpFort.Core/Helper/ClientInfoHelper.cs`

**问题**：`AccountService.PostLoginAsync`（第 126 行）同步调用 `ClientInfoHelper.GetClientInfo()`，该方法每次执行 `Parser.GetDefault()` 都会读取并解析 `regexes.yaml` 文件。高并发下此同步 I/O + 正则编译操作直接拖垮响应时间（Avg 28.7s）。

> ⚠️ 方案 v1 描述「触发了 `LoginLogHandler`」有误。实际类名为 `LoginEventHandler`，且 `GetClientInfo()` 在 `PostLoginAsync` 中**内联调用**（非事件处理器中），直接阻塞登录响应。

**修复方案**：将 `Parser.GetDefault()` 提升为 `static readonly` 字段，整个进程生命周期仅初始化一次。`UAParser.Parser.Parse()` 线程安全，可安全复用。

**修复后代码**：

```csharp
public static class ClientInfoHelper
{
    // 单例：应用启动时初始化一次，避免高并发下重复解析 regexes.yaml
    private static readonly Parser _uaParser = Parser.GetDefault();

    public class ClientResult
    {
        public string LoginIp { get; set; } = string.Empty;
        public string LoginLocation { get; set; } = string.Empty;
        public string Browser { get; set; } = string.Empty;
        public string Os { get; set; } = string.Empty;
    }

    public static ClientResult GetClientInfo(HttpContext context)
    {
        if (context == null) return new ClientResult();

        // 1. 解析 UserAgent → OS / Browser
        string uaStr = context.GetUserAgent();
        ClientInfo c;
        try
        {
            c = _uaParser.Parse(uaStr);   // ← 使用静态单例
        }
        catch
        {
            c = new ClientInfo("null",
                new OS("null", "null", "null", "null", "null"),
                new Device("null", "null", "null"),
                new UserAgent("null", "null", "null", "null"));
        }

        // ⚠️ 修复：Browser 字段应取 UA.Family（浏览器名），非 Device.Family（设备名）
        string browser = c?.UA?.Family ?? "Unknown";
        string os = c?.OS?.ToString() ?? "Unknown";

        // 2. IP 地理位置（IPTools.Core 内部使用内存缓存，无 I/O 瓶颈）
        string ipAddr = context.GetClientIp();
        string locationStr;
        if (ipAddr is "127.0.0.1" or "::1")
        {
            locationStr = "本地-本机";
        }
        else
        {
            try
            {
                IpInfo location = IpTool.Search(ipAddr);
                locationStr = $"{location.Province}-{location.City}";
            }
            catch
            {
                locationStr = "未知地区";
            }
        }

        return new ClientResult
        {
            LoginIp = ipAddr,
            LoginLocation = locationStr,
            Browser = browser,
            Os = os
        };
    }
}
```

**变更说明**：

| 行 | 变更 | 原因 |
|----|------|------|
| `Parser uaParser = Parser.GetDefault()` → `_uaParser` 静态字段 | 新增类级别 `private static readonly Parser _uaParser` | 避免每次调用重读 regexes.yaml |
| `c?.Device?.Family` → `c?.UA?.Family` | 第 42 行 | 原代码取的是设备系列（如 "iPhone"），修正为浏览器名（如 "Chrome"） |

---

## 2. P1 — 角色/用户 GET 接口缓存（GetSelectDataListAsync）

### 2.0 前置：提取共享缓存工具

> 🔴 **必须先行**：`MenuService`、`UserService`、`RoleService` 三个 Service 需要使用完全相同的缓存辅助方法。为避免三份重复代码，先创建共享扩展类。

**新建文件**：`module/casbin-rbac/SharpFort.CasbinRbac.Application/Extensions/DistributedCacheExtensions.cs`

```csharp
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace SharpFort.CasbinRbac.Application.Extensions;

/// <summary>
/// IDistributedCache 扩展方法，提供 JSON 序列化/反序列化的缓存读写。
/// MenuService / UserService / RoleService 统一使用。
/// </summary>
public static class DistributedCacheExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>默认缓存 TTL：30 分钟</summary>
    public static readonly DistributedCacheEntryOptions DefaultCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
    };

    /// <summary>短 TTL：1 分钟，用于空结果防穿透</summary>
    public static readonly DistributedCacheEntryOptions ShortCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
    };

    /// <summary>从缓存反序列化对象</summary>
    public static async Task<T?> GetFromCacheAsync<T>(this IDistributedCache cache, string key)
    {
        string? json = await cache.GetStringAsync(key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    /// <summary>将对象序列化写入缓存</summary>
    public static async Task SetCacheAsync<T>(
        this IDistributedCache cache, string key, T value,
        DistributedCacheEntryOptions? options = null)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        await cache.SetStringAsync(key, json, options ?? DefaultCacheOptions);
    }
}
```

**影响范围**：MenuService 中原有的私有辅助方法 `GetFromCacheAsync` / `SetCacheAsync` 可以改为调用此扩展，减少重复代码（可选，非本次强制）。

---

### 2.1 `UserService` — `GetSelectDataListAsync` 全表扫描

**目标文件**：`module/casbin-rbac/SharpFort.CasbinRbac.Application/Services/System/UserService.cs`

**问题**：基类 `SfCrudAppService.GetSelectDataListAsync` 执行 `Repository.GetListAsync()` 全表查询，无分页无缓存，导致下拉列表接口耗时 13-14s。

> ⚠️ 基类实现（`SfCrudAppService.cs` 第 183-192 行）**完全忽略 `keywords` 参数**——无论传什么值，始终查全表。因此缓存 Key 不需要 `keywords` 维度。

**修复方案**：

1. 构造函数注入 `IDistributedCache`
2. 引入 `static long _userSchemaVersion` + `Interlocked` 无锁版本控制
3. 重写 `GetSelectDataListAsync`：先查缓存，miss 时查库并写入
4. 在 `CreateAsync`、`UpdateAsync`、`DeleteAsync`、`UpdateStateAsync` 末尾调用 `InvalidateUserCache()` 使缓存失效
5. 空结果使用 `ShortCacheOptions`（1 分钟）防穿透

**修改后代码**：

```csharp
// ===== 头部新增 using =====
using Microsoft.Extensions.Caching.Distributed;
using SharpFort.CasbinRbac.Application.Extensions;

// ===== 构造函数改动 =====
// 新增参数 IDistributedCache distributedCache
public class UserService(ISqlSugarRepository<User, Guid> repository, UserManager userManager,
    ICurrentUser currentUser, IDeptService deptService,
    ILocalEventBus localEventBus,
    ICasbinPolicyManager casbinPolicyManager,
    IDistributedCache distributedCache)   // ← 新增
    : SfCrudAppService<User, UserGetOutputDto, UserGetListOutputDto, Guid,
    UserGetListInputVo, UserCreateInputVo, UserUpdateInputVo>(repository), IUserService
{
    // ← 新增字段声明：
    private readonly IDistributedCache _distributedCache = distributedCache;

    // ===== 新增：版本号缓存控制 =====
    private static long _userSchemaVersion = 1;

    private void InvalidateUserCache()
    {
        Interlocked.Increment(ref _userSchemaVersion);
    }

    private static string GetUserCachedKeyPrefix()
    {
        long ver = Interlocked.Read(ref _userSchemaVersion);
        return $"User:v{ver}:";
    }

    // ===== 新增：重写 GetSelectDataListAsync =====
    public override async Task<PagedResultDto<UserGetListOutputDto>> GetSelectDataListAsync(
        string? keywords = null)
    {
        // 注意：基类忽略 keywords，固定 cacheKey 即可
        string cacheKey = $"{GetUserCachedKeyPrefix()}Select:ALL";

        var cached = await _distributedCache.GetFromCacheAsync<PagedResultDto<UserGetListOutputDto>>(cacheKey);
        if (cached is not null) return cached;

        var result = await base.GetSelectDataListAsync(keywords);

        // 空结果用短 TTL 防缓存穿透
        var options = result.TotalCount == 0
            ? DistributedCacheExtensions.ShortCacheOptions
            : DistributedCacheExtensions.DefaultCacheOptions;
        await _distributedCache.SetCacheAsync(cacheKey, result, options);
        return result;
    }

    // ===== 以下方法末尾各加一行 InvalidateUserCache() =====

    // CreateAsync — 在第 141 行 return result; 之前插入
    public override async Task<UserGetOutputDto> CreateAsync(UserCreateInputVo input)
    {
        // ... 原有逻辑不变 ...
        UserGetOutputDto result = await MapToGetOutputDtoAsync(entitiy);
        InvalidateUserCache();   // ← 新增
        return result;
    }

    // UpdateAsync — 在第 202 行 return 之前插入
    // DeleteAsync(Guid id) — 在第 248 行 base.DeleteAsync(id) 之后插入
    // UpdateStateAsync — 在第 235 行 return 之前插入
}
```

**失效触发点精确位置**：

| 方法 | 源文件行号 | `InvalidateUserCache()` 插入位置 |
|------|:---:|---|
| `CreateAsync` | 第 141 行前 | `return result;` 之前 |
| `UpdateAsync` | 第 202 行前 | `return await MapToGetOutputDtoAsync(entity);` 之前 |
| `UpdateProfileAsync` | 第 219 行前 | `return dto;` 之前 |
| `UpdateStateAsync` | 第 235 行前 | `return await MapToGetOutputDtoAsync(entity);` 之前 |
| `DeleteAsync(Guid id)` | 第 248 行后 | `await base.DeleteAsync(id);` 之后 |

> ℹ️ `RoleService.CreateAuthUserAsync` / `DeleteAuthUserAsync` 修改 `UserRole` 关联表，但 `GetSelectDataListAsync` 返回的 DTO **不加载 Roles 导航属性**（返回 `null`），因此不需要触发 User 缓存失效。

**🔴 新增：重写批量删除方法**

`UserService` 当前仅重写了单删 `DeleteAsync(Guid id)`，但该方法被 `[RemoteService(isEnabled: false)]` 禁用远程访问。前端实际调用的批量删除走基类 `DeleteAsync(IEnumerable<TKey> ids)`（`SfCrudAppService` 第 198-202 行），**该方法未被重写**，导致批量删除时缓存不失效、Casbin 策略不清理。

```csharp
/// <summary>
/// 批量删除用户（前端实际调用入口）
/// 覆盖基类以追加 Casbin 清理 + 缓存失效
/// </summary>
public override async Task DeleteAsync(IEnumerable<Guid> ids)
{
    // 一次性查询所有待删用户，避免 N+1
    List<User> users = await _repository.GetListAsync(u => ids.Contains(u.Id));
    foreach (User user in users)
    {
        await _casbinPolicyManager.CleanUserPoliciesAsync(user.Id, user.TenantId);
    }

    await base.DeleteAsync(ids);
    InvalidateUserCache();
}
```

---

### 2.2 `RoleService` — `GetSelectDataListAsync` 全表扫描

**目标文件**：`module/casbin-rbac/SharpFort.CasbinRbac.Application/Services/System/RoleService.cs`

**问题**：同上（基类全表扫描）。

**额外注意**：经验证 `RoleGetListOutputDto` **包含 `RoleCode` 字段**。因此 `UpdateAsync` 中如果 `RoleCode` 变更（`MigrateRoleCodeAsync`），缓存中的旧 `RoleCode` 会返回给前端——必须失效。

**修复方案**：与 UserService 对称实现。

**修改后代码**：

```csharp
// ===== 头部新增 using =====
using Microsoft.Extensions.Caching.Distributed;
using SharpFort.CasbinRbac.Application.Extensions;

// ===== 构造函数改动 =====
// 新增参数 IDistributedCache distributedCache
public RoleService(RoleManager roleManager, ISqlSugarRepository<RoleDepartment> roleDeptRepository,
    ISqlSugarRepository<UserRole> userRoleRepository,
    ISqlSugarRepository<Role, Guid> repository,
    IEnforcer enforcer,
    ISqlSugarRepository<Menu, Guid> menuRepository,
    ICasbinPolicyManager casbinPolicyManager,
    ISqlSugarRepository<User, Guid> userRepository,
    IDistributedCache distributedCache)   // ← 新增
    : base(repository)
{
    // 新增赋值：
    _distributedCache = distributedCache;
    (_roleManager, _roleDeptRepository, _userRoleRepository, _repository, _enforcer,
        _menuRepository, _casbinPolicyManager, _userRepository) =
        (roleManager, roleDeptRepository, userRoleRepository, repository, enforcer,
            menuRepository, casbinPolicyManager, userRepository);
}

// ===== 新增字段 =====
private readonly IDistributedCache _distributedCache;

// ===== 新增：版本号缓存控制 =====
private static long _roleSchemaVersion = 1;

private void InvalidateRoleCache()
{
    Interlocked.Increment(ref _roleSchemaVersion);
}

private static string GetRoleCachedKeyPrefix()
{
    long ver = Interlocked.Read(ref _roleSchemaVersion);
    return $"Role:v{ver}:";
}

// ===== 新增：重写 GetSelectDataListAsync =====
public override async Task<PagedResultDto<RoleGetListOutputDto>> GetSelectDataListAsync(
    string? keywords = null)
{
    string cacheKey = $"{GetRoleCachedKeyPrefix()}Select:ALL";

    var cached = await _distributedCache.GetFromCacheAsync<PagedResultDto<RoleGetListOutputDto>>(cacheKey);
    if (cached is not null) return cached;

    var result = await base.GetSelectDataListAsync(keywords);

    var options = result.TotalCount == 0
        ? DistributedCacheExtensions.ShortCacheOptions
        : DistributedCacheExtensions.DefaultCacheOptions;
    await _distributedCache.SetCacheAsync(cacheKey, result, options);
    return result;
}

// ===== 以下方法末尾各加一行 InvalidateRoleCache() =====

// CreateAsync — 第 102 行 return outputDto; 之前
// UpdateAsync — 第 149 行 return dto; 之前（因 RoleCode 在 DTO 中，RoleCode 变更时需失效）
// UpdateStateAsync — 第 173 行 return 之前
// DeleteAsync(IEnumerable<Guid>) — 第 298 行 await base.DeleteAsync(ids); 之后
```

**失效触发点精确位置**：

| 方法 | 源文件行号 | `InvalidateRoleCache()` 插入位置 |
|------|:---:|---|
| `CreateAsync` | 第 102 行前 | `return outputDto;` 之前 |
| `UpdateAsync` | 第 149 行前 | `return dto;` 之前 |
| `UpdateStateAsync` | 第 173 行前 | `return await MapToGetOutputDtoAsync(entity);` 之前 |
| `UpdateDataScopeAsync` | 第 64 行后 | `await _repository._Db.Updateable(entity)...ExecuteCommandAsync();` 之后 |
| `DeleteAsync(IEnumerable)` | 第 298 行后 | `await base.DeleteAsync(ids);` 之后 |

> ℹ️ `UpdateDataScopeAsync` 修改角色的 `DataScope` 字段（`RoleGetListOutputDto` 包含此字段），修改后缓存中的旧 DataScope 会返回给前端，必须追加失效。

---

## 3. P1 — 菜单 POST（创建菜单 + Casbin 策略）

### 3.1 `MenuService.CreateInternalAsync` — 缺少超管 RoleMenu 关联

**目标文件**：`module/casbin-rbac/SharpFort.CasbinRbac.Application/Services/System/MenuService.cs`

**问题**：创建菜单仅写了 `Menu` 表，没有插入 `RoleMenu` 关联，导致新建菜单**不出现在超管的前端菜单树**中。

> ⚠️ **关键澄清**：超管的 Casbin 策略是 `p, admin, domain, *, *` 通配符（`CasbinPolicyManager.InitAdminPermissionAsync` 第 215-216 行），所有 API 路径已被覆盖。**后端 API 鉴权不受影响**，问题仅在前端菜单树展示层面。

> 🔴 **严禁调用 `SetRolePermissionsAsync`**：该方法的实现是 DELETE ALL + INSERT 逐菜单规则（`CasbinPolicyManager` 第 159 行起）。如果对超管调用此方法，会将 `*, *` 通配符**删除并替换为逐条菜单规则**——这是严重的权限降级 Bug。

**修复方案**：仅插入 `RoleMenu` 关联记录。为支持批量导入场景，增加 `associateAdminRole` 参数控制。

**修改后代码**（替换 `CreateInternalAsync` 和 `PostImportExcelAsync`）：

```csharp
/// <summary>
/// 内部创建方法
/// </summary>
/// <param name="associateAdminRole">是否关联超管 RoleMenu。单条创建时为 true，批量导入时由外部统一处理</param>
private async Task<MenuGetOutputDto> CreateInternalAsync(
    MenuCreateInputVo input, bool invalidateCache, bool associateAdminRole = true)
{
    // ... 原有 ApiUrl/ApiMethod 验证逻辑不变（第 168-185 行）...

    var result = await base.CreateAsync(input);

    // ===== 新增：将新菜单关联给超管（仅 RoleMenu，不碰 Casbin） =====
    if (associateAdminRole)
    {
        Role? adminRole = await _roleRepository.GetFirstAsync(
            r => r.RoleCode == UserConst.AdminRolesCode);
        if (adminRole != null)
        {
            await _roleMenuRepository.InsertAsync(
                new RoleMenu { RoleId = adminRole.Id, MenuId = result.Id });
            // 注意：不调用 SetRolePermissionsAsync —— 超管已有 *,* 通配符
        }
    }

    if (invalidateCache)
    {
        InvalidateMenuCache();
    }

    return result;
}

// CreateAsync 单条创建（默认关联超管）
public override async Task<MenuGetOutputDto> CreateAsync(MenuCreateInputVo input)
{
    return await CreateInternalAsync(input, invalidateCache: true, associateAdminRole: true);
}

// PostImportExcelAsync 批量导入（循环内不逐个关联，最后统一批量插入）
public override async Task PostImportExcelAsync(List<MenuCreateInputVo> input)
{
    List<Guid> newMenuIds = new();
    try
    {
        foreach (var item in input)
        {
            // 批量导入时不逐个关联超管（associateAdminRole: false）
            var result = await CreateInternalAsync(item, invalidateCache: false, associateAdminRole: false);
            newMenuIds.Add(result.Id);
        }

        // 批量结束后一次性插入所有 RoleMenu 关联
        if (newMenuIds.Count > 0)
        {
            Role? adminRole = await _roleRepository.GetFirstAsync(
                r => r.RoleCode == UserConst.AdminRolesCode);
            if (adminRole != null)
            {
                List<RoleMenu> roleMenus = newMenuIds
                    .Select(id => new RoleMenu { RoleId = adminRole.Id, MenuId = id })
                    .ToList();
                await _roleMenuRepository.InsertRangeAsync(roleMenus);
            }
        }
    }
    finally
    {
        InvalidateMenuCache();
    }
}
```

---

## 4. 压测环境与缓存容量评估

### 4.0 压测环境说明

| 项目 | 配置 |
|------|------|
| **数据库** | PostgreSQL `localhost:5432`（非 SQLite） |
| **数据库名** | `sharpfort` |
| **Redis** | `127.0.0.1:6379`, db=13 |
| **应用容器** | `ghcr.io/sharpfort/sharpfort.net:latest`, 端口 19001 |
| **压测工具** | k6 v2.0.0, Windows 侧 `C:\Program Files\k6\k6.exe` |
| **压测脚本** | `E:\Projects\SharpFort.Net-K6\k6-scripts\` |
| **连接池上限** | PostgreSQL Max Pool Size=100, 压测 VU 上限 250 |

> ℹ️ 13.6s 延迟在 PostgreSQL 环境下的成因并非 SQLite 文件锁，而是 Casbin 策略全量重建 + 缺少缓存导致的 DB 往返次数过多。

### 4.1 缓存容量预估

由于基类 `GetSelectDataListAsync` **忽略 `keywords` 参数**（始终查全表），cacheKey 维度简化为固定 `Select:ALL`。每个 Service 仅 **1 个有效缓存条目**（最多 2 个：正常数据 + 空结果哨兵）。

| Service | 缓存条目数 | 单条数据量（估算） | Redis 内存占用 |
|---------|:--:|------|:--:|
| `UserService` | 1 | ~1000 用户 × 500B/条 ≈ 500KB | ~500KB |
| `RoleService` | 1 | ~50 角色 × 300B/条 ≈ 15KB | ~15KB |
| **合计** | **2** | — | **< 1MB** |

> TTL 策略：正常结果 30 分钟（`DefaultCacheOptions`），空结果 1 分钟（`ShortCacheOptions`）。结合 `SlidingExpiration` 可进一步清理不活跃条目。

### 4.2 风险评估

| 风险 | 概率 | 影响 | 缓解措施 |
|------|:--:|------|------|
| Redis 连接故障 | 低 | 缓存全部 miss，退化为每次查库（性能回退到优化前，但功能不受影响） | `IDistributedCache.GetStringAsync` 异常时 catch 后走查库路径 |
| `static _schemaVersion` 溢出 | 极低 | `long` 最大值 9.2×10¹⁸，每秒 100 万次自增需 29 万年 | 无需处理 |
| 缓存与 DB 短暂不一致 | 中 | `InvalidateCache()` 后、下一次请求前，DB 已变更但 Redis 尚未回填（窗口 < 100ms） | 可接受；下拉列表场景对秒级一致性不敏感 |
| `UpdateProfileAsync` 遗漏失效 | — | ✅ 已在 v3.1 修复 | — |
| 批量删除未触发 Casbin 清理 + 缓存失效 | — | ✅ 已在 v3.1 修复 | — |

---

## 5. 验证计划

### 5.1 性能基准

| 接口 | 优化前 | 优化后目标 | 验证方式 |
|------|--------|-----------|----------|
| **登录** `/api/app/account/login` | Avg 28.7s, P95 39s | P95 < 2s（消除 yaml 解析瓶颈） | k6 压测 250 VU, 30s |
| **用户下拉** `/api/app/user/select-data` | Avg 13-14s | P95 < 500ms（Redis 命中）/ P95 < 2s（首次 miss） | k6 两次请求：首次（miss）+ 第二次（命中） |
| **角色下拉** `/api/app/role/select-data` | Avg 13-14s | 同上 | 同上 |
| **创建菜单** `/api/app/menu` | Avg 13.6s | P95 < 3s（仅 DB 插入 + 1 条 RoleMenu） | k6 10 VU 串行写入 |

### 5.2 功能验证

| 验证项 | 预期结果 |
|--------|----------|
| 登录后 `LoginLog.Browser` 字段 | 存储浏览器名（如 "Chrome"），非设备名（如 "Other"） |
| 创建菜单后超管刷新前端 | 新菜单出现在菜单树中 |
| 创建菜单后超管 Casbin 规则 | `casbin_rule` 表中超管 p 规则仍为 `*,*` 通配符（未被替换） |
| 新增用户后下拉列表 | 缓存立即失效，下次请求包含新用户 |
| 修改用户状态（启用/禁用）后 | 缓存立即失效 |
| 修改用户个人资料（昵称等）后 | 缓存立即失效 |
| 批量删除用户后 | 缓存立即失效 + `casbin_rule` 中 g 规则已清理 |
| 修改角色 RoleCode 后 | 缓存立即失效 |
| 修改角色 DataScope 后 | 缓存立即失效 |

### 5.3 回滚方案

| 操作 | 方法 |
|------|------|
| 代码级回滚 | `git revert` 对应 commit |
| 运行时回滚 | 重启应用 + 执行 `redis-cli FLUSHDB` 清除残留缓存 Key |
| 灰度 | 先部署到测试环境，压测通过后再上生产 |

---

## 6. 实施顺序

原则上 **P0 → P1 可并行**（各模块独立）。

| 步骤 | 内容 | 涉及文件 | 预估工作量 |
|:--:|---|------|:--:|
| 0 | 创建 `DistributedCacheExtensions.cs` | 1 个新文件 | 15 min |
| 1 | P0：`ClientInfoHelper` 静态单例 + Browser 字段修正 | `ClientInfoHelper.cs` | 10 min |
| 2a | P1：`UserService` 添加缓存 + 5 处失效触发点 + 批量删除重写 | `UserService.cs` | 40 min |
| 2b | P1：`RoleService` 添加缓存 + 5 处失效触发点 | `RoleService.cs` | 35 min |
| 3 | P1：`MenuService` 添加超管 RoleMenu 关联 + 批量导入重构 | `MenuService.cs` | 20 min |
| 4 | 编译 + 单元测试 | — | 10 min |
| 5 | 部署测试环境 + k6 压测验证 | — | 30 min |

**总计预估**：~2.5 小时（不含压测环境准备）。

---

## 7. 附录：修改清单总览

| 文件 | 改动类型 | 摘要 |
|------|:--:|------|
| `framework/.../ClientInfoHelper.cs` | 修改 | `_uaParser` 静态单例；`Device.Family` → `UA.Family` |
| `module/.../Extensions/DistributedCacheExtensions.cs` | **新建** | 共享缓存扩展方法 |
| `module/.../UserService.cs` | 修改 | 注入 `IDistributedCache`；新增版本控制 + `GetSelectDataListAsync` 重写；新增 `DeleteAsync(IEnumerable)` 批量删除重写（含 Casbin 清理）；5 处失效触发点（`Create`/`Update`/`UpdateProfile`/`UpdateState`/`Delete`） |
| `module/.../RoleService.cs` | 修改 | 注入 `IDistributedCache`；新增版本控制 + `GetSelectDataListAsync` 重写；5 处失效触发点（`Create`/`Update`/`UpdateState`/`UpdateDataScope`/`Delete`） |
| `module/.../MenuService.cs` | 修改 | `CreateInternalAsync` 增加 `associateAdminRole` 参数；`CreateAsync` + `PostImportExcelAsync` 中插入 `RoleMenu` 关联；**不调用 `SetRolePermissionsAsync`** |

---

> **文档结束**。确认本方案后回复「开始实施」，即可进入代码修改阶段。
