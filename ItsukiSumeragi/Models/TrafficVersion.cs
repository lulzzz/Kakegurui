using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ItsukiSumeragi.Models
{
    /// <summary>
    /// 系统版本
    /// </summary>
    public class TrafficVersion
    {
        /// <summary>
        /// 版本
        /// </summary>
        [Required]
        [Column("Version", TypeName = "VARCHAR(100)")]
        public string Version { get; set; }
    }
}
