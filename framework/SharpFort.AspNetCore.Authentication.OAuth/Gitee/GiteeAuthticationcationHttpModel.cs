using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// 属性名来自 Gitee OAuth API 返回的 JSON 字段，不可重命名
#pragma warning disable CA1707

namespace SharpFort.AspNetCore.Authentication.OAuth.Gitee
{
    public class GiteeAuthticationcationTokenResponse
    {
        public string access_token { get; set; } = null!;
        public string token_type { get; set; } = null!;
        public int expires_in { get; set; }
        public string refresh_token { get; set; } = null!;
        public string scope { get; set; } = null!;
        public long created_at { get; set; }
    }


    public class GiteeAuthticationcationOpenIdResponse
    {
        public string client_id { get; set; } = null!;

        public string openid { get; set; } = null!;

    }

    public class GiteeAuthticationcationUserInfoResponse
    {
        /// <summary>
        /// 也可以等于openId
        /// </summary>
        public int id { get; set; }
        public string login { get; set; } = null!;
        public string name { get; set; } = null!;
        public string avatar_url { get; set; } = null!;
        public string url { get; set; } = null!;
        public string html_url { get; set; } = null!;
        public string remark { get; set; } = null!;
        public string followers_url { get; set; } = null!;
        public string following_url { get; set; } = null!;
        public string gists_url { get; set; } = null!;
        public string starred_url { get; set; } = null!;
        public string subscriptions_url { get; set; } = null!;
        public string organizations_url { get; set; } = null!;
        public string repos_url { get; set; } = null!;
        public string events_url { get; set; } = null!;
        public string received_events_url { get; set; } = null!;
        public string type { get; set; } = null!;
        public string blog { get; set; } = null!;
        public string weibo { get; set; } = null!;
        public string bio { get; set; } = null!;
        public int public_repos { get; set; }
        public int public_gists { get; set; }
        public int followers { get; set; }
        public int following { get; set; }
        public int stared { get; set; }
        public int watched { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string email { get; set; } = null!;
    }
}

#pragma warning restore CA1707
