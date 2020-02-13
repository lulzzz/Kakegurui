using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ItsukiSumeragi.Data;
using ItsukiSumeragi.Models;
using Kakegurui.Log;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace ItsukiSumeragi.Controller
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
        /// 数据库实例
        /// </summary>
        private readonly DeviceContext _context;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<RoadCrossingsController> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库实例</param>
        /// <param name="logger">日志</param>
        public RoadCrossingsController(DeviceContext context, ILogger<RoadCrossingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 查询路口集合
        /// </summary>
        /// <param name="crossingName">路口名称</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        public PageModel<TrafficRoadCrossing> GetCrossings([FromQuery] string crossingName, [FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            IQueryable<TrafficRoadCrossing> queryable = _context.RoadCrossings;

            if (!string.IsNullOrEmpty(crossingName))
            {
                queryable = queryable.Where(d => d.CrossingName.Contains(crossingName));
            }

            return queryable.Page(pageNum, pageSize);

        }

        /// <summary>
        /// 查询路口
        /// </summary>
        /// <param name="crossingId">路口编号</param>
        /// <returns>查询结果</returns>
        [HttpGet("{crossingId}")]
        public IActionResult GetCrossing([FromRoute] int crossingId)
        {
            TrafficRoadCrossing roadCrossing = _context.RoadCrossings.SingleOrDefault(c => c.CrossingId == crossingId);

            if (roadCrossing == null)
            {
                return NotFound();
            }

            return Ok(roadCrossing);
        }

        /// <summary>
        /// 添加路口
        /// </summary>
        /// <param name="roadCrossing">路口</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public IActionResult PostCrossing([FromBody] TrafficRoadCrossing roadCrossing)
        {
            _context.RoadCrossings.Add(roadCrossing);
            _context.SaveChanges();
            _logger.LogInformation(new EventId((int)LogEvent.编辑路口, User?.Identity?.Name), $"添加路口 {roadCrossing}");
            return Ok(roadCrossing);
        }

        /// <summary>
        /// 导入路口
        /// </summary>
        /// <param name="file">文件</param>
        /// <returns>导入结果</returns>
        [HttpPost("import")]
        public async Task<IActionResult> ImportCrossings(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("File", "文件为空");
                return BadRequest(ModelState);
            }
            else
            {
                var filePath = Path.GetTempFileName();
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                FileInfo fileinfo = new FileInfo(filePath);
                using (ExcelPackage package = new ExcelPackage(fileinfo))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        ModelState.AddModelError("Sheet", "数据页少于1");
                        return BadRequest(ModelState);
                    }
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;
                    if (worksheet.Dimension.Columns < 1)
                    {
                        ModelState.AddModelError("Column", "数据列少于1");
                        return BadRequest(ModelState);
                    }
                    for (int row = 2; row <= rowCount; row++)
                    {
                        if (worksheet.Cells[row, 1].Value == null)
                        {
                            ModelState.AddModelError("Value", $"{row},1 路口名称不能为空");
                            return BadRequest(ModelState);
                        }
                       
                        _context.RoadCrossings.Add(new TrafficRoadCrossing { CrossingName = worksheet.Cells[row, 1].Value.ToString() });
                    }
                    _context.SaveChanges();
                    _logger.LogInformation(new EventId((int)LogEvent.编辑路口, User?.Identity?.Name), "导入路口");
                    return Ok();
                }
            }
        }

        /// <summary>
        /// 更新路口
        /// </summary>
        /// <param name="roadCrossing">路口</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public IActionResult PutCrossing([FromBody] TrafficRoadCrossing roadCrossing)
        {
            _context.Entry(roadCrossing).State = EntityState.Modified;

            try
            {
                _context.SaveChanges();
                _logger.LogInformation(new EventId((int)LogEvent.编辑路口, User?.Identity?.Name), $"更新路口 {roadCrossing}");
                return Ok();
            }
            catch (DbUpdateException)
            {
                if (_context.RoadCrossings.Count(d => d.CrossingId == roadCrossing.CrossingId) == 0)
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// 删除路口
        /// </summary>
        /// <param name="crossingId">路口编号</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{crossingId}")]
        public IActionResult DeleteCrossing([FromRoute] int crossingId)
        {
            TrafficRoadCrossing roadCrossing = _context.RoadCrossings.SingleOrDefault(d => d.CrossingId == crossingId);
            if (roadCrossing == null)
            {
                return NotFound();
            }

            try
            {
                _context.RoadCrossings.Remove(roadCrossing);
                _context.SaveChanges();
                _logger.LogInformation(new EventId((int)LogEvent.编辑路口, User?.Identity?.Name), $"删除路口 {roadCrossing}");
                return Ok(roadCrossing);
            }
            catch (DbUpdateException)
            {
                if (_context.Channels.Count(c => c.CrossingId == crossingId) > 0)
                {
                    ModelState.AddModelError("Channel", "存在关联的通道");
                    return BadRequest(ModelState);
                }
                else
                {
                    throw;
                }
            }

        }
    }
}
