using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace ItsukiSumeragi.Models
{
    /// <summary>
    /// 设备通道关联
    /// </summary>
    public class TrafficDevice_TrafficChannel
    {
        /// <summary>
        /// 设备编号
        /// </summary>
        [Column("DeviceId", TypeName = "INT")]
        public int DeviceId { get; set; }

        /// <summary>
        /// 通道编号
        /// </summary>
        [Column("ChannelId", TypeName = "VARCHAR(100)")]
        public string ChannelId { get; set; }

        /// <summary>
        /// 设备
        /// </summary>
        [JsonIgnore]
        public TrafficDevice Device { get; set; }

        /// <summary>
        /// 通道
        /// </summary>
        public TrafficChannel Channel { get; set; }
    }
}
