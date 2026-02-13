这是一个为您量身定制的 `CLAUDE.md` 文档。这份文档不仅是开发指南，也是未来的架构设计文档。它整合了我们之前所有深度的讨论，基于 .NET DDD、SqlSugar 和高性能并发场景进行了标准化定义。

---

# CLAUDE.md - 流水号生成模块 (FluidSequence) 开发规范与设计文档

## 1. 项目背景与目标 (Background & Objectives)

### 1.1 背景
在企业级 Admin 系统（如 ERP、CRM、WMS）中，业务单据（订单、入库单）、基础资料（用户、部门、物料）都需要唯一的、可读的编码标识。传统的做法是在业务代码中硬编码生成逻辑（如 `String.Format`），导致规则难以变更、并发处理复杂、代码重复度高。

### 1.2 目标
构建一个**通用、高性能、配置化**的流水号生成模块。
*   **配置化**：通过可视化界面配置“模板字符串”（Template），无需修改代码即可改变编号规则。
*   **高性能**：支持高并发场景下的原子递增，保证不重号、不跳号（除非系统崩溃）。
*   **DDD 架构**：作为基础设施域（Infrastructure/Shared Kernel），通过领域服务对外提供能力，业务层仅需关注“获取”动作。
*   **灵活性**：支持时间维度重置、自定义前缀后缀、随机数、进制转换及业务上下文（如部门编码）的动态拼接。

---

## 2. 模块命名 (Naming)

*   **模块名称**: `FluidSequence` (寓意：如流体般顺滑、灵活的序列生成)
*   **NuGet 包名预设**: `FluidSequence.Core` / `FluidSequence.SqlSugar`
*   **命名空间**: `FluidSequence.Domain`, `FluidSequence.Application`

---

## 3. 数据库设计 (Database Design)

基于 SqlSugar 和 PostgreSQL，采用乐观锁机制处理并发。

### 3.1 核心表：Sys_Sequence_Rule

继承自 Admin.NET 或标准 DDD 的 `AuditedAggregateRoot` (包含 CreateTime, UpdateTime, CreateBy, UpdateBy, IsDeleted)。

```csharp
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace FluidSequence.Domain.Entities
{
    /// <summary>
    /// 流水号规则表
    /// </summary>
    [SugarTable("sys_sequence_rule", "流水号规则配置")]
    public class SysSequenceRule : AuditedAggregateRoot<long>
    {
        /// <summary>
        /// 规则名称 (如：采购订单号)
        /// </summary>
        [SugarColumn(Length = 50, ColumnDescription = "规则名称", IsNullable = false)]
        public string RuleName { get; set; }

        /// <summary>
        /// 规则编码 (业务唯一键，如：PO_NO)
        /// </summary>
        [SugarColumn(Length = 50, ColumnDescription = "规则编码", IsNullable = false, IsUnique = true)]
        public string RuleCode { get; set; }

        /// <summary>
        /// 生成模板 (如：PO-{DeptCode}-{yyyy}{MM}-{SEQ})
        /// </summary>
        [SugarColumn(Length = 100, ColumnDescription = "生成模板", IsNullable = false)]
        public string Template { get; set; }

        /// <summary>
        /// 当前计数值 (核心状态，持久化存储)
        /// </summary>
        [SugarColumn(ColumnDescription = "当前值", IsNullable = false)]
        public long CurrentValue { get; set; }

        /// <summary>
        /// 步长 (默认为 1)
        /// </summary>
        [SugarColumn(ColumnDescription = "步长", DefaultValue = "1")]
        public int Step { get; set; } = 1;

        /// <summary>
        /// 序列号长度 (用于左补0，如 6 表示 000001)
        /// </summary>
        [SugarColumn(ColumnDescription = "序列长度", DefaultValue = "6")]
        public int SeqLength { get; set; } = 6;

        /// <summary>
        /// 最小值 (重置后的起始值)
        /// </summary>
        [SugarColumn(ColumnDescription = "最小值", DefaultValue = "1")]
        public long MinValue { get; set; } = 1;

        /// <summary>
        /// 最大值 (防止溢出)
        /// </summary>
        [SugarColumn(ColumnDescription = "最大值", DefaultValue = "999999999")]
        public long MaxValue { get; set; } = 999999999;

        /// <summary>
        /// 重置规则 (0:不重置, 1:按日, 2:按月, 3:按年)
        /// </summary>
        [SugarColumn(ColumnDescription = "重置规则")]
        public SequenceResetType ResetType { get; set; }

        /// <summary>
        /// 最后重置时间 (用于判断是否跨周期)
        /// </summary>
        [SugarColumn(ColumnDescription = "最后重置时间", IsNullable = true)]
        public DateTime? LastResetTime { get; set; }

        /// <summary>
        /// 乐观锁版本号 (并发控制核心)
        /// </summary>
        [SugarColumn(IsConcurrency = true, ColumnDescription = "版本号")]
        public long Version { get; set; }

        /// <summary>
        /// 租户ID (多租户隔离)
        /// </summary>
        [SugarColumn(ColumnDescription = "租户ID", IsNullable = true)]
        public long? TenantId { get; set; }
        
        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = true)]
        public string Remark { get; set; }
    }

    public enum SequenceResetType
    {
        None = 0,
        Daily = 1,
        Monthly = 2,
        Yearly = 3
    }
}
```

