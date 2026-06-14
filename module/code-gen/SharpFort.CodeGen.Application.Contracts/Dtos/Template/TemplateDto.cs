using Volo.Abp.Application.Dtos;

namespace SharpFort.CodeGen.Application.Contracts.Dtos.Template
{
    public class TemplateDto : EntityDto<Guid>
    {
        public new Guid Id { get; set; }

        /// <summary>
        /// Scriban 模板脚本内容
        /// </summary>
        public required string Content { get; set; } = string.Empty;

        /// <summary>
        /// 生成路径
        /// </summary>
        public required string BuildPath { get; set; }


        /// <summary>
        /// 模板名称
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remarks { get; set; }
    }
}
