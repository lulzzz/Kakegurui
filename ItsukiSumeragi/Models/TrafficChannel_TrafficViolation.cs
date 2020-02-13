using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace ItsukiSumeragi.Models
{
    /// <summary>
    /// 通道违法行为关联
    /// </summary>
    public class TrafficChannel_TrafficViolation
    {
        /// <summary>
        /// 通道编号
        /// </summary>
        [Column("ChannelId", TypeName = "VARCHAR(100)")]
        public string ChannelId { get; set; }

        /// <summary>
        /// 违法编号
        /// </summary>
        [Column("ViolationId", TypeName = "INT")]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ViolationId { get; set; }

        /// <summary>
        /// 通道
        /// </summary>
        [JsonIgnore]
        public TrafficChannel Channel { get; set; }

        /// <summary>
        /// 违法行为
        /// </summary>
        public TrafficViolation Violation { get; set; }
    }
}
