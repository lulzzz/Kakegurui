using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YumekoJabami.Models
{
    /// <summary>
    /// 角色
    /// </summary>
    public class Role
    {
        /// <summary>
        /// 角色名
        /// </summary>
        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Name { get; set; }

        /// <summary>
        /// 权限集合
        /// </summary>
        public List<Claim> Claims { get; set; }
    }
}
