using ItsukiSumeragi.DataFlow;
using MomobamiRirika.Cache;
using MomobamiRirika.Models;

namespace MomobamiRirika.DataFlow
{
    /// <summary>
    /// 交通事件缓存数据块
    /// </summary>
    public class EventCacheBlock:TrafficActionBlock<TrafficEvent>
    {
        protected override void Handle(TrafficEvent data)
        {
            if (EventCache.LastEventsCache.Count >= 10)
            {
                EventCache.LastEventsCache.TryTake(out _);
            }
            EventCache.LastEventsCache.Add(data);
        }
    }
}
