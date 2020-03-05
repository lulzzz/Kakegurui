using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ItsukiSumeragi.Models;
using Kakegurui.WebExtensions;

namespace MomobamiKirari.Models
{
    /// <summary>
    /// 设备
    /// </summary>
    public class FlowDevice:TrafficDevice
    {
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
        public List<FlowDevice_FlowChannel> FlowDevice_FlowChannels { get; set; }

    }

    /// <summary>
    /// 设备添加
    /// </summary>
    public class FlowDeviceInsert
    {
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
        /// 通道集合
        /// </summary>
        public List<FlowChannel> Channels { get; set; }
    }

    /// <summary>
    /// 设备更新
    /// </summary>
    public class FlowDeviceUpdate
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
        /// 通道集合
        /// </summary>
        public List<FlowChannel> Channels { get; set; }
    }

    /// <summary>
    /// 设备标记更新
    /// </summary>
    public class FlowDeviceUpdateLocation
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
    public class FlowDeviceUpdateStatus
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
