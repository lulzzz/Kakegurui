using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kakegurui.WebExtensions;
using MySql.Data.EntityFrameworkCore.DataAnnotations;

namespace ItsukiSumeragi.Models
{
    /// <summary>
    /// 设备
    /// </summary>
    public class TrafficDevice
    {
        /// <summary>
        /// 设备编码
        /// </summary>
        [Column("DeviceId", TypeName = "INT")]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DeviceId { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        [Column("DeviceName", TypeName = "VARCHAR(100)")]
        [Required]
        [MySqlCharset("utf8")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string DeviceName { get; set; }

        /// <summary>
        /// 设备状态
        /// </summary>
        [Column("DeviceStatus", TypeName = "INT")]
        [Required]
        public int DeviceStatus { get; set; }

        /// <summary>
        /// 设备状态描述
        /// </summary>
        [NotMapped]
        public string DeviceStatus_Desc { get; set; }

        /// <summary>
        /// ip
        /// </summary>
        [Column("Ip", TypeName = "VARCHAR(15)")]
        [Required]
        [IPAddress]
        public string Ip { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        [Column("Port", TypeName = "INT")]
        [Required]
        [Range(1,65525)]
        public int Port { get; set; }

        /// <summary>
        /// 是否已标注
        /// </summary>
        [Column("Marked", TypeName = "TINYINT")]
        [Required]
        public bool Marked { get; set; }
        
        /// <summary>
        /// 点的位置
        /// </summary>
        [Column("Location", TypeName = "VARCHAR(100)")]
        public string Location { get; set; }

        public override string ToString()
        {
            return $"设备编号：{DeviceId} 设备名称：{DeviceName} Ip:{Ip}";
        }
    }
}
