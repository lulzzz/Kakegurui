using System.Linq;
using Kakegurui.WebExtensions;
using Microsoft.EntityFrameworkCore;
using MomobamiRirika.Data;
using MomobamiRirika.Models;

namespace MomobamiRirika.Managers
{
    /// <summary>
    /// 区域数据库操作
    /// </summary>
    public class RegionsManager
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly DensityContext _context;

        /// <summary>
        /// 数据库实例
        /// </summary>
        /// <param name="context">数据库实例</param>
        public RegionsManager(DensityContext context)
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
        public PageModel<object> GetGroupByCrossing(string crossingName, int pageNum, int pageSize)
        {
            IQueryable<IGrouping<int, DensityChannel>> queryable =
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
    }
}
