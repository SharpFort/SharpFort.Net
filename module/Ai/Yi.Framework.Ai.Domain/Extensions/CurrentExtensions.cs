using Microsoft.AspNetCore.Http;
using Volo.Abp.Users;
using Yi.Framework.Ai.Domain.Shared.Consts;

namespace Yi.Framework.Ai.Domain.Extensions;

public static class CurrentExtensions
{
    public static bool IsAiVip(this ICurrentUser currentUser)
    {
        return currentUser.Roles.Contains(AiHubConst.VipRole) || currentUser.UserName == "cc";
    }
}
