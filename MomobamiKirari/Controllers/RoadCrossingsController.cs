using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomobamiKirari.Managers;
using MomobamiKirari.Models;

namespace MomobamiKirari.Controllers
{
    /// <summary>
    /// 路口
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class RoadCrossingsController : ControllerBase
    {
        /// <summary>
        /// 路口数据库操作实例
        /// </summary>
        private readonly RoadCrossingsManager _manager;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="manager">路口数据库操作实例</param>
        public RoadCrossingsController(RoadCrossingsManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// 查询路口集合
        /// </summary>
        /// <param name="crossingName">路口名称</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        public PageModel<RoadCrossing> GetList([FromQuery] string crossingName, [FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            return _manager.GetList(crossingName, pageNum, pageSize);
        }

        /// <summary>
        /// 查询路口
        /// </summary>
        /// <param name="crossingId">路口编号</param>
        /// <returns>查询结果</returns>
        [HttpGet("{crossingId}")]
        public IActionResult Get([FromRoute] int crossingId)
        {
            return _manager.Get(crossingId);
        }

        /// <summary>
        /// 添加路口
        /// </summary>
        /// <param name="roadCrossing">路口</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public IActionResult Add([FromBody] RoadCrossing roadCrossing)
        {
            return _manager.Add(roadCrossing, User?.Identity?.Name);
        }

        /// <summary>
        /// 导入路口
        /// </summary>
        /// <param name="file">文件</param>
        /// <returns>导入结果</returns>
        [HttpPost("import")]
        public IActionResult Import(IFormFile file)
        {
            return _manager.Import(file, User?.Identity?.Name);
        }

        /// <summary>
        /// 更新路口
        /// </summary>
        /// <param name="roadCrossing">路口</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public IActionResult Update([FromBody] RoadCrossing roadCrossing)
        {
            return _manager.Update(roadCrossing, User?.Identity?.Name);
        }

        /// <summary>
        /// 删除路口
        /// </summary>
        /// <param name="crossingId">路口编号</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{crossingId}")]
        public IActionResult Remove([FromRoute] int crossingId)
        {
            return _manager.Remove(crossingId, User?.Identity?.Name);
        }
    }
}
