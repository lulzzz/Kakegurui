using System.Collections.Concurrent;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Models;
using Microsoft.Extensions.Caching.Memory;
using MomobamiRirika.Models;

namespace MomobamiRirika.Cache
{
    /// <summary>
    /// 密度数据缓存
    /// </summary>
    public static class DensityCache
    {
        /// <summary>
        /// 密度缓存集合
        /// </summary>
        public static ConcurrentDictionary<string, ConcurrentQueue<TrafficDensity>> DensitiesCache { get; } = new ConcurrentDictionary<string, ConcurrentQueue<TrafficDensity>>();

        /// <summary>
        /// 填充密度集合缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="density">密度数据</param>
        /// <returns>密度数据</returns>
        public static TrafficDensity FillDensity(this IMemoryCache memoryCache,TrafficDensity density)
        {
            if (density != null)
            {
                TrafficRegion region = memoryCache.GetRegion(density.DataId);
                if (region != null)
                {
                    density.RegionName = region.RegionName;
                }
            }
            return density;
        }
    }
}