---

## 4. 模块架构划分 (Architecture)

### 4.1 定义模块 (Application Layer - Admin)
*   **职责**：负责 `SysSequenceRule` 的 CRUD。
*   **组件**：`SequenceRuleAppService`。
*   **功能**：提供 API 给前端管理界面，用于新增、修改规则，以及“测试生成（预览）”。

### 4.2 生成模块 (Domain Layer - Core Engine)
*   **职责**：解析模板、原子递增、重置逻辑、格式化。
*   **组件**：`SequenceDomainService`。
*   **核心方法**：
    ```csharp
    // context 用于传递 UserCode, DeptCode 等业务参数
    Task<string> GenerateNextAsync(string ruleCode, Dictionary<string, string> context = null);
    ```

### 4.3 获取模块 (Application Layer - Consumer)
*   **职责**：业务方调用生成模块，保存结果。
*   **组件**：各业务的 AppService (如 `UserAppService`)。
*   **模式**：依赖注入 `ISequenceDomainService`，在保存实体前调用。

---

## 5. 生成规则与解析逻辑 (Rules & Implementation)

解析引擎应使用 **正则表达式 (`Regex`)** 配合 **策略模式** 实现。

### 5.1 规则定义表

| 规则占位符 | 说明 | 示例 (假设当前Seq=15) | 依赖来源 |
| :--- | :--- | :--- | :--- |
| **{SEQ}** | 核心序列号，根据 `SeqLength` 补零 | `000015` (Len=6) | 数据库 `CurrentValue` |
| **{yyyy}** | 4位年份 | `2023` | 系统时间 |
| **{yy}** | 2位年份 | `23` | 系统时间 |
| **{MM}** | 2位月份 | `10` | 系统时间 |
| **{dd}** | 2位日期 | `27` | 系统时间 |
| **{HH}** | 2位小时 (24h) | `14` | 系统时间 |
| **{ww}** | 当年第几周 | `43` | 系统时间 (需算法) |
| **{RAND:NUM:x}** | x位随机数字 | `{RAND:NUM:4}` -> `8291` | 内存随机生成 |
| **{RAND:CHAR:x}**| x位随机大写字母 | `{RAND:CHAR:3}` -> `ABX` | 内存随机生成 |
| **{SEQ36}** | 36进制序列号 (0-9, A-Z) | `F` (15转36进制) | 数据库 `CurrentValue` 转换 |
| **{CheckDigit}** | 模10/Luhn 校验位 | 根据前面生成的字符串计算 | 算法计算 |
| **{TenantCode}** | 租户编码 | `ALI` | **Context 传入** |
| **{UserCode}** | 当前操作用户编码 | `E001` | **Context 传入** |
| **{DeptCode}** | 当前部门编码 | `HR` | **Context 传入** |
| **{Param:Key}** | 通用自定义参数 | `{Param:StoreId}` -> `S01` | **Context 传入** |

