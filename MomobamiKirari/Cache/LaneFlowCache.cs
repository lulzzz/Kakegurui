using System;
using ItsukiSumeragi.Cache;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using MomobamiKirari.Codes;
using MomobamiKirari.Models;
using Newtonsoft.Json;

namespace MomobamiKirari.Cache
{
    /// <summary>
    /// 流量缓存
    /// </summary>
    public static class LaneFlowCache
    {
        /// <summary>
        /// 填充车道流量缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="laneFlow">车道流量</param>
        /// <returns>车道流量</returns>
        public static LaneFlow FillLaneFlow(this IMemoryCache memoryCache, LaneFlow laneFlow)
        {
            if (laneFlow != null)
            {
                Lane lane = memoryCache.GetLane(laneFlow.DataId);
                if (lane != null)
                {
                    laneFlow.LaneName = lane.LaneName;
                    laneFlow.CrossingName = lane.Channel.RoadCrossing?.CrossingName;
                    laneFlow.Direction = lane.Direction;
                    laneFlow.Direction_Desc = memoryCache.GetCode(typeof(LaneDirection), laneFlow.Direction);
                    laneFlow.FlowDirection = lane.FlowDirection;
                    laneFlow.Direction_Desc = memoryCache.GetCode(typeof(FlowDirection), laneFlow.FlowDirection);
                }
            }
            return laneFlow;
        }

        /// <summary>
        /// 设置车道最后流量
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="flow">流量数据</param>
        public static void SetLaneLastFlow(this IMemoryCache memoryCache, LaneFlow flow)
        {
            memoryCache.Set(GetLaneLastFlowKey(flow.DataId),
                flow, TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// 获取车道最后流量
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="dataId">数据编号</param>
        /// <returns>车道最后流量</returns>
        public static LaneFlow GetLaneLastFlow(this IMemoryCache memoryCache, string dataId)
        {
            return memoryCache.TryGetValue(GetLaneLastFlowKey(dataId), out LaneFlow laneFlow)
                ? laneFlow
                : null;
        }

        /// <summary>
        /// 获取车道最后流量键
        /// </summary>
        /// <param name="dataId">数据键</param>
        /// <returns>键</returns>
        private static string GetLaneLastFlowKey(string dataId)
        {
            return $"laneflow_{dataId}";
        }

        /// <summary>
        /// 设置车道分钟流量
        /// </summary>
        /// <param name="distributedCache">缓存</param>
        /// <param name="flow">车道分钟流量</param>
        public static void SetLaneMinuteFlow(
            this IDistributedCache distributedCache, LaneFlow flow)
        {
            distributedCache.SetString(GetLaneMinuteFlowKey(flow.DataId,flow.DateTime),
                JsonConvert.SerializeObject(flow), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)+TimeSpan.FromMinutes(1)
                });
        }

        /// <summary>
        /// 获取车道分钟流量
        /// </summary>
        /// <param name="distributedCache">缓存</param>
        /// <param name="dataId">数据编号</param>
        /// <param name="time">数据时间</param>
        /// <returns>车道分钟流量</returns>
        public static LaneFlow GetLaneMinuteFlow(
            this IDistributedCache distributedCache, string dataId,DateTime time)
        {
            string value = distributedCache.GetString(GetLaneMinuteFlowKey(dataId, time));
            return value == null ? null : JsonConvert.DeserializeObject<LaneFlow>(value);
        }

        /// <summary>
        /// 获取车道分钟流量键
        /// </summary>
        /// <param name="dataId">数据键</param>
        /// <param name="time">时间</param>
        /// <returns>键</returns>
        private static string GetLaneMinuteFlowKey(string dataId, DateTime time)
        {
            return $"laneflow_{dataId}_{time:yyyyMMddHHmm}";
        }

