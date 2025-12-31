//using Volo.Abp.Data;
//using Volo.Abp.DependencyInjection;
//using Yi.Framework.CasbinRbac.Domain.Entities;
//using Yi.Framework.SqlSugarCore.Abstractions;

//namespace Yi.Framework.CasbinRbac.SqlSugarCore.DataSeeds
//{
//    public class PostDataSeed : IDataSeedContributor, ITransientDependency
//    {
//        private ISqlSugarRepository<Position> _repository;
//        public PostDataSeed(ISqlSugarRepository<Position> repository)
//        {
//            _repository = repository;
//        }
//        public async Task SeedAsync(DataSeedContext context)
//        {
//            if (!await _repository.IsAnyAsync(x => true))
//            {
//                await _repository.InsertManyAsync(GetSeedData());
//            }
//        }
//        public List<Position> GetSeedData()
//        {
//            var entites = new List<Position>();

//            Position Post1 = new Position()
//            {

//                PostName = "董事长",
//                PostCode = "ceo",
//                OrderNum = 100,
//                IsDeleted = false
//            };
//            entites.Add(Post1);

//            Position Post2 = new Position()
//            {

//                PostName = "项目经理",
//                PostCode = "se",
//                OrderNum = 100,
//                IsDeleted = false
//            };
//            entites.Add(Post2);

//            Position Post3 = new Position()
//            {

//                PostName = "人力资源",
//                PostCode = "hr",
//                OrderNum = 100,
//                IsDeleted = false
//            };
//            entites.Add(Post3);

//            Position Post4 = new Position()
//            {

//                PostName = "普通员工",
//                PostCode = "user",
//                OrderNum = 100,
//                IsDeleted = false
//            };

//            entites.Add(Post4);
//            return entites;
//        }
//    }


//}
