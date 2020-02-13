using System.Collections.Generic;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Codes.Device;
using ItsukiSumeragi.Models;
using Microsoft.Extensions.Caching.Memory;
using NishinotouinYuriko.Models;
using YumekoJabami.Cache;
using ItsukiSumeragi.Codes.Flow;
using YumekoJabami.Codes;
using ItsukiSumeragi.Codes.Violation;

namespace NishinotouinYuriko.Cache
{
    /// <summary>
    /// 违法数据缓存
    /// </summary>
    public static class ViolationCache
    {
        /// <summary>
        /// 填充违法数据集合缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="violationStruct">违法数据</param>
        /// <returns>违法数据</returns>
        public static ViolationStruct FillViolation(this IMemoryCache memoryCache, ViolationStruct violationStruct)
        {
            if (violationStruct != null)
            {
                TrafficLocation location = memoryCache.GetLocation(violationStruct.LocationId);
                if (location != null)
                {
                    violationStruct.LocationName = location.LocationName;
                }
                TrafficViolation violation = memoryCache.GetViolation(violationStruct.ViolationId);
                if (violation != null)
                {
                    violationStruct.ViolationName = violation.ViolationName;
                }
                violationStruct.CarType_Desc = memoryCache.GetCode(SystemType.智慧交通违法检测系统, typeof(CarType), violationStruct.CarType);
                violationStruct.TargetType_Desc = memoryCache.GetCode(SystemType.智慧交通违法检测系统, typeof(TargetType), violationStruct.TargetType);
                violationStruct.Direction_Desc = memoryCache.GetCode(SystemType.系统管理中心, typeof(ChannelDirection), violationStruct.Direction);
            }

            return violationStruct;
        }

        /// <summary>
        /// 初始化设备缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="devices">设备集合</param>
        public static void InitViolationChannelCache(this IMemoryCache memoryCache, List<TrafficDevice> devices)
        {
            List<TrafficDevice> oldDevices = memoryCache.GetDevices();
            foreach (TrafficDevice device in oldDevices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    memoryCache.Remove(GetChannelKey(relation.ChannelId));
                }
            }

            foreach (TrafficDevice device in devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    if (!string.IsNullOrEmpty(relation.Channel.ChannelDeviceId))
                    {
                        memoryCache.Set(GetChannelKey(relation.Channel.ChannelDeviceId), relation.Channel);
                    }
                }
            }
        }


        /// <summary>
        /// 获取通道
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="channelDeviceId">通道设备编号</param>
        /// <returns>通道</returns>
        public static TrafficChannel GetViolationChannel(this IMemoryCache memoryCache, string channelDeviceId)
        {
            return memoryCache.TryGetValue(GetChannelKey(channelDeviceId), out TrafficChannel channel)
                ? channel
                : null;
        }

        /// <summary>
        /// 获取通道键
        /// </summary>
        /// <param name="channelDeviceId">通道设备编号</param>
        /// <returns>路段集合键</returns>
        private static string GetChannelKey(string channelDeviceId)
        {
            return $"channel_v_{channelDeviceId}";
        }
    }
}
