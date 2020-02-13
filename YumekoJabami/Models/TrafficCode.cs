using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using YumekoJabami.Codes;

namespace YumekoJabami.Models
{
    /// <summary>
    /// 字典
    /// </summary>
    public class TrafficCode
    {
        /// <summary>
        /// 系统类型
        /// </summary>
        [Required]
        [Column("System", TypeName = "INT")]
        public SystemType System { get; set; }

        /// <summary>
        /// 键
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        [Column("Key", TypeName = "VARCHAR(100)")]
        public string Key { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        [Required]
        [Column("Value", TypeName = "INT")]
        public int Value { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [StringLength(100, ErrorMessage = "The {0} must be at max {1} characters long.")]
        [Column("Description", TypeName = "VARCHAR(100)")]
        [MySqlCharset("utf8")]
        public string Description { get; set; }
    }
}
