//using Volo.Abp.Data;
//using Volo.Abp.DependencyInjection;
//using Yi.Framework.CasbinRbac.Domain.Entities;
//using Yi.Framework.CasbinRbac.Domain.Shared.Enums;
//using Yi.Framework.SqlSugarCore.Abstractions;

//namespace Yi.Framework.CasbinRbac.SqlSugarCore.DataSeeds
//{
//    public class RoleDataSeed : IDataSeedContributor, ITransientDependency
//    {
//        private ISqlSugarRepository<Role> _repository;
//        public RoleDataSeed(ISqlSugarRepository<Role> repository)
//        {
//            _repository = repository;
//        }

//        public List<Role> GetSeedData()
//        {
//            var entities = new List<Role>();
//            Role role1 = new Role()
//            {

//                RoleName = "管理员",
//                RoleCode = "admin",
//                DataScope = DataScope.ALL,
//                OrderNum = 999,
//                Remark = "管理员",
//                IsDeleted = false
//            };
//            entities.Add(role1);

//            Role role2 = new Role()
//            {

//                RoleName = "测试角色",
//                RoleCode = "test",
//                DataScope = DataScope.ALL,
//                OrderNum = 1,
//                Remark = "测试用的角色",
//                IsDeleted = false
//            };
//            entities.Add(role2);

//            Role role3 = new Role()
//            {

//                RoleName = "普通角色",
//                RoleCode = "common",
//                DataScope = DataScope.ALL,
//                OrderNum = 1,
//                Remark = "正常用户",
//                IsDeleted = false
//            };
//            entities.Add(role3);

//            Role role4 = new Role()
//            {

//                RoleName = "默认角色",
//                RoleCode = "default",
//                DataScope = DataScope.ALL,
//                OrderNum = 1,
//                Remark = "可简单浏览",
//                IsDeleted = false
//            };
//            entities.Add(role4);


//            return entities;
//        }

//        public async Task SeedAsync(DataSeedContext context)
//        {
//            if (!await _repository.IsAnyAsync(x => true))
//            {
//                await _repository.InsertManyAsync(GetSeedData());
//            }
//        }
//    }
//}
