using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySql.Data.EntityFrameworkCore.DataAnnotations;

namespace YumekoJabami.Models
{
    /// <summary>
    /// 权限
    /// </summary>
    public class TrafficClaim
    {
        /// <summary>
        /// 权限类型
        /// </summary>
        [Required]
        [Column("Type", TypeName = "VARCHAR(100)")]
        public string Type { get; set; }

        /// <summary>
        /// 权限值
        /// </summary>
        [Required]
        [Column("Value", TypeName = "VARCHAR(100)")]
        public string Value { get; set; }

        /// <summary>
        /// 权限描述
        /// </summary>
        [Column("Descirption", TypeName = "VARCHAR(100)")]
        [MySqlCharset("utf8")]
        public string Descirption { get; set; }
    }
}
