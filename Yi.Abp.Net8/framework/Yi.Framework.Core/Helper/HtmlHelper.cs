namespace Yi.Framework.Core.Helper
{
    /// <summary>
    /// HTML 处理辅助类
    /// </summary>
    /// <remarks>
    /// ⚠️ 安全警告：此类仅用于提取纯文本，不能用于 XSS 防护！
    ///
    /// 1. 正确用途：
    ///    - 从富文本提取纯文本摘要（如文章列表预览）
    ///    - 搜索结果片段生成
    ///    - 纯文本邮件内容生成
    ///
    /// 2. 禁止用途（安全风险）：
    ///    - ❌ 不能用于 XSS 防护（正则无法可靠解析 HTML）
    ///    - ❌ 不能用于清理用户输入后存储
    ///    - ❌ 不能用于过滤后直接输出到 HTML
    ///
    /// 3. XSS 防护正确方案：
    ///    <code>
    ///    // 方案1：输出编码（最常用）
    ///    var safe = System.Web.HttpUtility.HtmlEncode(userInput);
    ///
    ///    // 方案2：使用专业的 HTML 清理库
    ///    // NuGet: HtmlSanitizer
    ///    var sanitizer = new HtmlSanitizer();
    ///    var safe = sanitizer.Sanitize(userHtml);
    ///
    ///    // 方案3：Razor 视图自动编码
    ///    @Model.UserInput  // 自动 HTML 编码
    ///    </code>
    ///
    /// 4. 已知限制：
    ///    - 正则无法处理嵌套/畸形标签（可能被绕过）
    ///    - 移除所有 HTML 实体可能丢失合法字符（如 &amp; → 空）
    ///    - null 输入会抛出 ArgumentNullException
    /// </remarks>
    public static class HtmlHelper
    {
        #region 去除富文本中的HTML标签
        /// <summary>
        /// 去除富文本中的 HTML 标签，提取纯文本内容
        /// </summary>
        /// <param name="html">包含 HTML 标签的富文本字符串</param>
        /// <param name="length">返回文本的最大长度，0 表示不限制</param>
        /// <returns>去除标签后的纯文本</returns>
        /// <exception cref="ArgumentNullException">html 参数为 null 时抛出</exception>
        /// <remarks>
        /// 使用场景：
        /// 1. 文章列表页显示内容摘要
        /// 2. 搜索结果预览文本
        /// 3. SEO meta description 生成
        ///
        /// 实现说明：
        /// - 使用正则移除所有 HTML 标签（&lt;...&gt;）
        /// - 移除所有 HTML 实体（&amp;xxx;）
        /// - 可选截取指定长度
        ///
        /// ⚠️ 安全警告：
        /// 此方法不能用于 XSS 防护！正则解析 HTML 不可靠，恶意构造的输入可能绕过过滤。
        /// 如需 XSS 防护，请使用 HtmlEncode 或 HtmlSanitizer 库。
        ///
        /// 示例：
        /// <code>
        /// var html = "&lt;p&gt;Hello &lt;b&gt;World&lt;/b&gt;&lt;/p&gt;";
        /// var text = HtmlHelper.ReplaceHtmlTag(html);
        /// // 结果: "Hello World"
        ///
        /// var excerpt = HtmlHelper.ReplaceHtmlTag(html, 5);
        /// // 结果: "Hello"
        /// </code>
        /// </remarks>
        public static string ReplaceHtmlTag(string html, int length = 0)
        {
            string strText = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
            strText = System.Text.RegularExpressions.Regex.Replace(strText, "&[^;]+;", "");

            if (length > 0 && strText.Length > length)
                return strText.Substring(0, length);

            return strText;
        }
        #endregion
    }
}
