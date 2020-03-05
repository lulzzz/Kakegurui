using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomobamiKirari.Managers;
using MomobamiKirari.Models;

namespace MomobamiKirari.Controllers
{
    /// <summary>
    /// 路段
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class RoadSectionsController : ControllerBase
    {
        /// <summary>
        /// 路段数据库操作实例
        /// </summary>
        private readonly RoadSectionsManager _manager;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="manager">路段数据库操作实例</param>
        public RoadSectionsController(RoadSectionsManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// 查询路段集合
        /// </summary>
        /// <param name="sectionName">路段名称</param>
        /// <param name="sectionType">路段类型</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        public PageModel<RoadSection> GetList([FromQuery] string sectionName, [FromQuery] int sectionType, [FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            return _manager.GetList(sectionName, sectionType, pageNum, pageSize);
        }

        /// <summary>
        /// 查询路段
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <returns>查询结果</returns>
        [HttpGet("{sectionId}")]
        public IActionResult Get([FromRoute] int sectionId)
        {
            return _manager.Get(sectionId);
        }

        /// <summary>
        /// 添加路段
        /// </summary>
        /// <param name="roadSection">路段</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public IActionResult Add([FromBody] RoadSection roadSection)
        {
            return _manager.Add(roadSection, User?.Identity?.Name);
        }

        /// <summary>
        /// 导入路段
        /// </summary>
        /// <param name="file">文件</param>
        /// <returns>导入结果</returns>
        [HttpPost("import")]
        public IActionResult Import(IFormFile file)
        {
            return _manager.Import(file, User?.Identity?.Name);
        }

        /// <summary>
        /// 更新路段
        /// </summary>
        /// <param name="roadSection">路段</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public IActionResult Update([FromBody] RoadSection roadSection)
        {
            return _manager.Update(roadSection, User?.Identity?.Name);
        }

        /// <summary>
        /// 删除路段
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{sectionId}")]
        public IActionResult Remove([FromRoute] int sectionId)
        {
            return _manager.Remove(sectionId, User?.Identity?.Name);
        }
    }
}