### 5.2 核心代码逻辑 (伪代码)

```csharp
public async Task<string> GenerateNextAsync(string ruleCode, Dictionary<string, string> context)
{
    // 1. 数据库原子更新 (利用 SqlSugar 乐观锁或悲观锁)
    // SELECT * FROM sys_sequence_rule WHERE rule_code = @code FOR UPDATE
    var rule = await _repo.GetByCodeAsync(ruleCode);
    
    // 2. 重置判断 (Domain Logic)
    var now = DateTime.Now;
    if (ShouldReset(rule, now)) {
        rule.CurrentValue = rule.MinValue;
        rule.LastResetTime = now;
    } else {
        rule.CurrentValue += rule.Step;
    }
    
    // 3. 持久化
    await _repo.UpdateAsync(rule);

    // 4. 解析模板 (纯内存操作)
    return ParseTemplate(rule.Template, rule.CurrentValue, rule.SeqLength, context);
}

private string ParseTemplate(string template, long val, int len, Dictionary<string, string> ctx)
{
    // 正则匹配 {...}
    return Regex.Replace(template, @"\{(.*?)\}", match => 
    {
        string key = match.Groups[1].Value;
        
        // A. 序列号处理
        if (key == "SEQ") return val.ToString().PadLeft(len, '0');
        if (key == "SEQ36") return ConvertToBase36(val); // 自定义进制转换函数

        // B. 时间处理
        if (key == "yyyy") return DateTime.Now.ToString("yyyy");
        // ... 其他时间格式

        // C. 随机数处理
        if (key.StartsWith("RAND:")) return GenerateRandom(key);

        // D. 上下文处理 (业务编码)
        // 优先匹配 Context 中的 Key
        if (ctx != null && ctx.ContainsKey(key)) return ctx[key];
        
        // E. 通用参数处理
        if (key.StartsWith("Param:")) {
            var paramKey = key.Substring(6);
            return ctx != null && ctx.ContainsKey(paramKey) ? ctx[paramKey] : "";
        }

        return match.Value; // 未知占位符原样返回
    });
}
```

---

## 6. 业务编码 (Business Code) 特别说明

**原则**：严禁在流水号生成器内部去查询 `User` 或 `Org` 表来获取 ID 或 Code。

*   **原因**：
    1.  **性能**：避免在生成流水号时产生额外的数据库 IO。
    2.  **解耦**：流水号模块不应依赖业务模块。
    3.  **可读性**：ID (UUID/Long) 无业务含义且过长，必须使用 Code。

*   **实现**：
    调用方（AppService）必须负责准备好所有需要的 Code，封装在 `Dictionary<string, string> context` 中传入。
    *   *错误做法*：模板写 `{UserId}`，生成器去查库拿 UUID。
    *   *正确做法*：模板写 `{UserCode}`，AppService 传入 `["UserCode": "E001"]`。

---

## 7. 性能与并发一致性 (Performance & Consistency)

### 7.1 数据库并发控制
*   **首选方案**：**乐观锁 (Optimistic Locking)**。
    *   利用 `Version` 字段。SqlSugar 更新时会自动带上 `WHERE Version = @OldVersion`。
    *   如果更新失败（返回行数0），说明有并发冲突，代码层需实现 **自旋重试 (Spin Lock / Retry)** 机制（例如重试 3 次）。
*   **备选方案**：**悲观锁 (Pessimistic Locking)**。
    *   对于极其重要的财务流水号，使用 `Select ... ForUpdate()`。

