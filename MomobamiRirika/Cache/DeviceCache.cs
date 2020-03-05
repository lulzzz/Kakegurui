using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Codes;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Caching.Memory;
using MomobamiRirika.Codes;
using MomobamiRirika.Models;

namespace MomobamiRirika.Cache
{
    /// <summary>
    /// 流量缓存
    /// </summary>
    public static class DeviceCache
    {
        /// <summary>
        /// 初始化设备缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="devices">设备集合</param>
        public static void InitDeviceCache(this IMemoryCache memoryCache,List<DensityDevice> devices)
        {
            List<DensityDevice> oldDevices = memoryCache.GetDevices();
            foreach (DensityDevice device in oldDevices)
            {
                memoryCache.Remove(GetDeviceKey(device.DeviceId));
                foreach (var relation in device.DensityDevice_DensityChannels)
                {
                    memoryCache.Remove(GetChannelKey(relation.ChannelId));

                    if(relation.Channel.Regions!=null)
                    {
                        foreach (TrafficRegion region in relation.Channel.Regions)
                        {
                            memoryCache.Remove(GetRegionKey(region.DataId));
                        }
                    }
                    
                }
            }

            foreach (DensityDevice device in devices)
            {
                memoryCache.Set(GetDeviceKey(device.DeviceId), device);
                foreach (var relation in device.DensityDevice_DensityChannels)
                {
                    relation.Device = device;
                    relation.Channel.DensityDevice_DensityChannel = relation;
                    memoryCache.Set(GetChannelKey(relation.Channel.ChannelId), relation.Channel);

                    if (relation.Channel.Regions != null)
                    {
                        foreach (TrafficRegion region in relation.Channel.Regions)
                        {
                            region.Channel = relation.Channel;
                            memoryCache.Set(GetRegionKey(region.DataId), region);
                        }
                    }
                }
            }

            memoryCache.Set(GetDevicesKey(),devices);
            memoryCache.Set(GetChannelsKey(), devices.SelectMany(d => d.DensityDevice_DensityChannels)
                .Select(r => r.Channel)
                .Distinct((c1, c2) => c1.ChannelId == c2.ChannelId)
                .ToList());
            memoryCache.Set(GetRegionsKey(),
                devices.SelectMany(d => d.DensityDevice_DensityChannels)
                    .Select(r => r.Channel)
                    .Where(c => c.Regions != null)
                    .SelectMany(c => c.Regions)
                    .Distinct((r1, r2) => r1.DataId == r2.DataId)
                    .ToList());
        }

        /// <summary>
        /// 填充设备缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="device">设备</param>
        /// <returns>设备</returns>
        public static DensityDevice FillDevice(this IMemoryCache memoryCache, DensityDevice device)
        {
            if (device != null)
            {
                device.DeviceStatus_Desc = memoryCache.GetCode(typeof(DeviceStatus), device.DeviceStatus);
                device.DeviceModel_Desc = memoryCache.GetCode(typeof(DeviceModel), device.DeviceModel);
            }
            return device;
        }

        /// <summary>
        /// 获取设备
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="deviceId">设备编号</param>
        /// <returns>设备</returns>
        public static DensityDevice GetDevice(this IMemoryCache memoryCache, int deviceId)
        {
            return memoryCache.TryGetValue(GetDeviceKey(deviceId), out DensityDevice device)
                ? device
                : null;
        }

        /// <summary>
        /// 获取设备键
        /// </summary>
        /// <param name="deviceId">设备编号</param>
        /// <returns>设备键</returns>
        private static string GetDeviceKey(int deviceId)
        {
            return $"device_{deviceId}";
        }

        /// <summary>
        /// 获取设备集合
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <returns>设备集合</returns>
        public static List<DensityDevice> GetDevices(this IMemoryCache memoryCache)
        {
            return memoryCache.TryGetValue(GetDevicesKey(), out List<DensityDevice> devices)
                ? devices
                : new List<DensityDevice>();
        }

        /// <summary>
        /// 获取车道集合键
        /// </summary>
        /// <returns>车道集合键</returns>
        private static string GetDevicesKey()
        {
            return "devices";
        }

        /// <summary>
        /// 填充通道缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="channel">通道</param>
        /// <returns>通道</returns>
        public static DensityChannel FillChannel(this IMemoryCache memoryCache, DensityChannel channel)
        {
            if (channel != null)
            {
                channel.ChannelStatus_Desc = memoryCache.GetCode(typeof(DeviceStatus), channel.ChannelStatus);
                channel.ChannelType_Desc = memoryCache.GetCode(typeof(ChannelType), channel.ChannelType);

                if (channel.RtspProtocol.HasValue)
                {
                    channel.RtspProtocol_Desc = memoryCache.GetCode(typeof(RtspProtocol), channel.RtspProtocol.Value);
                }

            }

            return channel;
        }

