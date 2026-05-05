using System;
using System.Collections.Generic;
using System.Globalization;
using SharpFort.Tool.Domain.Shared.Enums;

#pragma warning disable CA1716

namespace SharpFort.Tool.Domain.Shared.Dtos
{
    public class TemplateGenCreateDto
    {
        public TemplateGenCreateDto()
        {
            ReplaceStrData = new Dictionary<string, string>();
        }

        public void SetTemplateGiteeRef(string moduleType)
        {
            this.GiteeRef = moduleType.ToLower(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 模块名称
        /// </summary>
        public string Name { get; set; } = null!;
        /// <summary>
        /// 模块所属gitee分支
        /// </summary>
        public string GiteeRef { get; set; } = null!;

        /// <summary>
        /// 数据库提供者
        /// </summary>
        public Dbms Dbms { get; set; }


        /// <summary>
        /// 需要替换的字符串内容
        /// </summary>
        public Dictionary<string, string> ReplaceStrData { get; set; }
    }
}
