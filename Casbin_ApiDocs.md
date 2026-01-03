> ⚠️ 警告: 在路径 {xmlPath} 未找到 XML 文档文件。生成的文档将没有注释说明，只有方法签名。

## EnforceContext (Casbin)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `EnforceContext Create(IEnforcer enforcer, Boolean explain)`

#### `EnforceContext Create(IEnforcer enforcer, String requestType, String policyType, String effectType, String matcherType, Boolean explain)`

#### `EnforceContext CreateWithMatcher(IEnforcer enforcer, String matcher, Boolean explain)`

#### `EnforceContext CreateWithMatcher(IEnforcer enforcer, String matcher, String requestType, String policyType, String effectType, Boolean explain)`

---
## Enforcer (Casbin)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `Boolean Enforce(EnforceContext context, TRequest requestValues)`

#### `Task<Boolean> EnforceAsync(EnforceContext context, TRequest requestValues)`

#### `IEnumerable<Boolean> BatchEnforce(EnforceContext context, IEnumerable<TRequest> requestValues)`

#### `IEnumerable<Boolean> ParallelBatchEnforce(EnforceContext context, IReadOnlyList<TRequest> requestValues, Int32 maxDegreeOfParallelism)`

#### `IAsyncEnumerable<Boolean> BatchEnforceAsync(EnforceContext context, IEnumerable<TRequest> requestValues)`

---
## EnforcerExtension (Casbin)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `IEnumerable<Boolean> BatchEnforce(IEnforcer enforcer, IEnumerable<T> values)`

#### `IEnumerable<Boolean> ParallelBatchEnforce(Enforcer enforcer, IReadOnlyList<T> values, Int32 maxDegreeOfParallelism)`

#### `IAsyncEnumerable<Boolean> BatchEnforceAsync(IEnforcer enforcer, IEnumerable<T> values)`

#### `IEnumerable<Boolean> BatchEnforceWithMatcher(IEnforcer enforcer, String matcher, IEnumerable<T> values)`

#### `IEnumerable<Boolean> BatchEnforceWithMatcherParallel(Enforcer enforcer, String matcher, IReadOnlyList<T> values, Int32 maxDegreeOfParallelism)`

#### `IAsyncEnumerable<Boolean> BatchEnforceWithMatcherAsync(IEnforcer enforcer, String matcher, IEnumerable<T> values)`

#### `Void LoadModel(IEnforcer enforcer)`

#### `IEnforcer EnableEnforce(IEnforcer enforcer, Boolean enable)`

#### `IEnforcer EnableAutoSave(IEnforcer enforcer, Boolean autoSave)`

#### `IEnforcer EnableAutoBuildRoleLinks(IEnforcer enforcer, Boolean autoBuildRoleLinks)`

#### `IEnforcer EnableAutoNotifyWatcher(IEnforcer enforcer, Boolean autoNotifyWatcher)`

#### `IEnforcer EnableCache(IEnforcer enforcer, Boolean enableCache)`

#### `IEnforcer EnableAutoCleanEnforceCache(IEnforcer enforcer, Boolean autoCleanEnforceCache)`

#### `IEnforcer SetEffector(IEnforcer enforcer, IEffector effector)`

#### `IEnforcer SetModel(IEnforcer enforcer, String modelPath)`

#### `IEnforcer SetModel(IEnforcer enforcer, IModel model)`

#### `IEnforcer SetAdapter(IEnforcer enforcer, IReadOnlyAdapter adapter)`

#### `IEnforcer SetWatcher(IEnforcer enforcer, IWatcher watcher)`

#### `IEnforcer SetEnforceCache(IEnforcer enforcer, IEnforceCache enforceCache)`

#### `Boolean LoadPolicy(IEnforcer enforcer)`

#### `Task<Boolean> LoadPolicyAsync(IEnforcer enforcer)`

#### `Boolean LoadFilteredPolicy(IEnforcer enforcer, IPolicyFilter filter)`

#### `Task<Boolean> LoadFilteredPolicyAsync(IEnforcer enforcer, IPolicyFilter filter)`

#### `Task<Boolean> LoadFilteredPolicyAsync(IEnforcer enforcer, Filter filter)`

#### `Boolean LoadIncrementalFilteredPolicy(IEnforcer enforcer, IPolicyFilter filter)`

#### `Task<Boolean> LoadIncrementalFilteredPolicyAsync(IEnforcer enforcer, IPolicyFilter filter)`

#### `Boolean SavePolicy(IEnforcer enforcer)`

#### `Task<Boolean> SavePolicyAsync(IEnforcer enforcer)`

#### `Void ClearPolicy(IEnforcer enforcer)`

#### `Void ClearCache(IEnforcer enforcer)`

#### `Void BuildRoleLinks(IEnforcer enforcer)`

#### `IRoleManager GetRoleManager(IEnforcer enforcer, String roleType)`

#### `Void SetRoleManager(IEnforcer enforcer, IRoleManager roleManager)`

#### `Void SetRoleManager(IEnforcer enforcer, String roleType, IRoleManager roleManager)`

#### `Void AddMatchingFunc(IEnforcer enforcer, Func<String, String, Boolean> func)`

#### `Void AddDomainMatchingFunc(IEnforcer enforcer, Func<String, String, Boolean> func)`

#### `Void AddNamedMatchingFunc(IEnforcer enforcer, String roleType, Func<String, String, Boolean> func)`

#### `Void AddNamedDomainMatchingFunc(IEnforcer enforcer, String roleType, Func<String, String, Boolean> func)`

#### `EnforceContext CreateContext(IEnforcer enforcer, Boolean explain)`

#### `EnforceContext CreateContext(IEnforcer enforcer, String requestType, String policyType, String effectType, String matcherType, Boolean explain)`

#### `EnforceContext CreateContextWithMatcher(IEnforcer enforcer, String matcher, Boolean explain)`

#### `EnforceContext CreateContextWithMatcher(IEnforcer enforcer, String matcher, String requestType, String policyType, String effectType, Boolean explain)`

#### `Boolean Enforce(IEnforcer enforcer, T[] value)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, T[] value)`

#### `Boolean Enforce(IEnforcer enforcer, EnforceContext context, T[] value)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, EnforceContext context, T[] value)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceEx(IEnforcer enforcer, T[] value)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExAsync(IEnforcer enforcer, T[] value)`

#### `Boolean EnforceWithMatcher(IEnforcer enforcer, String matcher, T[] value)`

#### `Task<Boolean> EnforceWithMatcherAsync(IEnforcer enforcer, String matcher, T[] value)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceExWithMatcher(IEnforcer enforcer, String matcher, T[] value)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExWithMatcherAsync(IEnforcer enforcer, String matcher, T[] value)`

#### `Boolean Enforce(IEnforcer enforcer, T value)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, T value)`

#### `Boolean Enforce(IEnforcer enforcer, EnforceContext context, T value)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, EnforceContext context, T value)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceEx(IEnforcer enforcer, T value1)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExAsync(IEnforcer enforcer, T value)`

#### `Boolean EnforceWithMatcher(IEnforcer enforcer, String matcher, T value)`

#### `Task<Boolean> EnforceWithMatcherAsync(IEnforcer enforcer, String matcher, T value)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceExWithMatcher(IEnforcer enforcer, String matcher, T value)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExWithMatcherAsync(IEnforcer enforcer, String matcher, T value)`

#### `Boolean Enforce(IEnforcer enforcer, T1 value1, T2 value2)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, T1 value1, T2 value2)`

#### `Boolean Enforce(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceEx(IEnforcer enforcer, T1 value1, T2 value2)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExAsync(IEnforcer enforcer, T1 value1, T2 value2)`

#### `Boolean EnforceWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2)`

#### `Task<Boolean> EnforceWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceExWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2)`

#### `Boolean Enforce(IEnforcer enforcer, T1 value1, T2 value2, T3 value3)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3)`

#### `Boolean Enforce(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceEx(IEnforcer enforcer, T1 value1, T2 value2, T3 value3)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3)`

#### `Boolean EnforceWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3)`

#### `Task<Boolean> EnforceWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceExWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3)`

#### `Boolean Enforce(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4)`

#### `Boolean Enforce(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceEx(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4)`

#### `Boolean EnforceWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4)`

#### `Task<Boolean> EnforceWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceExWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4)`

#### `Boolean Enforce(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)`

#### `Boolean Enforce(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceEx(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)`

#### `Boolean EnforceWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)`

#### `Task<Boolean> EnforceWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceExWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)`

#### `Boolean Enforce(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)`

#### `Boolean Enforce(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceEx(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)`

#### `Boolean EnforceWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)`

#### `Task<Boolean> EnforceWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceExWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)`

#### `Boolean Enforce(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)`

#### `Boolean Enforce(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceEx(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)`

#### `Boolean EnforceWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)`

#### `Task<Boolean> EnforceWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceExWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)`

#### `Boolean Enforce(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)`

#### `Boolean Enforce(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceEx(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)`

#### `Boolean EnforceWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)`

#### `Task<Boolean> EnforceWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceExWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)`

#### `Boolean Enforce(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9)`

#### `Boolean Enforce(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceEx(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9)`

#### `Boolean EnforceWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9)`

#### `Task<Boolean> EnforceWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceExWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9)`

#### `Boolean Enforce(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10)`

#### `Boolean Enforce(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceEx(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10)`

#### `Boolean EnforceWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10)`

#### `Task<Boolean> EnforceWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceExWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10)`

