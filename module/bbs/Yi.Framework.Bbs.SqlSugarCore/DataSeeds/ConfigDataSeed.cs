//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using SqlSugar;
//using Volo.Abp.Data;
//using Volo.Abp.DependencyInjection;
//using Volo.Abp.Domain.Repositories;
//using Volo.Abp.Guids;
//using Yi.Framework.Rbac.Domain.Entities;
//using Yi.Framework.SqlSugarCore.Abstractions;

//namespace Yi.Framework.Bbs.SqlSugarCore.DataSeeds
//{
//    public class ConfigDataSeed : IDataSeedContributor, ITransientDependency
//    {
//        private ISqlSugarRepository<Config> _repository;
//        public ConfigDataSeed(ISqlSugarRepository<Config> repository)
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
//        public List<Config> GetSeedData()
//        {
//            List<Config> entities = new List<Config>();

//            Config config1 = new Config(
//               id: Guid.NewGuid(),           // 1. 必须生成一个 ID (ABP 建议用 IGuidGenerator，这里用 Guid.NewGuid() 也没问题)
//               configName: "站点名称",       // 2. 对应构造函数参数 configName
//               configKey: "bbs.site.name",   // 3. 对应构造函数参数 configKey
//               configValue: "意社区",        // 4. 对应构造函数参数 configValue
//               configType: "System",         // 5. (可选) 分类，不需要可传 null
//               remark: "论坛默认名称",       // 6. (可选) 备注，不需要可传 null
//               orderNum: 1                   // 7. (可选) 排序
//            );

//            entities.Add(config1);

//            Config config7 = new Config (
//                id: Guid.NewGuid(),
//                configKey : "bbs.site.name",
//                configName : "站点名称",
//                configValue : "意社区"
//            );
//            entities.Add(config7);

//            Config config2 = new Config(
//                id: Guid.NewGuid(),
//                configKey : "bbs.site.author",
//                configName: "站点作者",
//                configValue: "橙子"
//            );
//            entities.Add(config2);

//            Config config3 = new Config(
//                 id: Guid.NewGuid(),
//                configKey : "bbs.site.icp",
//                configName : "站点Icp备案",
//                configValue : "赣ICP备20008025号"
//            );
//            entities.Add(config3);


//            Config config4 = new Config(
//                 id: Guid.NewGuid(),
//                configKey: "bbs.site.bottom",
//                configName: "站点底部信息",
//                configValue: "你好世界"
//            );
//            entities.Add(config4);
//            return entities;
//        }
//    }


//}
