using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SharpFort.CodeGen.Application.Contracts.IServices
{
    /// <summary>
    /// CodeGen 代码生成核心服务接口
    /// </summary>
    public interface ICodeGenService : IApplicationService
    {
        /// <summary>
        /// 生成代码 (Web → Code)：根据选中的实体注册表条目，使用 Scriban 模板生成 DTO / IService / Service 代码文件
        /// </summary>
        Task PostWebBuildCodeAsync(List<Guid> ids);

        /// <summary>
        /// 同步实体到注册表 (Code → Web)：反射扫描所有带 [SugarTable] 特性的 C# 实体类，增量合并到 YiTable 注册表
        /// </summary>
        Task PostCodeBuildWebAsync();

        /// <summary>
        /// 手动刷新实体注册表：重新扫描所有实体类并增量同步到 YiTable
        /// </summary>
        Task PostRefreshAsync();
    }
}