#### `Boolean Enforce(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11)`

#### `Boolean Enforce(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceEx(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11)`

#### `Boolean EnforceWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11)`

#### `Task<Boolean> EnforceWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceExWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11)`

#### `Boolean Enforce(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12)`

#### `Boolean Enforce(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceEx(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12)`

#### `Boolean EnforceWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12)`

#### `Task<Boolean> EnforceWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceExWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12)`

#### `Boolean Enforce(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13)`

#### `Boolean Enforce(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceEx(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13)`

#### `Boolean EnforceWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13)`

#### `Task<Boolean> EnforceWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceExWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13)`

#### `Boolean Enforce(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14)`

#### `Boolean Enforce(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14)`

#### `Task<Boolean> EnforceAsync(IEnforcer enforcer, EnforceContext context, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceEx(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExAsync(IEnforcer enforcer, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14)`

#### `Boolean EnforceWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14)`

#### `Task<Boolean> EnforceWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14)`

#### `ValueTuple<Boolean, IEnumerable<IEnumerable<String>>> EnforceExWithMatcher(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14)`

#### `Task<ValueTuple<Boolean, IEnumerable<IEnumerable<String>>>> EnforceExWithMatcherAsync(IEnforcer enforcer, String matcher, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14)`

---
## EnforcerOptions (Casbin)
- **Type**: Public, BeforeFieldInit

### 方法列表
---
## EnforceSession (Casbin)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

---
## EnforceView (Casbin)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `EnforceView Create(IModel model, String requestType, String policyType, String effectType, String matcherType)`

#### `EnforceView CreateWithMatcher(IModel model, String matcher, String requestType, String policyType, String effectType)`

#### `String TransformMatcher(EnforceView& view, String matcher)`

---
## IEnforcer (Casbin)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Boolean Enforce(EnforceContext context, TRequest requestValues)`

#### `Task<Boolean> EnforceAsync(EnforceContext context, TRequest requestValues)`

#### `IEnumerable<Boolean> BatchEnforce(EnforceContext context, IEnumerable<TRequest> requestValues)`

#### `IAsyncEnumerable<Boolean> BatchEnforceAsync(EnforceContext context, IEnumerable<TRequest> requestValues)`

---
## IPAddressExtension (Casbin)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean Match(IPAddress matchIpAddress, IPAddress ipAddress, Byte matchCidr)`

---
## LoggerExtension (Casbin)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `Void LogEnforceCachedResult(ILogger logger, TRequest& requestValues, Boolean result)`

#### `Void LogEnforceResult(ILogger logger, TRequest& requestValues, Boolean result)`

#### `Void LogEnforceResult(ILogger logger, TRequest& requestValues, Boolean result, IEnumerable<IEnumerable<String>> explains)`

---
## ManagementEnforcerExtension (Casbin)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `Void AddFunction(IEnforcer enforcer, String name, T function)`

#### `IEnumerable<String> GetAllSubjects(IEnforcer enforcer)`

#### `IEnumerable<String> GetAllNamedSubjects(IEnforcer enforcer, String policyType)`

#### `IEnumerable<String> GetAllObjects(IEnforcer enforcer)`

#### `IEnumerable<String> GetAllNamedObjects(IEnforcer enforcer, String policyType)`

#### `IEnumerable<String> GetAllActions(IEnforcer enforcer)`

#### `IEnumerable<String> GetAllNamedActions(IEnforcer enforcer, String policyType)`

#### `IEnumerable<IEnumerable<String>> GetPolicy(IEnforcer enforcer)`

#### `IEnumerable<IEnumerable<String>> GetNamedPolicy(IEnforcer enforcer, String policyType)`

#### `IEnumerable<IEnumerable<String>> GetFilteredPolicy(IEnforcer enforcer, Int32 fieldIndex, String[] fieldValues)`

#### `IEnumerable<IEnumerable<String>> GetFilteredNamedPolicy(IEnforcer enforcer, String policyType, Int32 fieldIndex, String[] fieldValues)`

#### `Boolean HasPolicy(IEnforcer enforcer, IEnumerable<String> values)`

#### `Boolean HasPolicy(IEnforcer enforcer, String[] values)`

#### `Boolean HasNamedPolicy(IEnforcer enforcer, String policyType, IEnumerable<String> values)`

#### `Boolean HasNamedPolicy(IEnforcer enforcer, String policyType, String[] values)`

#### `Boolean AddPolicy(IEnforcer enforcer, IEnumerable<String> values)`

#### `Boolean AddPolicy(IEnforcer enforcer, String[] values)`

#### `Task<Boolean> AddPolicyAsync(IEnforcer enforcer, IEnumerable<String> values)`

#### `Task<Boolean> AddPolicyAsync(IEnforcer enforcer, String[] values)`

#### `Boolean AddNamedPolicy(IEnforcer enforcer, String policyType, IEnumerable<String> values)`

#### `Boolean AddNamedPolicy(IEnforcer enforcer, String policyType, String[] values)`

#### `Task<Boolean> AddNamedPolicyAsync(IEnforcer enforcer, String policyType, IEnumerable<String> values)`

#### `Task<Boolean> AddNamedPolicyAsync(IEnforcer enforcer, String policyType, String[] values)`

#### `Boolean AddPolicies(IEnforcer enforcer, IEnumerable<IEnumerable<String>> values)`

#### `Task<Boolean> AddPoliciesAsync(IEnforcer enforcer, IEnumerable<IEnumerable<String>> values)`

#### `Boolean AddNamedPolicies(IEnforcer enforcer, String policyType, IEnumerable<IEnumerable<String>> valuesList)`

#### `Task<Boolean> AddNamedPoliciesAsync(IEnforcer enforcer, String policyType, IEnumerable<IEnumerable<String>> valuesList)`

#### `Boolean UpdatePolicy(IEnforcer enforcer, IEnumerable<String> oldValues, IEnumerable<String> newValues)`

#### `Boolean UpdatePolicy(IEnforcer enforcer, IEnumerable<String> oldValues, String[] newValues)`

#### `Task<Boolean> UpdatePolicyAsync(IEnforcer enforcer, IEnumerable<String> oldValues, IEnumerable<String> newValues)`

#### `Task<Boolean> UpdatePolicyAsync(IEnforcer enforcer, IEnumerable<String> oldValues, String[] newValues)`

#### `Boolean UpdateNamedPolicy(IEnforcer enforcer, String policyType, IEnumerable<String> oldValues, IEnumerable<String> newValues)`

#### `Boolean UpdateNamedPolicy(IEnforcer enforcer, String policyType, IEnumerable<String> oldValues, String[] newValues)`

#### `Task<Boolean> UpdateNamedPolicyAsync(IEnforcer enforcer, String policyType, IEnumerable<String> oldValues, IEnumerable<String> newValues)`

#### `Task<Boolean> UpdateNamedPolicyAsync(IEnforcer enforcer, String policyType, IEnumerable<String> oldValues, String[] newValues)`

#### `Boolean UpdatePolicies(IEnforcer enforcer, IEnumerable<IEnumerable<String>> oldValues, IEnumerable<IEnumerable<String>> newValues)`

#### `Task<Boolean> UpdatePoliciesAsync(IEnforcer enforcer, IEnumerable<IEnumerable<String>> oldValues, IEnumerable<IEnumerable<String>> newValues)`

#### `Boolean UpdateNamedPolicies(IEnforcer enforcer, String policyType, IEnumerable<IEnumerable<String>> oldValues, IEnumerable<IEnumerable<String>> newValues)`

#### `Task<Boolean> UpdateNamedPoliciesAsync(IEnforcer enforcer, String policyType, IEnumerable<IEnumerable<String>> oldValues, IEnumerable<IEnumerable<String>> newValues)`

#### `Boolean RemovePolicy(IEnforcer enforcer, IEnumerable<String> values)`

#### `Boolean RemovePolicy(IEnforcer enforcer, String[] values)`

#### `Task<Boolean> RemovePolicyAsync(IEnforcer enforcer, IEnumerable<String> values)`

#### `Task<Boolean> RemovePolicyAsync(IEnforcer enforcer, String[] values)`

#### `Boolean RemoveNamedPolicy(IEnforcer enforcer, String policyType, IEnumerable<String> values)`

#### `Boolean RemoveNamedPolicy(IEnforcer enforcer, String policyType, String[] values)`

#### `Task<Boolean> RemoveNamedPolicyAsync(IEnforcer enforcer, String policyType, IEnumerable<String> values)`

#### `Task<Boolean> RemoveNamedPolicyAsync(IEnforcer enforcer, String policyType, String[] values)`

#### `Boolean RemovePolicies(IEnforcer enforcer, IEnumerable<IEnumerable<String>> values)`

#### `Task<Boolean> RemovePoliciesAsync(IEnforcer enforcer, IEnumerable<IEnumerable<String>> values)`

#### `Boolean RemoveNamedPolicies(IEnforcer enforcer, String policyType, IEnumerable<IEnumerable<String>> values)`

#### `Task<Boolean> RemoveNamedPoliciesAsync(IEnforcer enforcer, String policyType, IEnumerable<IEnumerable<String>> values)`

#### `Boolean RemoveFilteredPolicy(IEnforcer enforcer, Int32 fieldIndex, String[] fieldValues)`

#### `Task<Boolean> RemoveFilteredPolicyAsync(IEnforcer enforcer, Int32 fieldIndex, String[] fieldValues)`

