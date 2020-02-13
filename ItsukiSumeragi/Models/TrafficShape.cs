using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace ItsukiSumeragi.Models
{
    /// <summary>
    /// 违法图形
    /// </summary>
    public class TrafficShape
    {
        /// <summary>
        /// 通道编号
        /// </summary>
        [Column("ChannelId", TypeName = "VARCHAR(100)")]
        public string ChannelId { get; set; }

        /// <summary>
        /// 标签名称
        /// </summary>
        [Column("TagName", TypeName = "VARCHAR(100)")]
        public string TagName { get; set; }

        /// <summary>
        /// 图形序号
        /// </summary>
        [Column("ShapeIndex", TypeName = "INT")]
        public int ShapeIndex { get; set; }

        /// <summary>
        /// 绘制区域
        /// </summary>
        [Column("Region", TypeName = "TEXT")]
        [Required]
        public string Region { get; set; }

        /// <summary>
        /// 图形颜色
        /// </summary>
        [NotMapped]
        public string Color { get; set; }

        /// <summary>
        /// 通道
        /// </summary>
        [JsonIgnore]
        public TrafficChannel Channel { get; set; }
    }
}
