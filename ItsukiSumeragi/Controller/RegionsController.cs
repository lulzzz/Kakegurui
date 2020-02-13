using ItsukiSumeragi.Data;
using ItsukiSumeragi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Kakegurui.WebExtensions;

namespace ItsukiSumeragi.Controller
{
    /// <summary>
    /// 区域
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class RegionsController : ControllerBase
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly DeviceContext _context;

        /// <summary>
        /// 数据库实例
        /// </summary>
        /// <param name="context">数据库实例</param>
        public RegionsController(DeviceContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 按路口查询区域集合
        /// </summary>
        /// <param name="crossingName">路口名称</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet("group/crossing")]
        public PageModel<object> GetRegionsGroupByCrossing([FromQuery] string crossingName, [FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            IQueryable<IGrouping<int, TrafficChannel>> queryable =
                _context.Channels
                    .Where(c => c.CrossingId.HasValue && c.Regions.Count > 0)
                    .Include(c => c.RoadCrossing)
                    .Include(c => c.Regions)
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
                    Regions = g.SelectMany(c => c.Regions).ToList()
                }).ToList(),
                Total = model.Total
            };
        }

        /// <summary>
        /// 按路段查询区域集合
        /// </summary>
        /// <param name="sectionName">路段名称</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet("group/section")]
        public PageModel<object> GetRegionsGroupBySection([FromQuery] string sectionName, [FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            IQueryable<IGrouping<int, TrafficChannel>> queryable =
                _context.Channels
                    .Where(c => c.SectionId.HasValue && c.Regions.Count > 0)
                    .Include(c => c.RoadSection)
                    .Include(c => c.Regions)
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
                    Regions = g.SelectMany(c => c.Regions).ToList()
                }).ToList(),
                Total = model.Total
            };
        }
    }
}
