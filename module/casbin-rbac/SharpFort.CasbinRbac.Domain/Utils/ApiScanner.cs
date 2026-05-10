using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Volo.Abp.DependencyInjection;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.Domain.Utils
{
    /// <summary>
    /// API 扫描工具
    /// 扫描所有 Controller 中的 Action，提取 API 路径并同步到 Menu 表 (作为备选权限资源)
    /// </summary>
    public class ApiScanner(ISqlSugarRepository<Menu> menuRepo) : ITransientDependency
    {
        private readonly ISqlSugarRepository<Menu> _menuRepo = menuRepo;

        public async Task ScanAndSyncAsync(Assembly[] assemblies)
        {
            List<Type> controllers = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            List<Menu> newMenus = new();

            foreach (Type? controller in controllers)
            {
                RouteAttribute? routeAttr = controller.GetCustomAttribute<RouteAttribute>();
                string controllerPath = routeAttr?.Template ?? ""; // e.g., "api/app/[controller]"

                MethodInfo[] methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                foreach (MethodInfo method in methods)
                {
                    // 提取 Http Method 和 Template
                    HttpMethodAttribute? httpMethodAttr = method.GetCustomAttributes().OfType<HttpMethodAttribute>().FirstOrDefault();
                    if (httpMethodAttr == null)
                    {
                        continue;
                    }

                    string methodPath = httpMethodAttr.Template ?? "";
                    string httpMethod = httpMethodAttr.HttpMethods.FirstOrDefault() ?? "GET";

                    // 组合完整路径 (简单处理，实际可能需要更复杂的路由解析)
                    // Abp 自动路由规则比较复杂，这里仅处理显示声明 Route 的
                    // 假设 Controller Route 包含 [controller], [action] 等占位符

                    string fullPath = CombinePaths(controllerPath, methodPath);
                    fullPath = ReplacePlaceholders(fullPath, controller.Name, method.Name);

                    // 规范化路径
                    if (!fullPath.StartsWith('/'))
                    {
                        fullPath = "/" + fullPath;
                    }

                    // 检查是否已存在
                    bool exists = await _menuRepo.IsAnyAsync(m => m.ApiUrl == fullPath && m.ApiMethod == httpMethod);
                    if (!exists)
                    {
                        // 创建新的 API 资源 (作为 Menu 存储，Type=Button/Api)
                        // 注意：这里只是为了方便管理，实际 Menu 结构可能需要调整
                        Menu menu = new(
                            Guid.NewGuid(),
                            $"{controller.Name}.{method.Name}", // Name
                            fullPath, // Router/Url
                            Domain.Shared.Enums.MenuType.Button, // Type
                            Guid.Empty, // Parent (Root or specific category)
                            $"{controller.Name}:{method.Name}", // PermissionCode
                            null, // MenuIcon
                            null, // Component
                            fullPath, // ApiUrl
                            httpMethod, // ApiMethod
                            999 // OrderNum
                        );
                        menu.IsShow = false; // API 资源不显示在菜单栏

                        newMenus.Add(menu);
                    }
                }
            }

            if (newMenus.Count > 0)
            {
                await _menuRepo.InsertRangeAsync(newMenus);
            }
        }

        private static string CombinePaths(string p1, string p2)
        {
            if (string.IsNullOrEmpty(p1))
            {
                return p2;
            }

            return string.IsNullOrEmpty(p2) ? p1 : $"{p1.TrimEnd('/')}/{p2.TrimStart('/')}";
        }

        private static string ReplacePlaceholders(string path, string controllerName, string actionName)
        {
            // ControllerName usually ends with "Controller"
            string cName = controllerName.EndsWith("Controller", StringComparison.Ordinal) ? controllerName[..^10] : controllerName;

            path = path.Replace("[controller]", cName, StringComparison.OrdinalIgnoreCase);
            path = path.Replace("[action]", actionName, StringComparison.OrdinalIgnoreCase);
            return path;
        }
    }
}
