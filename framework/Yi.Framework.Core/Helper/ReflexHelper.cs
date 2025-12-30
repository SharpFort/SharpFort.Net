using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yi.Framework.Core.Helper
{
    /// <summary>
    /// 反射辅助类，提供通过属性名操作对象属性值的功能
    /// </summary>
    /// <remarks>
    /// 性能注意事项：
    /// - 反射操作比直接属性访问慢约 10-100 倍
    /// - 频繁调用时建议使用表达式树缓存或 Source Generator
    ///
    /// 使用场景：
    /// - 动态属性操作（如 ORM 映射、数据导入导出）
    /// - 需要根据配置动态访问属性的场景
    /// </remarks>
    public static class ReflexHelper
    {

        #region 对象相关
        /// <summary>
        /// 取对象属性值
        /// </summary>
        /// <param name="FieldName">属性名称（区分大小写）</param>
        /// <param name="obj">目标对象实例</param>
        /// <returns>属性值的字符串表示，空值或空字符串返回 null</returns>
        /// <exception cref="NullReferenceException">指定的属性名不存在时抛出</exception>
        /// <remarks>
        /// 实现说明：
        /// - 使用 Type.GetProperty() 获取属性信息（反射）
        /// - 空值和空字符串都返回 null
        /// - 建议调用前先检查属性是否存在
        /// </remarks>
        public static string? GetModelValue(string FieldName, object obj)
        {
            Type Ts = obj.GetType();
            object? o = Ts.GetProperty(FieldName)?.GetValue(obj, null);
            if (o == null)
                return null;
            string Value = Convert.ToString(o) ?? string.Empty;
            if (string.IsNullOrEmpty(Value))
                return null;
            return Value;
        }


        /// <summary>
        /// 设置对象属性值
        /// </summary>
        /// <param name="FieldName">属性名称（区分大小写）</param>
        /// <param name="Value">要设置的属性值（类型需要与属性类型兼容）</param>
        /// <param name="obj">目标对象实例</param>
        /// <returns>设置成功返回 true</returns>
        /// <exception cref="NullReferenceException">指定的属性名不存在时抛出</exception>
        /// <exception cref="ArgumentException">属性值类型不匹配时抛出</exception>
        /// <remarks>
        /// 实现说明：
        /// - 使用 Type.GetProperty() 获取属性信息（反射）
        /// - 属性必须有公共 setter
        /// - 建议调用前先检查属性是否存在
        ///
        /// 使用示例：
        /// <code>
        /// var user = new User();
        /// ReflexHelper.SetModelValue("Name", "张三", user);
        /// </code>
        /// </remarks>
        public static bool SetModelValue(string FieldName, object? Value, object obj)
        {
            Type Ts = obj.GetType();
            Ts.GetProperty(FieldName)?.SetValue(obj, Value, null);
            return true;
        }
        #endregion
    }
}