#### `Boolean RemoveFilteredNamedPolicy(IEnforcer enforcer, String policyType, Int32 fieldIndex, String[] fieldValues)`

#### `Task<Boolean> RemoveFilteredNamedPolicyAsync(IEnforcer enforcer, String policyType, Int32 fieldIndex, String[] fieldValues)`

#### `IEnumerable<String> GetAllRoles(IEnforcer enforcer)`

#### `IEnumerable<String> GetAllNamedRoles(IEnforcer enforcer, String policyType)`

#### `IEnumerable<IEnumerable<String>> GetGroupingPolicy(IEnforcer enforcer)`

#### `IEnumerable<IEnumerable<String>> GetFilteredGroupingPolicy(IEnforcer enforcer, Int32 fieldIndex, String[] fieldValues)`

#### `IEnumerable<IEnumerable<String>> GetNamedGroupingPolicy(IEnforcer enforcer, String policyType)`

#### `IEnumerable<IEnumerable<String>> GetFilteredNamedGroupingPolicy(IEnforcer enforcer, String policyType, Int32 fieldIndex, String[] fieldValues)`

#### `Boolean HasGroupingPolicy(IEnforcer enforcer, IEnumerable<String> values)`

#### `Boolean HasGroupingPolicy(IEnforcer enforcer, String[] values)`

#### `Boolean HasNamedGroupingPolicy(IEnforcer enforcer, String policyType, IEnumerable<String> values)`

#### `Boolean HasNamedGroupingPolicy(IEnforcer enforcer, String policyType, String[] values)`

#### `Boolean AddGroupingPolicy(IEnforcer enforcer, IEnumerable<String> values)`

#### `Boolean AddGroupingPolicy(IEnforcer enforcer, String[] values)`

#### `Task<Boolean> AddGroupingPolicyAsync(IEnforcer enforcer, IEnumerable<String> values)`

#### `Task<Boolean> AddGroupingPolicyAsync(IEnforcer enforcer, String[] values)`

#### `Boolean AddNamedGroupingPolicy(IEnforcer enforcer, String policyType, IEnumerable<String> values)`

#### `Boolean AddNamedGroupingPolicy(IEnforcer enforcer, String policyType, String[] values)`

#### `Task<Boolean> AddNamedGroupingPolicyAsync(IEnforcer enforcer, String policyType, IEnumerable<String> values)`

#### `Task<Boolean> AddNamedGroupingPolicyAsync(IEnforcer enforcer, String policyType, String[] values)`

#### `Boolean AddGroupingPolicies(IEnforcer enforcer, IEnumerable<IEnumerable<String>> valuesList)`

#### `Task<Boolean> AddGroupingPoliciesAsync(IEnforcer enforcer, IEnumerable<IEnumerable<String>> valuesList)`

#### `Boolean AddNamedGroupingPolicies(IEnforcer enforcer, String policyType, IEnumerable<IEnumerable<String>> valuesList)`

#### `Task<Boolean> AddNamedGroupingPoliciesAsync(IEnforcer enforcer, String policyType, IEnumerable<IEnumerable<String>> valuesList)`

#### `Boolean UpdateGroupingPolicy(IEnforcer enforcer, IEnumerable<String> oldValues, IEnumerable<String> newValues)`

#### `Boolean UpdateGroupingPolicy(IEnforcer enforcer, IEnumerable<String> oldValues, String[] newValues)`

#### `Task<Boolean> UpdateGroupingPolicyAsync(IEnforcer enforcer, IEnumerable<String> oldValues, IEnumerable<String> newValues)`

#### `Task<Boolean> UpdateGroupingPolicyAsync(IEnforcer enforcer, IEnumerable<String> oldValues, String[] newValues)`

#### `Boolean UpdateNamedGroupingPolicy(IEnforcer enforcer, String policyType, IEnumerable<String> oldValues, IEnumerable<String> newValues)`

#### `Boolean UpdateNamedGroupingPolicy(IEnforcer enforcer, String policyType, IEnumerable<String> oldValues, String[] newValues)`

#### `Task<Boolean> UpdateNamedGroupingPolicyAsync(IEnforcer enforcer, String policyType, IEnumerable<String> oldValues, IEnumerable<String> newValues)`

#### `Task<Boolean> UpdateNamedGroupingPolicyAsync(IEnforcer enforcer, String policyType, IEnumerable<String> oldValues, String[] newValues)`

#### `Boolean UpdateGroupingPolicies(IEnforcer enforcer, IEnumerable<IEnumerable<String>> oldValues, IEnumerable<IEnumerable<String>> newValues)`

#### `Task<Boolean> UpdateGroupingPoliciesAsync(IEnforcer enforcer, IEnumerable<IEnumerable<String>> oldValues, IEnumerable<IEnumerable<String>> newValues)`

#### `Boolean UpdateNamedGroupingPolicies(IEnforcer enforcer, String policyType, IEnumerable<IEnumerable<String>> oldValues, IEnumerable<IEnumerable<String>> newValues)`

#### `Task<Boolean> UpdateNamedGroupingPoliciesAsync(IEnforcer enforcer, String policyType, IEnumerable<IEnumerable<String>> oldValues, IEnumerable<IEnumerable<String>> newValues)`

#### `Boolean RemoveGroupingPolicy(IEnforcer enforcer, IEnumerable<String> values)`

#### `Boolean RemoveGroupingPolicy(IEnforcer enforcer, String[] values)`

#### `Task<Boolean> RemoveGroupingPolicyAsync(IEnforcer enforcer, IEnumerable<String> values)`

#### `Task<Boolean> RemoveGroupingPolicyAsync(IEnforcer enforcer, String[] values)`

#### `Boolean RemoveNamedGroupingPolicy(IEnforcer enforcer, String policyType, IEnumerable<String> values)`

#### `Boolean RemoveNamedGroupingPolicy(IEnforcer enforcer, String policyType, String[] values)`

#### `Task<Boolean> RemoveNamedGroupingPolicyAsync(IEnforcer enforcer, String policyType, IEnumerable<String> values)`

#### `Task<Boolean> RemoveNamedGroupingPolicyAsync(IEnforcer enforcer, String policyType, String[] values)`

#### `Boolean RemoveGroupingPolicies(IEnforcer enforcer, IEnumerable<IEnumerable<String>> valuesList)`

#### `Task<Boolean> RemoveGroupingPoliciesAsync(IEnforcer enforcer, IEnumerable<IEnumerable<String>> valuesList)`

#### `Boolean RemoveNamedGroupingPolicies(IEnforcer enforcer, String policyType, IEnumerable<IEnumerable<String>> valuesList)`

#### `Task<Boolean> RemoveNamedGroupingPoliciesAsync(IEnforcer enforcer, String policyType, IEnumerable<IEnumerable<String>> valuesList)`

#### `Boolean RemoveFilteredGroupingPolicy(IEnforcer enforcer, Int32 fieldIndex, String[] fieldValues)`

#### `Task<Boolean> RemoveFilteredGroupingPolicyAsync(IEnforcer enforcer, Int32 fieldIndex, String[] fieldValues)`

#### `Boolean RemoveFilteredNamedGroupingPolicy(IEnforcer enforcer, String policyType, Int32 fieldIndex, String[] fieldValues)`

#### `Task<Boolean> RemoveFilteredNamedGroupingPolicyAsync(IEnforcer enforcer, String policyType, Int32 fieldIndex, String[] fieldValues)`

---
## PermConstants (Casbin)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

---
## PolicyEffect (Casbin)
- **Type**: NestedPublic, Abstract, Sealed, BeforeFieldInit

---
## PolicyOperation (Casbin)
- **Type**: Public, Sealed

---
## RbacEnforcerExtension (Casbin)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean HasRoleForUser(IEnforcer enforcer, String name, String role, String domain)`

#### `IEnumerable<IEnumerable<String>> GetPermissionsForUser(IEnforcer enforcer, String user, String domain)`

#### `IEnumerable<String> GetRolesForUser(IEnforcer enforcer, String name, String domain)`

#### `IEnumerable<String> GetUsersForRole(IEnforcer enforcer, String name, String domain)`

#### `IEnumerable<String> GetUsersForRoles(IEnforcer enforcer, String[] names)`

#### `IEnumerable<String> GetUsersForRoles(IEnforcer enforcer, IEnumerable<String> names)`

#### `Boolean AddRoleForUser(IEnforcer enforcer, String user, String role, String domain)`

#### `Task<Boolean> AddRoleForUserAsync(IEnforcer enforcer, String user, String role, String domain)`

#### `Boolean AddRolesForUser(IEnforcer enforcer, String user, IEnumerable<String> role, String domain)`

#### `Task<Boolean> AddRolesForUserAsync(IEnforcer enforcer, String user, IEnumerable<String> role, String domain)`

#### `Boolean DeleteRoleForUser(IEnforcer enforcer, String user, String role, String domain)`

#### `Task<Boolean> DeleteRoleForUserAsync(IEnforcer enforcer, String user, String role, String domain)`

#### `Boolean DeleteRolesForUser(IEnforcer enforcer, String user, String domain)`

#### `Task<Boolean> DeleteRolesForUserAsync(IEnforcer enforcer, String user, String domain)`

#### `Boolean DeleteUser(IEnforcer enforcer, String user)`

#### `Task<Boolean> DeleteUserAsync(IEnforcer enforcer, String user)`

#### `Boolean DeleteRole(IEnforcer enforcer, String role)`

#### `Task<Boolean> DeleteRoleAsync(IEnforcer enforcer, String role)`

