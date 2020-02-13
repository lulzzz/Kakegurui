using System;
using System.Text;

namespace Kakegurui.Core
{
    /// <summary>
    /// 字符串转换
    /// </summary>
    public class StringConvert
    {
        /// <summary>
        /// 时间数组转换为分割字符串
        /// </summary>
        /// <param name="times">时间数组</param>
        /// <returns>分割字符串</returns>
        public static string ToSplitString(DateTime[] times)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var time in times)
            {
                builder.Append(time.ToString("yyyy-MM-dd HH:mm:ss"));
                builder.Append(",");
            }
            return builder.ToString();
        }

        /// <summary>
        /// 字符串数组转换为分割字符串
        /// </summary>
        /// <param name="values">字符串数组</param>
        /// <returns>分割字符串</returns>
        public static string ToSplitString(string[] values)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var value in values)
            {
                builder.Append(value);
                builder.Append(",");
            }
            return builder.ToString();
        }
    }
}
