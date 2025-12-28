# Masuit.Tools TreeExtensions PR 草案

> 创建日期: 2025-11-18
> 目的: 为 Masuit.Tools.TreeExtensions 提交功能增强 PR

---

## 一、PR 概述

### 标题
feat(TreeExtensions): Add node callback and custom comparer support for ToTree method

### 简介
增强 `ToTree()` 方法，支持节点处理回调和自定义排序比较器，提高树形结构转换的灵活性。

---

## 二、功能需求背景

### 2.1 当前使用场景

在 Yi.Framework 项目中，我们有一个 `TreeHelper` 类，它提供了一个独特的功能：

```csharp
// 当前 Yi.Framework.TreeHelper 的使用
var routers = TreeHelper.SetTree(menuList, item =>
{
    // 在构建树时对每个节点执行额外操作
    item.Label = item.Name;
    item.Value = item.Id;
});
```

### 2.2 Masuit.Tools 当前 API

```csharp
// 当前 Masuit.Tools ToTree() 签名
public static List<T> ToTree<T>(
    this IEnumerable<T> items,
    T parent = default)
    where T : class, ITree<T>;
```

### 2.3 功能差距

| 功能 | TreeHelper | Masuit.Tools |
|------|------------|--------------|
| 节点回调 | ✅ Action<T> | ❌ |
| 自定义排序 | ✅ OrderNum | ❌ |
| 接口定义 | ITreeModel<T> | ITree<T> |

---

## 三、建议的 API 增强

### 3.1 新增重载方法

```csharp
namespace Masuit.Tools.Core
{
    public static class TreeExtensions
    {
        /// <summary>
        /// 将平铺列表转换为树形结构，支持节点回调和自定义排序
        /// </summary>
        /// <typeparam name="T">树节点类型</typeparam>
        /// <param name="items">平铺的节点列表</param>
        /// <param name="parent">根节点的父节点值（默认 default）</param>
        /// <param name="onNode">节点处理回调，在每个节点添加到树时调用</param>
        /// <param name="comparer">子节点排序比较器</param>
        /// <returns>树形结构列表</returns>
        /// <example>
        /// <code>
        /// var tree = menuList.ToTree(default,
        ///     onNode: item => {
        ///         item.Label = item.Name;
        ///         item.Value = item.Id;
        ///     },
        ///     comparer: Comparer&lt;Menu&gt;.Create((a, b) => b.OrderNum.CompareTo(a.OrderNum)));
        /// </code>
        /// </example>
        public static List<T> ToTree<T>(
            this IEnumerable<T> items,
            T parent = default,
            Action<T>? onNode = null,
            IComparer<T>? comparer = null)
            where T : class, ITree<T>
        {
            var lookup = items.ToLookup(i => i.Parent);

            List<T> BuildTree(T parentNode)
            {
                var children = lookup[parentNode].ToList();

                if (comparer != null)
                {
                    children.Sort(comparer);
                }

                foreach (var child in children)
                {
                    // 执行节点回调
                    onNode?.Invoke(child);

                    // 递归构建子树
                    child.Children = BuildTree(child);
                }

                return children;
            }

            return BuildTree(parent);
        }

        /// <summary>
        /// 将平铺列表转换为树形结构（使用自定义 ID 选择器）
        /// </summary>
        /// <typeparam name="T">节点类型</typeparam>
        /// <typeparam name="TKey">ID 类型</typeparam>
        /// <param name="items">平铺的节点列表</param>
        /// <param name="idSelector">ID 选择器</param>
        /// <param name="parentIdSelector">父 ID 选择器</param>
        /// <param name="childrenSetter">子节点设置器</param>
        /// <param name="rootParentId">根节点的父 ID 值</param>
        /// <param name="onNode">节点处理回调</param>
        /// <param name="comparer">子节点排序比较器</param>
        /// <returns>树形结构列表</returns>
        public static List<T> ToTree<T, TKey>(
            this IEnumerable<T> items,
            Func<T, TKey> idSelector,
            Func<T, TKey> parentIdSelector,
            Action<T, List<T>> childrenSetter,
            TKey rootParentId = default,
            Action<T>? onNode = null,
            IComparer<T>? comparer = null)
            where T : class
        {
            var lookup = items.ToLookup(parentIdSelector);
            var equalityComparer = EqualityComparer<TKey>.Default;

            List<T> BuildTree(TKey parentId)
            {
                var children = lookup[parentId].ToList();

                if (comparer != null)
                {
                    children.Sort(comparer);
                }

                foreach (var child in children)
                {
                    // 执行节点回调
                    onNode?.Invoke(child);

                    // 递归构建子树
                    var grandChildren = BuildTree(idSelector(child));
                    childrenSetter(child, grandChildren);
                }

                return children;
            }

            return BuildTree(rootParentId);
        }
    }
}
```

### 3.2 使用示例

