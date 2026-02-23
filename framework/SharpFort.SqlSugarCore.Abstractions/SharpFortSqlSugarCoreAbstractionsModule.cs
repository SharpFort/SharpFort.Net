using Volo.Abp.Modularity;
using SharpFort.Core;

namespace SharpFort.SqlSugarCore.Abstractions
{
    /// <summary>
    /// SqlSugar Core抽象层模块
    /// 提供SqlSugar ORM的基础抽象接口和类型定义
    /// </summary>
    [DependsOn(typeof(SharpFortCoreModule))]
    public class SharpFortSqlSugarCoreAbstractionsModule : AbpModule
    {
        // 模块配置方法可在此添加
    }
}