#### `Boolean HasPermissionForUser(IEnforcer enforcer, String user, String[] permission)`

#### `Boolean HasPermissionForUser(IEnforcer enforcer, String user, IEnumerable<String> permission)`

#### `Boolean AddPermissionForUser(IEnforcer enforcer, String user, String[] permission)`

#### `Task<Boolean> AddPermissionForUserAsync(IEnforcer enforcer, String user, String[] permission)`

#### `Boolean AddPermissionForUser(IEnforcer enforcer, String user, IEnumerable<String> permission)`

#### `Task<Boolean> AddPermissionForUserAsync(IEnforcer enforcer, String user, IEnumerable<String> permission)`

#### `Boolean DeletePermission(IEnforcer enforcer, String[] permission)`

#### `Task<Boolean> DeletePermissionAsync(IEnforcer enforcer, String[] permission)`

#### `Boolean DeletePermission(IEnforcer enforcer, IEnumerable<String> permission)`

#### `Task<Boolean> DeletePermissionAsync(IEnforcer enforcer, IEnumerable<String> permission)`

#### `Boolean DeletePermissionForUser(IEnforcer enforcer, String user, String[] permission)`

#### `Task<Boolean> DeletePermissionForUserAsync(IEnforcer enforcer, String user, String[] permission)`

#### `Boolean DeletePermissionForUser(IEnforcer enforcer, String user, IEnumerable<String> permission)`

#### `Task<Boolean> DeletePermissionForUserAsync(IEnforcer enforcer, String user, IEnumerable<String> permission)`

#### `Boolean DeletePermissionsForUser(IEnforcer enforcer, String user)`

#### `Task<Boolean> DeletePermissionsForUserAsync(IEnforcer enforcer, String user)`

#### `IEnumerable<String> GetImplicitRolesForUser(IEnforcer enforcer, String name, String domain)`

#### `IEnumerable<IEnumerable<String>> GetImplicitPermissionsForUser(IEnforcer enforcer, String user, String domain)`

#### `IEnumerable<String> GetImplicitUsersForPermission(IEnforcer enforcer, String[] permission)`

#### `IEnumerable<String> GetImplicitUsersForPermission(IEnforcer enforcer, IEnumerable<String> permissions)`

#### `IEnumerable<String> GetDomainsForUser(IEnforcer enforcer, String name, String roleType)`

#### `IEnumerable<String> GetRolesForUserInDomain(IEnforcer enforcer, String name, String domain)`

#### `IEnumerable<IEnumerable<String>> GetPermissionsForUserInDomain(IEnforcer enforcer, String user, String domain)`

#### `Boolean AddRoleForUserInDomain(IEnforcer enforcer, String user, String role, String domain)`

#### `Task<Boolean> AddRoleForUserInDomainAsync(IEnforcer enforcer, String user, String role, String domain)`

#### `Boolean DeleteRoleForUserInDomain(IEnforcer enforcer, String user, String role, String domain)`

#### `Task<Boolean> DeleteRoleForUserInDomainAsync(IEnforcer enforcer, String user, String role, String domain)`

---
## ReadOnlyAssertionExtension (Casbin)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TryGetTokenIndex(IReadOnlyAssertion assertion, String tokenName, Int32& index)`

---
## RoleManagerExtension (Casbin)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `IRoleManager AddMatchingFunc(IRoleManager roleManager, Func<String, String, Boolean> matchingFunc)`

#### `IRoleManager AddDomainMatchingFunc(IRoleManager roleManager, Func<String, String, Boolean> domainMatchingFunc)`

---
## Section (Casbin)
- **Type**: NestedPublic, Abstract, Sealed, BeforeFieldInit

---
## Token (Casbin)
- **Type**: NestedPublic, Abstract, Sealed, BeforeFieldInit

---
## EnforceCache (Casbin.Caching)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `Boolean TryGetResult(TRequest& requestValues, Boolean& result)`

#### `Task<Nullable<Boolean>> TryGetResultAsync(TRequest& requestValues)`

#### `Boolean TrySetResult(TRequest& requestValues, Boolean result)`

#### `Task<Boolean> TrySetResultAsync(TRequest& requestValues, Boolean result)`

#### `Void Clear()`

#### `Task ClearAsync()`

---
## EnforceCacheOptions (Casbin.Caching)
- **Type**: Public, BeforeFieldInit

### 方法列表
---
## EnforceViewCache (Casbin.Caching)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `Boolean TryAdd(String name, EnforceView view)`

#### `Boolean TryGet(String name, EnforceView& view)`

---
## ExpressionCachePool (Casbin.Caching)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `Void SetFunc(String expression, TFunc func)`

#### `Boolean TryGetFunc(String expression, TFunc& func)`

#### `Void Clear()`

---
## GFunctionCache (Casbin.Caching)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `Void Set(String name1, String name2, Boolean result, String domain)`

#### `Boolean TryGet(String name1, String name2, Boolean& result, String domain)`

#### `Void Clear()`

---
## GFunctionCachePool (Casbin.Caching)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `Void Clear(String roleType)`

#### `IGFunctionCache GetCache(String roleType)`

---
## IEnforceCache (Casbin.Caching)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Boolean TryGetResult(TRequest& requestValues, Boolean& result)`

#### `Task<Nullable<Boolean>> TryGetResultAsync(TRequest& requestValues)`

#### `Boolean TrySetResult(TRequest& requestValues, Boolean result)`

#### `Task<Boolean> TrySetResultAsync(TRequest& requestValues, Boolean result)`

#### `Void Clear()`

#### `Task ClearAsync()`

---
## IEnforceViewCache (Casbin.Caching)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Boolean TryAdd(String name, EnforceView view)`

#### `Boolean TryGet(String name, EnforceView& view)`

---
## IExpressionCache (Casbin.Caching)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Void Clear()`

---
## IExpressionCache`1 (Casbin.Caching)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Boolean TryGet(String expressionString, T& t)`

#### `Void Set(String expressionString, T t)`

---
## IExpressionCachePool (Casbin.Caching)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Void SetFunc(String expression, TFunc func)`

#### `Boolean TryGetFunc(String expression, TFunc& func)`

#### `Void Clear()`

---
## IGFunctionCache (Casbin.Caching)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Void Set(String name1, String name2, Boolean result, String domain)`

#### `Boolean TryGet(String name1, String name2, Boolean& result, String domain)`

#### `Void Clear()`

---
## IGFunctionCachePool (Casbin.Caching)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `IGFunctionCache GetCache(String roleType)`

#### `Void Clear(String roleType)`

---
## DefaultConfig (Casbin.Config)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IConfig Create()`

#### `IConfig CreateFromFile(String configFilePath)`

#### `IConfig CreateFromText(String text)`

#### `String Get(String key)`

#### `Boolean GetBool(String key)`

#### `Int32 GetInt(String key)`

#### `Single GetFloat(String key)`

#### `String GetString(String key)`

#### `String[] GetStrings(String key)`

#### `Void Set(String key, String value)`

---
## IConfig (Casbin.Config)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `String Get(String key)`

#### `Boolean GetBool(String key)`

#### `Int32 GetInt(String key)`

#### `Single GetFloat(String key)`

#### `String GetString(String key)`

#### `String[] GetStrings(String key)`

#### `Void Set(String key, String value)`

---
## DefaultEffector (Casbin.Effect)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `EffectChain CreateChain(String effectExpression)`

#### `EffectChain CreateChain(String effectExpression, EffectExpressionType effectExpressionType)`

#### `PolicyEffect MergeEffects(String effectExpression, IReadOnlyList<PolicyEffect> effects, IReadOnlyList<Single> matches, Int32 policyIndex, Int32 policyCount, Int32& hitPolicyIndex)`

#### `EffectExpressionType ParseEffectExpressionType(String effectExpression)`

---
## EffectChain (Casbin.Effect)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean Chain(PolicyEffect effect)`

#### `Boolean TryChain(PolicyEffect effect)`

#### `Boolean TryChain(PolicyEffect effect, Nullable`1& result)`

---
## EffectExpressionType (Casbin.Effect)
- **Type**: Public, Sealed

---
## IEffectChain (Casbin.Effect)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Boolean Chain(PolicyEffect effect)`

#### `Boolean TryChain(PolicyEffect effect)`

#### `Boolean TryChain(PolicyEffect effect, Nullable`1& result)`

---
## IEffector (Casbin.Effect)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `PolicyEffect MergeEffects(String effectExpression, IReadOnlyList<PolicyEffect> effects, IReadOnlyList<Single> results, Int32 policyIndex, Int32 policyCount, Int32& hitPolicyIndex)`

---
## PolicyEffect (Casbin.Effect)
- **Type**: Public, Sealed

---
## IExpressionHandler (Casbin.Evaluation)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Void SetFunction(String name, Delegate function)`

#### `Boolean Invoke(EnforceContext& context, String expressionString, TRequest& request, TPolicy& policy)`

---
## BuiltInFunctions (Casbin.Functions)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `String KeyGet(String key1, String key2)`

#### `String KeyGet2(String key1, String key2, String pathVar)`

#### `String KeyGet3(String key1, String key2, String pathVar)`

#### `Boolean KeyMatch(String key1, String key2)`

#### `Boolean KeyMatch2(String key1, String key2)`

#### `Boolean KeyMatch3(String key1, String key2)`

#### `Boolean KeyMatch4(String key1, String key2)`

#### `Boolean KeyMatch5(String key1, String key2)`

#### `Boolean IPMatch(String ip1, String ip2)`

