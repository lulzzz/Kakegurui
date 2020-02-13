using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using YumekoJabami.Codes;

namespace YumekoJabami.Models
{
    /// <summary>
    /// 参数
    /// </summary>
    public class TrafficParameter
    {
        /// <summary>
        /// 参数类型
        /// </summary>
        [Column("Type", TypeName = "VARCHAR(100)")]
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Type { get; set; }

        /// <summary>
        /// 键
        /// </summary>
        [Column("Key", TypeName = "VARCHAR(100)")]
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Key { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        [Column("Value", TypeName = "VARCHAR(100)")]
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Value { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [Column("Description", TypeName = "VARCHAR(100)")]
        [MySqlCharset("utf8")]
        [StringLength(100, ErrorMessage = "The {0} must be at max {1} characters long.")]
        public string Description { get; set; }
    }

    /// <summary>
    /// 参数类型
    /// </summary>
    public class TrafficParameterType
    {
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Type { get; set; }
    }

}
