//using SqlSugar;
//using Volo.Abp.Data;
//using Volo.Abp.DependencyInjection;
//using Yi.Framework.CasbinRbac.Domain.Entities;
//using Yi.Framework.SqlSugarCore.Abstractions;

//namespace Yi.Framework.CasbinRbac.SqlSugarCore.DataSeeds
//{
//    public class DictionaryTypeDataSeed : IDataSeedContributor, ITransientDependency
//    {
//        private ISqlSugarRepository<DictionaryType> _repository;
//        public DictionaryTypeDataSeed(ISqlSugarRepository<DictionaryType> repository)
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
//        public List<DictionaryType> GetSeedData()
//        {
//            List<DictionaryType> entities = new List<DictionaryType>();
//            DictionaryType dict1 = new DictionaryType(
//                id: Guid.NewGuid(),
//                dictName : "用户性别",
//                dictType : "sys_user_sex",
//                orderNum : 100,
//                remark : "用户性别列表"
//            );
//            entities.Add(dict1);

//            DictionaryType dict2 = new DictionaryType(
//                id: Guid.NewGuid(),
//                dictName : "菜单状态",
//                dictType : "sys_show_hide",
//                orderNum : 100,
//                remark : "菜单状态列表"
//            );
//            entities.Add(dict2);

//            DictionaryType dict3 = new DictionaryType(
//                id: Guid.NewGuid(),
//                dictName : "系统开关",
//                dictType : "sys_normal_disable",
//                orderNum : 100,
//                remark : "系统开关列表"
//            );
//            entities.Add(dict3);

//            DictionaryType dict4 = new DictionaryType(
//                id: Guid.NewGuid(),
//                dictName : "任务状态",
//                dictType : "sys_job_status",
//                orderNum : 100,
//                remark : "任务状态列表"
//            );
//            entities.Add(dict4);

//            DictionaryType dict5 = new DictionaryType(
//                id: Guid.NewGuid(),
//                dictName : "任务分组",
//                dictType : "sys_job_group",
//                orderNum : 100,
//                remark : "任务分组列表"
//            );
//            entities.Add(dict5);

//            DictionaryType dict6 = new DictionaryType(
//                id: Guid.NewGuid(),
//                dictName : "系统是否",
//                dictType : "sys_yes_no",
//                orderNum : 100,
//                remark : "系统是否列表"
//            );
//            entities.Add(dict6);

//            DictionaryType dict7 = new DictionaryType(
//                id: Guid.NewGuid(),
//                dictName : "通知类型",
//                dictType : "sys_notice_type",
//                orderNum : 100,
//                remark : "通知类型列表"
//            );
//            entities.Add(dict7);
//            DictionaryType dict8 = new DictionaryType(
//                id: Guid.NewGuid(),
//                dictName : "通知状态",
//                dictType : "sys_notice_status",
//                orderNum : 100,
//                remark : "通知状态列表"
//            );
//            entities.Add(dict8);

//            DictionaryType dict9 = new DictionaryType(
//                id: Guid.NewGuid(),
//                dictName : "操作类型",
//                dictType : "sys_oper_type",
//                orderNum : 100,
//                remark : "操作类型列表"
//            );
//            entities.Add(dict9);


//            DictionaryType dict10 = new DictionaryType(
//                id: Guid.NewGuid(),
//                dictName : "系统状态",
//                dictType : "sys_common_status",
//                orderNum : 100,
//                remark : "登录状态列表"
//            );
//            entities.Add(dict10);
//            return entities;
//        }
//    }
//}
