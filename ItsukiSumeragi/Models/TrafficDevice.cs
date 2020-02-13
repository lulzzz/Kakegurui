using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kakegurui.WebExtensions;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using ItsukiSumeragi.Codes.Device;

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
        /// 设备类型
        /// </summary>
        [Column("DeviceType", TypeName = "INT")]
        [Required]
        [EnumDataType(typeof(DeviceType))]
        public DeviceType DeviceType { get; set; }

        /// <summary>
        /// 设备类型描述
        /// </summary>
        [NotMapped]
        public string DeviceType_Desc => DeviceType.ToString();

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
        /// 设备型号
        /// </summary>
        [Column("DeviceModel", TypeName = "INT")]
        [Required]
        public int DeviceModel { get; set; }

        /// <summary>
        /// 设备型号描述
        /// </summary>
        [NotMapped]
        public string DeviceModel_Desc { get; set; }

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
        /// 端口
        /// </summary>
        [Column("DataPort", TypeName = "INT")]
        [Required]
        [Range(1, 65525)]
        public int DataPort { get; set; }

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
        /// 授权状态
        /// </summary>
        [Column("License", TypeName = "VARCHAR(100)")]
        public string License { get; set; }

        /// <summary>
        /// Cpu
        /// </summary>
        [Column("Cpu", TypeName = "VARCHAR(100)")]
        public string Cpu { get; set; }

        /// <summary>
        /// 内存
        /// </summary>
        [Column("Memory", TypeName = "VARCHAR(100)")]
        public string Memory { get; set; }

        /// <summary>
        /// 硬盘空间
        /// </summary>
        [Column("Space", TypeName = "VARCHAR(100)")]
        public string Space { get; set; }

        /// <summary>
        /// 系统时间
        /// </summary>
        [Column("Systime", TypeName = "VARCHAR(100)")]
        public string Systime { get; set; }

        /// <summary>
        /// 运行时间
        /// </summary>
        [Column("Runtime", TypeName = "VARCHAR(100)")]
        public string Runtime { get; set; }

        /// <summary>
        /// 所属节点地址
        /// </summary>
        [Column("NodeUrl", TypeName = "VARCHAR(100)")]
        public string NodeUrl { get; set; }

        /// <summary>
        /// 通道集合
        /// </summary>
        public List<TrafficDevice_TrafficChannel> Device_Channels { get; set; }

        public override string ToString()
        {
            return $"设备编号：{DeviceId} 设备名称：{DeviceName} Ip:{Ip}";
        }
    }

    /// <summary>
    /// 设备添加
    /// </summary>
    public class TrafficDeviceInsert
    {
        /// <summary>
        /// 设备名称
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string DeviceName { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        [Required]
        [EnumDataType(typeof(DeviceType))]
        public DeviceType DeviceType { get; set; }

        /// <summary>
        /// 设备型号
        /// </summary>
        [Required]
        public int DeviceModel { get; set; }

        /// <summary>
        /// ip
        /// </summary>
        [Required]
        [IPAddress]
        public string Ip { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        [Required]
        [Range(1, 65525)]
        public int Port { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        [Required]
        [Range(1, 65525)]
        public int DataPort { get; set; }


        /// <summary>
        /// 通道集合
        /// </summary>
        public List<TrafficChannel> Channels { get; set; }
    }

    /// <summary>
    /// 设备更新
    /// </summary>
    public class TrafficDeviceUpdate
    {
        /// <summary>
        /// 设备编码
        /// </summary>
        [Required]
        public int DeviceId { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string DeviceName { get; set; }

        /// <summary>
        /// 设备型号
        /// </summary>
        [Required]
        public int DeviceModel { get; set; }

        /// <summary>
        /// ip
        /// </summary>
        [Required]
        [IPAddress]
        public string Ip { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        [Required]
        [Range(1, 65525)]
        public int Port { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        [Required]
        [Range(1, 65525)]
        public int DataPort { get; set; }


        /// <summary>
        /// 通道集合
        /// </summary>
        public List<TrafficChannel> Channels { get; set; }
    }

    /// <summary>
    /// 设备标记更新
    /// </summary>
    public class TrafficDeviceUpdateLocation
    {
        /// <summary>
        /// 设备编码
        /// </summary>
        [Required]
        public int DeviceId { get; set; }

        /// <summary>
        /// 标记点的坐标
        /// </summary>
        [Required]
        public string Location { get; set; }
    }

    /// <summary>
    /// 设备标记更新
    /// </summary>
    public class TrafficDeviceUpdateStatus
    {
        /// <summary>
        /// 设备编码
        /// </summary>
        [Required]
        public int DeviceId { get; set; }

        /// <summary>
        /// 设备状态
        /// </summary>
        [Required]
        public int DeviceStatus { get; set; }

        /// <summary>
        /// Cpu
        /// </summary>
        public string Cpu { get; set; }

        /// <summary>
        /// 内存
        /// </summary>
        public string Memory { get; set; }

        /// <summary>
        /// 硬盘空间
        /// </summary>
        public string Space { get; set; }

        /// <summary>
        /// 授权状态
        /// </summary>
        public string License { get; set; }

        /// <summary>
        /// 系统时间
        /// </summary>
        public string Systime { get; set; }

        /// <summary>
        /// 运行时间
        /// </summary>
        public string Runtime { get; set; }
    }
}
