//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Volo.Abp.Data;
//using Volo.Abp.DependencyInjection;
//using Volo.Abp.Domain.Repositories;
//using Yi.Framework.Rbac.Domain.Entities;
//using Yi.Framework.SqlSugarCore.Abstractions;

//namespace Yi.Framework.Bbs.SqlSugarCore.DataSeeds
//{
//    public class BbsDictionaryDataSeed : IDataSeedContributor, ITransientDependency
//    {
//        private ISqlSugarRepository<Dictionary> _repository;
//        private ISqlSugarRepository<DictionaryType> _typeRepository;
//        public BbsDictionaryDataSeed(ISqlSugarRepository<Dictionary> repository, ISqlSugarRepository<DictionaryType> typeRepository) {
//            _repository=repository;
//            _typeRepository=typeRepository;

//        }
//        public async Task SeedAsync(DataSeedContext context)
//        {
//            if (!await _typeRepository.IsAnyAsync(x => x.DictType== "bbs_type_lable"))
//            {
//                await _typeRepository.InsertManyAsync(GetSeedDictionaryTypeData());
//                await _repository.InsertManyAsync(GetSeedDictionaryData());
//            }
//        }
//        public List<Dictionary> GetSeedDictionaryData()
//        {
//            List<Dictionary> entities = new List<Dictionary>();
//            Dictionary dictInfo1 = new Dictionary(
//                id: Guid.NewGuid(),
//                dictLabel : "前端",
//                dictValue : "0",
//                dictType : "bbs_type_lable",
//                orderNum : 100,
//                remark : ""
//            );
//            entities.Add(dictInfo1);

//            Dictionary dictInfo2 = new Dictionary(
//                id : Guid.NewGuid(),
//                dictLabel : "后端",
//                dictValue : "1",
//                dictType : "bbs_type_lable",
//                orderNum : 99,
//                remark : ""
//            );
//            entities.Add(dictInfo2);

//            Dictionary dictInfo3 = new Dictionary(
//                id : Guid.NewGuid(),
//                dictLabel : "运维",
//                dictValue : "2",
//                dictType : "bbs_type_lable",
//                orderNum : 98,
//                remark : ""
//            );
//            entities.Add(dictInfo3);
//            Dictionary dictInfo4 = new Dictionary(
//                id : Guid.NewGuid(),
//                dictLabel : "测试",
//                dictValue : "3",
//                dictType : "bbs_type_lable",
//                orderNum : 97,
//                remark : ""
//            );
//            entities.Add(dictInfo4);

//            Dictionary dictInfo5 = new Dictionary(
//                id : Guid.NewGuid(),
//                dictLabel : "UI",
//                dictValue : "4",
//                dictType : "bbs_type_lable",
//                orderNum : 96,
//                remark : ""
//            );
//            entities.Add(dictInfo5);


//            Dictionary dictInfo6 = new Dictionary(
//                id : Guid.NewGuid(),
//                dictLabel : "产品",
//                dictValue : "5",
//                dictType : "bbs_type_lable",
//                orderNum : 95,
//                remark : ""
//            );
//            entities.Add(dictInfo6);

//            Dictionary dictInfo7 = new Dictionary(
//                id : Guid.NewGuid(),
//                dictLabel : "项目",
//                dictValue : "6",
//                dictType : "bbs_type_lable",
//                orderNum : 94,
//                remark : ""
//            );
//            entities.Add(dictInfo7);

//            Dictionary dictInfo8 = new Dictionary(
//                id : Guid.NewGuid(),
//                dictLabel : "C#",
//                dictValue : "7",
//                dictType : "bbs_type_lable",
//                orderNum : 93,
//                remark : ""
//            );
//            entities.Add(dictInfo8);

//            Dictionary dictInfo9 = new Dictionary(
//                id : Guid.NewGuid(),
//                dictLabel : ".Net",
//                dictValue : "8",
//                dictType : "bbs_type_lable",
//                orderNum : 92,
//                remark : ""
//            );
//            entities.Add(dictInfo9);


//            Dictionary dictInfo10 = new Dictionary(
//                id : Guid.NewGuid(),
//                dictLabel : ".NetCore",
//                dictValue : "9",
//                dictType : "bbs_type_lable",
//                orderNum : 91,
//                remark : ""
//            );
//            entities.Add(dictInfo10);


//            Dictionary dictInfo11 = new Dictionary(
//                id : Guid.NewGuid(),
//                dictLabel : "Asp.NetCore",
//                dictValue : "10",
//                dictType : "bbs_type_lable",
//                orderNum : 90,
//                remark : ""
//            );
//            entities.Add(dictInfo11);

//            Dictionary dictInfo12 = new Dictionary(
//                id : Guid.NewGuid(),
//                dictLabel : "Abp.vNext",
//                dictValue : "11",
//                dictType : "bbs_type_lable",
//                orderNum : 89,
//                remark : ""
//            );
//            entities.Add(dictInfo12);

//            return entities;
//        }

//        public List<DictionaryType> GetSeedDictionaryTypeData()
//        {
//            List<DictionaryType> entities = new List<DictionaryType>();
//            DictionaryType dict1 = new DictionaryType(
//                id : Guid.NewGuid(),
//                dictName : "BBS类型标签",
//                dictType : "bbs_type_lable",
//                orderNum : 200,
//                remark : "BBS类型标签"
//            );
//            entities.Add(dict1);
//            return entities;
//        }
//    }
//}
