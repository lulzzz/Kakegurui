using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Kakegurui.WebExtensions
{
    /// <summary>
    /// 枚举数组验证
    /// </summary>
    public class EnumArrayAttribute : ValidationAttribute
    {
        /// <summary>
        /// 枚举类型
        /// </summary>
        private readonly Type _enumType;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="enumType">是否允许值空</param>
        public EnumArrayAttribute(Type enumType)
        {
            _enumType = enumType;
        }

        protected override ValidationResult IsValid(
            object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            IList keys = (IList) value;
            return keys.Cast<object>().Any(key => !Enum.IsDefined(_enumType, key)) 
                ? new ValidationResult("无效的枚举类型") 
                : ValidationResult.Success;
        }

    }
}
