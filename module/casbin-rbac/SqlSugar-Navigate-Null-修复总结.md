# SqlSugar Includes 导航属性失效 — 完整修复总结

## 前情回顾

在 [SqlSugar-Includes-Bug-分析报告.md](./SqlSugar-Includes-Bug-分析报告.md) 中，我们通过诊断码 `LOGIN_ERR_004` 确认了用户登录时角色列表为空，并初步将问题定位在 SqlSugar 的 `Includes` 导航加载机制失效上。但由于诊断方法学瑕疵（多查询并发干扰），得出了错误的根因结论（".NET 10 下 Includes 全局 Bug"）。

本文档从该报告之后继续，记录完整的排除过程和最终根因。

---

## 一、假说验证过程（共 7 轮）

### 假说 1：UnaryExpression 导致表达式树解析失败

> 另一 AI 专家提出：`!r.IsDeleted`（UnaryExpression）与 `r.IsDeleted == false`（BinaryExpression）在 SqlSugar 表达式树解析中行为不同，前者在嵌套 Includes 中无法正确翻译为 SQL。

**验证**：还原 `== false` 语法 + `#pragma warning disable IDE0075, IDE0083`

```csharp
.Includes(u => u.Roles.Where(r => r.IsDeleted == false).ToList(), ...)
```

**结果**：❌ 失败，RoleCount=0

**结论**：表达式树类型不是根因。

---

### 假说 2：`InSingleAsync` 不支持 Includes

**验证**：将所有 `InSingleAsync` 替换为 `FirstAsync` + `Where`、`ToListAsync`、`Take(1)` 等变体

**结果**：❌ 全部失败，RoleCount=0

**结论**：与查询方法无关。

---

### 假说 3：`[]` 集合表达式生成的特殊类型干扰反射

> 另一 AI 专家提出：C# 12 的 `= []` 集合表达式可能生成与 `new List<T>()` 不同的运行时类型，导致 SqlSugar 的 `SetValue` 反射失败。

**验证**：将所有导航属性的 `= []` 改为 `= new()`

**结果**：❌ 失败，RoleCount=0

**结论**：`[]` 语法不是根因。

---

### 假说 4：`Where` 过滤与全局软删除过滤器冲突

> 建议信任全局软删除过滤器，移除 Includes 中的 Where 条件，只保留 `.ToList()`

**验证**：使用最简 Include `.Includes(u => u.Roles.ToList(), r => r.Menus.ToList())`

**结果**：❌ 失败，RoleCount=0

**结论**：Where 过滤不是根因。

---

### 假说 5：`SqlSugarClient` vs `SqlSugarScope` 线程模型

> SqlSugarScope 是官方推荐的 DI 线程安全容器，可能影响导航查询

**验证**：替换 `new SqlSugarClient(...)` → `new SqlSugarScope(...)`

**结果**：❌ 未改善（且 SqlSugar 社区 Issue 证实与容器类型无关）

**结论**：容器类型不是根因。

---

### 假说 6：5 合 1 隔离测试

> 排除多查询并发干扰，每个 Include 变体独立执行

```csharp
测试A: Take(1) + ToListAsync + 单层Include → Roles=0
测试B: 手动查 UserRole（对照组）→ RoleIds=[9f8a1a52...] ✓
测试C: Where + ToListAsync + 单层Include → Roles=0
测试D: Where + ToListAsync + 两层Include → Roles=0
测试E: InSingleAsync + 单层Include → Roles=0
```

**结果**：全部 Include 变体返回 0，仅手动查询正常

**结论**：SqlSugar Includes 导航加载机制**彻底不工作**。

---

### 假说 7（最终）：导航属性赋了默认值

> SqlSugar 官方社区（果糖网）明确声明：**"一对多不能加默认值"**。
> ORM 底层通过反射判断属性是否为 null 来决定是否触发子表查询。
> 如果属性已被初始化为空集合，SqlSugar 会跳过导航加载。

**验证**：对比三个版本的导航属性初始化方式

| 版本 | 初始化方式 | 属性运行时状态 | SqlSugar 行为 |
|---|---|---|---|
| 最初版（正常） | `= null!;` | `null` | 触发导航查询 ✓ |
| 警告修复版（403） | `= []` | 空 List 实例 | 跳过导航查询 ✗ |
| 第一次 `new()` 修复（403） | `= new()` | 空 List 实例 | 跳过导航查询 ✗ |

**结果**：✅ 将导航属性还原为 `= null!;` 后，Includes 立即恢复正常，登录成功。

**结论**：**这是根因。** 为消除 CS8618 警告而给导航属性赋默认值（`= []` 或 `= new()`），导致 SqlSugar 认为属性已被手动初始化，从而跳过导航查询。

---

## 二、根因总结

### 故障链

```
消除 CS8618 警告
    ↓
将导航属性 = null! 改为 = [] 或 = new()
    ↓
SqlSugar 反射检查发现属性非 null（有空集合实例）
    ↓
SqlSugar 跳过 Includes 子表查询
    ↓
user.Roles.Count == 0
    ↓
GetTokenByUserIdAsync 抛出 UserFriendlyException("无任何角色")
    ↓
ABP 返回 HTTP 403 Forbidden
```

### SqlSugar 官方规则

