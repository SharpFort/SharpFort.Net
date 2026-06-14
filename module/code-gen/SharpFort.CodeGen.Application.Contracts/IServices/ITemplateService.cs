using SharpFort.CodeGen.Application.Contracts.Dtos.Template;
using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.CodeGen.Application.Contracts.IServices
{
    /// <summary>
    /// Scriban 代码生成模板服务接口
    /// ABP 常规控制器仅从接口方法生成端点，新增自定义端点必须在此声明
    /// </summary>
    public interface ITemplateService : ISfCrudAppService<TemplateDto, Guid, TemplateGetListInput>
    {
        /// <summary>
        /// 导入模板（本地 → DB）：扫描本地 Templates/*.scriban 文件，增量同步到 gen_template 表
        /// 规则（Upsert by Name）：
        ///   本地存在 + DB 不存在 → INSERT 新模板
        ///   本地存在 + DB 已存在 → UPDATE Content（保留用户 Remarks）
        ///   本地不存在 + DB 存在 → 跳过（不删除，由用户手动管理）
        /// </summary>
        Task PostImportTemplatesAsync();

        /// <summary>
        /// 导出模板（DB → 本地）：遍历数据库中所有模板，写入本地 Templates/{Name}.scriban 文件
        /// 规则：
        ///   DB 存在 + 本地不存在 → 写入本地文件（补全缺失）
        ///   DB 存在 + 本地存在   → 覆写本地文件（保持一致）
        /// </summary>
        Task PostExportTemplatesAsync();
    }
}
