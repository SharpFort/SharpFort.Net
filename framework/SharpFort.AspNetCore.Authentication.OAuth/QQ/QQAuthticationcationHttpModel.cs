using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// 属性名来自 QQ OAuth API 返回的 JSON 字段，不可重命名
#pragma warning disable CA1707

namespace SharpFort.AspNetCore.Authentication.OAuth.QQ
{
    public class QQAuthticationcationTokenResponse
    {
        public string access_token { get; set; } = null!;

        public string expires_in { get; set; } = null!;

        public string refresh_token { get; set; } = null!;

        public string openid { get; set; } = null!;
    }


    public class QQAuthticationcationOpenIdResponse
    {
        public string client_id { get; set; } = null!;

        public string openid { get; set; } = null!;

    }

    public class QQAuthticationcationUserInfoResponse
    {
        // 返回码
        public int ret { get; set; }

        // 如果ret<0，会有相应的错误信息提示
        // 返回数据全部用UTF-8编码
        public string msg { get; set; } = null!;

        // 判断是否有数据丢失
        // 0或者不返回：没有数据丢失，可以缓存
        // 1：有部分数据丢失或错误，不要缓存
        public int is_lost { get; set; }

        // 用户在QQ空间的昵称
        public string nickname { get; set; } = null!;

        // 大小为30x30像素的QQ空间头像URL
        public string figureurl { get; set; } = null!;

        // 大小为50x50像素的QQ空间头像URL
        public string figureurl_1 { get; set; } = null!;

        // 大小为100x100像素的QQ空间头像URL
        public string figureurl_2 { get; set; } = null!;

        // 大小为40x40像素的QQ头像URL
        public string figureurl_qq_1 { get; set; } = null!;

        // 大小为100x100像素的QQ头像URL
        // 需要注意，不是所有的用户都拥有QQ的100x100的头像，但40x40像素则是一定会有
        public string figureurl_qq_2 { get; set; } = null!;

        // 性别。如果获取不到则默认返回"男"
        public string gender { get; set; } = null!;

        // 性别类型。默认返回2
        public int gender_type { get; set; }

        // 省
        public string province { get; set; } = null!;

        // 市
        public string city { get; set; } = null!;

        // 年
        public int year { get; set; }

        // 星座
        public string constellation { get; set; } = null!;

        // 标识用户是否为黄钻用户
        public int is_yellow_vip { get; set; }

        // 黄钻等级
        public int yellow_vip_level { get; set; }

        // 是否为年费黄钻用户
        public int is_yellow_year_vip { get; set; }
    }
}

#pragma warning restore CA1707