### 7.2 缓存策略 (Redis)
*   **Rule 配置缓存**：`Template`, `Step`, `ResetType` 等配置信息极少变更，**必须缓存**到 Redis，减少数据库读取。
*   **CurrentValue 不缓存**：为了保证数据严格连续和不丢失，`CurrentValue` **必须每次读写数据库**。
    *   *例外*：如果业务允许断号且追求极致性能（如日志ID），可使用 Redis `INCR` 原子递增，但这不适用于合同号/发票号。

### 7.3 预取机制 (Hi-Lo 算法 - 可选)
*   如果未来遇到每秒数千次的并发瓶颈，可实现 Hi-Lo 算法：一次数据库更新申请 100 个号段到内存，内存中分发。
*   *注意*：服务重启会丢失未使用的号段（断号），需根据业务接受度开启。

---

## 8. 开发检查清单 (Checklist)

- [ ] **数据库**：创建 `sys_sequence_rule` 表，确保 `RuleCode` 唯一索引，`Version` 字段支持并发。
- [ ] **实体**：实现 `TryReset` 和 `NextValue` 领域逻辑。
- [ ] **服务**：实现 `SequenceDomainService`，包含正则解析器和重试机制。
- [ ] **API**：实现规则配置的 CRUD 接口。
- [ ] **前端**：开发配置弹窗，支持模板输入和预览功能。
- [ ] **测试**：编写并发单元测试（模拟 20 个线程同时获取同一规则），验证无重复、无跳号（非重置情况）。
- [ ] **集成**：在用户/订单模块接入，验证 Context 参数传递是否正确。

好的，这是针对 `CLAUDE.md` 的补充内容。请将以下部分按章节插入或替换到原文档的对应位置。

---

### 补充 1：更新数据库枚举与规则定义 (对应原文档 3.1 和 5.1)

**修改 `SequenceResetType` 枚举：**
增加周、季度、财年支持。

```csharp
public enum SequenceResetType
{
    [Description("从不重置")] None = 0,
    [Description("按日重置")] Daily = 1,
    [Description("按月重置")] Monthly = 2,
    [Description("按年重置")] Yearly = 3,
    [Description("按周重置")] Weekly = 4,      // 新增：通常按 ISO 8601 周或自然周
    [Description("按季度重置")] Quarterly = 5, // 新增
    [Description("按财年重置")] FiscalYearly = 6 // 新增：需额外配置财年起始月
}
```

**更新 5.1 规则定义表 (新增行)：**

| 规则占位符 | 说明 | 示例 | 备注 |
| :--- | :--- | :--- | :--- |
| **{mm}** | 2位分钟 | `05` | 时间维度 |
| **{ss}** | 2位秒 | `59` | 时间维度 |
| **{QQ}** | 季度 (Q1-Q4) | `Q3` | 时间维度 |
| **{FY}** | 财年 (如2023) | `2023` | 需定义财年逻辑 |
| **{RAND:MIX:x}** | x位混合随机(数字+大写字母) | `A9B2` | 包含易混淆字符 |
| **{RAND:SAFE:x}**| x位安全随机字符 | `H7K9` | **排除** I,O,Z,0,1,2 等易混淆字符 |

---

### 补充 2：前端元数据交互策略 (对应原文档 4.1 或 5.2)

**关于规则占位符的提供方式：**

不要在前端硬编码枚举。后端应提供一个元数据接口，前端动态渲染“插入标签”按钮。

*   **后端接口**: `GET /api/sequence-rule/placeholders`
*   **返回结构**:
    ```json
    [
      { "key": "{yyyy}", "label": "年份(4位)", "group": "时间" },
      { "key": "{SEQ}", "label": "序列号", "group": "核心" },
      { "key": "{RAND:SAFE:4}", "label": "4位安全随机码", "group": "随机" }
    ]
    ```
*   **解析模块**: 后端解析器应维护一个 `Dictionary<string, IPlaceholderHandler>` 或类似的注册机制，确保文档、API 和解析逻辑的一致性。

