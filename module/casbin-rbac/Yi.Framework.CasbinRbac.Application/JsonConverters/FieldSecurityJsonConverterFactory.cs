using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Users;
using Yi.Framework.CasbinRbac.Domain.Managers;
using Yi.Framework.CasbinRbac.Domain.Shared.Attributes;

namespace Yi.Framework.CasbinRbac.Application.JsonConverters
{
    public class FieldSecurityJsonConverterFactory : JsonConverterFactory
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FieldSecurityJsonConverterFactory(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            // 只有标记了 [SecureResource] 的类型才会被拦截
            // 且必须是类 (不包括数组、列表等，因为 Converter 是挂在 T 上的)
            return typeToConvert.IsClass && typeToConvert.GetCustomAttribute<SecureResourceAttribute>() != null;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            // 获取资源名
            var attr = typeToConvert.GetCustomAttribute<SecureResourceAttribute>();
            var resourceName = attr!.ResourceName;

            var converterType = typeof(FieldSecurityConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType, _httpContextAccessor, resourceName)!;
        }
    }

    public class FieldSecurityConverter<T> : JsonConverter<T> where T : class
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _resourceName;
        // 静态缓存属性元数据，避免每次 Write 都反射
        private static readonly PropertyInfo[] _properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        public FieldSecurityConverter(IHttpContextAccessor httpContextAccessor, string resourceName)
        {
            _httpContextAccessor = httpContextAccessor;
            _resourceName = resourceName;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 反序列化通常不需要字段权限限制，或者使用默认行为
            // 但如果不想自己实现 Read，比较麻烦，因为 JsonConverter 必须同时实现 Read 和 Write
            // 这里使用一个简单的 Trick：调用 JsonSerializer.Deserialize 可能会导致无限递归，除非我们移除当前 Converter
            // 但 JsonSerializerOptions 是不可变的...
            
            // 简单方案：对于 DTO，通常只输出。如果是 Input DTO，可能不需要字段黑名单？
            // 假设我们主要关注 Output (Read).
            
            // 如果必须实现 Read，可以使用 JsonDocument 解析然后手动赋值，或者创建一个新的 Options (无 Converter)
            // 考虑性能，这里如果 Read 不常用，可以用 fallback.
            
            // 实际上，对于输出控制，Read 很少用到 (除非是 Update 接口的入参 DTO 复用)
            // 我们暂时实现一个基于默认反序列化的逻辑 (Clone options 移除 factory)
             var newOptions = new JsonSerializerOptions(options);
             // 必须移除当前的 Factory 否则死循环
             // 但很难找到当前 Factory 实例... 
             
             // 更好的做法：只对 Default 实现 Read，或者抛出异常表示不支持
             // 为了兼容性，我们尝试反序列化
             // 由于本 Converter 是由 Factory 创建的，newOptions 如果不加 Factory 就行
             // 但 options 可能包含其他重要的 converter
             throw new NotImplementedException("FieldSecurityConverter generally used for Output (Serialize) only.");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            // 1. 获取当前用户
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                // 无上下文 (如后台任务)，默认全输出 (通过默认 WriteObject 防止递归)
                WriteObjectDefault(writer, value, options, null);
                return;
            }

            // 2. 获取服务
            var currentUser = httpContext.RequestServices.GetService<ICurrentUser>();
            var cache = httpContext.RequestServices.GetService<IFieldPermissionCache>();
            
            HashSet<string> denyFields = null;

            if (currentUser != null && currentUser.IsAuthenticated && cache != null && currentUser.Roles != null)
            {
                // CurrentUser.Roles 通常是 RoleCode (Name)
                denyFields = cache.GetDenyFieldsByCodes(currentUser.Roles, _resourceName);
            }

            // 3. 序列化
            WriteObjectDefault(writer, value, options, denyFields);
        }

        private void WriteObjectDefault(Utf8JsonWriter writer, T value, JsonSerializerOptions options, HashSet<string> denyFields)
        {
            writer.WriteStartObject();

            foreach (var prop in _properties)
            {
                // 过滤 WriteOnly
                if (!prop.CanRead) continue;
                
                // 字段过滤核心逻辑
                if (denyFields != null && denyFields.Contains(prop.Name)) 
                {
                    continue; // Skip
                }

                // 获取值
                var propVal = prop.GetValue(value);
                
                // 忽略 Null (如果设置了)
                if (propVal == null && options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
                {
                    continue;
                }

                // 写入属性名
                // 注意：这里需要遵循 NamingPolicy (e.g. camelCase)
                var propName = options.PropertyNamingPolicy?.ConvertName(prop.Name) ?? prop.Name;
                writer.WritePropertyName(propName);

                // 递归序列化属性值
                JsonSerializer.Serialize(writer, propVal, prop.PropertyType, options);
            }

            writer.WriteEndObject();
        }
    }
}
