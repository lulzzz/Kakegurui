using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ItsukiSumeragi.Models;
using Kakegurui.WebExtensions;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using Newtonsoft.Json;

namespace MomobamiKirari.Models
{
    /// <summary>
    /// 车道
    /// </summary>
    public class Lane:TrafficItem
    {
        /// <summary>
        /// 通道编号
        /// </summary>
        [Column("ChannelId", TypeName = "VARCHAR(100)")]
        public string ChannelId { get; set; }

        /// <summary>
        /// 车道编号
        /// </summary>
        [Required]
        [Column("LaneId", TypeName = "VARCHAR(100)")]
        public string LaneId { get; set; }

        /// <summary>
        /// 车道序号
        /// </summary>
        [Required]
        [Column("LaneIndex", TypeName = "INT")]
        public int LaneIndex { get; set; }

        /// <summary>
        /// 车道名称
        /// </summary>
        [Column("LaneName", TypeName = "VARCHAR(100)")]
        [MySqlCharset("utf8")]
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string LaneName{ get; set; }

        /// <summary>
        /// 绘制区域
        /// </summary>
        [Column("Region", TypeName = "TEXT")]
        [Required]
        public string Region { get; set; }

        /// <summary>
        /// 车道方向
        /// </summary>
        [Column("Direction", TypeName = "INT")]
        [Required]
        public int Direction { get; set; }

        /// <summary>
        /// 车道方向描述
        /// </summary>
        [NotMapped]
        public string Direction_Desc { get; set; }

        /// <summary>
        /// 车道流向
        /// </summary>
        [Column("FlowDirection", TypeName = "INT")]
        [Required]
        public int FlowDirection { get; set; }

        /// <summary>
        /// 车道流向描述
        /// </summary>
        [NotMapped]
        public string FlowDirection_Desc { get; set; }

        /// <summary>
        /// 绘制区域
        /// </summary>
        [Column("Length", TypeName = "INT")]
        [Required]
        [Range(1,1000)]
        public int Length { get; set; }

        /// <summary>
        /// IO地址
        /// </summary>
        [Column("IOIp", TypeName = "VARCHAR(15)")]
        [IPAddress(true)]
        public string IOIp { get; set; }

        /// <summary>
        /// IO端口
        /// </summary>
        [Column("IOPort", TypeName = "INT")]
        [Range(0, 65525)]
        public int? IOPort { get; set; }

        /// <summary>
        /// IO序号
        /// </summary>
        [Column("IOIndex", TypeName = "INT")]
        public int? IOIndex { get; set; }

        /// <summary>
        /// 车道性质
        /// </summary>
        [Column("LaneType", TypeName = "INT")]
        public int? LaneType { get; set; }

        /// <summary>
        /// 车道性质描述
        /// </summary>
        [NotMapped]
        public string LaneType_Desc { get; set; }

        /// <summary>
        /// 数据编号
        /// </summary>
        [NotMapped]
        public override string DataId => $"{ChannelId}_{LaneId}";

        [JsonIgnore]
        public FlowChannel Channel { get; set; }
    }
}