---

### 补充 3：代码结构与命名空间 (对应原文档 4.2)

**代码组织策略：策略模式 (Strategy Pattern)**

不要将所有逻辑写在一个巨大的 `ParseTemplate` 方法中。应采用策略模式将每种规则解耦。

*   **命名空间**: `FluidSequence.Domain.Services.Strategies`
*   **接口定义**:
    ```csharp
    public interface IPlaceholderStrategy
    {
        // 能够处理的占位符前缀，如 "RAND:", "yyyy"
        bool CanHandle(string placeholderKey); 
        // 处理逻辑
        string Handle(string placeholderKey, long currentSeq, Dictionary<string, string> context);
    }
    ```
*   **文件结构**:
    *   `FluidSequence.Domain/Services/SequenceDomainService.cs` (主入口，负责编排)
    *   `FluidSequence.Domain/Services/Strategies/TimeStrategy.cs` (处理 yyyy, MM, dd, mm, ss, QQ, ww)
    *   `FluidSequence.Domain/Services/Strategies/RandomStrategy.cs` (处理 RAND:NUM, RAND:SAFE 等)
    *   `FluidSequence.Domain/Services/Strategies/ContextStrategy.cs` (处理 UserCode, DeptCode 等)
    *   `FluidSequence.Domain/Services/Strategies/SequenceStrategy.cs` (处理 SEQ, SEQ36)

---

### 补充 4：细化开发检查清单 (替换原文档 8)

**8. 开发检查清单 (Detailed Checklist)**

#### Phase 1: 基础设施与核心 (Infrastructure & Core)
- [ ] **Database**: 创建 `sys_sequence_rule` 表，配置 PostgreSQL 索引与并发字段。
- [ ] **Entity**: 编写 `SysSequenceRule` 实体，实现 `TryReset` (含周/季/财年逻辑) 和 `NextValue` 方法。
- [ ] **Repository**: 确保 SqlSugar 仓储层正确处理 `Version` 乐观锁异常。
- [ ] **Strategies**: 
    - [ ] 实现 `TimeStrategy` (含 ISO 周计算、财年计算)。
    - [ ] 实现 `RandomStrategy` (实现“安全字符”字典，排除 I/O/Z/0/1)。
    - [ ] 实现 `SequenceStrategy` (含 Base36/Base62 转换算法)。

#### Phase 2: 领域服务 (Domain Service)
- [ ] **Engine**: 实现 `SequenceDomainService`，集成所有策略类。
- [ ] **Regex**: 优化正则匹配性能（使用 `RegexOptions.Compiled`）。
- [ ] **Concurrency**: 实现“乐观锁失败重试机制” (Polly 或 手写 while 循环)。

#### Phase 3: 应用层与 API (Application & API)
- [ ] **AppService**: 实现 `SequenceRuleAppService` (CRUD)。
- [ ] **Metadata API**: 实现 `GetPlaceholders` 接口供前端使用。
- [ ] **Preview API**: 实现 `TestGenerate` 接口（不存库，仅返回解析后的字符串）。

#### Phase 4: 前端实现 (UI)
- [ ] **UI**: 开发规则配置弹窗。
- [ ] **Interaction**: 实现点击标签按钮自动插入 `{Template}` 输入框的功能。
- [ ] **Validation**: 前端正则校验 Template 格式是否合法。

#### Phase 5: 测试与文档 (QA & Docs)
- [ ] **Unit Test**: 测试跨年、跨月、跨周重置逻辑是否正确。
- [ ] **Load Test**: 模拟高并发（50线程+）下的序列号唯一性与连续性。
- [ ] **Integration**: 模拟业务上下文缺失时的容错处理（如未传 DeptCode 时的默认行为）。

---

### 补充 5：文档生成提示 (新增章节 9)

## 9. 后续文档生成指令 (Post-Development Documentation)

