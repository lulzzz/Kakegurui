using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using Newtonsoft.Json;

namespace ItsukiSumeragi.Models
{
    /// <summary>
    /// 地点
    /// </summary>
    public class TrafficLocation
    {
        /// <summary>
        /// 地点编号
        /// </summary>
        [Column("LocationId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LocationId { get; set; }

        /// <summary>
        /// 地点代码
        /// </summary>
        [Column("LocationCode")]
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string LocationCode{ get; set; }

        /// <summary>
        /// 地点名称
        /// </summary>
        [Column("LocationName", TypeName = "VARCHAR(100)")]
        [MySqlCharset("utf8")]
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string LocationName { get; set; }

        /// <summary>
        /// 通道
        /// </summary>
        [JsonIgnore]
        public List<TrafficChannel> Channels { get; set; }

        public override string ToString()
        {
            return $"地点代码：{LocationCode} 地点名称：{LocationName}";
        }
    }
}