```csharp
// 示例 1：基础用法（ITree<T> 接口）
public class Menu : ITree<Menu>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int OrderNum { get; set; }
    public Menu Parent { get; set; }
    public ICollection<Menu> Children { get; set; }

    // 额外属性
    public string Label { get; set; }
    public int Value { get; set; }
}

var tree = menuList.ToTree(
    onNode: item => {
        // 在构建树时设置额外属性
        item.Label = item.Name;
        item.Value = item.Id;
    },
    comparer: Comparer<Menu>.Create((a, b) => b.OrderNum.CompareTo(a.OrderNum))
);

// 示例 2：不实现 ITree<T> 的类型
public class Department
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; }
    public int Sort { get; set; }
    public List<Department> Children { get; set; }
}

var deptTree = departments.ToTree(
    idSelector: d => d.Id,
    parentIdSelector: d => d.ParentId ?? Guid.Empty,
    childrenSetter: (d, children) => d.Children = children,
    rootParentId: Guid.Empty,
    onNode: d => Console.WriteLine($"Processing: {d.Name}"),
    comparer: Comparer<Department>.Create((a, b) => a.Sort.CompareTo(b.Sort))
);
```

---

## 四、向后兼容性

### 4.1 保持现有 API 不变

所有新增参数都有默认值，现有代码无需修改：

```csharp
// 现有代码继续工作
var tree = items.ToTree();

// 新功能为可选
var tree = items.ToTree(onNode: x => x.Label = x.Name);
```

### 4.2 性能考虑

- 当 `onNode` 为 null 时，无额外开销
- 当 `comparer` 为 null 时，保持原有顺序
- 使用 `ToLookup` 优化查找性能 O(1)

---

## 五、单元测试建议

```csharp
[TestClass]
public class TreeExtensionsTests
{
    [TestMethod]
    public void ToTree_WithOnNodeCallback_ShouldInvokeForEachNode()
    {
        // Arrange
        var items = new List<TestNode>
        {
            new TestNode { Id = 1, ParentId = 0, Name = "Root" },
            new TestNode { Id = 2, ParentId = 1, Name = "Child1" },
            new TestNode { Id = 3, ParentId = 1, Name = "Child2" }
        };
        var processedCount = 0;

        // Act
        var tree = items.ToTree(
            onNode: _ => processedCount++
        );

        // Assert
        Assert.AreEqual(3, processedCount);
    }

    [TestMethod]
    public void ToTree_WithComparer_ShouldSortChildren()
    {
        // Arrange
        var items = new List<TestNode>
        {
            new TestNode { Id = 1, ParentId = 0, OrderNum = 1 },
            new TestNode { Id = 2, ParentId = 1, OrderNum = 3 },
            new TestNode { Id = 3, ParentId = 1, OrderNum = 1 },
            new TestNode { Id = 4, ParentId = 1, OrderNum = 2 }
        };

        // Act
        var tree = items.ToTree(
            comparer: Comparer<TestNode>.Create((a, b) => a.OrderNum.CompareTo(b.OrderNum))
        );

        // Assert
        var children = tree[0].Children.ToList();
        Assert.AreEqual(3, children[0].Id); // OrderNum = 1
        Assert.AreEqual(4, children[1].Id); // OrderNum = 2
        Assert.AreEqual(2, children[2].Id); // OrderNum = 3
    }
}
```

---

## 六、PR 提交计划

### 6.1 准备工作

1. Fork Masuit.Tools 仓库
2. 创建功能分支 `feature/tree-extensions-callback`
3. 实现代码更改
4. 编写单元测试
5. 更新 README 文档

### 6.2 PR 描述模板

```markdown
## What does this PR do?

Adds support for node processing callback and custom sorting comparer to the `ToTree()` method in TreeExtensions.

## Why is this change needed?

When building tree structures, developers often need to:
1. Perform additional processing on each node (e.g., setting computed properties)
2. Sort children in a specific order

Currently, this requires post-processing the tree or implementing custom logic.

## Changes

- Add `onNode` parameter to invoke callback for each node
- Add `comparer` parameter to sort children
- Add overload with custom ID selectors for non-ITree types
- All new parameters are optional with backward compatibility

## Testing

- Added unit tests for callback invocation
- Added unit tests for sorting behavior
- Verified backward compatibility with existing API

## Breaking Changes

None. All new parameters have default values.
```

---

## 七、替代方案

如果 PR 未被接受，可以考虑以下替代方案：

### 7.1 扩展方法包装

```csharp
// 在项目中创建扩展方法包装 Masuit.Tools
public static class TreeExtensionsWrapper
{
    public static List<T> ToTreeWithCallback<T>(
        this IEnumerable<T> items,
        Action<T> onNode)
        where T : class, ITree<T>
    {
        var tree = items.ToTree();
        TraverseTree(tree, onNode);
        return tree;
    }

    private static void TraverseTree<T>(List<T> nodes, Action<T> action)
        where T : ITree<T>
    {
        foreach (var node in nodes)
        {
            action(node);
            if (node.Children?.Any() == true)
            {
                TraverseTree(node.Children.ToList(), action);
            }
        }
    }
}
```

### 7.2 保留 TreeHelper

继续使用现有的 `TreeHelper.cs`，但添加文档说明其与 Masuit.Tools 的差异。

---

## 八、联系信息

- Masuit.Tools 仓库: https://github.com/ldqk/Masuit.Tools
- 作者: 懒得勤快
- Issue 跟踪: 建议先创建 Issue 讨论功能需求

---

**文档版本**: v1.0
**最后更新**: 2025-11-18