> **⚠️ 重要提示 (To Developer/AI):**
> 
> 当本模块代码开发完成并通过测试后，**必须**执行以下文档生成任务，以确保交付完整性：
>
> 1.  **生成《详细使用文档》 (User Guide)**:
>     *   面向对象：业务开发人员。
>     *   内容：如何注入服务、如何构建 Context 字典、常见错误代码排查、如何扩展自定义策略。
> 2.  **生成《项目功能说明文档》 (Functional Specification)**:
>     *   面向对象：产品经理/最终用户。
>     *   内容：支持的占位符列表大全、重置规则的详细定义（如“周”是按自然周还是ISO周）、配置界面的操作截图说明。

---

你的思考非常深入，这些问题触及了系统架构的**灵活性**与**复杂度**之间的平衡点。

以下是对你 6 个问题的逐一深度解析，以及最终合并到 `CLAUDE.md` 的补充内容。

### 问题逐一回复

1.  **是否可以用机器内存替代 Redis 缓存？**
    *   **回复**: **可以，但有限制。**
    *   **场景**: 如果你的 Admin 系统是**单实例部署**（只部署在一台服务器上），使用 `IMemoryCache` (内存) 是最快且最省钱的。
    *   **风险**: 如果系统是**集群/负载均衡**部署（多台服务器），内存是不共享的。A 服务器缓存了规则，B 服务器没缓存，或者 A 锁了内存锁，B 照样能生成，会导致**重号**。
    *   **建议**: 采用 **抽象接口** 设计。代码依赖 `IDistributedCache`，在 `Program.cs` 中根据 `appsettings.json` 配置决定注入 `RedisCache` 还是 `MemoryDistributedCache`。

2.  **如果实现了"预取机制"，如何实现配置项开启？**
    *   **回复**: 预取（Hi-Lo）通常是针对**特定规则**的（例如“日志流水号”需要预取，“合同号”严禁预取）。
    *   **位置**: 应该配置在 **`Sys_Sequence_Rule` 表** 中。
    *   **实现**: 建议在表中增加一个 `ExtensionProps` (JSON) 字段，存储 `{ "EnableBuffer": true, "BufferCount": 100 }`，避免频繁修改表结构。

3.  **"数据库并发控制" 是否也可以以配置形式开启？**
    *   **回复**: **不建议动态配置**。
    *   **原因**: 乐观锁（Version）和悲观锁（For Update）的代码写法完全不同。动态切换会极大增加代码复杂度。
    *   **建议**: 默认统一使用 **乐观锁 + 重试机制**（这是最通用的）。只有在极少数“预取机制”开启时，配合悲观锁使用。这属于代码逻辑的内部决策，不建议暴露给用户配置。

4.  **"按财年重置" 如何配置起始时间？是否影响"按季度"？**
    *   **回复**:
        *   **配置**: 在 `Sys_Sequence_Rule` 表的扩展字段中配置 `{ "FiscalYearStartMonth": 4 }` (假设4月开始)。
        *   **影响**: **互不影响**。
            *   `ResetType.Quarterly` 默认指 **自然季度** (1-3月为Q1)。
            *   如果需要“财年季度”，建议新增枚举 `FiscalQuarterly`，或者在代码策略中约定：如果配置了财年起始月，季度也随之偏移。**建议保持独立**，即季度永远是自然季度，财年才看起始月，这样逻辑最清晰。

5.  **前端元数据的 key/label/group 存在哪里？数据库吗？**
    *   **回复**: **绝对不要存在数据库**。
    *   **原因**: 这些是**代码能力的映射**。如果你在代码里写了一个新的策略 `RandomStrategy`，却忘了往数据库插一条记录，系统就崩了或者选不出来。
    *   **建议**: 定义在 **代码常量** 或 **特性 (Attribute)** 中。应用启动时，通过反射或单例注册表加载，通过 API 返回给前端。

