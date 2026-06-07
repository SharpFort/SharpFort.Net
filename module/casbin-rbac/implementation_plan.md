# SharpFort Casbin RBAC 性能优化与修复实施方案

根据初步排查与专家的二次审查，我已对实施方案进行了深度修正与完善。本方案旨在解决压测中暴露的性能瓶颈，并严格遵循项目的代码规范与架构设计。

## 1. P0 登录接口性能修复 (`AccountService.PostLoginAsync`)

**问题定位**：
*   **主要瓶颈**：`PostLoginAsync` 触发了 `LoginLogHandler`，进而调用 `ClientInfoHelper.GetClientInfo`。原有代码在每次调用时同步实例化 `UAParser.Parser.GetDefault()`，由于其会读取并解析巨大的 `regexes.yaml` 资源文件，高并发下导致严重的 I/O 与 CPU 瓶颈。
*   **次要排查点**：`IpTool.Search(ipAddr)` 虽然也是每次调用，但 `IPTools.Core` 内部使用的是内存缓存机制（默认将 ip2region.db 载入内存），因此不会造成磁盘 I/O 瓶颈。

**修改方案**：
*   **目标文件**：`E:\Projects\SharpFort.Net\framework\SharpFort.Core\Helper\ClientInfoHelper.cs`
*   **具体操作**：
    将 `Parser.GetDefault()` 提升为静态只读字段（单例）。`UAParser.Parser.Parse()` 是线程安全的，完全可以复用。
    ```csharp
    public static class ClientInfoHelper
    {
        // 声明为静态单例，避免高并发下重复解析 yaml
        private static readonly Parser _uaParser = Parser.GetDefault();
        
        public static ClientResult GetClientInfo(HttpContext context)
        {
            if (context == null) return new ClientResult();

            string uaStr = context.GetUserAgent();
            ClientInfo c;
            try
            {
                c = _uaParser.Parse(uaStr);
            }
            catch
            {
                c = new ClientInfo("null", new OS("null", "null", "null", "null", "null"), new Device("null", "null", "null"), new UserAgent("null", "null", "null", "null"));
            }
            
            // ... IpTool.Search 逻辑保持不变（其内部已缓存）
        }
    }
    ```

> **🔍 审查补充（V2）**：
> 
> 本方案诊断准确、修改方向正确，无待补充项。`IpTool.Search()` 已确认内部使用内存缓存，`Parser.GetDefault()` 单例化是核心修复。可直接实施。

## 2. P1 角色/用户 GET 接口全表扫描修复 (`GetSelectDataListAsync`)

**问题定位**：
基类的 `GetSelectDataListAsync` 会直接查询全表且无缓存，这导致前端获取下拉列表时（如 `/api/app/user/select-data`）产生长达 13-14s 的响应延迟。`MenuService` 已有成熟的 `IDistributedCache` Redis 缓存实现可供参考。

**修改方案**：
*   **目标文件**：
    *   `E:\Projects\SharpFort.Net\module\casbin-rbac\SharpFort.CasbinRbac.Application\Services\System\UserService.cs`
    *   `E:\Projects\SharpFort.Net\module\casbin-rbac\SharpFort.CasbinRbac.Application\Services\System\RoleService.cs`
*   **具体操作**：
    1.  **依赖注入**：在 `UserService` 和 `RoleService` 的构造函数中注入 `IDistributedCache`。
    2.  **缓存架构**：引入类似于 `MenuService` 的 `_schemaVersion` 版本控制。
    3.  **缓存失效兜底**：设置 `_defaultCacheOptions`（如 30 分钟）和防止穿透的 `_shortCacheOptions`（1 分钟兜底空集合）。
    4.  **失效触发点**：在 `CreateAsync`, `UpdateAsync`, `DeleteAsync` 以及 **`UpdateStateAsync`** 方法中均加入 `Interlocked.Increment(ref _schemaVersion);` 来保证缓存的一致性。
    5.  **重写查询**：
        ```csharp
        // 以 UserService 为例：
        private static long _userSchemaVersion = 1;
        private string GetCachedKeyPrefix() => $"Userv{Interlocked.Read(ref _userSchemaVersion)}:";

        public override async Task<PagedResultDto<UserGetListOutputDto>> GetSelectDataListAsync(string? keywords = null)
        {
            string cacheKey = $"{GetCachedKeyPrefix()}Select:{keywords ?? "__ALL__"}";
            var cached = await GetFromCacheAsync<PagedResultDto<UserGetListOutputDto>>(cacheKey);
            if (cached != null) return cached;

            var result = await base.GetSelectDataListAsync(keywords);
            await SetCacheAsync(cacheKey, result);
            return result;
        }
        ```

