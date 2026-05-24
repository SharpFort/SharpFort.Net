namespace SharpFort.CasbinRbac.Domain.Shared.Options
{
    public class RbacOptions
    {
        /// <summary>
        /// [已废弃] 超级管理员默认密码 (S-06)
        /// 当前管理员通过 UI 创建，密码由操作者指定。此字段不再使用。
        /// </summary>
        [Obsolete("管理员通过 UI 创建，密码由操作者输入，此字段不再使用")]
        public string AdminPassword { get; set; } = "123456";

        /// <summary>
        /// [已废弃] 租户超级管理员默认密码 (S-06)
        /// </summary>
        [Obsolete("同上")]
        public string TenantAdminPassword { get; set; } = "123456";

        /// <summary>
        /// [已废弃] 使用 EnableImageCaptcha 替代 (S-09)
        /// </summary>
        [Obsolete("使用 EnableImageCaptcha 替代")]
        public bool EnableCaptcha { get; set; }

        /// <summary>
        /// 是否开启图片验证码（登录） (S-09)
        /// </summary>
        public bool EnableImageCaptcha { get; set; }

        /// <summary>
        /// 是否开启手机短信验证码（注册/找回密码） (S-09)
        /// </summary>
        public bool EnablePhoneCaptcha { get; set; }

        /// <summary>
        /// 是否开启用户注册功能
        /// </summary>
        public bool EnableRegister { get; set; }

        /// <summary>
        /// 是否开启数据库备份
        /// </summary>
        public bool EnableDataBaseBackup { get; set; }
    }
}
