using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;
using Yi.Framework.Bbs.Domain.Entities;
using Yi.Framework.Bbs.Domain.Entities.Forum;
using Yi.Framework.Bbs.Domain.Shared.Consts;
using Yi.Framework.Bbs.Domain.Shared.Enums;
using Yi.Framework.Bbs.Domain.Shared.Etos;
using Yi.Framework.Rbac.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Bbs.Domain.EventHandlers
{
    /// <summary>
    /// 被点赞
    /// </summary>
    public class AgreeChangeEventHandler : ILocalEventHandler<EntityCreatedEventData<Agree>>,
        ITransientDependency
    {
        private ISqlSugarRepository<User> _userRepository;
        private ISqlSugarRepository<BbsUserExtraInfo> _userInfoRepository;
        private ISqlSugarRepository<Agree> _agreeRepository;
        private ILocalEventBus _localEventBus;

        public AgreeChangeEventHandler(ISqlSugarRepository<BbsUserExtraInfo> userInfoRepository,
            ISqlSugarRepository<Agree> agreeRepository, ILocalEventBus localEventBus,
            ISqlSugarRepository<User> userRepository)
        {
            _userInfoRepository = userInfoRepository;
            _agreeRepository = agreeRepository;
            _localEventBus = localEventBus;
            _userRepository = userRepository;
        }

        public async Task HandleEventAsync(EntityCreatedEventData<Agree> eventData)
        {
            var Agree = eventData.Entity;

            //查询主题的信息
            var discussAndAgreeDto = await _agreeRepository._DbQueryable
                .Where(agree=>agree.Id==Agree.Id)
                .LeftJoin<Discuss>((agree, discuss) => agree.DiscussId == discuss.Id)
                .Select((agree, discuss) =>
                    new
                    {
                        DiscussId = discuss.Id,
                        DiscussTitle = discuss.Title,
                        DiscussCreatorId = discuss.CreatorId,
                    })
                .FirstAsync();

            //查询点赞者用户
            var agreeUser = await _userRepository.GetFirstAsync(x => x.Id == Agree.CreatorId);

            //给创建者点赞数量+1
            await _userInfoRepository._Db.Updateable<BbsUserExtraInfo>()
                .SetColumns(it => it.AgreeNumber == it.AgreeNumber + 1)
                .Where(it => it.UserId == discussAndAgreeDto.DiscussCreatorId)
                .ExecuteCommandAsync();

            //通知主题作者，有人点赞
            await _localEventBus.PublishAsync(
                new BbsNoticeEventArgs(discussAndAgreeDto.DiscussCreatorId!.Value,
                    string.Format(DiscussConst.AgreeNotice, discussAndAgreeDto.DiscussTitle, agreeUser.UserName,
                        discussAndAgreeDto.DiscussId)), false);

            //最后发布任务触发事件
            await _localEventBus.PublishAsync(
                new AssignmentEventArgs(AssignmentRequirements.Agree, agreeUser.Id),false);
        }
    }

    /// <summary>
    /// 取消点赞
    /// </summary>
    public class AgreeDeletedEventHandler : ILocalEventHandler<EntityDeletedEventData<Agree>>,
        ITransientDependency
    {
        private ISqlSugarRepository<BbsUserExtraInfo> _userRepository;
        private ISqlSugarRepository<Agree> _agreeRepository;
        private ILocalEventBus _localEventBus;

        public AgreeDeletedEventHandler(ISqlSugarRepository<BbsUserExtraInfo> userRepository,
            ISqlSugarRepository<Agree> agreeRepository, ILocalEventBus localEventBus)
        {
            _userRepository = userRepository;
            _agreeRepository = agreeRepository;
            _localEventBus = localEventBus;
        }

        public async Task HandleEventAsync(EntityDeletedEventData<Agree> eventData)
        {
            var Agree = eventData.Entity;
            var userId = await _agreeRepository._DbQueryable
                .LeftJoin<Discuss>((agree, discuss) => agree.DiscussId == discuss.Id)
                .Select((agree, discuss) => discuss.CreatorId).FirstAsync();

            //给创建者点赞数量-1
            await _userRepository._Db.Updateable<BbsUserExtraInfo>()
                .SetColumns(it => it.AgreeNumber == it.AgreeNumber - 1)
                .Where(it => it.UserId == userId)
                .ExecuteCommandAsync();
        }
    }
}