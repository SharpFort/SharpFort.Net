using Casbin.Adapter.SqlSugar;
using Casbin.Model;
using Casbin.Persist;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.SqlSugarCore.Adapters
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
        public void LoadPolicy(IPolicyStore model)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ISqlSugarDbContext>();
            var adapter = new SqlSugarAdapter(dbContext.SqlSugarClient);
            adapter.LoadPolicy(model);
        }

        public async Task LoadPolicyAsync(IPolicyStore model)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ISqlSugarDbContext>();
            var adapter = new SqlSugarAdapter(dbContext.SqlSugarClient);
            await adapter.LoadPolicyAsync(model);
        }

        // IEpochAdapter (Empty implementation or delegate if stateful - but here state is ephemeral)
        public void SavePolicy(IPolicyStore model) => throw new NotSupportedException("AutoSave is disabled");
        public Task SavePolicyAsync(IPolicyStore model) => throw new NotSupportedException("AutoSave is disabled");

        // ISingleAdapter
        public void AddPolicy(string section, string policyType, IPolicyValues rule) => throw new NotSupportedException("AutoSave is disabled");
        public Task AddPolicyAsync(string section, string policyType, IPolicyValues rule) => throw new NotSupportedException("AutoSave is disabled");

        public void RemovePolicy(string section, string policyType, IPolicyValues rule) => throw new NotSupportedException("AutoSave is disabled");
        public Task RemovePolicyAsync(string section, string policyType, IPolicyValues rule) => throw new NotSupportedException("AutoSave is disabled");

        public void UpdatePolicy(string section, string policyType, IPolicyValues oldRule, IPolicyValues newRule) => throw new NotSupportedException("AutoSave is disabled");
        public Task UpdatePolicyAsync(string section, string policyType, IPolicyValues oldRules, IPolicyValues newRules) => throw new NotSupportedException("AutoSave is disabled");

        // IBatchAdapter
        public void AddPolicies(string section, string policyType, IReadOnlyList<IPolicyValues> rules) => throw new NotSupportedException("AutoSave is disabled");
        public Task AddPoliciesAsync(string section, string policyType, IReadOnlyList<IPolicyValues> rules) => throw new NotSupportedException("AutoSave is disabled");

        public void RemovePolicies(string section, string policyType, IReadOnlyList<IPolicyValues> rules) => throw new NotSupportedException("AutoSave is disabled");
        public Task RemovePoliciesAsync(string section, string policyType, IReadOnlyList<IPolicyValues> rules) => throw new NotSupportedException("AutoSave is disabled");

        public void UpdatePolicies(string section, string policyType, IReadOnlyList<IPolicyValues> oldRules, IReadOnlyList<IPolicyValues> newRules) => throw new NotSupportedException("AutoSave is disabled");
        public Task UpdatePoliciesAsync(string section, string policyType, IReadOnlyList<IPolicyValues> oldRules, IReadOnlyList<IPolicyValues> newRules) => throw new NotSupportedException("AutoSave is disabled");

        // IFilteredAdapter (Optional but good to match interface if inherited)
        // Note: IAdapter might not inherit IFilteredAdapter by default, but error log showed RemoveFilteredPolicy
        public void RemoveFilteredPolicy(string section, string policyType, int fieldIndex, IPolicyValues fieldValues) => throw new NotSupportedException("AutoSave is disabled");
        public Task RemoveFilteredPolicyAsync(string section, string policyType, int fieldIndex, IPolicyValues fieldValues) => throw new NotSupportedException("AutoSave is disabled");
    }
}
