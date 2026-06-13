using System;
using Volo.Abp.Application.Dtos;
using SharpFort.CodeGen.Application.Contracts.Dtos.Field;

namespace SharpFort.CodeGen.Application.Contracts.Dtos.Table
{
    /// <summary>
    /// 实体注册表 DTO
    /// </summary>
    public class TableDto : EntityDto<Guid>
    {
        public new Guid Id { get; set; }

        /// <summary>
        /// 实体类名称 (如: SystemUser)
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// 物理数据库表名 (如: sys_user)
        /// </summary>
        public string? PhysicalTableName { get; set; }

        /// <summary>
        /// 实体描述/备注
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 所属模块名称
        /// </summary>
        public string? ModuleName { get; set; }

        /// <summary>
        /// 解决方案根命名空间
        /// </summary>
        public string? RootNamespace { get; set; }

        /// <summary>
        /// 生成代码时是否覆盖已有文件
        /// </summary>
        public bool IsOverwrite { get; set; }

        /// <summary>
        /// 所属项目名称
        /// </summary>
        public string? ProjectName { get; set; }

        /// <summary>
        /// 最后同步时间
        /// </summary>
        public DateTime? LastSyncTime { get; set; }

        /// <summary>
        /// 最后代码生成时间
        /// </summary>
        public DateTime? LastBuildTime { get; set; }

        /// <summary>
        /// 一表多字段 (导航属性)
        /// </summary>
        public List<FieldDto>? Fields { get; set; }
    }
}
