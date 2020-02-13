using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Kakegurui.WebExtensions
{
    /// <summary>
    /// ip格式验证
    /// </summary>
    public class IPAddressAttribute : ValidationAttribute
    {
        /// <summary>
        /// 是否允许值空
        /// </summary>
        private readonly bool _allowNull;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="allowNull">是否允许值空</param>
        public IPAddressAttribute(bool allowNull = false)
        {
            _allowNull = allowNull;
        }

        protected override ValidationResult IsValid(
            object value, ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(value?.ToString()))
            {
                return _allowNull ? ValidationResult.Success : new ValidationResult("ip格式不正确");
            }
            return IPAddress.TryParse(value.ToString(), out _) ? ValidationResult.Success : new ValidationResult("ip格式不正确");
        }

    }
}
