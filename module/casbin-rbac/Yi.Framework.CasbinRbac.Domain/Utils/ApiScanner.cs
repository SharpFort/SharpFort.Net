using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Volo.Abp.DependencyInjection;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.CasbinRbac.Domain.Utils
{
    /// <summary>
    /// API 扫描工具
    /// 扫描所有 Controller 中的 Action，提取 API 路径并同步到 Menu 表 (作为备选权限资源)
    /// </summary>
    public class ApiScanner : ITransientDependency
    {
        private readonly ISqlSugarRepository<Menu> _menuRepo;

        public ApiScanner(ISqlSugarRepository<Menu> menuRepo)
        {
            _menuRepo = menuRepo;
        }

        public async Task ScanAndSyncAsync(Assembly[] assemblies)
        {
            var controllers = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            var newMenus = new List<Menu>();

            foreach (var controller in controllers)
            {
                var routeAttr = controller.GetCustomAttribute<RouteAttribute>();
                var controllerPath = routeAttr?.Template ?? ""; // e.g., "api/app/[controller]"

                var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                foreach (var method in methods)
                {
                    // 提取 Http Method 和 Template
                    var httpMethodAttr = method.GetCustomAttributes().OfType<HttpMethodAttribute>().FirstOrDefault();
                    if (httpMethodAttr == null) continue;

                    var methodPath = httpMethodAttr.Template ?? "";
                    var httpMethod = httpMethodAttr.HttpMethods.FirstOrDefault() ?? "GET";

                    // 组合完整路径 (简单处理，实际可能需要更复杂的路由解析)
                    // Abp 自动路由规则比较复杂，这里仅处理显示声明 Route 的
                    // 假设 Controller Route 包含 [controller], [action] 等占位符
                    
                    var fullPath = CombinePaths(controllerPath, methodPath);
                    fullPath = ReplacePlaceholders(fullPath, controller.Name, method.Name);

                    // 规范化路径
                    if (!fullPath.StartsWith("/")) fullPath = "/" + fullPath;

                    // 检查是否已存在
                    var exists = await _menuRepo.IsAnyAsync(m => m.ApiUrl == fullPath && m.ApiMethod == httpMethod);
                    if (!exists)
                    {
                        // 创建新的 API 资源 (作为 Menu 存储，Type=Button/Api)
                        // 注意：这里只是为了方便管理，实际 Menu 结构可能需要调整
                        var menu = new Menu(
                            Guid.NewGuid(),
                            $"{controller.Name}.{method.Name}", // Name
                            fullPath, // Router/Url
                            Domain.Shared.Enums.MenuType.Button, // Type
                            Guid.Empty, // Parent (Root or specific category)
                            $"{controller.Name}:{method.Name}", // PermissionCode
                            null, null, 999
                        );
                        menu.ApiUrl = fullPath;
                        menu.ApiMethod = httpMethod;
                        menu.IsShow = false; // API 资源不显示在菜单栏

                        newMenus.Add(menu);
                    }
                }
            }

            if (newMenus.Any())
            {
                await _menuRepo.InsertRangeAsync(newMenus);
            }
        }

        private string CombinePaths(string p1, string p2)
        {
            if (string.IsNullOrEmpty(p1)) return p2;
            if (string.IsNullOrEmpty(p2)) return p1;
            return $"{p1.TrimEnd('/')}/{p2.TrimStart('/')}";
        }

        private string ReplacePlaceholders(string path, string controllerName, string actionName)
        {
            // ControllerName usually ends with "Controller"
            var cName = controllerName.EndsWith("Controller") ? controllerName.Substring(0, controllerName.Length - 10) : controllerName;
            
            path = path.Replace("[controller]", cName, StringComparison.OrdinalIgnoreCase);
            path = path.Replace("[action]", actionName, StringComparison.OrdinalIgnoreCase);
            return path;
        }
    }
}
