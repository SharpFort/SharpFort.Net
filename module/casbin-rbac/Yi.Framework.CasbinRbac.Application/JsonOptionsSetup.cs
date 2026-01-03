using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Yi.Framework.CasbinRbac.Application.JsonConverters;

namespace Yi.Framework.CasbinRbac.Application
{
    public class JsonOptionsSetup : IConfigureOptions<JsonOptions>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JsonOptionsSetup(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Configure(JsonOptions options)
        {
            options.JsonSerializerOptions.Converters.Add(new FieldSecurityJsonConverterFactory(_httpContextAccessor));
        }
    }
}
