# SqlSugar 导航属性写法指南

## 一、`virtual` vs `= null!;` 对比

这两个特性本质上是**正交的**（解决不同的问题），可以并存：

```csharp
// 两全其美
public virtual List<Role> Roles { get; set; } = null!;
```

### `virtual` — 为代理类服务

| | 说明 |
|---|---|
| **来源** | Entity Framework 时代的遗产 |
| **作用** | 允许 ORM 在运行时生成动态代理子类，拦截属性访问实现**懒加载**（Lazy Loading） |
| **在 SqlSugar 中** | **无意义**——SqlSugar 不使用代理类，懒加载通过 `Includes` / `Mapper` 显式触发 |
| **开销** | 几乎为零（一个虚方法表条目），不影响性能 |

### `= null!;` — 为 SqlSugar 服务

| | 说明 |
|---|---|
| **来源** | C# 8+ nullable reference types |
| **作用** | 保持属性运行时为 `null`，同时**抑制 CS8618 编译警告** |
| **在 SqlSugar 中** | **关键**——属性非 null 会导致 ORM 跳过导航查询 |
| **开销** | 零（纯编译期概念，运行时就是 `null`） |

### 为什么 AuditLogging 模块用 `virtual`

那是 ABP 官方模块代码风格，最初为 EF Core 设计。迁移到 SqlSugar 后 `virtual` 成了摆设——**不产生影响，但也没必要去掉**。

---

## 二、推荐写法

```csharp
// 最佳实践：两者兼有
[Navigate(typeof(UserRole), nameof(UserRole.UserId), nameof(UserRole.RoleId))]
public virtual List<Role> Roles { get; set; } = null!;
//      ^^^^^^^                              ^^^^^^
//      无实际作用但无害                      保证 SqlSugar 导航加载
```

如果追求极简，去掉 `virtual` 干净利落：

```csharp
// 极简版：只有必须的部分
[Navigate(typeof(UserRole), nameof(UserRole.UserId), nameof(UserRole.RoleId))]
public List<Role> Roles { get; set; } = null!;
```

**两者运行时行为完全一致**。选哪个纯粹是代码风格偏好，对 SqlSugar 导航加载没有任何区别。

---

## 三、OneToOne vs OneToMany：SqlSugar 的 null 要求

**SqlSugar 要求完全一致**——所有导航属性在加载前必须为 `null`。但实际工程中风险分布不同：

### OneToOne（单引用导航）

项目中 13 处 OneToOne，**全部声明为可空类型 `T?`**：

```csharp
public Department? Dept { get; set; }       // 默认 null，无 CS8618 警告
public DirectoryDescriptor? Parent { get; set; }
public User? User { get; set; }
```

| 特征 | 说明 |
|---|---|
| 默认值 | `null`（引用类型自然默认） |
| CS8618 | 无（`T?` 允许 null） |
| 被误改风险 | **极低**——没人会对单引用写 `= new Department()` |
| SqlSugar 兼容 | ✅ 天然兼容 |

### OneToMany / ManyToMany（集合导航）

项目中 11 处集合导航，存在**三种写法**：

```csharp
// 写法 1：= null!;（正确，SqlSugar 兼容）
public List<Role> Roles { get; set; } = null!;       // ✅ 运行时 null，无警告

// 写法 2：可空类型（正确，SqlSugar 兼容）
public List<Menu>? Menus { get; set; }               // ✅ 运行时 null，无警告

// 写法 3：无初始化（可用但不够安全）
public List<EntityChange> EntityChanges { get; set; } // ⚠ 运行时 null，但 CS8618
```

| 特征 | 说明 |
|---|---|
| 默认值 | `null`（但 IDE 会建议初始化） |
| CS8618 | 有（非空集合未初始化） |
| 被误改风险 | **极高**——IDE 会主动建议改为 `= []` / `= new()` |
| SqlSugar 兼容 | ✅ 只有保持 null 才兼容 |

### 结论

> OneToOne 天然免疫此问题（`T?` 不会触发初始化冲动）。**风险集中在集合导航属性**。规则就一条：`List<T>` 导航要么 `= null!;`，要么 `List<T>?`，绝不能有默认实例。

---

## 四、完整写法速查表

| 导航类型 | 推荐写法 | 说明 |
|---|---|---|
| OneToOne（聚合内） | `public Department? Dept { get; set; }` | 可空类型，天然 null |
| OneToOne（跨聚合） | `public User? User { get; set; }` | 可空类型，天然 null |
| OneToMany | `public List<T> Children { get; set; } = null!;` | 必须抑制 CS8618 |
| ManyToMany（中间表） | `public List<Role> Roles { get; set; } = null!;` | 必须抑制 CS8618 |
| ManyToMany（可空集合） | `public List<Menu>? Menus { get; set; }` | `T?` 允许 null |

---

## 五、错误写法（禁止）

```csharp
// ❌ 会导致 SqlSugar Includes 跳过导航加载
[Navigate(...)]
public List<Role> Roles { get; set; } = [];          // C# 12 集合表达式

// ❌ 同上
[Navigate(...)]
public List<Role> Roles { get; set; } = new();        // 目标类型 new

// ❌ 同上
[Navigate(...)]
public List<Role> Roles { get; set; } = new List<Role>(); // 显式 new
```

以上三种写法都会创建一个**空的 List 实例**，使 SqlSugar 判断导航属性"已被初始化"而跳过子表查询。