> **🔍 审查补充（V2）**—— 共 5 项待补充：
>
> **(1) 🔴 缺少 `GetFromCacheAsync` / `SetCacheAsync` 辅助方法**
>
> `MenuService` 已有的这两个方法是缓存的基石（第 65-76 行），`UserService` 和 `RoleService` 当前没有。每个 Service 都需要增加约 15 行样板代码。建议提取为共享基类或扩展方法，避免重复：
>
> ```csharp
> // 需要添加到 UserService / RoleService 的辅助方法
> private static readonly DistributedCacheEntryOptions _defaultCacheOptions = new()
> {
>     AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
> };
> private static readonly DistributedCacheEntryOptions _shortCacheOptions = new()
> {
>     AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
> };
> private static readonly JsonSerializerOptions _jsonOptions = new()
> {
>     PropertyNameCaseInsensitive = true,
>     PropertyNamingPolicy = JsonNamingPolicy.CamelCase
> };
>
> private async Task<T?> GetFromCacheAsync<T>(string key)
> {
>     string? json = await _distributedCache.GetStringAsync(key);
>     if (json is null) return default;
>     return JsonSerializer.Deserialize<T>(json, _jsonOptions);
> }
>
> private async Task SetCacheAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null)
> {
>     string json = JsonSerializer.Serialize(value, _jsonOptions);
>     await _distributedCache.SetStringAsync(key, json, options ?? _defaultCacheOptions);
> }
> ```
>
> **(2) 🟡 UserService 构造函数改动需特殊处理**
>
> `UserService` 使用 C# 12 主构造函数语法（第 25 行），添加 `IDistributedCache distributedCache` 参数后需要同时添加字段声明：
>
> ```csharp
> // 当前 UserService 构造函数：
> public class UserService(ISqlSugarRepository<User, Guid> repository, UserManager userManager,
>     ICurrentUser currentUser, IDeptService deptService,
>     ILocalEventBus localEventBus,
>     ICasbinPolicyManager casbinPolicyManager,
>     IDistributedCache distributedCache)  // ← 新增参数
>     : SfCrudAppService<...>(repository), IUserService
> {
>     // ← 需要新增字段声明：
>     private readonly IDistributedCache _distributedCache = distributedCache;
>     // ... 其余字段不变
> }
> ```
>
> `RoleService` 使用传统构造函数，在参数列表中添加 `IDistributedCache distributedCache` 并在构造函数体内赋值即可，方式不同。
>
> **(3) 🟡 缓存失效触发点需精确标注插入位置**
>
> `UserService.CreateAsync` 在第 121 行调用 `_userManager.CreateAsync(entitiy)` 后才真正落库。`InvalidateCache()` 必须在所有 DB 写操作**之后**调用，否则存在短暂时间窗口（缓存已失效但 DB 尚未提交）读到旧数据的风险。各方法的精确插入位置：
>
> | 方法 | 插入 `InvalidateCache()` 的位置 |
> |------|-------------------------------|
> | `UserService.CreateAsync` | 第 141 行之前（`return result;` 之前） |
> | `UserService.UpdateAsync` | 第 202 行之前（`return result;` 之前） |
> | `UserService.DeleteAsync(Guid id)` | 第 248 行 `await base.DeleteAsync(id);` 之后 |
> | `UserService.UpdateStateAsync` | 第 235 行 `return ...` 之前 |
> | `RoleService.CreateAsync` | 第 102 行 `return outputDto;` 之前 |
> | `RoleService.UpdateAsync` | 第 149 行 `return dto;` 之前 |
> | `RoleService.UpdateStateAsync` | 第 173 行 `return ...` 之前 |
> | `RoleService.DeleteAsync(IEnumerable<Guid>)` | 第 298 行 `await base.DeleteAsync(ids);` 之后 |
>
> **(4) 🟡 `DeleteAsync` 分清单删和批量删除**
>
> `UserService` 重写的是 `DeleteAsync(Guid id)`（第 239 行，单删），但该方法是 `[RemoteService(isEnabled: false)]` 禁用远程访问的。实际批量删除走基类的 `DeleteAsync(IEnumerable<TKey> ids)`（在 `SfCrudAppService` 第 199-202 行），**该方法未被 UserService 重写**。如果只在单删方法中加 `InvalidateCache()`，批量删除时缓存不会失效。建议：在 `UserService` 中也重写批量删除方法，或确认前端是否只会触发单删。
>
> `RoleService` 已重写 `DeleteAsync(IEnumerable<Guid> ids)`（第 288 行），在此方法中加失效即可。
>
> **(5) 🟢 下拉列表返回全字段的性能隐患（建议优化）**
>
> 基类 `GetSelectDataListAsync` 执行 `Repository.GetListAsync()` 拉取全部字段（含 `User.PasswordHash` 等敏感大字段），然后全量映射到 `UserGetListOutputDto`。对于下拉列表通常只需 `Id`、`UserName`、`Name` 三个字段。建议后续优化为投影查询：
>
> ```csharp
> // 可选优化：用 Select 投影只取必要字段，避免拉取 PasswordHash 等大字段
> var entities = await _repository._DbQueryable
>     .WhereIF(!string.IsNullOrEmpty(keywords), x => x.UserName!.Contains(keywords!) || x.Name!.Contains(keywords!))
>     .Select(x => new UserGetListOutputDto { Id = x.Id, UserName = x.UserName, Name = x.Name })
>     .ToListAsync();
> ```
>
> 此项不影响功能正确性，可在性能调优阶段处理。

