using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Yi.Framework.Bbs.Domain.Entities.Forum;
using Yi.Framework.Bbs.Domain.Shared.Etos;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Bbs.Domain.EventHandlers
{
    public class SeeDiscussEventHandler : ILocalEventHandler<SeeDiscussEventArgs>, ITransientDependency
    {
        private ISqlSugarRepository<Discuss, Guid> _repository;

        public SeeDiscussEventHandler(ISqlSugarRepository<Discuss, Guid> repository)
        {
            _repository = repository;
        }

        public async Task HandleEventAsync(SeeDiscussEventArgs eventData)
        {
            await _repository._Db.Updateable<Discuss>()
                .SetColumns(x => new Discuss { SeeNum = x.SeeNum + 1 })
                .Where(x => x.Id == eventData.DiscussId).ExecuteCommandAsync();
        }
    }
}