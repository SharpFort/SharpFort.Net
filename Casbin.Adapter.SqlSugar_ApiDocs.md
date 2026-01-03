> ⚠️ 警告: 在路径 {xmlPath} 未找到 XML 文档文件。生成的文档将没有注释说明，只有方法签名。

## CustomMappingClientProvider (Casbin.Adapter.SqlSugar)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `ISqlSugarClient GetClientForPolicyType(String policyType)`

#### `IEnumerable<ISqlSugarClient> GetAllClients()`

---
## DefaultSqlSugarClientProvider (Casbin.Adapter.SqlSugar)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `ISqlSugarClient GetClientForPolicyType(String policyType)`

#### `IEnumerable<ISqlSugarClient> GetAllClients()`

---
## ISqlSugarClientProvider (Casbin.Adapter.SqlSugar)
- **Type**: Public, ClassSemanticsMask, Abstract, BeforeFieldInit

### 方法列表
#### `ISqlSugarClient GetClientForPolicyType(String policyType)`

#### `IEnumerable<ISqlSugarClient> GetAllClients()`

#### `String GetTableNameForPolicyType(String policyType)`

---
## PolicyTypeClientProvider (Casbin.Adapter.SqlSugar)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `ISqlSugarClient GetClientForPolicyType(String policyType)`

#### `IEnumerable<ISqlSugarClient> GetAllClients()`

---
## SqlSugarAdapter (Casbin.Adapter.SqlSugar)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `Void LoadPolicy(IPolicyStore store)`

#### `Task LoadPolicyAsync(IPolicyStore store)`

#### `Void SavePolicy(IPolicyStore store)`

#### `Task SavePolicyAsync(IPolicyStore store)`

#### `Void AddPolicy(String section, String policyType, IPolicyValues values)`

#### `Task AddPolicyAsync(String section, String policyType, IPolicyValues values)`

#### `Void RemovePolicy(String section, String policyType, IPolicyValues values)`

#### `Task RemovePolicyAsync(String section, String policyType, IPolicyValues values)`

#### `Void AddPolicies(String section, String policyType, IReadOnlyList<IPolicyValues> valuesList)`

#### `Task AddPoliciesAsync(String section, String policyType, IReadOnlyList<IPolicyValues> valuesList)`

#### `Void RemovePolicies(String section, String policyType, IReadOnlyList<IPolicyValues> valuesList)`

#### `Task RemovePoliciesAsync(String section, String policyType, IReadOnlyList<IPolicyValues> valuesList)`

#### `Void RemoveFilteredPolicy(String section, String policyType, Int32 fieldIndex, IPolicyValues fieldValues)`

#### `Task RemoveFilteredPolicyAsync(String section, String policyType, Int32 fieldIndex, IPolicyValues fieldValues)`

#### `Void UpdatePolicy(String section, String policyType, IPolicyValues oldValues, IPolicyValues newValues)`

#### `Task UpdatePolicyAsync(String section, String policyType, IPolicyValues oldValues, IPolicyValues newValues)`

#### `Void UpdatePolicies(String section, String policyType, IReadOnlyList<IPolicyValues> oldValuesList, IReadOnlyList<IPolicyValues> newValuesList)`

#### `Task UpdatePoliciesAsync(String section, String policyType, IReadOnlyList<IPolicyValues> oldValuesList, IReadOnlyList<IPolicyValues> newValuesList)`

#### `Void LoadFilteredPolicy(IPolicyStore store, IPolicyFilter filter)`

#### `Task LoadFilteredPolicyAsync(IPolicyStore store, IPolicyFilter filter)`

---
## CasbinRule (Casbin.Adapter.SqlSugar.Entities)
- **Type**: Public, BeforeFieldInit

### 方法列表
#### `String ToString()`

---
## CasbinRuleExtension (Casbin.Adapter.SqlSugar.Extensions)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `Void LoadPolicyLine(CasbinRule rule, IPolicyStore store)`

#### `CasbinRule ToCasbinRule(String ptype, IList<String> rule)`

#### `CasbinRule ToCasbinRule(String ptype, IPolicyValues values)`

---
## LoadPolicyLineHandler`2 (Casbin.Adapter.SqlSugar.Extensions)
- **Type**: NestedPublic, Sealed

### 方法列表
#### `Void Invoke(TKey key, TValue value)`

#### `IAsyncResult BeginInvoke(TKey key, TValue value, AsyncCallback callback, Object object)`

#### `Void EndInvoke(IAsyncResult result)`

---
## ServiceCollectionExtensions (Casbin.Adapter.SqlSugar.Extensions)
- **Type**: Public, Abstract, Sealed, BeforeFieldInit

### 方法列表
#### `IServiceCollection AddSqlSugarCasbinAdapter(IServiceCollection services, Action<ConnectionConfig> configAction, ServiceLifetime lifetime)`

#### `IServiceCollection AddSqlSugarCasbinAdapter(IServiceCollection services, ServiceLifetime lifetime)`

#### `IServiceCollection AddSqlSugarCasbinAdapterWithProvider(IServiceCollection services, Func<IServiceProvider, ISqlSugarClientProvider> providerFactory, ServiceLifetime lifetime)`

#### `IServiceCollection AddSqlSugarCasbinAdapterWithProvider(IServiceCollection services, ServiceLifetime lifetime)`

---
