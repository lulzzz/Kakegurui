using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using MomobamiRirika.Models;

namespace MomobamiRirika.Cache
{
    /// <summary>
    /// 密度数据缓存
    /// </summary>
    public static class EventCache
    {
        /// <summary>
        /// 交通时间缓存队列
        /// </summary>
        public static ConcurrentBag<TrafficEvent> LastEventsCache { get; } = new ConcurrentBag<TrafficEvent>();

        /// <summary>
        /// 填充事件缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="trafficEvent">事件数据</param>
        /// <returns>事件</returns>
        public static TrafficEvent FillEvent(this IMemoryCache memoryCache, TrafficEvent trafficEvent)
        {
            if (trafficEvent != null)
            {
                TrafficRegion region = memoryCache.GetRegion(trafficEvent.DataId);
                if (region != null)
                {
                    trafficEvent.RegionName = region.RegionName;
                    trafficEvent.CrossingId = region.Channel.CrossingId ?? 0;
                    trafficEvent.CrossingName = region.Channel.RoadCrossing?.CrossingName;
                }
            }
            return trafficEvent;
        }
    }
}
