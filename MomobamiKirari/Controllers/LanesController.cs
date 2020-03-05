using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Mvc;
using MomobamiKirari.Managers;

namespace MomobamiKirari.Controllers
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
        /// 车道数据库操作实例
        /// </summary>
        private readonly LanesManager _lanesManager;

        /// <summary>
        /// 数据库实例
        /// </summary>
        /// <param name="lanesManager">车道数据库操作实例</param>
        public LanesController(LanesManager lanesManager)
        {
            _lanesManager = lanesManager;
        }

        /// <summary>
        /// 按路口查询车道集合
        /// </summary>
        /// <param name="crossingName">路口名称</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet("group/crossing")]
        public PageModel<object> GetGroupByCrossing([FromQuery] string crossingName,
            [FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            return _lanesManager.GetGroupByCrossing(crossingName, pageNum, pageSize);
        }

        /// <summary>
        /// 按路段查询车道集合
        /// </summary>
        /// <param name="sectionName">路段名称</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet("group/section")]
        public PageModel<object> GetGroupBySection([FromQuery] string sectionName,
            [FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            return _lanesManager.GetGroupBySection(sectionName, pageNum, pageSize);
        }
    }
}
