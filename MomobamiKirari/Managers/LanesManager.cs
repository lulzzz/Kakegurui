using System.Linq;
using Kakegurui.WebExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MomobamiKirari.Cache;
using MomobamiKirari.Data;
using MomobamiKirari.Models;

namespace MomobamiKirari.Managers
{
    /// <summary>
    /// 车道数据库操作
    /// </summary>
    public class LanesManager
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly FlowContext _context;

        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// 数据库实例
        /// </summary>
        /// <param name="context">数据库实例</param>
        /// <param name="memoryCache">缓存</param>
        public LanesManager(FlowContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// 按路口查询车道集合
        /// </summary>
        /// <param name="crossingName">路口名称</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        public PageModel<object> GetGroupByCrossing(string crossingName,
            int pageNum, int pageSize)
        {
            IQueryable<IGrouping<int, FlowChannel>> queryable =
                _context.Channels
                    .Where(c => c.CrossingId.HasValue && c.Lanes.Count > 0)
                    .Include(c => c.RoadCrossing)
                    .Include(c => c.Lanes)
                    .GroupBy(c => c.CrossingId.Value);

            if (!string.IsNullOrEmpty(crossingName))
            {
                queryable = queryable.Where(g => g.First().RoadCrossing.CrossingName.Contains(crossingName));
            }

            var model = queryable.Page(pageNum, pageSize);

            return new PageModel<object>
            {
                Datas = model.Datas.Select(g => (object)new
                {
                    CrossingId = g.Key,
                    g.First().RoadCrossing.CrossingName,
                    Lanes = g.SelectMany(c => c.Lanes.Select(l => _memoryCache.FillLane(l))).ToList()
                }).ToList(),
                Total = model.Total
            };

        }

        /// <summary>
        /// 按路段查询车道集合
        /// </summary>
        /// <param name="sectionName">路段名称</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        public PageModel<object> GetGroupBySection(string sectionName,
            int pageNum, int pageSize)
        {
            IQueryable<IGrouping<int, FlowChannel>> queryable =
                _context.Channels
                    .Where(c => c.SectionId.HasValue && c.Lanes.Count > 0)
                    .Include(c => c.RoadSection)
                    .Include(c => c.Lanes)
                    .GroupBy(c => c.SectionId.Value);

            if (!string.IsNullOrEmpty(sectionName))
            {
                queryable = queryable.Where(g => g.First().RoadSection.SectionName.Contains(sectionName));
            }

            var model = queryable.Page(pageNum, pageSize);

            return new PageModel<object>
            {
                Datas = model.Datas.Select(g => (object)new
                {
                    SectionId = g.Key,
                    g.First().RoadSection.SectionName,
                    Lanes = g.SelectMany(c => c.Lanes.Select(l => _memoryCache.FillLane(l))).ToList()
                }).ToList(),
                Total = model.Total
            };
        }
    }
}
