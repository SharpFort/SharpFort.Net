using IPTools.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using SharpFort.Core.Extensions;
using SharpFort.CasbinRbac.Domain.Shared.OperLog;

namespace SharpFort.CasbinRbac.Domain.Operlog
{
    public class OperLogGlobalAttribute(ILogger<OperLogGlobalAttribute> logger, IRepository<OperationLogEntity> repository, ICurrentUser currentUser, IUnitOfWorkManager unitOfWorkManager) : ActionFilterAttribute, ITransientDependency
    {
        private readonly ILogger<OperLogGlobalAttribute> _logger = logger;
        private readonly IRepository<OperationLogEntity> _repository = repository;
        private readonly ICurrentUser _currentUser = currentUser;

        private readonly IUnitOfWorkManager _unitOfWorkManager = unitOfWorkManager;

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ActionExecutedContext resultContext = await next();
            //执行后

            //判断标签是在方法上
            if (resultContext.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor)
            {
                return;
            }

            //查找标签，获取标签对象
            //空对象直接返回
            if (controllerActionDescriptor.MethodInfo
                .GetCustomAttributes(inherit: true)
                .FirstOrDefault(a => a.GetType().Equals(typeof(OperLogAttribute))) is not OperLogAttribute operLogAttribute)
            {
                return;
            }

            ////获取控制器名
            //string controller = context.RouteData.Values["Controller"].ToString();

            ////获取方法名
            //string action = context.RouteData.Values["Action"].ToString();
            //获取Ip
            string ip = resultContext.HttpContext.GetClientIp();

            //根据ip获取地址
            string location = "";
            try
            {
                IpInfo ipTool = IpTool.Search(ip);
                location = ipTool.Province + " " + ipTool.City;
            }
            catch
            {
                location = "搜索地址失败，可能是内网地址:" + ip;
            }


            //日志服务插入一条操作记录即可

            OperationLogEntity logEntity = new()
            {
                OperIp = ip,
                //logEntity.OperLocation = location;
                OperationType = operLogAttribute.OperationType,
                Title = operLogAttribute.Title,
                RequestMethod = resultContext.HttpContext.Request.Method,
                Method = resultContext.HttpContext.Request.Path.Value,
                OperLocation = location,
                OperUser = _currentUser.UserName
            };
            if (operLogAttribute.IsSaveResponseData)
            {
                if (resultContext.Result is ContentResult result && result.ContentType == "application/json")
                {
                    logEntity.RequestResult = result.Content?.Replace("\r\n", "").Trim();
                }

                if (resultContext.Result is JsonResult result2)
                {
                    logEntity.RequestResult = result2.Value?.ToString();
                }

                if (resultContext.Result is ObjectResult result3)
                {
                    logEntity.RequestResult = JsonConvert.SerializeObject(result3.Value);
                }

            }

            if (operLogAttribute.IsSaveRequestData)
            {
                // S-10/R-03: 深层脱敏 — 递归遍历 DTO 属性，遮蔽敏感字段
                logEntity.RequestParam = GetDesensitizedRequestParam(context);
            }

            using (IUnitOfWork uow = _unitOfWorkManager.Begin())
            {
                await _repository.InsertAsync(logEntity);
            }
        }

        // S-10/R-03: 敏感字段关键词（不区分大小写）
        private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "newPassword", "oldPassword", "confirmPassword", "pass", "pwd",
            "token", "accessToken", "refreshToken", "secret", "securityKey",
            "code", "smsCode"
        };

        private static string GetDesensitizedRequestParam(ActionExecutingContext context)
        {
            if (context.ActionArguments == null || context.ActionArguments.Count == 0)
            {
                return string.Empty;
            }

            try
            {
                string json = JsonConvert.SerializeObject(context.ActionArguments);
                JToken token = JToken.Parse(json);
                MaskSensitiveProperties(token);
                return token.ToString(Formatting.None);
            }
            catch
            {
                return "[Serialization Failed]";
            }
        }

        private static void MaskSensitiveProperties(JToken token)
        {
            if (token is JObject obj)
            {
                foreach (JProperty prop in obj.Properties().ToList())
                {
                    if (SensitiveKeys.Contains(prop.Name))
                    {
                        prop.Value = "***";
                    }
                    else
                    {
                        MaskSensitiveProperties(prop.Value);
                    }
                }
            }
            else if (token is JArray arr)
            {
                foreach (JToken item in arr)
                {
                    MaskSensitiveProperties(item);
                }
            }
        }

    }
}
