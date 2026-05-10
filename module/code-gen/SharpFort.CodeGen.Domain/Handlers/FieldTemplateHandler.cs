using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.CodeGen.Domain.Shared.Enums;

namespace SharpFort.CodeGen.Domain.Handlers
{
    public class FieldTemplateHandler : TemplateHandlerBase, ITemplateHandler
    {
        public HandledTemplate Invoker(string str, string path)
        {
            HandledTemplate output = new()
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
            StringBuilder fieldStrs = new();


            foreach (Field field in Table.Fields)
            {
                string? typeStr = typeof(FieldType).GetFields().FirstOrDefault(x => x.Name == field.FieldType.ToString())?.GetCustomAttribute<DisplayAttribute>()?.Name;

                if (typeStr is null)
                {
                    continue;
                }
                string nameStr = field.Name;

                //添加备注
                if (!string.IsNullOrEmpty(field.Description))
                {
                    string desStr = "/// <summary>\n" +
                                $"///{field.Description}\n" +
                                 "/// </summary>\n";
                    fieldStrs.AppendLine(desStr);
                }

                //添加长度
                if (field.Length != 0)
                {
                    string lengthStr = $"[SugarColumn(Length ={field.Length})]";
                    fieldStrs.AppendLine(lengthStr);
                }

                //添加可空类型
                string nullStr = "";
                if (field.IsRequired == false)
                {
                    nullStr = "?";
                }

                //添加字段
                string fieldStr = $"public {typeStr}{nullStr} {nameStr} {{ get; set; }}";

                fieldStrs.AppendLine(fieldStr);
            }

            return fieldStrs.ToString();
        }
    }
}
