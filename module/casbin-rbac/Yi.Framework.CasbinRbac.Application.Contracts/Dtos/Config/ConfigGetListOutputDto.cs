using Volo.Abp.Application.Dtos;

namespace Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Config
{
    public class ConfigGetListOutputDto : EntityDto<Guid>
    {
        public Guid Id { get; set; }
        /// <summary>
        /// Config Name
        /// </summary>
        public string ConfigName { get; set; } = string.Empty;

        /// <summary>
        /// Config Key
        /// </summary>
        public string ConfigKey { get; set; } = string.Empty;
        /// <summary>
        /// Config Value
        /// </summary>
        public string ConfigValue { get; set; } = string.Empty;
        /// <summary>
        /// Config Type
        /// </summary>
        public string? ConfigType { get; set; }
        /// <summary>
        /// Order Num
        /// </summary>
        public int OrderNum { get; set; }

        /// <summary>
        /// Remark
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// Creation Time
        /// </summary>
        public DateTime CreationTime { get; set; }
    }
}
