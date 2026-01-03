# 字段级权限高性能实现方案

## 1. 核心思路
利用 `System.Text.Json` 的 `JsonConverter` 机制进行拦截。为了避免反射性能损耗，在系统启动时（或首次序列化时）构建缓存。

## 2. 组件设计

### 2.1 缓存服务 (FieldPermissionCache)
*   **位置**: `Yi.Framework.CasbinRbac.Domain.Infrastructure` (或 Managers)
*   **结构**: `ConcurrentDictionary<Guid, Dictionary<string, HashSet<string>>>`
    *   Key: `RoleId`
    *   Value: `Map<TableName, Set<FieldName>>`
*   **更新机制**: 
    *   启动时全量加载.
    *   当 `RoleField` 表变更时，发布事件刷新缓存.

### 2.2 标记特性 (SecureResourceAttribute)
*   **位置**: `Yi.Framework.CasbinRbac.Domain.Shared.Attributes`
*   **作用**: 标记在 DTO 或 Entity 上，指定其对应的 `TableName` (资源名).
*   **示例**: `[SecureResource("sys_user")] public class UserDto ...`

### 2.3 安全序列化转换器 (FieldSecurityConverter)
*   **类型**: `JsonConverterFactory`
*   **逻辑**:
    1.  `CanConvert`: 检查类型是否有 `[SecureResource]` 特性。
    2.  `CreateConverter`: 返回泛型转换器 `FieldSecurityConverter<T>`.
    3.  `Write`:
        *   获取 `CurrentUser` 的所有 RoleIds.
        *   调用 `FieldPermissionCache` 计算该用户针对当前资源 (TableName) 的 **Union DenyList**.
        *   遍历 T 的属性 (Properties).
        *   如果属性名在 DenyList 中，跳过.
        *   否则，序列化属性值.

### 2.4 性能优化点 (Critical)
*   **属性元数据缓存**: `FieldSecurityConverter<T>` 内部静态缓存 `List<PropertyInfo>` 或 `JsonTypeInfo`，避免每次 `Write` 都反射 `Type.GetProperties()`.
*   **快速查找**: DenyList 使用 `HashSet<string>` (Case-insensitive) 保证 O(1) 查找.

## 3. 实施步骤

1.  创建 `SecureResourceAttribute`.
2.  创建 `IFieldPermissionCache` 及实现 (Singleton).
3.  创建 `FieldSecurityJsonConverterFactory` 和 `FieldSecurityConverter<T>`.
4.  在 `YiFrameworkCasbinRbacWebModule` (或 Application) 中注册:
    ```csharp
    context.Services.Configure<JsonOptions>(options => {
        options.JsonSerializerOptions.Converters.Add(new FieldSecurityJsonConverterFactory(...));
    });
    ```

## 4. 依赖
需要引入 `Microsoft.AspNetCore.HttpContextAccessor` 以便在 Converter 中获取当前用户.
需要引入 `System.Text.Json`.
