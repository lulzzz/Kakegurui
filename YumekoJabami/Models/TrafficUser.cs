using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YumekoJabami.Models
{
    /// <summary>
    /// 注册用户
    /// </summary>
    public class UserPassword
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
        public string Password { get; set; }

        /// <summary>
        /// 确认密码
        /// </summary>
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

    }

    /// <summary>
    /// 用户角色
    /// </summary>
    public class UserRole
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string UserName { get; set; }

        /// <summary>
        /// 角色集合
        /// </summary>
        public List<TrafficRole> Roles { get; set; }
    }

}
