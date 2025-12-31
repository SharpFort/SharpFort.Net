using SqlSugar;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.CasbinRbac.SqlSugarCore.DataSeeds
{
    public class DictionaryDataSeed : IDataSeedContributor, ITransientDependency
    {
        private ISqlSugarRepository<Dictionary> _repository;
        public DictionaryDataSeed(ISqlSugarRepository<Dictionary> repository)
        {
            _repository= repository;
        }
        public async Task SeedAsync(DataSeedContext context)
        {
            if (!await _repository.IsAnyAsync(x => true))
            {
                await _repository.InsertManyAsync(GetSeedData());
            }
        }
        public  List<Dictionary> GetSeedData()
        {
            List<Dictionary> entities = new List<Dictionary>();
            Dictionary dictInfo1 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "男",
                dictValue : "Male",
                dictType : "sys_user_sex",
                orderNum : 100,
                remark : "性别男"

            );
            entities.Add(dictInfo1);

            Dictionary dictInfo2 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "女",
                dictValue : "Woman",
                dictType : "sys_user_sex",
                orderNum : 99,
                remark : "性别女"
            );
            entities.Add(dictInfo2);

            Dictionary dictInfo3 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "未知",
                dictValue : "Unknown",
                dictType : "sys_user_sex",
                orderNum : 98,
                remark : "性别未知"
            );
            entities.Add(dictInfo3);



            Dictionary dictInfo4 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "显示",
                dictValue : "true",
                dictType : "sys_show_hide",
                orderNum : 100,
                remark : "显示菜单"
            );
            entities.Add(dictInfo4);

            Dictionary dictInfo5 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "隐藏",
                dictValue : "false",
                dictType : "sys_show_hide",
                orderNum : 99,
                remark : "隐藏菜单"
            );
            entities.Add(dictInfo5);



            Dictionary dictInfo6 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "正常",
                dictValue : "true",
                dictType : "sys_normal_disable",
                orderNum : 100,
                remark : "正常状态"
            );
            entities.Add(dictInfo6);
            Dictionary dictInfo7 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "停用",
                dictValue : "false",
                dictType : "sys_normal_disable",
                orderNum : 99,
                remark : "停用状态",
                listClass : "danger"
            );
            entities.Add(dictInfo7);



            Dictionary dictInfo8 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "正常",
                dictValue : "0",
                dictType : "sys_job_status",
                orderNum : 100,
                remark : "正常状态"
            );
            entities.Add(dictInfo8);
            Dictionary dictInfo9 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "暂停",
                dictValue : "1",
                dictType : "sys_job_status",
                orderNum : 99,
                remark : "停用状态",
                listClass : "danger"
            );
            entities.Add(dictInfo9);

            Dictionary dictInfo10 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "默认",
                dictValue : "DEFAULT",
                dictType : "sys_job_group",
                orderNum : 100,
                remark : "默认分组"
            );
            entities.Add(dictInfo10);
            Dictionary dictInfo11 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "系统",
                dictValue : "SYSTEM",
                dictType : "sys_job_group",
                orderNum : 99,
                remark : "系统分组"
            );
            entities.Add(dictInfo11);



            Dictionary dictInfo12 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "是",
                dictValue : "Y",
                dictType : "sys_yes_no",
                orderNum : 100,
                remark : "系统默认是"
            );
            entities.Add(dictInfo12);
            Dictionary dictInfo13 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "否",
                dictValue : "N",
                dictType : "sys_yes_no",
                orderNum : 99,
                remark : "系统默认否",
                listClass : "danger"
            );
            entities.Add(dictInfo13);



            Dictionary dictInfo14 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "通知",
                dictValue : "1",
                dictType : "sys_notice_type",
                orderNum : 100,
                remark : "通知"
            );
            entities.Add(dictInfo14);
            Dictionary dictInfo15 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "公告",
                dictValue : "2",
                dictType : "sys_notice_type",
                orderNum : 99,
                remark : "公告"
            );
            entities.Add(dictInfo15);

            Dictionary dictInfo16 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "正常",
                dictValue : "0",
                dictType : "sys_notice_status",
                orderNum : 100,
                remark : "正常状态"
            );
            entities.Add(dictInfo16);
            Dictionary dictInfo17 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "关闭",
                dictValue : "1",
                dictType : "sys_notice_status",
                orderNum : 99,
                remark : "关闭状态",
                listClass : "danger"
            );
            entities.Add(dictInfo17);

            Dictionary dictInfo18 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "新增",
                dictValue : "Insert",
                dictType : "sys_oper_type",
                orderNum : 100,
                remark : "新增操作"
            );
            entities.Add(dictInfo18);
            Dictionary dictInfo19 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "修改",
                dictValue : "Update",
                dictType : "sys_oper_type",
                orderNum : 99,
                remark : "修改操作"
            );
            entities.Add(dictInfo19);
            Dictionary dictInfo22 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "删除",
                dictValue : "Delete",
                dictType : "sys_oper_type",
                orderNum : 98,
                remark : "删除操作",
                listClass : "danger"
            );
            entities.Add(dictInfo22);
            Dictionary dictInfo23 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "授权",
                dictValue : "Auth",
                dictType : "sys_oper_type",
                orderNum : 97,
                remark : "授权操作"
            );
            entities.Add(dictInfo23);
            Dictionary dictInfo24 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "导出",
                dictValue : "Export",
                dictType : "sys_oper_type",
                orderNum : 96,
                remark : "导出操作"
            );
            entities.Add(dictInfo24);
            Dictionary dictInfo25 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "导入",
                dictValue : "Import",
                dictType : "sys_oper_type",
                orderNum : 95,
                remark : "导入操作"
            );
            entities.Add(dictInfo25);
            Dictionary dictInfo26 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "强退",
                dictValue : "ForcedOut",
                dictType : "sys_oper_type",
                orderNum : 94,
                remark : "强退操作"
            );
            entities.Add(dictInfo26);
            Dictionary dictInfo27 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "生成代码",
                dictValue : "GenerateCode",
                dictType : "sys_oper_type",
                orderNum : 93,
                remark : "生成代码操作"
            );
            entities.Add(dictInfo27);
            Dictionary dictInfo28 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "清空数据",
                dictValue : "ClearData",
                dictType : "sys_oper_type",
                orderNum : 92,
                remark : "清空数据操作",
                listClass : "danger"
            );
            entities.Add(dictInfo28);

            Dictionary dictInfo20 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "成功",
                dictValue : "false",
                dictType : "sys_common_status",
                orderNum : 100,
                remark : "正常状态"
            );
            entities.Add(dictInfo20);
            Dictionary dictInfo21 = new Dictionary(
                id: Guid.NewGuid(),
                dictLabel : "失败",
                dictValue : "true",
                dictType : "sys_common_status",
                orderNum : 99,
                remark : "失败状态",
                listClass : "danger"
            );
            entities.Add(dictInfo21);

            return entities;
        }
    }
}
