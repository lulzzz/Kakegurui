using System;
using Microsoft.Extensions.Caching.Memory;
using MomobamiKirari.Models;

namespace MomobamiKirari.Cache
{
    /// <summary>
    /// 流量缓存
    /// </summary>
    public static class ChannelFlowCache
    {
        /// <summary>
        /// 设置通道当天流量
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="flow">流量数据</param>
        /// <param name="date">日期</param>
        public static void SetChannelDayFlow(this IMemoryCache memoryCache, ChannelDayFlow flow, DateTime date)
        {
            memoryCache.Set(GetChannelDayFlowKey(flow.ChannelId,date), flow, TimeSpan.FromDays(1));
        }

        /// <summary>
        /// 获取通道当天流量
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="channelId">通道编号</param>
        /// <param name="date">日期</param>
        /// <returns>通道当天流量</returns>
        public static ChannelDayFlow GetChannelDayFlow(this IMemoryCache memoryCache, string channelId,DateTime date)
        {
            return memoryCache.TryGetValue(GetChannelDayFlowKey(channelId, date), out ChannelDayFlow flow)
                ? flow
                : null;
        }

        /// <summary>
        /// 获取通道当天流量键
        /// </summary>
        /// <param name="channelId">通道键</param>
        /// <param name="date">日期</param>
        /// <returns>键</returns>
        private static string GetChannelDayFlowKey(string channelId, DateTime date)
        {
            return $"channelflow_{channelId}_{date:yyyyMMdd}";
        }
    }
}
