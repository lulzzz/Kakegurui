using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace MomobamiRirika.Models
{
    /// <summary>
    /// 设备通道关联
    /// </summary>
    public class DensityDevice_DensityChannel
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
        public DensityDevice Device { get; set; }

        /// <summary>
        /// 通道
        /// </summary>
        public DensityChannel Channel { get; set; }
    }
}
