using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Auditing;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundWorkers;
using SharpFort.Rbac.Application;
using SharpFort.Rbac.Domain.Entities;
using SharpFort.Rbac.Domain.Managers;
using SharpFort.Rbac.Domain.Shared.Consts;
using SharpFort.Rbac.SqlSugarCore;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Rbac.Test
{
    [DependsOn(
        typeof(SharpFortRbacApplicationModule),
        typeof(SharpFortRbacSqlSugarCoreModule),

        typeof(AbpAutofacModule)
        )]
    public class SharpFortRbacTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {

            // Configure<AbpBackgroundWorkerQuartzOptions>(options =>
            // {
            //     options.IsAutoRegisterEnabled = false;
            // });
            Configure<AbpBackgroundWorkerOptions> (options =>
            {
                options.IsEnabled = false; //禁用作业执行
            });
            Configure<DbConnOptions>(options =>
            {
                options.Url = $"DataSource=yi-rbac-test-{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.db";
            });
        }

        public override async Task OnPostApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var services = context.ServiceProvider;

            #region 给默认角色设置一些权限，防止注册后无权限禁止登录
            var roleManager = services.GetRequiredService<RoleManager>();
            var roleRep = services.GetRequiredService<ISqlSugarRepository<Role>>();
            var menuRep = services.GetRequiredService<ISqlSugarRepository<Menu>>();
            var defaultRoleEntity = await roleRep._DbQueryable.Where(x => x.RoleCode == UserConst.DefaultRoleCode).FirstAsync();
            var menuIds = await menuRep._DbQueryable.Where(x => x.PermissionCode.Contains("user")).Select(x => x.Id).ToListAsync();
            await roleManager.GiveRoleSetMenuAsync(new List<Guid> { defaultRoleEntity.Id }, menuIds);
            #endregion
        }

    }
}
