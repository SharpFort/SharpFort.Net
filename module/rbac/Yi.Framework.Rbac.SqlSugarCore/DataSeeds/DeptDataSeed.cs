//using Volo.Abp.Data;
//using Volo.Abp.DependencyInjection;
//using Volo.Abp.Guids;
//using Yi.Framework.Rbac.Domain.Entities;
//using Yi.Framework.SqlSugarCore.Abstractions;

//namespace Yi.Framework.Rbac.SqlSugarCore.DataSeeds
//{

//    public class DeptDataSeed : IDataSeedContributor, ITransientDependency
//    {
//        private ISqlSugarRepository<Department> _repository;
//        private IGuidGenerator _guidGenerator;
//        public DeptDataSeed(ISqlSugarRepository<Department> repository, IGuidGenerator guidGenerator)
//        {
//            _repository = repository;
//            _guidGenerator = guidGenerator;
//        }
//        public async Task SeedAsync(DataSeedContext context)
//        {
//            if (!await _repository.IsAnyAsync(x => true))
//            {
//                await _repository.InsertManyAsync(GetSeedData());
//            }
//        }
//        public List<Department> GetSeedData()
//        {
//            var entities = new List<Department>();

//            Department chengziDept = new Department(
//                id:Guid.NewGuid(),
//                deptName: "橙子科技",
//                deptCode: "Yi1",
//                parentId: Guid.Empty,
//                orderNum: 100,
//                leader: "橙子",
//                remark: "如名所指"
//            );
//            entities.Add(chengziDept);


//            Department shenzhenDept = new Department(
//                id: Guid.NewGuid(),
//                deptCode : "SZ1",
//                deptName : "深圳总公司",
//                orderNum : 100,
//                isDeleted: false,
//                parentId : chengziDept.Id
//            );
//            entities.Add(shenzhenDept);


//            Department jiangxiDept = new Department(
//                id: Guid.NewGuid(),
//                deptCode : "JX1",
//                deptName : "江西总公司",
//                orderNum : 100,
//                isDeleted: false,
//                parentId : chengziDept.Id
//            );
//            entities.Add(jiangxiDept);



//            Department szDept1 = new Department(
//                id: Guid.NewGuid(),
//                deptCode : "YF1",
//                deptName : "研发部门",
//                orderNum : 100,
//                isDeleted: false,
//                parentId : shenzhenDept.Id
//            );
//            entities.Add(szDept1);

//            Department szDept2 = new Department(
//                id: Guid.NewGuid(),
//                deptCode : "1SC",
//                deptName : "市场部门",
//                orderNum : 100,
//                isDeleted: false,
//                parentId : shenzhenDept.Id
//            );
//            entities.Add(szDept2);

//            Department szDept3 = new Department(
//                id: Guid.NewGuid(),
//                deptCode : "1CS",
//                deptName : "测试部门",
//                orderNum : 100,
//                isDeleted: false,
//                parentId : shenzhenDept.Id
//            );
//            entities.Add(szDept3);

//            Department szDept4 = new Department(
//                id: Guid.NewGuid(),
//                deptCode : "1CW",
//                deptName : "财务部门",
//                orderNum : 100,
//                isDeleted: false,
//                parentId : shenzhenDept.Id
//            );
//            entities.Add(szDept4);

//            Department szDept5 = new Department(
//                id: Guid.NewGuid(),
//                deptCode : "YW1",
//                deptName : "运维部门",
//                orderNum : 100,
//                isDeleted: false,
//                parentId : shenzhenDept.Id
//            );
//            entities.Add(szDept5);


//            Department jxDept1 = new Department(
//                id: Guid.NewGuid(),
//                deptCode : "SC1",
//                deptName : "市场部门",
//                orderNum : 100,
//                isDeleted: false,
//                parentId : jiangxiDept.Id
//            );
//            entities.Add(jxDept1);


//            Department jxDept2 = new Department(
//                id: Guid.NewGuid(),
//                deptCode : "CW12",
//                deptName : "财务部门",
//                orderNum : 100,
//                isDeleted : false,
//                parentId : jiangxiDept.Id
//            );
//            entities.Add(jxDept2);


//            return entities;
//        }
//    }
//}