#### `Boolean RegexMatch(String key1, String key2)`

#### `Boolean GlobMatch(String key1, String key2)`

---
## Assertion (Casbin.Model)
- **Type**: Public, Abstract, BeforeFieldInit

### 方法列表
---
## DefaultModel (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `Void LoadModelFromFile(String path)`

#### `Void LoadModelFromText(String text)`

#### `Boolean AddDef(String section, String key, String value)`

#### `IModel Create()`

#### `IModel CreateFromFile(String path)`

#### `IModel CreateFromText(String text)`

#### `IModel NewModelFromFile(String path)`

#### `IModel NewModelFromText(String text)`

#### `Boolean Validate()`

---
## DefaultPolicyManager (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `PolicyScanner Scan()`

#### `IEnumerable<IPolicyValues> GetPolicy()`

#### `IEnumerable<IPolicyValues> GetFilteredPolicy(Int32 fieldIndex, IPolicyValues fieldValues)`

#### `IEnumerable<String> GetValuesForFieldInPolicy(Int32 fieldIndex)`

#### `Boolean HasPolicy(IPolicyValues values)`

#### `Boolean HasPolicies(IReadOnlyList<IPolicyValues> valueList)`

#### `Boolean HasAllPolicies(IReadOnlyList<IPolicyValues> rules)`

#### `Boolean AddPolicy(IPolicyValues values)`

#### `Boolean AddPolicies(IReadOnlyList<IPolicyValues> rules)`

#### `Boolean UpdatePolicy(IPolicyValues oldRule, IPolicyValues newRule)`

#### `Boolean UpdatePolicies(IReadOnlyList<IPolicyValues> oldRules, IReadOnlyList<IPolicyValues> newRules)`

#### `Boolean RemovePolicy(IPolicyValues rule)`

#### `Boolean RemovePolicies(IReadOnlyList<IPolicyValues> rules)`

#### `IEnumerable<IPolicyValues> RemoveFilteredPolicy(Int32 fieldIndex, IPolicyValues fieldValues)`

#### `Task<Boolean> AddPolicyAsync(IPolicyValues rule)`

#### `Task<Boolean> AddPoliciesAsync(IReadOnlyList<IPolicyValues> rules)`

#### `Task<Boolean> UpdatePolicyAsync(IPolicyValues oldRule, IPolicyValues newRule)`

#### `Task<Boolean> UpdatePoliciesAsync(IReadOnlyList<IPolicyValues> oldRules, IReadOnlyList<IPolicyValues> newRules)`

#### `Task<Boolean> RemovePolicyAsync(IPolicyValues rule)`

#### `Task<Boolean> RemovePoliciesAsync(IReadOnlyList<IPolicyValues> rules)`

#### `Task<IEnumerable<IPolicyValues>> RemoveFilteredPolicyAsync(Int32 fieldIndex, IPolicyValues fieldValues)`

#### `Task<IEnumerable<IPolicyValues>> GetPolicyAsync()`

#### `Void ClearPolicy()`

---
## DefaultPolicyStore (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `Boolean AddNode(String section, String type, PolicyAssertion policyAssertion)`

#### `Boolean ContainsNodes(String section)`

#### `Boolean ContainsNode(String section, String policyType)`

#### `Int32 GetRequiredValuesCount(String section, String policyType)`

#### `Boolean ValidatePolicy(String section, String policyType, IPolicyValues values)`

#### `Boolean ValidatePolicies(String section, String policyType, IReadOnlyList<IPolicyValues> valuesList)`

#### `PolicyScanner Scan(String section, String policyType)`

#### `IEnumerable<IPolicyValues> GetPolicy(String section, String policyType)`

#### `IEnumerable<String> GetPolicyTypes(String section)`

#### `IDictionary<String, IEnumerable<String>> GetPolicyTypesAllSections()`

#### `IDictionary<String, IEnumerable<IPolicyValues>> GetPolicyAllType(String section)`

#### `IEnumerable<IPolicyValues> GetFilteredPolicy(String section, String policyType, Int32 fieldIndex, IPolicyValues fieldValues)`

#### `IEnumerable<IPolicyValues> RemoveFilteredPolicy(String section, String policyType, Int32 fieldIndex, IPolicyValues fieldValues)`

#### `Boolean SortPolicyByPriority(String section, String policyType)`

#### `Boolean SortPolicyBySubjectHierarchy(String section, String policyType, IDictionary<String, Int32> subjectHierarchyMap)`

#### `IEnumerable<String> GetValuesForFieldInPolicyAllTypes(String section, Int32 fieldIndex)`

#### `IEnumerable<String> GetValuesForFieldInPolicy(String section, String policyType, Int32 fieldIndex)`

#### `Boolean AddPolicy(String section, String policyType, IPolicyValues values)`

#### `Boolean HasPolicy(String section, String policyType, IPolicyValues values)`

#### `Boolean HasPolicies(String section, String policyType, IReadOnlyList<IPolicyValues> valuesList)`

#### `Boolean HasAllPolicies(String section, String policyType, IReadOnlyList<IPolicyValues> valuesList)`

#### `Boolean AddPolicies(String section, String policyType, IReadOnlyList<IPolicyValues> valuesList)`

#### `Boolean UpdatePolicy(String section, String policyType, IPolicyValues oldValues, IPolicyValues newValues)`

#### `Boolean UpdatePolicies(String section, String policyType, IReadOnlyList<IPolicyValues> oldValuesList, IReadOnlyList<IPolicyValues> newValuesList)`

#### `Boolean RemovePolicy(String section, String policyType, IPolicyValues values)`

#### `Boolean RemovePolicies(String section, String policyType, IReadOnlyList<IPolicyValues> valuesList)`

#### `Void ClearPolicy()`

---
## DefaultSections (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `Boolean ContainsSection(String section)`

#### `Boolean AddSection(String section, String key, String value)`

#### `T GetAssertion(String section, String type)`

#### `Boolean TryGetAssertion(String section, String type, T& outAssertion)`

#### `IDictionary<String, T> GetAssertions(String section)`

#### `Void LoadSection(IConfig config, String section)`

---
## IModel (Casbin.Model)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Void LoadModelFromFile(String path)`

#### `Void LoadModelFromText(String text)`

#### `Boolean AddDef(String section, String key, String value)`

---
## IPolicyManager (Casbin.Model)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Boolean AddPolicy(IPolicyValues rule)`

#### `Task<Boolean> AddPolicyAsync(IPolicyValues rule)`

#### `Boolean AddPolicies(IReadOnlyList<IPolicyValues> rules)`

#### `Task<Boolean> AddPoliciesAsync(IReadOnlyList<IPolicyValues> rules)`

#### `Boolean UpdatePolicy(IPolicyValues oldRule, IPolicyValues newRule)`

#### `Task<Boolean> UpdatePolicyAsync(IPolicyValues oldRule, IPolicyValues newRule)`

#### `Boolean UpdatePolicies(IReadOnlyList<IPolicyValues> oldRules, IReadOnlyList<IPolicyValues> newRules)`

#### `Task<Boolean> UpdatePoliciesAsync(IReadOnlyList<IPolicyValues> oldRules, IReadOnlyList<IPolicyValues> newRules)`

#### `Boolean RemovePolicy(IPolicyValues rule)`

#### `Task<Boolean> RemovePolicyAsync(IPolicyValues rule)`

#### `Boolean RemovePolicies(IReadOnlyList<IPolicyValues> rules)`

#### `Task<Boolean> RemovePoliciesAsync(IReadOnlyList<IPolicyValues> rules)`

#### `IEnumerable<IPolicyValues> RemoveFilteredPolicy(Int32 fieldIndex, IPolicyValues fieldValues)`

#### `Task<IEnumerable<IPolicyValues>> RemoveFilteredPolicyAsync(Int32 fieldIndex, IPolicyValues fieldValues)`

---
## IPolicyStore (Casbin.Model)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Boolean AddNode(String section, String type, PolicyAssertion policyAssertion)`

#### `Boolean AddPolicy(String section, String policyType, IPolicyValues values)`

#### `Boolean AddPolicies(String section, String policyType, IReadOnlyList<IPolicyValues> rules)`

#### `Boolean UpdatePolicy(String section, String policyType, IPolicyValues oldRule, IPolicyValues newRule)`

#### `Boolean UpdatePolicies(String section, String policyType, IReadOnlyList<IPolicyValues> oldRules, IReadOnlyList<IPolicyValues> newRules)`

#### `Boolean RemovePolicy(String section, String policyType, IPolicyValues rule)`

#### `Boolean RemovePolicies(String section, String policyType, IReadOnlyList<IPolicyValues> rules)`

#### `IEnumerable<IPolicyValues> RemoveFilteredPolicy(String section, String policyType, Int32 fieldIndex, IPolicyValues fieldValues)`

#### `Boolean SortPolicyByPriority(String section, String policyType)`

#### `Boolean SortPolicyBySubjectHierarchy(String section, String policyType, IDictionary<String, Int32> subjectHierarchyMap)`

#### `Void ClearPolicy()`

---
## IPolicyValues (Casbin.Model)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `String ToText()`

#### `Boolean Equals(IPolicyValues other)`

---
## IReadOnlyAssertion (Casbin.Model)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
---
## IReadOnlyPolicyManager (Casbin.Model)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `PolicyScanner Scan()`

#### `IEnumerable<IPolicyValues> GetPolicy()`

#### `IEnumerable<IPolicyValues> GetFilteredPolicy(Int32 fieldIndex, IPolicyValues fieldValues)`