        /// <summary>
        /// 获取通道
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="channelId">通道编号</param>
        /// <returns>通道</returns>
        public static DensityChannel GetChannel(this IMemoryCache memoryCache, string channelId)
        {
            return memoryCache.TryGetValue(GetChannelKey(channelId), out DensityChannel channel)
                ? channel
                : null;
        }

        /// <summary>
        /// 获取通道键
        /// </summary>
        /// <param name="channelId">通道编号</param>
        /// <returns>路段集合键</returns>
        private static string GetChannelKey(string channelId)
        {
            return $"channel_{channelId}";
        }

        /// <summary>
        /// 获取通道集合
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <returns>通道集合</returns>
        public static List<DensityChannel> GetChannels(this IMemoryCache memoryCache)
        {
            return memoryCache.TryGetValue(GetChannelsKey(), out List<DensityChannel> channels)
                ? channels
                : new List<DensityChannel>();
        }

        /// <summary>
        /// 获取通道集合键
        /// </summary>
        /// <returns>通道集合键</returns>
        private static string GetChannelsKey()
        {
            return "channels";
        }

        /// <summary>
        /// 初始化路口缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="crossings">路口集合</param>
        public static void InitCrossingCache(this IMemoryCache memoryCache, List<RoadCrossing> crossings)
        {
            List<RoadCrossing> oldCrossings = memoryCache.GetCrossings();
            foreach (RoadCrossing oldCrossing in oldCrossings)
            {
                memoryCache.Remove(GetCrossingKey(oldCrossing.CrossingId));
            }

            memoryCache.Set(GetCrossingsKey(), crossings);
            foreach (RoadCrossing crossing in crossings)
            {
                memoryCache.Set(GetCrossingKey(crossing.CrossingId), crossing);
            }
        }

        /// <summary>
        /// 获取路口
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="crossingId">路段编号</param>
        /// <param name="defaultCrossing">默认返回结果</param>
        /// <returns>路段</returns>
        public static RoadCrossing GetCrossing(this IMemoryCache memoryCache, int? crossingId,RoadCrossing defaultCrossing)
        {
            if (crossingId.HasValue)
            {
                return memoryCache.TryGetValue(GetCrossingKey(crossingId.Value), out RoadCrossing crossing)
                    ? crossing
                    : defaultCrossing;
            }
            else
            {
                return defaultCrossing;
            }
        }

        /// <summary>
        /// 获取路段键
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <returns>路段键</returns>
        private static string GetCrossingKey(int sectionId)
        {
            return $"crossing_{sectionId}";
        }

        /// <summary>
        /// 获取路口集合
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <returns>路口集合</returns>
        public static List<RoadCrossing> GetCrossings(this IMemoryCache memoryCache)
        {
            return memoryCache.TryGetValue(GetCrossingsKey(), out List<RoadCrossing> crossings)
                ? crossings
                : new List<RoadCrossing>();
        }

        /// <summary>
        /// 获取路口集合键
        /// </summary>
        /// <returns>路口集合键</returns>
        private static string GetCrossingsKey()
        {
            return "crossings";
        }

        /// <summary>
        /// 获取区域
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="dataId">数据编号</param>
        /// <returns>区域</returns>
        public static TrafficRegion GetRegion(this IMemoryCache memoryCache, string dataId)
        {
            return memoryCache.GetRegion(dataId, null);
        }

        /// <summary>
        /// 获取区域
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="dataId">数据编号</param>
        /// <param name="defaultRegion">区域</param>
        /// <returns>区域</returns>
        public static TrafficRegion GetRegion(this IMemoryCache memoryCache, string dataId,TrafficRegion defaultRegion)
        {
            return memoryCache.TryGetValue(GetRegionKey(dataId), out TrafficRegion region)
                ? region
                : defaultRegion;
        }

        /// <summary>
        /// 获取区域键
        /// </summary>
        /// <param name="dataId">数据编号</param>
        /// <returns>区域键</returns>
        private static string GetRegionKey(string dataId)
        {
            return $"region_{dataId}";
        }

        /// <summary>
        /// 获取区域集合
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <returns>区域集合</returns>
        public static List<TrafficRegion> GetRegions(this IMemoryCache memoryCache)
        {
            return memoryCache.TryGetValue(GetRegionsKey(), out List<TrafficRegion> regions)
                ? regions
                : new List<TrafficRegion>();
        }

        /// <summary>
        /// 获取车道集合键
        /// </summary>
        /// <returns>车道集合键</returns>
        private static string GetRegionsKey()
        {
            return "regions";
        }
    }
}
