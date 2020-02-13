using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using Newtonsoft.Json;
using ItsukiSumeragi.Codes.Device;

namespace ItsukiSumeragi.Models
{
    /// <summary>
    /// 通道
    /// </summary>
    public class TrafficChannel
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
        /// 通道设备编号
        /// </summary>
        [Column("ChannelDeviceId", TypeName = "VARCHAR(100)")]
        public string ChannelDeviceId { get; set; }

        /// <summary>
        /// 通道设备类型
        /// </summary>
        [Column("ChannelDeviceType", TypeName = "INT")]
        public int? ChannelDeviceType { get; set; }

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
        /// 帧率
        /// </summary>
        [Column("FrameRate", TypeName = "INT")]
        public int? FrameRate { get; set; }

        /// <summary>
        /// 限速值
        /// </summary>
        [Column("SpeedLimit", TypeName = "INT")]
        public int? SpeedLimit { get; set; }

        /// <summary>
        /// 备案号
        /// </summary>
        [Column("RecordNumber", TypeName = "VARCHAR(100)")]
        public string RecordNumber { get; set; }

        /// <summary>
        /// 视频朝向
        /// </summary>
        [Column("Direction", TypeName = "INT")]
        public int? Direction { get; set; }

        /// <summary>
        /// 视频朝向描述
        /// </summary>
        [NotMapped]
        public string Direction_Desc { get; set; }

        /// <summary>
        /// 关联路口
        /// </summary>
        [Column("CrossingId", TypeName = "INT")]
        public int? CrossingId { get; set; }

        /// <summary>
        /// 关联路段
        /// </summary>
        [Column("SectionId", TypeName = "INT")]
        public int? SectionId { get; set; }

        /// <summary>
        /// 关联地点
        /// </summary>
        [Column("LocationId", TypeName = "INT")]
        public int? LocationId { get; set; }

        /// <summary>
        /// 关联路口
        /// </summary>
        public TrafficRoadCrossing RoadCrossing { get; set; }

        /// <summary>
        /// 关联路段
        /// </summary>
        public TrafficRoadSection RoadSection { get; set; }

        /// <summary>
        /// 关联地点
        /// </summary>
        public TrafficLocation TrafficLocation { get; set; }

        /// <summary>
        /// 车道集合
        /// </summary>
        public List<TrafficLane> Lanes { get; set; }

        /// <summary>
        /// 区域集合
        /// </summary>
        public List<TrafficRegion> Regions { get; set; }

        /// <summary>
        /// 图形集合
        /// </summary>
        public List<TrafficShape> Shapes { get; set; }

        /// <summary>
        /// 关联设备
        /// </summary>
        [JsonIgnore]
        public TrafficDevice_TrafficChannel Device_Channel { get; set; }

        /// <summary>
        /// 违法行为集合
        /// </summary>
        public List<TrafficChannel_TrafficViolation> Channel_Violations { get; set; }

        /// <summary>
        /// 违法参数集合
        /// </summary>
        public List<TrafficChannel_TrafficViolationParameter> Channel_ViolationParameters { get; set; }

        /// <summary>
        /// ip
        /// </summary>
        [NotMapped]
        public string Ip => Device_Channel?.Device?.Ip;

        /// <summary>
        /// 端口
        /// </summary>
        [NotMapped]
        public int Port => Device_Channel?.Device?.Port ?? 0;

        /// <summary>
        /// 设备名称
        /// </summary>
        [NotMapped]
        public string DeviceName => Device_Channel?.Device?.DeviceName;

        public override string ToString()
        {
            return $"通道编号：{ChannelId} 通道名称：{ChannelName}";
        }
    }

    /// <summary>
    /// 通道坐标更新
    /// </summary>
    public class TrafficChannelUpdateLocation
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
    public class TrafficChannelUpdateStatus
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

    /// <summary>
    /// 设备违法参数更新
    /// </summary>
    public class TrafficChannelUpdateParameter
    {
        /// <summary>
        /// 设备编码
        /// </summary>
        [Required]
        public string ChannelId { get; set; }

        /// <summary>
        /// 通道参数关联集合
        /// </summary>
        public List<TrafficChannel_TrafficViolationParameter> Channel_ViolationParameters { get; set; }

    }
}
