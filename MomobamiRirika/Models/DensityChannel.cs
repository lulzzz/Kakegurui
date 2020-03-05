using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MomobamiRirika.Codes;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using Newtonsoft.Json;

namespace MomobamiRirika.Models
{
    /// <summary>
    /// 通道
    /// </summary>
    public class DensityChannel
    {
        /// <summary>
        /// 通道地址
        /// </summary>
        [Column("ChannelId", TypeName = "VARCHAR(100)")]
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string ChannelId { get; set; }

        /// <summary>
        /// 通道序号
        /// </summary>
        [Column("ChannelIndex", TypeName = "INT")]
        public int ChannelIndex { get; set; }

        /// <summary>
        /// 通道名称
        /// </summary>
        [Column("ChannelName", TypeName = "VARCHAR(100)")]
        [MySqlCharset("utf8")]
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string ChannelName { get; set; }

        /// <summary>
        /// 通道状态
        /// </summary>
        [Column("ChannelStatus", TypeName = "INT")]
        public int ChannelStatus { get; set; }

        /// <summary>
        /// 通道状态描述
        /// </summary>
        [NotMapped]
        public string ChannelStatus_Desc { get; set; }

        /// <summary>
        /// 通道类型
        /// </summary>
        [Column("ChannelType", TypeName = "INT")]
        [Required]
        public int ChannelType { get; set; }

        /// <summary>
        /// 通道类型描述
        /// </summary>
        [NotMapped]
        public string ChannelType_Desc { get; set; }

        /// <summary>
        /// 通道设备类型描述
        /// </summary>
        [NotMapped]
        public string ChannelDeviceType_Desc { get; set; }

        /// <summary>
        /// rtsp用户名
        /// </summary>
        [Column("RtspUser", TypeName = "VARCHAR(100)")]
        [StringLength(100, ErrorMessage = "The {0} must be at max {1} characters long.")]
        public string RtspUser { get; set; }

        /// <summary>
        /// rtsp密码
        /// </summary>
        [Column("RtspPassword", TypeName = "VARCHAR(100)")]
        [StringLength(100, ErrorMessage = "The {0} must be at max {1} characters long.")]
        public string RtspPassword { get; set; }

        /// <summary>
        /// rtsp协议类型
        /// </summary>
        [Column("RtspProtocol", TypeName = "INT")]
        [EnumDataType(typeof(RtspProtocol))]
        public int? RtspProtocol { get; set; }

        /// <summary>
        /// rtsp协议类型描述
        /// </summary>
        [NotMapped]
        public string RtspProtocol_Desc { get; set; }

        /// <summary>
        /// 是否循环播放
        /// </summary>
        [Required]
        [Column("IsLoop", TypeName = "TINYINT")]
        public bool IsLoop { get; set; }

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

        /// <summary>
        /// 关联路口
        /// </summary>
        [Column("CrossingId", TypeName = "INT")]
        public int? CrossingId { get; set; }

        /// <summary>
        /// 关联路口
        /// </summary>
        public RoadCrossing RoadCrossing { get; set; }

        /// <summary>
        /// 区域集合
        /// </summary>
        public List<TrafficRegion> Regions { get; set; }

        /// <summary>
        /// 关联设备
        /// </summary>
        [JsonIgnore]
        public DensityDevice_DensityChannel DensityDevice_DensityChannel { get; set; }

        /// <summary>
        /// ip
        /// </summary>
        [NotMapped]
        public string Ip => DensityDevice_DensityChannel?.Device?.Ip;

        /// <summary>
        /// 端口
        /// </summary>
        [NotMapped]
        public int Port => DensityDevice_DensityChannel?.Device?.Port ?? 0;

        /// <summary>
        /// 设备名称
        /// </summary>
        [NotMapped]
        public string DeviceName => DensityDevice_DensityChannel?.Device?.DeviceName;

        public override string ToString()
        {
            return $"通道编号：{ChannelId} 通道名称：{ChannelName}";
        }
    }

    /// <summary>
    /// 通道坐标更新
    /// </summary>
    public class DensityChannelUpdateLocation
    {
        /// <summary>
        /// 通道编号
        /// </summary>
        [Required]
        public string ChannelId { get; set; }

        /// <summary>
        /// 标记点的坐标
        /// </summary>
        [Required]
        public string Location { get; set; }
    }

    /// <summary>
    /// 通道状态更新
    /// </summary>
    public class DensityChannelUpdateStatus
    {
        /// <summary>
        /// 设备编码
        /// </summary>
        [Required]
        public string ChannelId { get; set; }

        /// <summary>
        /// 标记点的坐标
        /// </summary>
        [Required]
        public int ChannelStatus { get; set; }
    }
}
