using Casbin.Adapter.SqlSugar;
using Casbin.Model;
using Casbin.Persist;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.CasbinRbac.SqlSugarCore.Adapters
{
    /// <summary>
    /// 作用域工厂适配器
    /// 为 Singleton 的 Enforcer 提供按需创建 Scope 的能力
    /// </summary>
    public class ScopeFactoryCasbinAdapter : IAdapter
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ScopeFactoryCasbinAdapter(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        // 核心方法：加载策略
        public void LoadPolicy(IPolicyStore store)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ISqlSugarDbContext>();
            var adapter = new SqlSugarAdapter(dbContext.SqlSugarClient);
            adapter.LoadPolicy(store);
        }

        public async Task LoadPolicyAsync(IPolicyStore store)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ISqlSugarDbContext>();
            var adapter = new SqlSugarAdapter(dbContext.SqlSugarClient);
            await adapter.LoadPolicyAsync(store);
        }

        // IEpochAdapter (Empty implementation or delegate if stateful - but here state is ephemeral)
        public void SavePolicy(IPolicyStore store) => throw new NotSupportedException("AutoSave is disabled");
        public Task SavePolicyAsync(IPolicyStore store) => throw new NotSupportedException("AutoSave is disabled");

        // ISingleAdapter
        public void AddPolicy(string sec, string ptype, IPolicyValues values) => throw new NotSupportedException("AutoSave is disabled");
        public Task AddPolicyAsync(string sec, string ptype, IPolicyValues values) => throw new NotSupportedException("AutoSave is disabled");
        
        public void RemovePolicy(string sec, string ptype, IPolicyValues values) => throw new NotSupportedException("AutoSave is disabled");
        public Task RemovePolicyAsync(string sec, string ptype, IPolicyValues values) => throw new NotSupportedException("AutoSave is disabled");

        public void UpdatePolicy(string sec, string ptype, IPolicyValues oldValues, IPolicyValues newValues) => throw new NotSupportedException("AutoSave is disabled");
        public Task UpdatePolicyAsync(string sec, string ptype, IPolicyValues oldValues, IPolicyValues newValues) => throw new NotSupportedException("AutoSave is disabled");

        // IBatchAdapter
        public void AddPolicies(string sec, string ptype, IReadOnlyList<IPolicyValues> values) => throw new NotSupportedException("AutoSave is disabled");
        public Task AddPoliciesAsync(string sec, string ptype, IReadOnlyList<IPolicyValues> values) => throw new NotSupportedException("AutoSave is disabled");

        public void RemovePolicies(string sec, string ptype, IReadOnlyList<IPolicyValues> values) => throw new NotSupportedException("AutoSave is disabled");
        public Task RemovePoliciesAsync(string sec, string ptype, IReadOnlyList<IPolicyValues> values) => throw new NotSupportedException("AutoSave is disabled");

        public void UpdatePolicies(string sec, string ptype, IReadOnlyList<IPolicyValues> oldValues, IReadOnlyList<IPolicyValues> newValues) => throw new NotSupportedException("AutoSave is disabled");
        public Task UpdatePoliciesAsync(string sec, string ptype, IReadOnlyList<IPolicyValues> oldValues, IReadOnlyList<IPolicyValues> newValues) => throw new NotSupportedException("AutoSave is disabled");

        // IFilteredAdapter (Optional but good to match interface if inherited)
        // Note: IAdapter might not inherit IFilteredAdapter by default, but error log showed RemoveFilteredPolicy
        public void RemoveFilteredPolicy(string sec, string ptype, int fieldIndex, IPolicyValues fieldValues) => throw new NotSupportedException("AutoSave is disabled");
        public Task RemoveFilteredPolicyAsync(string sec, string ptype, int fieldIndex, IPolicyValues fieldValues) => throw new NotSupportedException("AutoSave is disabled");
    }
}
