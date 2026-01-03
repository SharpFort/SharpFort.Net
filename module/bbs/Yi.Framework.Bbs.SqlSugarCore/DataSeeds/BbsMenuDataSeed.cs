//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using SqlSugar.DistributedSystem.Snowflake;
//using Volo.Abp.Data;
//using Volo.Abp.DependencyInjection;
//using Volo.Abp.Guids;
//using Yi.Framework.Rbac.Domain.Entities;
//using Yi.Framework.Rbac.Domain.Shared.Enums;
//using Yi.Framework.SqlSugarCore.Abstractions;

//namespace Yi.Framework.Bbs.SqlSugarCore.DataSeeds
//{
//    public class BbsMenuDataSeed : IDataSeedContributor, ITransientDependency
//    {
//        private IGuidGenerator _guidGenerator;
//        private ISqlSugarRepository<Menu, Guid> _repository;
//        public BbsMenuDataSeed(ISqlSugarRepository<Menu,Guid> repository, IGuidGenerator guidGenerator)
//        {
//            _repository=repository;
//            _guidGenerator=guidGenerator;
//        }
//        public async Task SeedAsync(DataSeedContext context)
//        {
//            if (!await _repository.IsAnyAsync(x => x.MenuName == "BBS"))
//            {
//                await _repository.InsertManyAsync(GetSeedData());
//            }
//        }

//        public List<Menu> GetSeedData()
//        {
//            List<Menu> entities = new List<Menu>();
//            //BBS
//            Menu bbs = new Menu(
//                id: Guid.NewGuid(),
//                menuName : "BBS",
//                menuType : MenuType.Catalogue,
//                router : "/bbs",
//                parentId: Guid.Empty,
//                menuIcon : "monitor",
//                orderNum : 91
//            );
//            entities.Add(bbs);



//            //板块管理
//            Menu plate = new Menu(
//                id: Guid.NewGuid(),
//                menuName : "板块管理",
//                permissionCode : "bbs:plate:list",
//                menuType : MenuType.Menu,
//                router : "plate",
//                component : "bbs/plate/index",
//                menuIcon : "component",
//                orderNum : 100,
//                parentId : bbs.Id
//            );
//            entities.Add(plate);

//            //文章管理
//            Menu article = new Menu(
//                id: Guid.NewGuid(),
//                menuName : "文章管理",
//                permissionCode : "bbs:article:list",
//                menuType : MenuType.Menu,
//                router : "article",
//                component : "bbs/article/index",
//                menuIcon : "documentation",
//                orderNum : 99,
//                parentId : bbs.Id
//            );
//            entities.Add(article);


//            return entities;
//        }
//    }
//}
