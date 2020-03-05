using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Codes;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Caching.Memory;
using MomobamiKirari.Codes;
using MomobamiKirari.Models;

namespace MomobamiKirari.Cache
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
        public static void InitDeviceCache(this IMemoryCache memoryCache,List<FlowDevice> devices)
        {
            List<FlowDevice> oldDevices = memoryCache.GetDevices();
            foreach (FlowDevice device in oldDevices)
            {
                memoryCache.Remove(GetDeviceKey(device.DeviceId));
                foreach (var relation in device.FlowDevice_FlowChannels)
                {
                    memoryCache.Remove(GetChannelKey(relation.ChannelId));

                    if (relation.Channel.Lanes != null)
                    {
                        foreach (Lane lane in relation.Channel.Lanes)
                        {
                            memoryCache.Remove(GetLaneKey(lane.DataId));
                        }
                    }
                }
            }

            foreach (FlowDevice device in devices)
            {
                memoryCache.Set(GetDeviceKey(device.DeviceId), device);
                foreach (var relation in device.FlowDevice_FlowChannels)
                {
                    relation.Device = device;
                    relation.Channel.FlowDevice_FlowChannel = relation;
                    memoryCache.Set(GetChannelKey(relation.Channel.ChannelId), relation.Channel);
                    if (relation.Channel.Lanes != null)
                    {
                        foreach (Lane lane in relation.Channel.Lanes)
                        {
                            lane.Channel = relation.Channel;
                            memoryCache.Set(GetLaneKey(lane.DataId), lane);
                        }
                    }
                }
            }

            memoryCache.Set(GetDevicesKey(),devices);
            memoryCache.Set(GetChannelsKey(), devices.SelectMany(d => d.FlowDevice_FlowChannels)
                .Select(r => r.Channel)
                .Distinct((c1, c2) => c1.ChannelId == c2.ChannelId)
                .ToList());
            memoryCache.Set(GetLanesKey(),
                devices.SelectMany(d => d.FlowDevice_FlowChannels)
                    .Select(r => r.Channel)
                    .Where(c=>c.Lanes!=null)
                    .SelectMany(c=>c.Lanes)
                    .Distinct((l1, l2) => l1.DataId == l2.DataId)
                    .ToList());
        }

        /// <summary>
        /// 填充设备缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="device">设备</param>
        /// <returns>设备</returns>
        public static FlowDevice FillDevice(this IMemoryCache memoryCache, FlowDevice device)
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
        public static FlowDevice GetDevice(this IMemoryCache memoryCache, int deviceId)
        {
            return memoryCache.TryGetValue(GetDeviceKey(deviceId), out FlowDevice device)
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
        public static List<FlowDevice> GetDevices(this IMemoryCache memoryCache)
        {
            return memoryCache.TryGetValue(GetDevicesKey(), out List<FlowDevice> devices)
                ? devices
                : new List<FlowDevice>();
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
        public static FlowChannel FillChannel(this IMemoryCache memoryCache, FlowChannel channel)
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
        public static FlowChannel GetChannel(this IMemoryCache memoryCache, string channelId)
        {
            return memoryCache.TryGetValue(GetChannelKey(channelId), out FlowChannel channel)
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
        public static List<FlowChannel> GetChannels(this IMemoryCache memoryCache)
        {
            return memoryCache.TryGetValue(GetChannelsKey(), out List<FlowChannel> channels)
                ? channels
                : new List<FlowChannel>();
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
        /// 填充路段缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="section">路段</param>
        /// <returns>路段</returns>
        public static RoadSection FillSection(this IMemoryCache memoryCache, RoadSection section)
        {
            if (section != null)
            {
                section.SectionType_Desc = memoryCache.GetCode(typeof(SectionType), section.SectionType);
                section.Direction_Desc = memoryCache.GetCode(typeof(SectionDirection), section.Direction);
            }
            return section;
        }


        /// <summary>
        /// 初始化路段缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="sections">路段集合</param>
        public static void InitSectionCache(this IMemoryCache memoryCache, List<RoadSection> sections)
        {
            List<RoadSection> oldSections = memoryCache.GetSections();
            foreach (RoadSection oldSection in oldSections)
            {
                memoryCache.Remove(GetSectionKey(oldSection.SectionId));
            }

            memoryCache.Set(GetSectionsKey(), sections);
            foreach (RoadSection section in sections)
            {
                memoryCache.Set(GetSectionKey(section.SectionId), section);
            }
        }

        /// <summary>
        /// 获取路段
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="sectionId">路段编号</param>
        /// <returns>路段</returns>
        public static RoadSection GetSection(this IMemoryCache memoryCache, int sectionId)
        {
            return memoryCache.TryGetValue(GetSectionKey(sectionId), out RoadSection section)
                ? section
                : null;
        }

        /// <summary>
        /// 获取路段键
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <returns>路段键</returns>
        private static string GetSectionKey(int sectionId)
        {
            return $"section_{sectionId}";
        }

        /// <summary>
        /// 获取路段集合
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <returns>路段集合</returns>
        public static List<RoadSection> GetSections(this IMemoryCache memoryCache)
        {
            return memoryCache.TryGetValue(GetSectionsKey(), out List<RoadSection> sections)
                ? sections
                : new List<RoadSection>();
        }

        /// <summary>
        /// 获取路段集合键
        /// </summary>
        /// <returns>路段集合键</returns>
        private static string GetSectionsKey()
        {
            return "sections";
        }

        /// <summary>
        /// 填充车道缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="lane">车道</param>
        /// <returns>车道</returns>
        public static Lane FillLane(this IMemoryCache memoryCache, Lane lane)
        {
            if (lane.LaneType.HasValue)
            {
                lane.LaneType_Desc = memoryCache.GetCode(typeof(LaneType), lane.LaneType.Value);
            }
            lane.Direction_Desc = memoryCache.GetCode(typeof(LaneDirection), lane.Direction);
            lane.FlowDirection_Desc = memoryCache.GetCode(typeof(FlowDirection), lane.FlowDirection);
            return lane;
        }

        /// <summary>
        /// 获取车道
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="dataId">数据编号</param>
        /// <returns>车道</returns>
        public static Lane GetLane(this IMemoryCache memoryCache, string dataId)
        {
            return memoryCache.TryGetValue(GetLaneKey(dataId), out Lane lane)
                ? lane
                : null;
        }

        /// <summary>
        /// 获取车道键
        /// </summary>
        /// <param name="dataId">数据编号</param>
        /// <returns>路段键</returns>
        private static string GetLaneKey(string dataId)
        {
            return $"lane_{dataId}";
        }

        /// <summary>
        /// 获取车道集合
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <returns>车道集合</returns>
        public static List<Lane> GetLanes(this IMemoryCache memoryCache)
        {
            return memoryCache.TryGetValue(GetLanesKey(), out List<Lane> lanes)
                ? lanes
                : new List<Lane>();
        }

        /// <summary>
        /// 获取车道集合键
        /// </summary>
        /// <returns>车道集合键</returns>
        private static string GetLanesKey()
        {
            return "lanes";
        }
    }
}
