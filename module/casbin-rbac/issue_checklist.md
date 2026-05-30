# Casbin-RBAC 性能优化 — 问题跟踪清单

多轮审查（初始 → V2 → V3 → V4 → 交叉评估）全部问题汇总。按严重程度分类，实施时逐项核验。

| # | 级别 | 问题描述 | 涉及文件 | 来源 | 修复方案 | 已完成 | 备注 |
|---|------|---------|---------|------|---------|--------|------|
| | | **🔴 严重 — 必须修复** | | | | | |
| 1 | 🔴 | `RemoveFilteredGroupingPolicyAsync` 参数错位——g-rule 只有 3 个字段，传入 4 个参数导致 `domain` 匹配到不存在的 V3，V2 被 `""` 字面量匹配，永远删不掉任何规则 | CasbinPolicyManager | R2 BUG-V2-1 | 角色级：`RemoveFilteredGroupingPolicyAsync(1, roleSub, domain)`（匹配 V1,V2）。用户级：枚举旧规则+domain 过滤+逐个精确删除。`""` 不是通配符，必须避开 | ✅ | 影响 CleanRolePoliciesAsync / CleanRolePoliciesByRoleCodeAsync / SetUserRolesAsync / CleanUserPoliciesAsync |
| 2 | 🔴 | `SetUserRolesAsync` 内存清理去掉了 domain 过滤——`RemoveFilteredGroupingPolicyAsync(0, sub)` 跨租户清洗，而 DB 层仍过滤 domain，双侧不一致 | CasbinPolicyManager | R4 C-3 | 内存侧改用"枚举旧域内规则 → 按 domain 过滤 → 逐个精确删除"，与 DB 保持一致 | ✅ | |
| 3 | 🔴 | `CleanUserPoliciesAsync` DB 和内存均去掉了 domain 过滤——跨租户误删用户策略 | CasbinPolicyManager | R4 C-2 | DB 侧恢复 `.Where(x => x.PType == "g" && x.V0 == sub && x.V2 == domain)`；内存侧与 C-3 同方案 | ✅ | |
| 4 | 🔴 | `ReloadAllPoliciesAsync` 未在 `ICasbinPolicyManager` 接口声明——`CasbinSeedService` 通过接口注入无法调用，编译失败 | ICasbinPolicyManager | R4 C-4 | 接口添加 `Task ReloadAllPoliciesAsync();` 方法签名 | ✅ | |
| 5 | 🔴 | `MenuSource.Fluid` 不存在——枚举实际只有 `Ruoyi=0` 和 `Pure=1`，编译失败 | MenuService | R2 BUG-V2-2 | 改为 `MenuSource.Pure` | ✅ | WarmupCacheAsync 中 |
| 6 | 🔴 | 绕过 `base.UpdateAsync` 丢失了 `CheckUpdatePolicyAsync()` 权限验证和 `CheckUpdateInputDtoAsync()` 输入校验——安全漏洞 | MenuService | R2 BUG-V2-3 | 在 MapToEntityAsync 前显式调用：`await CheckUpdatePolicyAsync()` + `await CheckUpdateInputDtoAsync(oldMenu, input)` | ✅ | |
| 7 | 🔴 | `bool? State` 缓存键冲突——`null`（查全部）和 `false`（仅禁用）都拼成 `False`，两个语义不同的查询共享缓存，造成越权数据泄露 | MenuService | R3 BUG-V4-1 | `string stateKey = input.State?.ToString() ?? "all"` → 隔离为 True / False / all 三态 | ✅ | GetListAsync 中 |
| 8 | 🔴 | `IDistributedCache` 会走 Redis 网络调用（模块依赖 `SharpFortCachingFreeRedisModule`），序列化+网络往返 2-5ms，无法达到 <0.1ms 目标 | MenuService | R1 问题1 | 改用 .NET 原生 `IMemoryCache`（ABP `AbpCachingModule` 已自动注册，零配置） | ✅ | |
| 9 | 🔴 | 版本号"读-算-写"竞态——并发写可能读到相同版本号，导致一次失效丢失 | MenuService | R1 问题4 | `private static long _menuSchemaVersion = 1;` + `Interlocked.Increment(ref _menuSchemaVersion)` 原子操作 | ✅ | |
| 10 | 🔴 | `UpdateAsync` 中 `oldMenu` 与 `base.UpdateAsync` 双重 DB 读取——`base.UpdateAsync` 内部还会调 `GetEntityByIdAsync` 再查一次同一条记录 | MenuService | R1 问题7 | 绕过 `base.UpdateAsync`，直接 `MapToEntityAsync(input, oldMenu)` + `_repository.UpdateAsync(oldMenu, autoSave: true)` | ✅ | 必须同时修复 #6（补齐权限校验） |
| 11 | 🔴 | `UpdateAsync` 和 `DeleteAsync` 中存在 N+1 级联查询——对每个角色逐个查 RoleMenu 和 Menu，N 个角色 = 2N 次额外 DB 查询 | MenuService | R1 初始 | 批量 I/O：1 次查出所有 roleId → 1 次查出所有 roles → 1 次查出所有 roleMenu mappings → 1 次查出所有 menus → 内存归类分发 | ✅ | |
| | | **🟡 重要 — 强烈建议** | | | | | | |
| 12 | 🟡 | 无 UOW 时 `_unitOfWorkManager.Current?.OnCompleted(...)` 静默跳过内存同步——后台任务/种子数据场景 DB 写入成功但 Enforcer 内存不更新 | CasbinPolicyManager | R2 BUG-2 / R4 C-1 | 引入 `SyncOrFallback`：有 UOW → `OnCompleted(增量)`；无 UOW → 立即执行增量，失败则 `ReloadAllPoliciesAsync` 全量兜底 | ✅ | 影响全部 9 个写方法 |
| 13 | 🟡 | `PostImportExcelAsync` 中途异常导致 `InvalidateMenuCache()` 永不执行——前 N 条已写入 DB 但缓存版本未递增，读写数据分裂 | MenuService | R2 M-2 | `try { foreach... } finally { InvalidateMenuCache(); }` | ✅ | |
| 14 | 🟡 | `WarmupCacheAsync` 启动失败会阻止应用启动——DB 未就绪/迁移未完成时直接崩溃 | SharpFortCasbinRbacApplicationModule | R2 M-1 | `try { await WarmupCacheAsync(); } catch (Exception ex) { logger.LogWarning(...); }` | ✅ | OnApplicationInitializationAsync 中 |
| 15 | 🟡 | `MenuListCacheItem.Items` 使用 `object` 类型——完全丢失类型安全，每次取用需强制转型 | MenuService | R1 问题3 | 直接使用 `IMemoryCache.GetOrCreateAsync<T>()` 泛型方法，彻底废除 `MenuListCacheItem` 包装类 | ✅ | |
| 16 | 🟡 | 全部 9 个写方法从全量 `LoadPolicyAsync()` 改为增量 API——旧 `TriggerMemorySync` 每次全量重载 DB 策略，策略量大时严重性能毛刺 | CasbinPolicyManager | R1 问题2 | 9 个写方法全部重构：DB 写（锁外）→ 内存增量 API（锁内）。p-rule 用 `RemoveFilteredPolicyAsync`/`AddPoliciesAsync`，g-rule 用相应分组 API | ✅ | |
| 17 | 🟡 | `_writeLock` 持有 DB 写入时间——锁覆盖整个 DB 操作，并发写吞吐受限 | CasbinPolicyManager | R1 建议 | DB 写入移出锁范围，锁仅保护 Enforcer 纯内存操作（微秒级），并发吞吐提升数倍 | ✅ | |
| 18 | 🟡 | `CasbinSeedService` 直接操作 `_enforcer.LoadPolicyAsync()`——绕过全局写锁保护，存在并发安全隐患 | CasbinSeedService | R1 问题8 | 注入 `ICasbinPolicyManager`，Phase 4 改用 `_casbinPolicyManager.ReloadAllPoliciesAsync()` | ✅ | |
| | | **🟢 优化 — 可选改进** | | | | | | |
| 19 | 🟢 | `GetListAsync` 无真正 DB 分页——全量加载到内存再包装成 `PagedResultDto`，数据量大时内存压力大 | MenuService | R1 问题6 | 已记录为"全量列表缓存"设计决策（导航树数据量小），暂不修改 | ⬜ | 设计决策，非 bug |
| 20 | 🟢 | `GetAsync(id)` 缓存穿透——频繁查询不存在的 ID 每次都穿透到 DB 抛异常 | MenuService | R4 M-3 | 可选：缓存 sentinel 空对象 + 短 TTL。当前 ABP 鉴权+GUID 不可枚举性已大幅降低风险，暂不处理 | ✅ | |
| 21 | 🟢 | `GetSelectDataListAsync` / `GetExportExcelAsync` 未走缓存——继承自基类，直接查询 DB | MenuService | R4 M-4 | 低频接口，暂不缓存。若未来需要可覆盖 | ✅ | |
| 22 | 🟢 | 缓存键 `input.MenuName ?? "*"` ——用户若恰好搜索 `*` 会与 null 共享缓存键 | MenuService | R4 M-1 | 极低概率，可暂不处理。如需完善可改为 `input.MenuName ?? "__ALL__"` | ✅ | |
| 23 | 🟢 | 预热只覆盖 2 个 MenuSource 的默认查询——State/关键字过滤组合未预热 | MenuService | R4 L-1 | 可根据生产流量分析追加更多预热路径，当前够用 | ✅ | |
| 24 | 🟢 | `allMenus.First(m => m.Id == x.MenuId)` 可能抛 `InvalidOperationException`——数据完整性异常时会炸 | MenuService | R4 L-4 | 可选改为 `FirstOrDefault` + 过滤 null，或用 `Dictionary<Guid, Menu>` 做 O(1) 查找 | ✅ | UpdateAsync / DeleteAsync 中 |
| | | **📝 记录 — 设计约束** | | | | | | |
| 25 | 📝 | `_menuSchemaVersion` 为进程内 `static` 字段——多实例部署时各实例缓存失效不同步 | — | R4 M-2 | 当前单实例部署无影响。记录约束供运维参考。若未来多实例需 Redis pub/sub 广播失效 | ⬜ | |
| 26 | 📝 | Redis Watcher 与增量更新不兼容——`AutoSave=false` 使增量操作不经过 Adapter，Watcher 不感知变更 | — | R4 新发现 | 当前 `EnableRedisWatcher: false` 无影响。若未来启用多实例，需在 `syncAction` 后显式调用 `watcher.Update()` | ⬜ | 见 `SharpFortCasbinRbacSqlSugarCoreModule.cs:82-106` |
| | | **❌ 误报 — 已撤销** | | | | | | |
| 27 | ❌ | `IMemoryCache` 未注册到 DI——声称需手动 `AddMemoryCache()` | — | R4 C-5 | ~~误报~~ ABP `AbpCachingModule` 内部已注册，开箱即用 | — | 已验证 |
| 28 | ❌ | 多角色 OnCompleted 无去重导致性能退化——旧 `syncKey` 去重被移除 | — | R4 C-6 | ~~误报~~ 增量操作为纯内存、微秒级开销（`AutoSave=false` 确认），无全量重载 DB roundtrip，去重无必要 | — | 已验证 |
| 29 | ❌ | `MigrateRoleCodeAsync` 中 `ReloadAllPoliciesAsync()` 双重锁获取 | — | R4 M-5 | ~~误报~~ DB 操作在锁外，`syncAction` 中获取锁是首次也是唯一一次，无嵌套 | — | 已验证 |

---

## 统计

| 类别 | 数量 |
|------|------|
| 🔴 严重 | 11 |
| 🟡 重要 | 7 |
| 🟢 优化 | 6 |
| 📝 约束 | 2 |
| ❌ 误报 | 3 |
| **合计** | **29** |
| **实际需修复** | **18** (#1-#18) |
