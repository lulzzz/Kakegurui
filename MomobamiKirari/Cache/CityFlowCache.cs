using System;
using Microsoft.Extensions.Caching.Memory;

namespace MomobamiKirari.Cache
{
    /// <summary>
    /// 城市流量缓存
    /// </summary>
    public static class CityFlowCache
    {
        /// <summary>
        /// 设置城市小时流量
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="time">数据时间</param>
        /// <param name="congestionData">拥堵指数</param>
        public static void SetCityHourCongestionData(this IMemoryCache memoryCache, DateTime time, double congestionData)
        {
            memoryCache.Set(GetCityHourCongestionDataKey(time), congestionData.ToString("f1"), TimeSpan.FromDays(1));
        }

        /// <summary>
        /// 获取城市小时流量
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="time">数据时间</param>
        /// <returns>城市小时流量</returns>
        public static double? GetCityHourCongestionData(this IMemoryCache memoryCache, DateTime time)
        {
            return memoryCache.TryGetValue(GetCityHourCongestionDataKey(time),out double? congestionData)
                ? congestionData
                : null;
        }

        /// <summary>
        /// 获取城市小时流量键
        /// </summary>
        /// <param name="time">时间</param>
        /// <returns>键</returns>
        private static string GetCityHourCongestionDataKey(DateTime time)
        {
            return $"laneflow_{time:yyyyMMddHH}";
        }

    }
}