#### `IEnumerable<String> GetValuesForFieldInPolicy(Int32 fieldIndex)`

#### `Boolean HasPolicy(IPolicyValues values)`

#### `Boolean HasPolicies(IReadOnlyList<IPolicyValues> valueList)`

#### `Boolean HasAllPolicies(IReadOnlyList<IPolicyValues> values)`

---
## IReadOnlyPolicyStore (Casbin.Model)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Boolean ContainsNodes(String section)`

#### `Boolean ContainsNode(String section, String policyType)`

#### `Int32 GetRequiredValuesCount(String section, String policyType)`

#### `Boolean ValidatePolicy(String section, String policyType, IPolicyValues values)`

#### `Boolean ValidatePolicies(String section, String policyType, IReadOnlyList<IPolicyValues> valuesList)`

#### `PolicyScanner Scan(String section, String policyType)`

#### `IEnumerable<IPolicyValues> GetPolicy(String section, String policyType)`

#### `IEnumerable<String> GetPolicyTypes(String section)`

#### `IDictionary<String, IEnumerable<String>> GetPolicyTypesAllSections()`

#### `IDictionary<String, IEnumerable<IPolicyValues>> GetPolicyAllType(String section)`

#### `IEnumerable<IPolicyValues> GetFilteredPolicy(String section, String policyType, Int32 fieldIndex, IPolicyValues fieldValues)`

#### `IEnumerable<String> GetValuesForFieldInPolicy(String section, String policyType, Int32 fieldIndex)`

#### `IEnumerable<String> GetValuesForFieldInPolicyAllTypes(String section, Int32 fieldIndex)`

#### `Boolean HasPolicy(String section, String policyType, IPolicyValues values)`

#### `Boolean HasPolicies(String section, String policyType, IReadOnlyList<IPolicyValues> valuesList)`

#### `Boolean HasAllPolicies(String section, String policyType, IReadOnlyList<IPolicyValues> valuesList)`

---
## IRequestValues (Casbin.Model)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

---
## ISections (Casbin.Model)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Boolean ContainsSection(String section)`

#### `Void LoadSection(IConfig config, String section)`

#### `Boolean AddSection(String section, String type, String value)`

#### `T GetAssertion(String section, String type)`

#### `Boolean TryGetAssertion(String section, String type, T& outAssertion)`

#### `IDictionary<String, T> GetAssertions(String section)`

---
## ListRequestValues`1 (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

---
## MatcherAssertion (Casbin.Model)
- **Type**: Public, BeforeFieldInit

---
## ModelExtension (Casbin.Model)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean SortPolicy(IModel model)`

#### `Boolean LoadPolicy(IModel model)`

#### `Task<Boolean> LoadPolicyAsync(IModel model)`

#### `Boolean LoadFilteredPolicy(IModel model, IPolicyFilter filter)`

#### `Task<Boolean> LoadFilteredPolicyAsync(IModel model, IPolicyFilter filter)`

#### `Boolean LoadIncrementalFilteredPolicy(IModel model, IPolicyFilter filter)`

#### `Task<Boolean> LoadIncrementalFilteredPolicyAsync(IModel model, IPolicyFilter filter)`

#### `Boolean SavePolicy(IModel model)`

#### `Task<Boolean> SavePolicyAsync(IModel model)`

#### `Void SetAutoSave(IModel model, Boolean autoSave)`

#### `IPolicyManager GetPolicyManager(IModel model, String section, String policyType)`

#### `IRoleManager GetRoleManager(IModel model, String roleType)`

#### `Void SetRoleManager(IModel model, String roleType, IRoleManager roleManager)`

#### `Void BuildRoleLinks(IModel model, String roleType)`

#### `Void BuildIncrementalRoleLink(IModel model, PolicyOperation policyOperation, String roleType, IPolicyValues rule)`

#### `Void BuildIncrementalRoleLink(IModel model, PolicyOperation policyOperation, String roleType, IPolicyValues oldRule, IPolicyValues newRule)`

#### `Void BuildIncrementalRoleLinks(IModel model, PolicyOperation policyOperation, String roleType, IEnumerable<IPolicyValues> rules)`

#### `Void BuildIncrementalRoleLinks(IModel model, PolicyOperation policyOperation, String roleType, IEnumerable<IPolicyValues> oldRules, IEnumerable<IPolicyValues> newRules)`

---
## Policy (Casbin.Model)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean SupportGeneric(Int32 count)`

#### `PolicyValues<T1> CreateValues(T1 value1)`

#### `PolicyValues<T1, T2> CreateValues(T1 value1, T2 value2)`

#### `PolicyValues<T1, T2, T3> CreateValues(T1 value1, T2 value2, T3 value3)`

#### `PolicyValues<T1, T2, T3, T4> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4)`

#### `PolicyValues<T1, T2, T3, T4, T5> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6, T7> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14)`

#### `IPolicyValues ValuesFrom(IReadOnlyList<String> values)`

#### `IPolicyValues ValuesFrom(IReadOnlyList<String> values, Int32 requiredCount)`

#### `IPolicyValues ValuesFrom(IEnumerable<String> values)`

#### `IPolicyValues ValuesFrom(IEnumerable<String> values, Int32 requiredCount)`

#### `IPolicyValues ValuesFrom(IPersistPolicy values)`

#### `IPolicyValues ValuesFrom(IPersistPolicy values, Int32 requiredCount)`

#### `IReadOnlyList<IPolicyValues> ValuesListFrom(IEnumerable<IEnumerable<String>> valuesList)`

#### `IReadOnlyList<IPolicyValues> ValuesListFrom(IEnumerable<IEnumerable<String>> valuesList, Int32 requiredCount)`

---
## PolicyAssertion (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `PolicyScanner<TRequest> Scan(TRequest& request)`

#### `Boolean TryGetPriorityIndex(Int32& index)`

#### `Boolean TryGetDomainIndex(Int32& index)`

#### `Boolean TryGetSubjectIndex(Int32& index)`

---
## PolicyEffectAssertion (Casbin.Model)
- **Type**: Public, BeforeFieldInit

---
## PolicyScanner (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean HasNext()`

#### `Boolean GetNext(IPolicyValues& outValues)`

#### `Void Interrupt()`

---
## PolicyScanner`1 (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean HasNext()`

#### `Boolean GetNext(IPolicyValues& outValues)`

#### `Void Interrupt()`

---
## PolicyValues (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IEnumerator<String> GetEnumerator()`

#### `String ToText()`

#### `Boolean Equals(IPolicyValues other)`

#### `String ToString()`

#### `Int32 GetHashCode()`

#### `Boolean Equals(Object obj)`

#### `Boolean Equals(PolicyValues other)`

#### `PolicyValues <Clone>$()`

---
## PolicyValues`1 (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IEnumerator<String> GetEnumerator()`

#### `String ToString()`

#### `Int32 GetHashCode()`

#### `Boolean Equals(Object obj)`

#### `Boolean Equals(PolicyValues other)`

#### `Boolean Equals(PolicyValues<T1> other)`

#### `PolicyValues<T1> <Clone>$()`

#### `Void Deconstruct(T1& Value1)`

---
## PolicyValues`10 (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IEnumerator<String> GetEnumerator()`

#### `String ToString()`

#### `Int32 GetHashCode()`

#### `Boolean Equals(Object obj)`

#### `Boolean Equals(PolicyValues other)`

#### `Boolean Equals(PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> other)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> <Clone>$()`

#### `Void Deconstruct(T1& Value1, T2& Value2, T3& Value3, T4& Value4, T5& Value5, T6& Value6, T7& Value7, T8& Value8, T9& Value9, T10& Value10)`

---
## PolicyValues`11 (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IEnumerator<String> GetEnumerator()`

#### `String ToString()`

#### `Int32 GetHashCode()`

#### `Boolean Equals(Object obj)`

#### `Boolean Equals(PolicyValues other)`

#### `Boolean Equals(PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> other)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> <Clone>$()`

#### `Void Deconstruct(T1& Value1, T2& Value2, T3& Value3, T4& Value4, T5& Value5, T6& Value6, T7& Value7, T8& Value8, T9& Value9, T10& Value10, T11& Value11)`

---
## PolicyValues`12 (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IEnumerator<String> GetEnumerator()`

#### `String ToString()`

#### `Int32 GetHashCode()`

#### `Boolean Equals(Object obj)`

#### `Boolean Equals(PolicyValues other)`

#### `Boolean Equals(PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> other)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> <Clone>$()`

#### `Void Deconstruct(T1& Value1, T2& Value2, T3& Value3, T4& Value4, T5& Value5, T6& Value6, T7& Value7, T8& Value8, T9& Value9, T10& Value10, T11& Value11, T12& Value12)`

---
## PolicyValues`13 (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IEnumerator<String> GetEnumerator()`

#### `String ToString()`

#### `Int32 GetHashCode()`

#### `Boolean Equals(Object obj)`

#### `Boolean Equals(PolicyValues other)`

#### `Boolean Equals(PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> other)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> <Clone>$()`

#### `Void Deconstruct(T1& Value1, T2& Value2, T3& Value3, T4& Value4, T5& Value5, T6& Value6, T7& Value7, T8& Value8, T9& Value9, T10& Value10, T11& Value11, T12& Value12, T13& Value13)`

---
## PolicyValues`14 (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IEnumerator<String> GetEnumerator()`

#### `String ToString()`

#### `Int32 GetHashCode()`

