using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpFort.AspNetCore.Authentication.OAuth.Gitee;

// 属性名来自 OAuth API 返回的 JSON 字段，不可重命名
#pragma warning disable CA1707

namespace SharpFort.AspNetCore.Authentication.OAuth
{
    public class AuthticationErrCodeModel
    {
        public string error { get; set; } = null!;

        public string error_description { get; set; } = null!;

        public static void VerifyErrResponse(string content)
        {

            var model = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthticationErrCodeModel>(content);
            if (model?.error != null)
            {

                throw new InvalidOperationException($"第三方授权返回错误，错误码：【{model.error}】，错误详情：【{model.error_description}】");
            }
        }
    }
}

#pragma warning restore CA1707
