using System.Reflection;
using System.Web;
using SharpFort.WeChat.MiniProgram.Abstract;

namespace SharpFort.WeChat.MiniProgram;

public static class WeChatMiniProgramExtensions
{
    /// <summary>
    /// 效验请求是否成功
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    internal static void ValidateSuccess(this IErrorObjct response)
    {

        if (response.Errcode != 0)
        {
            throw new WeChatMiniProgramException(response.errmsg);
        }
    }

    internal static string ToQueryString<T>(this T obj)
    {
        PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        List<string> queryParams = new();

        foreach (PropertyInfo prop in properties)
        {
            object? value = prop.GetValue(obj, null);
            if (value != null)
            {
                // 处理集合
                if (value is IEnumerable<object> enumerable)
                {
                    foreach (object item in enumerable)
                    {
                        queryParams.Add($"{HttpUtility.UrlEncode(prop.Name)}={HttpUtility.UrlEncode(item.ToString())}");
                    }
                }
                else
                {
                    queryParams.Add($"{HttpUtility.UrlEncode(prop.Name)}={HttpUtility.UrlEncode(value.ToString())}");
                }
            }
        }

        return string.Join("&", queryParams);
    }
}