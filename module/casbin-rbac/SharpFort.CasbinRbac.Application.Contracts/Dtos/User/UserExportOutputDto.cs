using System;
using MiniExcelLibs.Attributes;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.User
{
    public class UserExportOutputDto
    {
        [ExcelColumn(Name = "用户名", Width = 15)]
        public string UserName { get; set; } = string.Empty;

        [ExcelColumn(Name = "姓名", Width = 15)]
        public string? Name { get; set; }

        [ExcelColumn(Name = "昵称", Width = 15)]
        public string? Nick { get; set; }

        [ExcelColumn(Name = "性别", Width = 10)]
        public string Gender { get; set; } = string.Empty;

        [ExcelColumn(Name = "部门", Width = 20)]
        public string DeptName { get; set; } = string.Empty;

        [ExcelColumn(Name = "岗位", Width = 25)]
        public string PostNames { get; set; } = string.Empty;

        [ExcelColumn(Name = "角色", Width = 25)]
        public string RoleNames { get; set; } = string.Empty;

        [ExcelColumn(Name = "电话", Width = 15)]
        public long? Phone { get; set; }

        [ExcelColumn(Name = "邮箱", Width = 20)]
        public string? Email { get; set; }

        [ExcelColumn(Name = "状态", Width = 10)]
        public string State { get; set; } = string.Empty;

        [ExcelColumn(Name = "备注", Width = 30)]
        public string? Remark { get; set; }

        [ExcelColumn(Name = "创建时间", Width = 20, Format = "yyyy-MM-dd HH:mm:ss")]
        public DateTime CreationTime { get; set; }
    }
}
