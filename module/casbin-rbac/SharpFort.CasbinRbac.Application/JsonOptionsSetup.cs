using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SharpFort.CasbinRbac.Application.JsonConverters;

namespace SharpFort.CasbinRbac.Application
{
    public class JsonOptionsSetup(IHttpContextAccessor httpContextAccessor) : IConfigureOptions<JsonOptions>
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public void Configure(JsonOptions options)
        {
            options.JsonSerializerOptions.Converters.Add(new FieldSecurityJsonConverterFactory(_httpContextAccessor));
        }
    }
}
