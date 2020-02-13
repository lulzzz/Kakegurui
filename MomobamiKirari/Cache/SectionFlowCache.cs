using System;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using MomobamiKirari.Models;
using Newtonsoft.Json;
using ItsukiSumeragi.Codes.Flow;

namespace MomobamiKirari.Cache
{
    /// <summary>
    /// 路段流量缓存
    /// </summary>
    public static class SectionFlowCache
    {
        /// <summary>
        /// 填充路段流量缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="sectionFlow">车道流量</param>
        /// <returns>路段流量</returns>
        public static SectionFlow FillSectionFlowCache(this IMemoryCache memoryCache, SectionFlow sectionFlow)
        {
            if (sectionFlow != null)
            {
                TrafficRoadSection section = memoryCache.GetSection(sectionFlow.SectionId);
                if (section != null)
                {
                    sectionFlow.SectionName = section.SectionName;
                }
            }
            return sectionFlow;
        }

        /// <summary>
        /// 设置路段最后流量缓存
        /// </summary>
        /// <param name="memoryCache">缓存实例</param>
        /// <param name="flow">流量数据</param>
        public static void SetSectionLastFlow(this IMemoryCache memoryCache, SectionFlow flow)
        {
            memoryCache.Set(GetSectionLastFlowKey(flow.SectionId),
                flow, TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// 获取路段最后流量缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="sectionId">车道编号</param>
        /// <returns>路段最后流量缓存</returns>
        public static SectionFlow GetSectionLastFlow(this IMemoryCache memoryCache, int sectionId)
        {
            return memoryCache.TryGetValue(GetSectionLastFlowKey(sectionId), out SectionFlow sectionFlow)
                ? sectionFlow
                : null;
        }

        /// <summary>
        /// 获取路段最后流量键
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <returns>键</returns>
        private static string GetSectionLastFlowKey(int sectionId)
        {
            return $"sectionflow_{sectionId}";
        }

        /// <summary>
        /// 设置路段小时流量
        /// </summary>
        /// <param name="distributedCache">缓存</param>
        /// <param name="flow">路段小时流量</param>
        public static void SetSectionHourFlow(
            this IDistributedCache distributedCache, SectionFlow flow)
        {
            SectionFlow flowCache = GetSectionHourFlow(distributedCache,flow.SectionId, flow.DateTime);
            if (flowCache == null)
            {
                flowCache = flow;
            }
            else
            {
                flowCache.Vkt += flow.Vkt;
                flowCache.TravelTimeProportion += flow.TravelTimeProportion;
                flowCache.Count += flow.Count;
            }

            distributedCache.SetString(
                GetSectionHourFlowKey(flowCache.SectionId, flowCache.DateTime),
                JsonConvert.SerializeObject(flowCache),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                });
        }

        /// <summary>
        /// 获取路段小时流量
        /// </summary>
        /// <param name="distributedCache">缓存</param>
        /// <param name="sectionId">路段编号</param>
        /// <param name="time">数据时间</param>
        /// <returns>路段小时流量</returns>
        public static SectionFlow GetSectionHourFlow(
            this IDistributedCache distributedCache, int sectionId,DateTime time)
        {
            string value = distributedCache.GetString(GetSectionHourFlowKey(sectionId,time));
            return value == null ? null : JsonConvert.DeserializeObject<SectionFlow>(value);
        }

        /// <summary>
        /// 获取路段小时流量键
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <param name="time">数据时间</param>
        /// <returns>键</returns>
        private static string GetSectionHourFlowKey(int sectionId, DateTime time)
        {
            return $"sectionflow_{sectionId}_{time:yyyyMMddHH}";
        }

        /// <summary>
        /// 设置路段当天流量
        /// </summary>
        /// <param name="distributedCache">缓存</param>
        /// <param name="flow">路段当天流量</param>
        public static void SetSectionDayFlow(
            this IDistributedCache distributedCache, SectionFlow flow)
        {
            SectionFlow flowCache = GetSectionDayFlow(distributedCache, flow.SectionId, flow.DateTime) 
                                    ?? new SectionFlow
                                    {
                                        SectionId = flow.SectionId,
                                        SectionType = flow.SectionType,
                                        DateTime = flow.DateTime,

                                        //平均速度
                                        Vkt = flow.Vkt,
                                        Fls = flow.Fls,

                                        //拥堵状态
                                        //这里需要设置为通常才能触发如果是拥堵则设置开始时间的条件
                                        TrafficStatus = TrafficStatus.通畅,
                                        CurrentCongestionSpan = flow.CurrentCongestionSpan,
                                        CongestionSpan = flow.CongestionSpan,
                                        CongestionStartTime = flow.CongestionStartTime
                                    };

            if (flow.TrafficStatus == TrafficStatus.通畅)
            {
                flowCache.CurrentCongestionSpan = 0;
            }
            else if (flow.TrafficStatus == TrafficStatus.基本通畅)
            {
                flowCache.CurrentCongestionSpan = 0;
            }
            else if (flow.TrafficStatus == TrafficStatus.轻度拥堵)
            {
                flowCache.CurrentCongestionSpan += 1;
                flowCache.CongestionSpan += 1;
            }
            else if (flow.TrafficStatus == TrafficStatus.中度拥堵)
            {
                flowCache.CurrentCongestionSpan += 1;
                flowCache.CongestionSpan += 1;
            }
            else
            {
                flowCache.CurrentCongestionSpan += 1;
                flowCache.CongestionSpan += 1;
            }

            if (flowCache.TrafficStatus < TrafficStatus.轻度拥堵
                && flow.TrafficStatus >= TrafficStatus.轻度拥堵)
            {
                flowCache.CongestionStartTime = flow.DateTime;
            }
            flowCache.TrafficStatus = flow.TrafficStatus;

            flowCache.Total += flow.Total;
            flowCache.Vkt += flow.Vkt;
            flowCache.Fls += flow.Fls;
            flowCache.TravelTimeProportion += flow.TravelTimeProportion;
            flowCache.Count += flow.Count;

            distributedCache.SetString(
                GetSectionDayFlowKey(flowCache.SectionId, flowCache.DateTime),
                JsonConvert.SerializeObject(flowCache),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                });
        }

        /// <summary>
        /// 获取路段当天流量
        /// </summary>
        /// <param name="distributedCache">缓存</param>
        /// <param name="sectionId">路段编号</param>
        /// <param name="time">数据时间</param>
        /// <returns>路段当天流量</returns>
        public static SectionFlow GetSectionDayFlow(
            this IDistributedCache distributedCache, int sectionId, DateTime time)
        {
            string value = distributedCache.GetString(GetSectionDayFlowKey(sectionId, time));
            return value == null ? null : JsonConvert.DeserializeObject<SectionFlow>(value);
        }

        /// <summary>
        /// 获取路段天流量键
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <param name="date">数据日期</param>
        /// <returns>键</returns>
        private static string GetSectionDayFlowKey(int sectionId, DateTime date)
        {
            return $"sectionflow_{sectionId}_{date:yyyyMMdd}";
        }

    }
}