        /// <summary>
        /// 设置车道小时流量
        /// </summary>
        /// <param name="distributedCache">缓存</param>
        /// <param name="flow">车道小时流量</param>
        public static void SetLaneHourFlow(
            this IDistributedCache distributedCache, LaneFlow flow)
        {
            LaneFlow flowCache = GetLaneHourFlow(distributedCache, flow.DataId, flow.DateTime);
            if (flowCache == null)
            {
                flowCache = flow;
            }
            else
            {
                flowCache.Cars += flow.Cars;
                flowCache.Vans += flow.Vans;
                flowCache.Buss += flow.Buss;
                flowCache.Tricycles += flow.Tricycles;
                flowCache.Trucks += flow.Trucks;
                flowCache.Motorcycles += flow.Motorcycles;
                flowCache.Bikes += flow.Bikes;
                flowCache.Persons += flow.Persons;
                flowCache.Occupancy += flow.Occupancy;
                flowCache.TimeOccupancy += flow.TimeOccupancy;
                flowCache.Count += flow.Count;
            }

            distributedCache.SetString(
                GetLaneHourFlowKey(flowCache.DataId, flowCache.DateTime),
                JsonConvert.SerializeObject(flowCache),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(2)
                });
        }

        /// <summary>
        /// 获取车道小时流量
        /// </summary>
        /// <param name="distributedCache">缓存</param>
        /// <param name="dataId">数据时间</param>
        /// <param name="time"></param>
        /// <returns>车道小时流量</returns>
        public static LaneFlow GetLaneHourFlow(
            this IDistributedCache distributedCache, string dataId, DateTime time)
        {
            string value = distributedCache.GetString(GetLaneHourFlowKey(dataId, time));
            return value == null ? null : JsonConvert.DeserializeObject<LaneFlow>(value);
        }

        /// <summary>
        /// 获取车道小时流量键
        /// </summary>
        /// <param name="dataId">数据键</param>
        /// <param name="time">时间</param>
        /// <returns>键</returns>
        private static string GetLaneHourFlowKey(string dataId, DateTime time)
        {
            return $"laneflow_{dataId}_{time:yyyyMMddHH}";
        }

        /// <summary>
        /// 设置车道当天流量
        /// </summary>
        /// <param name="distributedCache">缓存</param>
        /// <param name="flow">车道当天流量</param>
        public static void SetLaneDayFlow(
            this IDistributedCache distributedCache, LaneFlow flow)
        {
            LaneFlow flowCache = GetLaneDayFlow(distributedCache, flow.DataId, flow.DateTime);
            if (flowCache == null)
            {
                flowCache = flow;
            }
            else
            {
                flowCache.Cars += flow.Cars;
                flowCache.Vans += flow.Vans;
                flowCache.Buss += flow.Buss;
                flowCache.Tricycles += flow.Tricycles;
                flowCache.Trucks += flow.Trucks;
                flowCache.Motorcycles += flow.Motorcycles;
                flowCache.Bikes += flow.Bikes;
                flowCache.Persons += flow.Persons;
                flowCache.Occupancy += flow.Occupancy;
                flowCache.TimeOccupancy += flow.TimeOccupancy;
                flowCache.Count += flow.Count;
            }

            distributedCache.SetString(
                GetLaneDayFlowKey(flowCache.DataId, flowCache.DateTime),
                JsonConvert.SerializeObject(flowCache),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(2)
                });
        }

        /// <summary>
        /// 获取车道当天流量
        /// </summary>
        /// <param name="distributedCache">缓存</param>
        /// <param name="dataId">数据编号</param>
        /// <param name="time">数据时间</param>
        /// <returns>车道当天流量</returns>
        public static LaneFlow GetLaneDayFlow(
            this IDistributedCache distributedCache, string dataId, DateTime time)
        {
            string value = distributedCache.GetString(GetLaneDayFlowKey(dataId, time));
            return value == null ? null : JsonConvert.DeserializeObject<LaneFlow>(value);
        }

        /// <summary>
        /// 获取车道天流量键
        /// </summary>
        /// <param name="dataId">数据键</param>
        /// <param name="date">日期</param>
        /// <returns>键</returns>
        private static string GetLaneDayFlowKey(string dataId, DateTime date)
        {
            return $"laneflow_{dataId}_{date:yyyyMMdd}";
        }
    }
}
