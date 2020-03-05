using System.Linq;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MomobamiRirika.Data;
using MomobamiRirika.Managers;
using MomobamiRirika.Models;

namespace MomobamiRirika.Controllers
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
        /// 区域数据库操作实例
        /// </summary>
        private readonly RegionsManager _manager;

        /// <summary>
        /// 数据库实例
        /// </summary>
        /// <param name="manager">区域数据库操作实例</param>
        public RegionsController(RegionsManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// 按路口查询区域集合
        /// </summary>
        /// <param name="crossingName">路口名称</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet("group/crossing")]
        public PageModel<object> GetGroupByCrossing([FromQuery] string crossingName, [FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            return _manager.GetGroupByCrossing(crossingName, pageNum, pageSize);
        }

    }
}