6.  **配置内容放在 appsettings.json 还是 Setting Providers？**
    *   **回复**: 分层放置。
        *   **基础设施配置** (Redis连接串、使用内存还是Redis)：放 `appsettings.json`。
        *   **业务规则配置** (步长、重置规则、财年月份)：放 **数据库表 (`Sys_Sequence_Rule`)**。
        *   **模块默认值** (默认重试次数)：放 `Setting Providers` (代码常量)。

---

### 补充内容 (追加到 CLAUDE.md)

请将以下内容追加到 `CLAUDE.md` 的末尾或对应章节。

---

### 补充 6：配置管理与扩展性设计 (Configuration & Extensibility)

#### 6.1 缓存与基础设施配置 (appsettings.json)
模块应支持单机与集群模式的切换，通过 .NET 标准抽象实现。

*   **配置文件**:
    ```json
    "FluidSequence": {
      "CacheProvider": "Redis", // 选项: "Redis" (集群推荐) 或 "Memory" (单机)
      "MaxRetryCount": 3,       // 乐观锁并发重试次数
      "LockTimeoutSeconds": 5   // 悲观锁/分布式锁超时时间
    }
    ```
*   **代码实现**: 在 `ServiceCollection` 注册时，根据 `CacheProvider` 决定注入 `AddStackExchangeRedisCache` 还是 `AddDistributedMemoryCache`。

#### 6.2 规则的扩展配置 (Sys_Sequence_Rule 表结构升级)
为了支持“财年起始月”、“预取数量”等非通用配置，且不频繁修改表结构，**强烈建议**在 `SysSequenceRule` 表中增加 JSON 扩展列。

*   **新增字段**:
    ```csharp
    /// <summary>
    /// 扩展属性 (JSON格式)
    /// 存储: FiscalYearStartMonth, EnableBuffer, BufferCount 等
    /// </summary>
    [SugarColumn(IsJson = true, ColumnDescription = "扩展属性")]
    public Dictionary<string, object> ExtensionProps { get; set; }
    ```

*   **配置示例 (JSON)**:
    ```json
    {
      "FiscalYearStartMonth": 4,   // 财年从4月开始
      "EnableBuffer": true,        // 开启预取(Hi-Lo)
      "BufferStep": 50             // 每次预取50个
    }
    ```

#### 6.3 财年与季度逻辑定义
*   **按季度重置 (`Quarterly`)**: 严格遵循**自然季度** (Jan-Mar=Q1, Apr-Jun=Q2...)，不受财年配置影响。
*   **按财年重置 (`FiscalYearly`)**: 读取 `ExtensionProps["FiscalYearStartMonth"]`。
    *   *逻辑*: 若起始月为 4，当前是 2024-02，则归属于 2023 财年；当前是 2024-04，则归属于 2024 财年。

#### 6.4 前端元数据存储 (Code-First)
元数据（占位符定义）是代码逻辑的反射，**严禁存储在数据库**。

*   **实现方式**: 定义一个静态注册表 `PlaceholderRegistry`。
*   **代码结构**:
    ```csharp
    public static class PlaceholderRegistry
    {
        public static readonly List<PlaceholderMeta> Definitions = new()
        {
            new() { Key = "{yyyy}", Label = "年份", Group = "时间" },
            new() { Key = "{SEQ}", Label = "序列号", Group = "核心" },
            // ... 新增策略时在此添加代码
        };
    }
    ```
*   **API**: `GET /api/sequence-rule/placeholders` 直接返回上述静态列表。

#### 6.5 预取机制 (Hi-Lo) 的配置开关
*   **开启方式**: 修改 `Sys_Sequence_Rule` 表中某条规则的 `ExtensionProps` -> `"EnableBuffer": true`。
*   **生效逻辑**: `SequenceDomainService` 在加载规则时，若发现此标记为 true，则跳过常规的 `NextValue` 逻辑，转而调用 `BufferStrategy` (从 Redis 或 内存队列中取号，若队列为空则去 DB 申请一批)。

---