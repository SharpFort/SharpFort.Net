using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using SharpFort.CodeGen.Domain.Shared.Enums;

namespace SharpFort.CodeGen.Domain.Handlers
{
    public class FieldTemplateHandler : TemplateHandlerBase, ITemplateHandler
    {
        public HandledTemplate Invoker(string str, string path)
        {
            var output = new HandledTemplate
            {
                TemplateStr = str.Replace("@field", BuildFields()),
                BuildPath = path
            };
            return output;
        }


        /// <summary>
        /// 生成Fields
        /// </summary>
        /// <returns></returns>
        public string BuildFields()
        {
            StringBuilder fieldStrs = new StringBuilder();


            foreach (var field in Table.Fields)
            {
                var typeStr = typeof(FieldType).GetFields().FirstOrDefault(x => x.Name == field.FieldType.ToString())?.GetCustomAttribute<DisplayAttribute>()?.Name;

                if (typeStr is null)
                {
                    continue;
                }
                var nameStr = field.Name;

                //添加备注
                if (!string.IsNullOrEmpty(field.Description))
                {
                    var desStr = "/// <summary>\n" +
                                $"///{field.Description}\n" +
                                 "/// </summary>\n";
                    fieldStrs.AppendLine(desStr);
                }

                //添加长度
                if (field.Length != 0)
                {
                    var lengthStr = $"[SugarColumn(Length ={field.Length})]";
                    fieldStrs.AppendLine(lengthStr);
                }

                //添加可空类型
                string nullStr = "";
                if (field.IsRequired == false)
                {
                    nullStr = "?";
                }

                //添加字段
                var fieldStr = $"public {typeStr}{nullStr} {nameStr} {{ get; set; }}";

                fieldStrs.AppendLine(fieldStr);
            }

            return fieldStrs.ToString();
        }
    }
}
