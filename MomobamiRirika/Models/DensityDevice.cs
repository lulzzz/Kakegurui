using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ItsukiSumeragi.Models;
using Kakegurui.WebExtensions;

namespace MomobamiRirika.Models
{
    /// <summary>
    /// 设备
    /// </summary>
    public class DensityDevice:TrafficDevice
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
        /// 端口
        /// </summary>
        [Column("DataPort", TypeName = "INT")]
        [Required]
        [Range(1, 65525)]
        public int DataPort { get; set; }

        /// <summary>
        /// 所属节点地址
        /// </summary>
        [Column("NodeUrl", TypeName = "VARCHAR(100)")]
        public string NodeUrl { get; set; }

        /// <summary>
        /// 通道集合
        /// </summary>
        public List<DensityDevice_DensityChannel> DensityDevice_DensityChannels { get; set; }

        public override string ToString()
        {
            return $"设备编号：{DeviceId} 设备名称：{DeviceName} Ip:{Ip}";
        }
    }

    /// <summary>
    /// 设备添加
    /// </summary>
    public class DensityDeviceInsert
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
        /// 端口
        /// </summary>
        [Required]
        [Range(1, 65525)]
        public int DataPort { get; set; }


        /// <summary>
        /// 通道集合
        /// </summary>
        public List<DensityChannel> Channels { get; set; }
    }

    /// <summary>
    /// 设备更新
    /// </summary>
    public class DensityDeviceUpdate
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
        public List<DensityChannel> Channels { get; set; }
    }

    /// <summary>
    /// 设备标记更新
    /// </summary>
    public class DensityDeviceUpdateLocation
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
    public class DensityDeviceUpdateStatus
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
    }
}