> **一对多 / 多对多导航属性不能赋默认值，必须保持为 `null`。**
>
> —— SqlSugar 作者 fate sta，来源：[果糖网 Ask/9/725](https://www.donet5.com/Ask/9/725)

同时确认的 Gitee Issue：[#IC7Z7O](https://gitee.com/dotnetchina/SqlSugar/issues/IC7Z7O)（相同问题，待解决）

---

## 三、修复方案

### 实体层：导航属性初始化

**错误写法**（导致 Includes 失效）：
```csharp
[Navigate(typeof(UserRole), nameof(UserRole.UserId), nameof(UserRole.RoleId))]
public List<Role> Roles { get; set; } = [];         // ❌

[Navigate(typeof(UserRole), nameof(UserRole.UserId), nameof(UserRole.RoleId))]
public List<Role> Roles { get; set; } = new();       // ❌

[Navigate(typeof(UserRole), nameof(UserRole.UserId), nameof(UserRole.RoleId))]
public List<Role> Roles { get; set; } = new List<Role>(); // ❌
```

**正确写法**（SqlSugar 能正常加载导航）：
```csharp
[Navigate(typeof(UserRole), nameof(UserRole.UserId), nameof(UserRole.RoleId))]
public List<Role> Roles { get; set; } = null!;       // ✅ 推荐
```

### Repository 层：正常使用 Includes

```csharp
public async Task<User> GetUserAllInfoAsync(Guid userId)
{
    User user = await _DbQueryable
        .Includes(u => u.Roles.Where(r => !r.IsDeleted).ToList(),
                  r => r.Menus.Where(m => !m.IsDeleted).ToList())
        .InSingleAsync(userId);
    return user;
}
```

---

## 四、`= null!;` vs 无初始值 的区别

| 写法 | 运行时 | 编译时 | 适用场景 |
|---|---|---|---|
| `= null!;` | `null` | 无警告（显式抑制 CS8618） | ORM 导航属性、DI 注入字段 |
| 无初始值 | `null` | CS8618 警告 | 不关心警告时可省略 |
| `= [];` / `= new()` | 空集合实例 | 无警告 | **普通集合**，但**不能用于 ORM 导航属性** |

**推荐**：导航属性使用 `= null!;`，明确表达"ORM 负责初始化"的语义，同时消除编译警告。

---

## 五、同类问题排查清单

项目中可能存在同样问题的导航属性，按以下步骤排查：

### 已修复（4 处）
- [x] `User.Roles` → `= null!;`
- [x] `User.Posts` → `= null!;`
- [x] `Menu.Children` → `= null!;`
- [x] `Department.Children` → `= null!;`

### 待排查

使用以下 grep 命令查找所有带 `[Navigate]` 且赋了默认值的属性：

```bash
# 查找 Navigate 属性
grep -rn "\[Navigate" --include="*.cs" module/ framework/ src/

# 查找赋了默认值的 List 属性（可疑模式）
grep -rn "List<.*>.*= \[\]" --include="*.cs" module/ framework/ src/
grep -rn "List<.*>.*= new()" --include="*.cs" module/ framework/ src/
grep -rn "List<.*>.*= new List" --include="*.cs" module/ framework/ src/
```

### 更全面的排查（用 PowerShell）

```powershell
# 找到所有 [Navigate] 属性并检查其初始化方式
$files = Get-ChildItem -Recurse -Filter "*.cs" -Path "E:\Projects\SharpFort.Net"
foreach ($f in $files) {
    $content = Get-Content $f.FullName -Raw
    if ($content -match '\[Navigate') {
        # 检查下一行是否有 = [] 或 = new()
        if ($content -match '\[Navigate[^\]]*\]\s*\n\s*public\s+\S+\s+\S+\s*\{\s*get;\s*set;\s*\}\s*=\s*(\[\]|new\(\)|new List)') {
            Write-Host "WARNING: $($f.FullName)" -ForegroundColor Yellow
        }
    }
}
```

### 已确认受影响的其他文件

| 文件 | 属性 | Navigate 类型 | 当前状态 |
|---|---|---|---|
| `UserService.cs:80` | `u => u.Roles` | ManyToMany via UserRole | 使用 Includes，实体修复后应正常 |
| `UserService.cs:81` | `u => u.Posts` | ManyToMany via UserPosition | 使用 Includes，实体修复后应正常 |
| `UserService.cs:183` | `u => u.Roles, u.Posts, u.Dept` | 混合 | 使用 Includes，实体修复后应正常 |
| `CodeGenService.cs:29` | `x => x.Fields` | OneToMany | 需检查 `Table.Fields` 初始化方式 |

---

## 六、经验教训

1. **代码分析器警告并非总是正确的**。CS8618 警告建议初始化非空属性，但 ORM 导航属性恰好需要保持 `null` 才能工作（这是 ORM 的"抽象漏洞" Leaky Abstraction）。

2. **`null!` 是正确的工具**。它不是 Hack，而是 C# 语言为"我知道它会是 null 但运行时会被填充"这一场景设计的标准方案。

3. **ORM 的行为规则比语法正确性更重要**。理解 SqlSugar 对导航属性的 null 检查机制，比消除编译警告更优先。

4. **多查询并发诊断可能导致误判**。最初的诊断中三个 Include 查询连续执行，导致无法准确判断哪个条件真正影响结果。

5. **查阅官方社区和 Issue 是关键的排查手段**。SqlSugar 作者在果糖网上的明确答复（"一对多不能加默认值"）直接指出了根因。
