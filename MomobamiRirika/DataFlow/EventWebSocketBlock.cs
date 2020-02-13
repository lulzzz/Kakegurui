using System;
using ItsukiSumeragi.DataFlow;
using Kakegurui.WebExtensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using MomobamiRirika.Cache;
using MomobamiRirika.Models;

namespace MomobamiRirika.DataFlow
{
    /// <summary>
    /// 密度数据websocket数据块
    /// </summary>
    public class EventWebSocketBlock : TrafficActionBlock<TrafficEvent>
    {
        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// 拥堵事件ws地址
        /// </summary>
        public const string EventUrl = "/websocket/event/";

        public EventWebSocketBlock(IServiceProvider serviceProvider)
        {
            _memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
        }
        protected override void Handle(TrafficEvent data)
        {
            _memoryCache.FillEvent(data);
            WebSocketMiddleware.Broadcast(EventUrl, data);
        }
    }
}
