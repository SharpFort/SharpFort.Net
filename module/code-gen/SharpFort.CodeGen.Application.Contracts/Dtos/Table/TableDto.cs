using Volo.Abp.Application.Dtos;
using SharpFort.CodeGen.Application.Contracts.Dtos.Field;

namespace SharpFort.CodeGen.Application.Contracts.Dtos.Table
{
    public class TableDto : EntityDto<Guid>
    {
        public new Guid Id { get; set; }
        /// <summary>
        /// 表名
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 目标模块名称
        /// </summary>
        public string? ModuleName { get; set; }

        /// <summary>
        /// 解决方案命名空间
        /// </summary>
        public string? RootNamespace { get; set; }

        /// <summary>
        /// 是否覆盖已有文件
        /// </summary>
        public bool IsOverwrite { get; set; }

        /// <summary>
        /// 模板引擎类型
        /// </summary>
        public string? TemplateEngine { get; set; }

        /// <summary>
        /// 一表多字段
        /// </summary>
        public List<FieldDto>? Fields { get; set; }
    }
}
