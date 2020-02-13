using System.Linq;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Data;
using ItsukiSumeragi.Models;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ItsukiSumeragi.Controller
{
    /// <summary>
    /// 车道
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class LanesController : ControllerBase
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly DeviceContext _context;

        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// 数据库实例
        /// </summary>
        /// <param name="context">数据库实例</param>
        /// <param name="memoryCache">缓存</param>
        public LanesController(DeviceContext context, IMemoryCache memoryCache)
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
        [HttpGet("group/crossing")]
        public PageModel<object> GetLanesGroupByCrossing([FromQuery] string crossingName,
            [FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            IQueryable<IGrouping<int, TrafficChannel>> queryable =
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
                Datas = model.Datas.Select(g => (object) new
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
        [HttpGet("group/section")]
        public PageModel<object> GetLanesGroupBySection([FromQuery] string sectionName,
            [FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            IQueryable<IGrouping<int, TrafficChannel>> queryable =
                _context.Channels
                    .Where(c => c.SectionId.HasValue&& c.Lanes.Count > 0)
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
