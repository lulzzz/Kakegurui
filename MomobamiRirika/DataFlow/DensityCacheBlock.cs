using System.Collections.Concurrent;
using ItsukiSumeragi.DataFlow;
using MomobamiRirika.Cache;
using MomobamiRirika.Models;

namespace MomobamiRirika.DataFlow
{
    /// <summary>
    /// 密度缓存数据块
    /// </summary>
    public class DensityCacheBlock:TrafficActionBlock<TrafficDensity>
    {
        protected override void Handle(TrafficDensity data)
        {
            if (DensityCache.DensitiesCache.ContainsKey(data.DataId))
            {
                ConcurrentQueue<TrafficDensity> queue = DensityCache.DensitiesCache[data.DataId];
                if (queue.Count >= 2 * 24 * 60)
                {
                    queue.TryDequeue(out _);
                }
                queue.Enqueue(data);
            }
        }
    }
}