#### `Boolean Equals(Object obj)`

#### `Boolean Equals(PolicyValues other)`

#### `Boolean Equals(PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> other)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> <Clone>$()`

#### `Void Deconstruct(T1& Value1, T2& Value2, T3& Value3, T4& Value4, T5& Value5, T6& Value6, T7& Value7, T8& Value8, T9& Value9, T10& Value10, T11& Value11, T12& Value12, T13& Value13, T14& Value14)`

---
## PolicyValues`2 (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IEnumerator<String> GetEnumerator()`

#### `String ToString()`

#### `Int32 GetHashCode()`

#### `Boolean Equals(Object obj)`

#### `Boolean Equals(PolicyValues other)`

#### `Boolean Equals(PolicyValues<T1, T2> other)`

#### `PolicyValues<T1, T2> <Clone>$()`

#### `Void Deconstruct(T1& Value1, T2& Value2)`

---
## PolicyValues`3 (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IEnumerator<String> GetEnumerator()`

#### `String ToString()`

#### `Int32 GetHashCode()`

#### `Boolean Equals(Object obj)`

#### `Boolean Equals(PolicyValues other)`

#### `Boolean Equals(PolicyValues<T1, T2, T3> other)`

#### `PolicyValues<T1, T2, T3> <Clone>$()`

#### `Void Deconstruct(T1& Value1, T2& Value2, T3& Value3)`

---
## PolicyValues`4 (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IEnumerator<String> GetEnumerator()`

#### `String ToString()`

#### `Int32 GetHashCode()`

#### `Boolean Equals(Object obj)`

#### `Boolean Equals(PolicyValues other)`

#### `Boolean Equals(PolicyValues<T1, T2, T3, T4> other)`

#### `PolicyValues<T1, T2, T3, T4> <Clone>$()`

#### `Void Deconstruct(T1& Value1, T2& Value2, T3& Value3, T4& Value4)`

---
## PolicyValues`5 (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IEnumerator<String> GetEnumerator()`

#### `String ToString()`

#### `Int32 GetHashCode()`

#### `Boolean Equals(Object obj)`

#### `Boolean Equals(PolicyValues other)`

#### `Boolean Equals(PolicyValues<T1, T2, T3, T4, T5> other)`

#### `PolicyValues<T1, T2, T3, T4, T5> <Clone>$()`

#### `Void Deconstruct(T1& Value1, T2& Value2, T3& Value3, T4& Value4, T5& Value5)`

---
## PolicyValues`6 (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IEnumerator<String> GetEnumerator()`

#### `String ToString()`

#### `Int32 GetHashCode()`

#### `Boolean Equals(Object obj)`

#### `Boolean Equals(PolicyValues other)`

#### `Boolean Equals(PolicyValues<T1, T2, T3, T4, T5, T6> other)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6> <Clone>$()`

#### `Void Deconstruct(T1& Value1, T2& Value2, T3& Value3, T4& Value4, T5& Value5, T6& Value6)`

---
## PolicyValues`7 (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IEnumerator<String> GetEnumerator()`

#### `String ToString()`

#### `Int32 GetHashCode()`

#### `Boolean Equals(Object obj)`

#### `Boolean Equals(PolicyValues other)`

#### `Boolean Equals(PolicyValues<T1, T2, T3, T4, T5, T6, T7> other)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6, T7> <Clone>$()`

#### `Void Deconstruct(T1& Value1, T2& Value2, T3& Value3, T4& Value4, T5& Value5, T6& Value6, T7& Value7)`

---
## PolicyValues`8 (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IEnumerator<String> GetEnumerator()`

#### `String ToString()`

#### `Int32 GetHashCode()`

#### `Boolean Equals(Object obj)`

#### `Boolean Equals(PolicyValues other)`

#### `Boolean Equals(PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8> other)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8> <Clone>$()`

#### `Void Deconstruct(T1& Value1, T2& Value2, T3& Value3, T4& Value4, T5& Value5, T6& Value6, T7& Value7, T8& Value8)`

---
## PolicyValues`9 (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IEnumerator<String> GetEnumerator()`

#### `String ToString()`

#### `Int32 GetHashCode()`

#### `Boolean Equals(Object obj)`

#### `Boolean Equals(PolicyValues other)`

#### `Boolean Equals(PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9> other)`

#### `PolicyValues<T1, T2, T3, T4, T5, T6, T7, T8, T9> <Clone>$()`

#### `Void Deconstruct(T1& Value1, T2& Value2, T3& Value3, T4& Value4, T5& Value5, T6& Value6, T7& Value7, T8& Value8, T9& Value9)`

---
## Request (Casbin.Model)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean SupportGeneric(Int32 count)`

#### `RequestValues<T> CreateValues(T value)`

#### `RequestValues<T1, T2> CreateValues(T1 value1, T2 value2)`

#### `RequestValues<T1, T2, T3> CreateValues(T1 value1, T2 value2, T3 value3)`

#### `RequestValues<T1, T2, T3, T4> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4)`

#### `RequestValues<T1, T2, T3, T4, T5> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)`

#### `RequestValues<T1, T2, T3, T4, T5, T6> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)`

#### `RequestValues<T1, T2, T3, T4, T5, T6, T7> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)`

#### `RequestValues<T1, T2, T3, T4, T5, T6, T7, T8> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)`

#### `RequestValues<T1, T2, T3, T4, T5, T6, T7, T8, T9> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9)`

#### `RequestValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10)`

#### `RequestValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11)`

#### `RequestValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12)`

#### `RequestValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13)`

#### `RequestValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> CreateValues(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14)`

#### `ListRequestValues<T> CreateValues(T[] value)`

#### `Boolean TryGetStringKey(TRequest requestValues, String& key)`

---
## RequestAssertion (Casbin.Model)
- **Type**: Public, BeforeFieldInit

---
## RequestValues (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

---
## RequestValues`1 (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T1 value)`

---
## RequestValues`10 (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

---
## RequestValues`11 (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

---
## RequestValues`12 (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

---
## RequestValues`13 (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

---
## RequestValues`14 (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

---
## RequestValues`2 (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

---
## RequestValues`3 (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

---
## RequestValues`4 (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

---
## RequestValues`5 (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

---
## RequestValues`6 (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

---
## RequestValues`7 (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

---
## RequestValues`8 (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

---
## RequestValues`9 (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

---
## RoleAssertion (Casbin.Model)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `Void BuildRoleLinks()`

---
## SectionsExtension (Casbin.Model)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `String GetValue(ISections sections, String section, String type)`

#### `RequestAssertion GetRequestAssertion(ISections sections, String type)`

#### `RequestAssertion GetRequestAssertion(ISections sections, String section, String type)`

#### `PolicyAssertion GetPolicyAssertion(ISections sections, String type)`

#### `PolicyAssertion GetPolicyAssertion(ISections sections, String section, String type)`

#### `IDictionary<String, PolicyAssertion> GetPolicyAssertions(ISections sections)`

#### `IDictionary<String, PolicyAssertion> GetPolicyAssertions(ISections sections, String section)`

#### `RoleAssertion GetRoleAssertion(ISections sections, String type)`

#### `RoleAssertion GetRoleAssertion(ISections sections, String section, String type)`

#### `IDictionary<String, RoleAssertion> GetRoleAssertions(ISections sections)`

#### `IDictionary<String, RoleAssertion> GetRoleAssertions(ISections sections, String section)`

#### `PolicyEffectAssertion GetPolicyEffectAssertion(ISections sections, String type)`

#### `PolicyEffectAssertion GetPolicyEffectAssertion(ISections sections, String section, String type)`

#### `MatcherAssertion GetMatcherAssertion(ISections sections, String type)`

#### `MatcherAssertion GetMatcherAssertion(ISections sections, String section, String type)`

---
## StringRequestValues (Casbin.Model)
- **Type**: Public, SequentialLayout, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TrySetValue(Int32 index, T value)`

#### `Boolean TrySetValue(Int32 index, String value)`

---
## AdapterHolder (Casbin.Model.Holder)
- **Type**: Public, BeforeFieldInit

### 方法列表
---
## EffectorHolder (Casbin.Model.Holder)
- **Type**: Public, BeforeFieldInit

### 方法列表
---
## PolicyStoreHolder (Casbin.Model.Holder)
- **Type**: Public, BeforeFieldInit

### 方法列表
---
## WatcherHolder (Casbin.Model.Holder)
- **Type**: Public, BeforeFieldInit

### 方法列表
---
## AdapterExtension (Casbin.Persist)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `Void LoadPolicy(IReadOnlyAdapter adapter, IModel model)`

#### `Task LoadPolicyAsync(IReadOnlyAdapter adapter, IModel model)`

#### `Void SavePolicy(IEpochAdapter adapter, IModel model)`

#### `Task SavePolicyAsync(IEpochAdapter adapter, IModel model)`

---
## BaseAdapter (Casbin.Persist)
- **Type**: Public, Abstract, BeforeFieldInit

### 方法列表
#### `Void LoadPolicy(IPolicyStore store)`

#### `Task LoadPolicyAsync(IPolicyStore store)`

#### `Void SavePolicy(IPolicyStore store)`

#### `Task SavePolicyAsync(IPolicyStore store)`

#### `Void LoadFilteredPolicy(IPolicyStore store, IPolicyFilter filter)`

#### `Task LoadFilteredPolicyAsync(IPolicyStore store, IPolicyFilter filter)`

#### `Void LoadFilteredPolicy(IPolicyStore store, Filter filter)`

#### `Task LoadFilteredPolicyAsync(IPolicyStore store, Filter filter)`

---
## Filter (Casbin.Persist)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IQueryable<T> Apply(IQueryable<T> policies)`

