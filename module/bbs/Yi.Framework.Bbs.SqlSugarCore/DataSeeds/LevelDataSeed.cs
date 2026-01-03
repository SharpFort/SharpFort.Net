//using Volo.Abp.Data;
//using Volo.Abp.DependencyInjection;
//using Yi.Framework.Bbs.Domain.Entities.Integral;
//using Yi.Framework.SqlSugarCore.Abstractions;

//namespace Yi.Framework.Bbs.SqlSugarCore.DataSeeds
//{
//    public class LevelDataSeed : IDataSeedContributor, ITransientDependency
//    {
//        private ISqlSugarRepository<Level> _repository;
//        public LevelDataSeed(ISqlSugarRepository<Level> repository)
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
//        public List<Level> GetSeedData()
//        {
//            List<Level> entities = new List<Level>()
//            {
//                new Level(1,"小白",10),
//                new Level(2,"中白",30),
//                new Level(3,"大白",100),
//                new Level(4,"精英",300),
//                new Level(5,"熟练",600),
//                new Level(6,"高手",1000),
//                new Level(7,"老手",1500),
//                new Level(8,"大佬",2000),
//                new Level(9,"巨佬",2500),
//                new Level(10,"大神",3000),
//            };

//            return entities;
//        }
//    }
//}
