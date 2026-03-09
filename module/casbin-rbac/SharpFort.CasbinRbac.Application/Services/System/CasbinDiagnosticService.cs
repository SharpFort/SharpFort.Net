//using Casbin;
//using Casbin.Adapter.SqlSugar.Entities;
//using Microsoft.AspNetCore.Mvc;
//using Volo.Abp.Application.Services;
//using SharpFort.SqlSugarCore.Abstractions;

//namespace SharpFort.CasbinRbac.Application.Services.System
//{
//    /// <summary>
//    /// Casbin 诊断服务 - 临时用于调试权限问题
//    /// </summary>
//    public class CasbinDiagnosticService : ApplicationService
//    {
//        private readonly IEnforcer _enforcer;
//        private readonly ISqlSugarRepository<CasbinRule> _casbinRuleRepo;

//        public CasbinDiagnosticService(
//            IEnforcer enforcer,
//            ISqlSugarRepository<CasbinRule> casbinRuleRepo)
//        {
//            _enforcer = enforcer;
//            _casbinRuleRepo = casbinRuleRepo;
//        }

//        /// <summary>
//        /// 获取所有 Casbin 规则
//        /// </summary>
//        [HttpGet]
//        [Route("api/diagnostic/casbin/rules")]
//        public async Task<object> GetAllRulesAsync([FromQuery] string? pType = null, [FromQuery] int limit = 100)
//        {
//            var query = _casbinRuleRepo._DbQueryable;

//            if (!string.IsNullOrEmpty(pType))
//            {
//                query = query.Where(r => r.PType == pType);
//            }

//            var rules = await query.Take(limit).ToListAsync();

//            return new
//            {
//                Total = await _casbinRuleRepo.CountAsync(),
//                Limit = limit,
//                FilteredBy = pType,
//                Rules = rules.Select(r => new
//                {
//                    r.Id,
//                    r.PType,
//                    r.V0,
//                    r.V1,
//                    r.V2,
//                    r.V3,
//                    r.V4,
//                    r.V5
//                })
//            };
//        }

//        /// <summary>
//        /// 测试权限验证
//        /// </summary>
//        [HttpPost]
//        [Route("api/diagnostic/casbin/test")]
//        public async Task<object> TestEnforceAsync([FromBody] TestEnforceInput input)
//        {
//            var result = await _enforcer.EnforceAsync(input.Sub, input.Dom, input.Obj, input.Act);

//            // 获取用户的所有角色
//            var roles = await _enforcer.GetRolesForUserAsync(input.Sub, input.Dom);

//            // 获取角色的所有权限
//            var permissions = new List<object>();
//            foreach (var role in roles)
//            {
//                var perms = await _enforcer.GetPermissionsForUserAsync(role, input.Dom);
//                permissions.AddRange(perms.Select(p => new { Role = role, Permission = p }));
//            }

//            return new
//            {
//                Input = input,
//                Result = result,
//                UserRoles = roles,
//                RolePermissions = permissions,
//                AllPolicies = await _enforcer.GetPolicyAsync(),
//                AllGroupings = await _enforcer.GetGroupingPolicyAsync()
//            };
//        }

//        /// <summary>
//        /// 获取统计信息
//        /// </summary>
//        [HttpGet]
//        [Route("api/diagnostic/casbin/stats")]
//        public async Task<object> GetStatsAsync()
//        {
//            var pCount = await _casbinRuleRepo._DbQueryable.Where(r => r.PType == "p").CountAsync();
//            var gCount = await _casbinRuleRepo._DbQueryable.Where(r => r.PType == "g").CountAsync();

//            // 获取 p 策略中 v0 的不同格式
//            var pV0Samples = await _casbinRuleRepo._DbQueryable
//                .Where(r => r.PType == "p")
//                .GroupBy(r => r.V0)
//                .Select(g => new { V0 = g.Key, Count = SqlSugar.SqlFunc.AggregateCount(g.Key) })
//                .Take(20)
//                .ToListAsync();

//            // 获取 g 策略中 v1 的不同格式
//            var gV1Samples = await _casbinRuleRepo._DbQueryable
//                .Where(r => r.PType == "g")
//                .GroupBy(r => r.V1)
//                .Select(g => new { V1 = g.Key, Count = SqlSugar.SqlFunc.AggregateCount(g.Key) })
//                .Take(20)
//                .ToListAsync();

//            return new
//            {
//                TotalRules = await _casbinRuleRepo.CountAsync(),
//                PolicyCount = pCount,
//                GroupingCount = gCount,
//                PSampleV0 = pV0Samples,
//                GSampleV1 = gV1Samples
//            };
//        }
//    }

//    public class TestEnforceInput
//    {
//        public string Sub { get; set; } = "";
//        public string Dom { get; set; } = "default";
//        public string Obj { get; set; } = "";
//        public string Act { get; set; } = "GET";
//    }
//}
