using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using Newtonsoft.Json;

namespace ItsukiSumeragi.Models
{
    /// <summary>
    /// 区域
    /// </summary>
    public class TrafficRegion:TrafficItem
    {
        /// <summary>
        /// 通道编号
        /// </summary>
        [Column("ChannelId", TypeName = "VARCHAR(100)")]
        public string ChannelId { get; set; }

        /// <summary>
        /// 区域序号
        /// </summary>
        [Required]
        [Column("RegionIndex", TypeName = "INT")]
        public int RegionIndex { get; set; }

        /// <summary>
        /// 区域名称
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        [Column("RegionName", TypeName = "VARCHAR(100)")]
        [MySqlCharset("utf8")]
        public string RegionName { get; set; }

        /// <summary>
        /// 绘制区域
        /// </summary>
        [Required]
        [Column("Region", TypeName = "TEXT")]
        public string Region { get; set; }

        /// <summary>
        /// 是否是重点区域
        /// </summary>
        [Required]
        [Column("IsVip", TypeName = "TINYINT")]
        public bool IsVip { get; set; }

        /// <summary>
        /// 饱和值
        /// </summary>
        [Required]
        [Range(1, 10000)]
        [Column("Saturation", TypeName = "INT")]
        public int Saturation { get; set; }

        /// <summary>
        /// 警戒值
        /// </summary>
        [Required]
        [Range(1, 10000)]
        [Column("Warning", TypeName = "INT")]
        public int Warning { get; set; }

        /// <summary>
        /// 报警时间限制
        /// </summary>
        [Required]
        [Range(1, 10000)]
        [Column("WarningDuration", TypeName = "INT")]
        public int WarningDuration { get; set; }

        /// <summary>
        /// 密集度
        /// </summary>
        [Required]
        [Range(1, 10000)]
        [Column("Density", TypeName = "INT")]
        public int Density { get; set; }

        /// <summary>
        /// 密集度范围
        /// </summary>
        [Required]
        [Range(1, 10000)]
        [Column("DensityRange", TypeName = "INT")]
        public int DensityRange { get; set; }

        /// <summary>
        /// 车辆数
        /// </summary>
        [Required]
        [Range(1, 10000)]
        [Column("CarCount", TypeName = "INT")]
        public int CarCount { get; set; }

        /// <summary>
        /// 连续次数
        /// </summary>
        [Required]
        [Range(1, 10000)]
        [Column("Frequency", TypeName = "INT")]
        public int Frequency { get; set; }

        /// <summary>
        /// 数据编号
        /// </summary>
        [NotMapped]
        public override string DataId => $"{ChannelId}_{RegionIndex}";

        /// <summary>
        /// 数据匹配编号
        /// </summary>
        [NotMapped]
        public string MatchId => Channel?.Device_Channel == null ? null : $"{Channel.Device_Channel.Device.Ip}_{Channel.ChannelIndex}_{RegionIndex}";

        [JsonIgnore]
        public TrafficChannel Channel { get; set; }
    }
}