## 3. P1 菜单 POST (创建菜单) 及 Casbin 策略写入补全

**问题定位**：
原压测创建菜单（`Menu POST`）耗时 13.6s 暴露出两方面问题：
1. **业务缺失**：创建菜单仅写了菜单表，没有将其关联给超管并同步 Casbin。这导致新建菜单超管无法访问。
2. **并发锁竞争**：在 SQLite 等数据库中，Casbin 的 `casbin_rule` 表由于高频读写或长时间锁定容易发生死锁/严重排队。

**修改方案**：
遵循专家建议，**严禁绕过 `CasbinPolicyManager` 自己拼接 SQL 写库**，必须复用 `CasbinPolicyManager.SetRolePermissionsAsync()` 进行全量一致性更新。
*   **目标文件**：`E:\Projects\SharpFort.Net\module\casbin-rbac\SharpFort.CasbinRbac.Application\Services\System\MenuService.cs`
*   **具体操作**：
    在 `CreateInternalAsync` 中插入菜单后，补充超管菜单的分配和策略同步。

    ```csharp
    var result = await base.CreateAsync(input);

    // 1. 获取超级管理员角色
    Role adminRole = await _roleRepository.GetFirstAsync(r => r.RoleCode == UserConst.AdminRolesCode);
    if (adminRole != null)
    {
        // 2. 插入 RoleMenu 关联表
        await _roleMenuRepository.InsertAsync(new RoleMenu { RoleId = adminRole.Id, MenuId = result.Id });

        // 3. 严格复用 CasbinPolicyManager（包含内存与 DB 锁安全同步）
        // 查询超管现有的菜单列表
        List<Menu> adminMenus = await _roleMenuRepository._DbQueryable
            .LeftJoin<Menu>((rm, m) => rm.MenuId == m.Id)
            .Where((rm, m) => rm.RoleId == adminRole.Id)
            .Select((rm, m) => m)
            .ToListAsync();
            
        // 确保刚刚新建的菜单也在集合中（应对某些 UOW 未即时 Flush 的情况）
        if (!adminMenus.Any(m => m.Id == result.Id))
        {
            adminMenus.Add(await _repository.GetByIdAsync(result.Id));
        }

        // 统一全量刷新超管的 Casbin 策略
        await _casbinPolicyManager.SetRolePermissionsAsync(adminRole, adminMenus);
    }

    if (invalidateCache) { InvalidateMenuCache(); }
    return result;
    ```
    *说明：复用 `SetRolePermissionsAsync` 是维护系统架构一致性的唯一途径。关于 SQLite 并发冲突这一客观性能瓶颈（这其实是 13.6s 压测耗时的根本原因），如果在生产环境遇到此量级的写并发，强烈建议按专家提议将数据库底座迁移至 PostgreSQL 等具备行级锁的数据库。*

