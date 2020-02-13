using System.ComponentModel.DataAnnotations;

namespace YumekoJabami.Models
{
    /// <summary>
    /// 登陆
    /// </summary>
    public class Login
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required]
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    public class ChangePassword
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string UserName { get; set; }

        /// <summary>
        /// 旧密码
        /// </summary>
        [Required]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; }

        /// <summary>
        /// 新密码
        /// </summary>
        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        /// <summary>
        /// 新密码确认
        /// </summary>
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
    
}
