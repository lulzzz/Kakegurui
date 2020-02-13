using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Models;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Caching.Memory;
using YumekoJabami.Cache;
using YumekoJabami.Codes;
using ItsukiSumeragi.Codes.Device;
using ItsukiSumeragi.Codes.Flow;
using ItsukiSumeragi.Codes.Violation;

namespace ItsukiSumeragi.Cache
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
        public static void InitDeviceCache(this IMemoryCache memoryCache,List<TrafficDevice> devices)
        {
            List<TrafficDevice> oldDevices = memoryCache.GetDevices();
            foreach (TrafficDevice device in oldDevices)
            {
                memoryCache.Remove(GetDeviceKey(device.DeviceId));
                foreach (var relation in device.Device_Channels)
                {
                    memoryCache.Remove(GetChannelKey(relation.ChannelId));

                    if (relation.Channel.Lanes != null)
                    {
                        foreach (TrafficLane lane in relation.Channel.Lanes)
                        {
                            memoryCache.Remove(GetLaneKey(lane.DataId));
                        }
                    }
                    
                    if(relation.Channel.Regions!=null)
                    {
                        foreach (TrafficRegion region in relation.Channel.Regions)
                        {
                            memoryCache.Remove(GetRegionKey(region.DataId));
                        }
                    }
                    
                }
            }

            foreach (TrafficDevice device in devices)
            {
                memoryCache.Set(GetDeviceKey(device.DeviceId), device);
                foreach (var relation in device.Device_Channels)
                {
                    relation.Device = device;
                    relation.Channel.Device_Channel = relation;
                    memoryCache.Set(GetChannelKey(relation.Channel.ChannelId), relation.Channel);
                    if (relation.Channel.Lanes != null)
                    {
                        foreach (TrafficLane lane in relation.Channel.Lanes)
                        {
                            lane.Channel = relation.Channel;
                            memoryCache.Set(GetLaneKey(lane.DataId), lane);
                        }
                    }

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
            memoryCache.Set(GetChannelsKey(), devices.SelectMany(d => d.Device_Channels)
                .Select(r => r.Channel)
                .Distinct((c1, c2) => c1.ChannelId == c2.ChannelId)
                .ToList());
            memoryCache.Set(GetLanesKey(),
                devices.SelectMany(d => d.Device_Channels)
                    .Select(r => r.Channel)
                    .Where(c=>c.Lanes!=null)
                    .SelectMany(c=>c.Lanes)
                    .Distinct((l1, l2) => l1.DataId == l2.DataId)
                    .ToList());
            memoryCache.Set(GetRegionsKey(),
                devices.SelectMany(d => d.Device_Channels)
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
        public static TrafficDevice FillDevice(this IMemoryCache memoryCache, TrafficDevice device)
        {
            if (device != null)
            {
                device.DeviceStatus_Desc = memoryCache.GetCode(SystemType.系统管理中心, typeof(DeviceStatus), device.DeviceStatus);
                device.DeviceModel_Desc = memoryCache.GetCode(SystemType.系统管理中心, typeof(DeviceModel), device.DeviceModel);
            }
            return device;
        }

        /// <summary>
        /// 获取设备
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="deviceId">设备编号</param>
        /// <returns>设备</returns>
        public static TrafficDevice GetDevice(this IMemoryCache memoryCache, int deviceId)
        {
            return memoryCache.TryGetValue(GetDeviceKey(deviceId), out TrafficDevice device)
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
        public static List<TrafficDevice> GetDevices(this IMemoryCache memoryCache)
        {
            return memoryCache.TryGetValue(GetDevicesKey(), out List<TrafficDevice> devices)
                ? devices
                : new List<TrafficDevice>();
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
        public static TrafficChannel FillChannel(this IMemoryCache memoryCache, TrafficChannel channel)
        {
            if (channel != null)
            {
                channel.ChannelStatus_Desc = memoryCache.GetCode(SystemType.系统管理中心, typeof(DeviceStatus), channel.ChannelStatus);
                channel.ChannelType_Desc = memoryCache.GetCode(SystemType.系统管理中心, typeof(ChannelType), channel.ChannelType);
               
                if (channel.ChannelDeviceType.HasValue)
                {
                    channel.ChannelDeviceType_Desc = memoryCache.GetCode(SystemType.系统管理中心, typeof(ChannelDeviceType), channel.ChannelDeviceType.Value);
                }

                if (channel.RtspProtocol.HasValue)
                {
                    channel.RtspProtocol_Desc = memoryCache.GetCode(SystemType.系统管理中心, typeof(RtspProtocol), channel.RtspProtocol.Value);
                }

                if (channel.Direction.HasValue)
                {
                    channel.Direction_Desc = memoryCache.GetCode(SystemType.系统管理中心, typeof(ChannelDirection), channel.Direction.Value);
                }

                foreach (TrafficShape shape in channel.Shapes)
                {
                    string[] tagNames=shape.TagName.Split("/");

                    foreach (var cv in channel.Channel_Violations)
                    {
                        foreach (var vg in cv.Violation.Violation_Tags)
                        {
                            if (vg.TagName == tagNames[0])
                            {
                                shape.Color = vg.Tag.Color;
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(shape.Color))
                        {
                            break;
                        }
                    }
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
        public static TrafficChannel GetChannel(this IMemoryCache memoryCache, string channelId)
        {
            return memoryCache.TryGetValue(GetChannelKey(channelId), out TrafficChannel channel)
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
        public static List<TrafficChannel> GetChannels(this IMemoryCache memoryCache)
        {
            return memoryCache.TryGetValue(GetChannelsKey(), out List<TrafficChannel> channels)
                ? channels
                : new List<TrafficChannel>();
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
        public static void InitCrossingCache(this IMemoryCache memoryCache, List<TrafficRoadCrossing> crossings)
        {
            List<TrafficRoadCrossing> oldCrossings = memoryCache.GetCrossings();
            foreach (TrafficRoadCrossing oldCrossing in oldCrossings)
            {
                memoryCache.Remove(GetCrossingKey(oldCrossing.CrossingId));
            }

            memoryCache.Set(GetCrossingsKey(), crossings);
            foreach (TrafficRoadCrossing crossing in crossings)
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
        public static TrafficRoadCrossing GetCrossing(this IMemoryCache memoryCache, int? crossingId,TrafficRoadCrossing defaultCrossing)
        {
            if (crossingId.HasValue)
            {
                return memoryCache.TryGetValue(GetCrossingKey(crossingId.Value), out TrafficRoadCrossing crossing)
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
        public static List<TrafficRoadCrossing> GetCrossings(this IMemoryCache memoryCache)
        {
            return memoryCache.TryGetValue(GetCrossingsKey(), out List<TrafficRoadCrossing> crossings)
                ? crossings
                : new List<TrafficRoadCrossing>();
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
        public static TrafficRoadSection FillSection(this IMemoryCache memoryCache, TrafficRoadSection section)
        {
            if (section != null)
            {
                section.SectionType_Desc = memoryCache.GetCode(SystemType.系统管理中心, typeof(SectionType), section.SectionType);
                section.Direction_Desc = memoryCache.GetCode(SystemType.系统管理中心, typeof(SectionDirection), section.Direction);
            }
            return section;
        }


        /// <summary>
        /// 初始化路段缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="sections">路段集合</param>
        public static void InitSectionCache(this IMemoryCache memoryCache, List<TrafficRoadSection> sections)
        {
            List<TrafficRoadSection> oldSections = memoryCache.GetSections();
            foreach (TrafficRoadSection oldSection in oldSections)
            {
                memoryCache.Remove(GetSectionKey(oldSection.SectionId));
            }

            memoryCache.Set(GetSectionsKey(), sections);
            foreach (TrafficRoadSection section in sections)
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
        public static TrafficRoadSection GetSection(this IMemoryCache memoryCache, int sectionId)
        {
            return memoryCache.TryGetValue(GetSectionKey(sectionId), out TrafficRoadSection section)
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
        public static List<TrafficRoadSection> GetSections(this IMemoryCache memoryCache)
        {
            return memoryCache.TryGetValue(GetSectionsKey(), out List<TrafficRoadSection> sections)
                ? sections
                : new List<TrafficRoadSection>();
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
        /// 初始化地点缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="locations">地点集合</param>
        public static void InitLocationCache(this IMemoryCache memoryCache, List<TrafficLocation> locations)
        {
            List<TrafficLocation> oldLocations = memoryCache.GetLocations();
            foreach (TrafficLocation oldLocation in oldLocations)
            {
                memoryCache.Remove(GetLocationKey(oldLocation.LocationId));
            }

            memoryCache.Set(GetLocationsKey(), locations);
            foreach (TrafficLocation location in locations)
            {
                memoryCache.Set(GetLocationKey(location.LocationId), location);
            }
        }

        /// <summary>
        /// 获取地点
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="locationId">地点编号</param>
        /// <returns>地点</returns>
        public static TrafficLocation GetLocation(this IMemoryCache memoryCache, int locationId)
        {
            return memoryCache.GetLocation(locationId, null);
        }

        /// <summary>
        /// 获取地点
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="locationId">地点编号</param>
        /// <param name="defaultLocation">默认地点</param>
        /// <returns>地点</returns>
        public static TrafficLocation GetLocation(this IMemoryCache memoryCache, int locationId,TrafficLocation defaultLocation)
        {
            return memoryCache.TryGetValue(GetLocationKey(locationId), out TrafficLocation location)
                ? location
                : defaultLocation;
        }

        /// <summary>
        /// 获取地点键
        /// </summary>
        /// <param name="locationId">地点编号</param>
        /// <returns>路段键</returns>
        private static string GetLocationKey(int locationId)
        {
            return $"location_{locationId}";
        }

        /// <summary>
        /// 获取地点集合
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <returns>路段集合</returns>
        public static List<TrafficLocation> GetLocations(this IMemoryCache memoryCache)
        {
            return memoryCache.TryGetValue(GetLocationsKey(), out List<TrafficLocation> locations)
                ? locations
                : new List<TrafficLocation>();
        }

        /// <summary>
        /// 获取地点集合键
        /// </summary>
        /// <returns>路段集合键</returns>
        private static string GetLocationsKey()
        {
            return "locations";
        }

        /// <summary>
        /// 填充车道缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="lane">车道</param>
        /// <returns>车道</returns>
        public static TrafficLane FillLane(this IMemoryCache memoryCache, TrafficLane lane)
        {
            if (lane.LaneType.HasValue)
            {
                lane.LaneType_Desc = memoryCache.GetCode(SystemType.智慧交通视频检测系统, typeof(LaneType), lane.LaneType.Value);
            }
            lane.Direction_Desc = memoryCache.GetCode(SystemType.智慧交通视频检测系统, typeof(LaneDirection), lane.Direction);
            lane.FlowDirection_Desc = memoryCache.GetCode(SystemType.智慧交通视频检测系统, typeof(FlowDirection), lane.FlowDirection);
            return lane;
        }

        /// <summary>
        /// 获取车道
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="dataId">数据编号</param>
        /// <returns>车道</returns>
        public static TrafficLane GetLane(this IMemoryCache memoryCache, string dataId)
        {
            return memoryCache.TryGetValue(GetLaneKey(dataId), out TrafficLane lane)
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
        public static List<TrafficLane> GetLanes(this IMemoryCache memoryCache)
        {
            return memoryCache.TryGetValue(GetLanesKey(), out List<TrafficLane> lanes)
                ? lanes
                : new List<TrafficLane>();
        }

        /// <summary>
        /// 获取车道集合键
        /// </summary>
        /// <returns>车道集合键</returns>
        private static string GetLanesKey()
        {
            return "lanes";
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

        /// <summary>
        /// 填充标签缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="tag">标签</param>
        /// <returns>标签</returns>
        public static TrafficTag FillTag(this IMemoryCache memoryCache, TrafficTag tag)
        {
            if (tag != null)
            {
                tag.TagType_Desc = memoryCache.GetCode(SystemType.智慧交通违法检测系统, typeof(TagType), tag.TagType);
            }
            return tag;
        }

        /// <summary>
        /// 初始化违法行为缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="violations">违法行为集合</param>
        public static void InitViolationCache(this IMemoryCache memoryCache, List<TrafficViolation> violations)
        {
            List<TrafficViolation> oldViolations = memoryCache.GetViolations();
            foreach (TrafficViolation oldViolation in oldViolations)
            {
                memoryCache.Remove(GetViolationKey(oldViolation.ViolationId));
            }

            memoryCache.Set(GetViolationsKey(), violations);
            foreach (TrafficViolation violation in violations)
            {
                memoryCache.Set(GetViolationKey(violation.ViolationId), violation);
            }
        }

        /// <summary>
        /// 获取违法行为
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="violationId">数据编号</param>
        /// <returns>违法行为</returns>
        public static TrafficViolation GetViolation(this IMemoryCache memoryCache, int violationId)
        {
            return memoryCache.GetViolation(violationId, null);
        }

        /// <summary>
        /// 获取违法行为
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="violationId">数据编号</param>
        /// <param name="defaultViolation">默认违法</param>
        /// <returns>违法行为</returns>
        public static TrafficViolation GetViolation(this IMemoryCache memoryCache, int violationId,TrafficViolation defaultViolation)
        {
            return memoryCache.TryGetValue(GetViolationKey(violationId), out TrafficViolation violation)
                ? violation
                : defaultViolation;
        }

        /// <summary>
        /// 获取违法行为键
        /// </summary>
        /// <param name="violationId">数据编号</param>
        /// <returns>违法行为键</returns>
        private static string GetViolationKey(int violationId)
        {
            return $"violation_{violationId}";
        }

        /// <summary>
        /// 获取违法行为集合
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <returns>违法行为集合</returns>
        public static List<TrafficViolation> GetViolations(this IMemoryCache memoryCache)
        {
            return memoryCache.TryGetValue(GetViolationsKey(), out List<TrafficViolation> violations)
                ? violations
                : new List<TrafficViolation>();
        }

        /// <summary>
        /// 获取违法行为集合键
        /// </summary>
        /// <returns>违法行为集合键</returns>
        private static string GetViolationsKey()
        {
            return "violations";
        }
    }
}