> **🔍 审查补充（V2）**—— 共 3 项待补充：
>
> **(1) 🔴 `PostImportExcelAsync` 批量导入导致 N 次全量 Casbin 重建**
>
> `PostImportExcelAsync`（第 198-211 行）循环调用 `CreateInternalAsync(item, invalidateCache: false)`。如果每次创建都触发 `SetRolePermissionsAsync`（DELETE ALL + INSERT ALL），导入 100 条菜单将对超管执行 **100 次全量策略重建**，严重浪费且可能超时。
>
> **修正方案**：将超管 Casbin 同步逻辑从 `CreateInternalAsync` 中**移出**，改为在调用方控制：
>
> ```csharp
> // 方案 A（推荐）：增加参数控制
> private async Task<MenuGetOutputDto> CreateInternalAsync(
>     MenuCreateInputVo input, bool invalidateCache, bool syncAdminCasbin = true)
> {
>     // ... 原有验证逻辑 ...
>     var result = await base.CreateAsync(input);
>
>     if (syncAdminCasbin)
>     {
>         await SyncAdminCasbinAsync(result.Id);
>     }
>
>     if (invalidateCache) { InvalidateMenuCache(); }
>     return result;
> }
>
> // CreateAsync 保持不变（单条创建时同步 Casbin）
> public override async Task<MenuGetOutputDto> CreateAsync(MenuCreateInputVo input)
> {
>     return await CreateInternalAsync(input, invalidateCache: true, syncAdminCasbin: true);
> }
>
> // PostImportExcelAsync 改为批量结束后统一同步一次
> public override async Task PostImportExcelAsync(List<MenuCreateInputVo> input)
> {
>     try
>     {
>         List<Guid> newMenuIds = new();
>         foreach (var item in input)
>         {
>             // 批量导入时不逐个同步 Casbin
>             var result = await CreateInternalAsync(item, invalidateCache: false, syncAdminCasbin: false);
>             newMenuIds.Add(result.Id);
>         }
>         // 批量结束后统一同步一次
>         await SyncAdminCasbinForBatchAsync(newMenuIds);
>     }
>     finally { InvalidateMenuCache(); }
> }
>
> // 新增辅助方法：为超管同步 Casbin（单条 / 批量复用）
> private async Task SyncAdminCasbinAsync(Guid newMenuId)
> {
>     Role? adminRole = await _roleRepository.GetFirstAsync(r =>
>         r.RoleCode == UserConst.AdminRolesCode);
>     if (adminRole == null) return;
>
>     await _roleMenuRepository.InsertAsync(
>         new RoleMenu { RoleId = adminRole.Id, MenuId = newMenuId });
>
>     List<Menu> adminMenus = await _roleMenuRepository._DbQueryable
>         .LeftJoin<Menu>((rm, m) => rm.MenuId == m.Id)
>         .Where((rm, m) => rm.RoleId == adminRole.Id)
>         .Select((rm, m) => m)
>         .ToListAsync();
>
>     if (!adminMenus.Any(m => m.Id == newMenuId))
>         adminMenus.Add(await _repository.GetByIdAsync(newMenuId));
>
>     await _casbinPolicyManager.SetRolePermissionsAsync(adminRole, adminMenus);
> }
>
> private async Task SyncAdminCasbinForBatchAsync(List<Guid> newMenuIds)
> {
>     if (newMenuIds.Count == 0) return;
>     // 批量 ID 合并为一次全量重建
>     await SyncAdminCasbinAsync(newMenuIds.First());
>     // 注意：上面的查询已经拿到 admin 的全部菜单列表（包含所有新菜单），
>     // 只需执行一次 SetRolePermissionsAsync 即可
> }
> ```
>
> **(2) 🟡 新代码插入位置不精确**
>
> 方案代码应插入 `CreateInternalAsync` 中第 187 行 `var result = await base.CreateAsync(input);` **之后**、第 189 行 `if (invalidateCache) { InvalidateMenuCache(); }` **之前**。当前代码结构的边界为：
>
> ```
> 第 187 行: var result = await base.CreateAsync(input);
> 第 188 行: [空行]
> 第 189 行: if (invalidateCache) { InvalidateMenuCache(); }   ← 在这之前插入
> 第 190-195 行: [空行 + return]
> ```
>
> **(3) 🟡 防御性 `GetByIdAsync` 可能掩盖 UOW 排序问题**
>
> 方案第 104-108 行的 `if (!adminMenus.Any(m => m.Id == result.Id))` 防御逻辑承认了一个潜在问题：同一 UOW 内刚插入的 `RoleMenu` 关联，`LeftJoin` 查询可能查不到。如果确实查不到，根本原因是**操作顺序**：应该先 `InsertAsync` RoleMenu，再执行 LeftJoin 查询，确保同一 SqlSugar 上下文可见。防御逻辑作为兜底可以保留，但应加注释说明这是「安全性兜底，不应成为正常路径」。

---

**实施结论**：
      COL