---
## IAdapter (Casbin.Persist)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

---
## IBatchAdapter (Casbin.Persist)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Void AddPolicies(String section, String policyType, IReadOnlyList<IPolicyValues> rules)`

#### `Task AddPoliciesAsync(String section, String policyType, IReadOnlyList<IPolicyValues> rules)`

#### `Void UpdatePolicies(String section, String policyType, IReadOnlyList<IPolicyValues> oldRules, IReadOnlyList<IPolicyValues> newRules)`

#### `Task UpdatePoliciesAsync(String section, String policyType, IReadOnlyList<IPolicyValues> oldRules, IReadOnlyList<IPolicyValues> newRules)`

#### `Void RemovePolicies(String section, String policyType, IReadOnlyList<IPolicyValues> rules)`

#### `Task RemovePoliciesAsync(String section, String policyType, IReadOnlyList<IPolicyValues> rules)`

#### `Void RemoveFilteredPolicy(String section, String policyType, Int32 fieldIndex, IPolicyValues fieldValues)`

#### `Task RemoveFilteredPolicyAsync(String section, String policyType, Int32 fieldIndex, IPolicyValues fieldValues)`

---
## IEpochAdapter (Casbin.Persist)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Void SavePolicy(IPolicyStore model)`

#### `Task SavePolicyAsync(IPolicyStore model)`

---
## IFilteredAdapter (Casbin.Persist)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Void LoadFilteredPolicy(IPolicyStore store, IPolicyFilter filter)`

#### `Task LoadFilteredPolicyAsync(IPolicyStore store, IPolicyFilter filter)`

---
## IFullWatcher (Casbin.Persist)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Void SetUpdateCallback(Action callback)`

#### `Void SetUpdateCallback(Func<Task> callback)`

#### `Void Update()`

#### `Task UpdateAsync()`

---
## IIncrementalWatcher (Casbin.Persist)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Void SetUpdateCallback(Action<IPolicyChangeMessage> callback)`

#### `Void SetUpdateCallback(Func<IPolicyChangeMessage, Task> callback)`

#### `Void Update(IPolicyChangeMessage message)`

#### `Task UpdateAsync(IPolicyChangeMessage message)`

---
## IPersistPolicy (Casbin.Persist)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
---
## IPolicyChangeMessage (Casbin.Persist)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
---
## IPolicyFilter (Casbin.Persist)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `IQueryable<T> Apply(IQueryable<T> policies)`

---
## IReadOnlyAdapter (Casbin.Persist)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Void LoadPolicy(IPolicyStore model)`

#### `Task LoadPolicyAsync(IPolicyStore model)`

---
## IReadOnlyPersistPolicy (Casbin.Persist)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
---
## IReadOnlyWatcher (Casbin.Persist)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Void Close()`

#### `Task CloseAsync()`

---
## ISingleAdapter (Casbin.Persist)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Void AddPolicy(String section, String policyType, IPolicyValues rule)`

#### `Task AddPolicyAsync(String section, String policyType, IPolicyValues rule)`

#### `Void UpdatePolicy(String section, String policyType, IPolicyValues oldRule, IPolicyValues newRule)`

#### `Task UpdatePolicyAsync(String section, String policyType, IPolicyValues oldRules, IPolicyValues newRules)`

#### `Void RemovePolicy(String section, String policyType, IPolicyValues rule)`

#### `Task RemovePolicyAsync(String section, String policyType, IPolicyValues rule)`

---
## IWatcher (Casbin.Persist)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

---
## IWatcherEx (Casbin.Persist)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `Void UpdateForAddPolicy(String section, String policyType, IPolicyValues values)`

#### `Task UpdateForAddPolicyAsync(String section, String policyType, IPolicyValues values)`

#### `Void UpdateForRemovePolicy(String section, String policyType, IPolicyValues values)`

#### `Task UpdateForRemovePolicyAsync(String section, String policyType, IPolicyValues values)`

#### `Void UpdateForRemoveFilteredPolicy(String section, String policyType, Int32 fieldIndex, IPolicyValues fieldValues)`

#### `Task UpdateForRemoveFilteredPolicyAsync(String section, String policyType, Int32 fieldIndex, IPolicyValues fieldValues)`

#### `Void UpdateForSavePolicy()`

#### `Task UpdateForSavePolicyAsync()`

#### `Void UpdateForAddPolicies(String section, String policyType, IReadOnlyList<IPolicyValues> values)`

#### `Task UpdateForAddPoliciesAsync(String section, String policyType, IReadOnlyList<IPolicyValues> values)`

#### `Void UpdateForRemovePolicies(String section, String policyType, IReadOnlyList<IPolicyValues> values)`

#### `Task UpdateForRemovePoliciesAsync(String section, String policyType, IReadOnlyList<IPolicyValues> values)`

#### `Void UpdateForUpdatePolicy(String section, String policyType, IPolicyValues values, IPolicyValues newValues)`

#### `Task UpdateForUpdatePolicyAsync(String section, String policyType, IPolicyValues values, IPolicyValues newValues)`

#### `Void UpdateForUpdatePolicies(String section, String policyType, IReadOnlyList<IPolicyValues> valuesList, IReadOnlyList<IPolicyValues> newValues)`

#### `Task UpdateForUpdatePoliciesAsync(String section, String policyType, IReadOnlyList<IPolicyValues> valuesList, IReadOnlyList<IPolicyValues> newValues)`

---
## PersistPolicy (Casbin.Persist)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `TPersistPolicy Create(String type, IPolicyValues values)`

#### `TPersistPolicy Create(String section, String type, IPolicyValues values)`

---
## PolicyChangedMessage (Casbin.Persist)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `PolicyChangedMessage CreateAddPolicy(String section, String policyType, IPolicyValues rule)`

#### `PolicyChangedMessage CreateUpdatePolicy(String section, String policyType, IPolicyValues values, IPolicyValues newValues)`

#### `PolicyChangedMessage CreateRemovePolicy(String section, String policyType, IPolicyValues values)`

#### `PolicyChangedMessage CreateRemoveFilteredPolicy(String section, String policyType, Int32 fieldIndex, IReadOnlyList<IPolicyValues> fieldValues)`

#### `PolicyChangedMessage CreateAddPolicies(String section, String policyType, IReadOnlyList<IPolicyValues> rules)`

#### `PolicyChangedMessage CreateUpdatePolicies(String section, String policyType, IReadOnlyList<IPolicyValues> valuesList, IReadOnlyList<IPolicyValues> newValueList)`

#### `PolicyChangedMessage CreateRemovePolicies(String section, String policyType, IReadOnlyList<IPolicyValues> valuesList)`

#### `PolicyChangedMessage CreateSavePolicy()`

---
## PolicyFilter (Casbin.Persist)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IQueryable<T> Apply(IQueryable<T> policies)`

---
## PolicyOperationExtension (Casbin.Persist)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean IsBatch(PolicyOperation operation)`

#### `Boolean IsFilter(PolicyOperation operation)`

#### `Boolean IsEpoch(PolicyOperation operation)`

---
## PolicyStoreExtension (Casbin.Persist)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `Boolean TryLoadPolicyLine(IPolicyStore store, String line)`

#### `Boolean TryLoadPolicyLine(IPolicyStore store, IReadOnlyList<String> lineTokens)`

---
## ReadSource (Casbin.Persist)
- **Type**: Public, Sealed

---
## WatcherExExtension (Casbin.Persist)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

---
## FileAdapter (Casbin.Persist.Adapter.File)
- **Type**: Public, BeforeFieldInit

---
## FileFilteredAdapter (Casbin.Persist.Adapter.File)
- **Type**: Public, BeforeFieldInit

---
## StreamAdapter (Casbin.Persist.Adapter.Stream)
- **Type**: Public, BeforeFieldInit

---
## TextAdapter (Casbin.Persist.Adapter.Text)
- **Type**: Public, BeforeFieldInit

---
## DefaultRoleManager (Casbin.Rbac)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `IEnumerable<String> GetDomains(String name)`

#### `IEnumerable<String> GetRoles(String name, String domain)`

#### `IEnumerable<String> GetUsers(String name, String domain)`

#### `Boolean HasLink(String name1, String name2, String domain)`

#### `Void AddLink(String name1, String name2, String domain)`

#### `Void DeleteLink(String name1, String name2, String domain)`

#### `Void Clear()`

---
## GroupRoleManager (Casbin.Rbac)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `Boolean HasLink(String name1, String name2, String domain)`

---
## IRoleManager (Casbin.Rbac)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `IEnumerable<String> GetRoles(String name, String domain)`

#### `IEnumerable<String> GetUsers(String name, String domain)`

#### `IEnumerable<String> GetDomains(String name)`

#### `Boolean HasLink(String name1, String name2, String domain)`

#### `Void AddLink(String name1, String name2, String domain)`

#### `Void DeleteLink(String name1, String name2, String domain)`

#### `Void Clear()`

---
## Role (Casbin.Rbac)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `Void AddRole(Role role)`

#### `Void DeleteRole(Role role)`

#### `Boolean HasRole(String name, Int32 hierarchyLevel, Func<String, String, Boolean> matchingFunc)`

#### `Boolean HasDirectRole(String name, Func<String, String, Boolean> matchingFunc)`

#### `IEnumerable<String> GetRoles()`

---
