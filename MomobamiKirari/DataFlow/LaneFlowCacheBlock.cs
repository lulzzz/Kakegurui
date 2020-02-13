using System;
using ItsukiSumeragi.DataFlow;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using MomobamiKirari.Cache;
using MomobamiKirari.Models;

namespace MomobamiKirari.DataFlow
{
    /// <summary>
    /// 流量缓存数据块
    /// </summary>
    public class LaneFlowCacheBlock : TrafficActionBlock<LaneFlow>
    {
        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IDistributedCache _distributedCache;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">实例工厂</param>
        public LaneFlowCacheBlock(IServiceProvider serviceProvider)
        {
            _memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
            _distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();
        }

        protected override void Handle(LaneFlow data)
        {
            _memoryCache.SetLaneLastFlow(data);
            _distributedCache.SetLaneMinuteFlow(data);
            _distributedCache.SetLaneHourFlow(data);
            _distributedCache.SetLaneDayFlow(data);
        }
    }
}
