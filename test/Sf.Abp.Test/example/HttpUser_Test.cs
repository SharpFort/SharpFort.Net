using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

namespace Sf.Abp.Test.example
{
    public class HttpUser_Test : SfAbpTestWebBase
    {
        [Fact]
        public void Http_Test()
        {
            var httpContext = GetRequiredService<IHttpContextAccessor>();
            httpContext.HttpContext.Request.Path.ToString().ShouldBe("/test");
        }
    }
}
