using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

namespace Sf.Abp.Test.example
{
    public class HttpUserTest : SfAbpTestWebBase
    {
        [Fact]
        public void HttpTest()
        {
            IHttpContextAccessor httpContext = GetRequiredService<IHttpContextAccessor>();
            httpContext.HttpContext!.Request.Path.ToString().ShouldBe("/test");
        }
    }
}
