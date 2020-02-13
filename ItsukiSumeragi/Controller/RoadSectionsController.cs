using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Data;
using ItsukiSumeragi.Models;
using Kakegurui.Log;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace ItsukiSumeragi.Controller
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
        /// 数据库实例
        /// </summary>
        private readonly DeviceContext _context;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<RoadSectionsController> _logger;

        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库实例</param>
        /// <param name="logger">日志</param>
        /// <param name="memoryCache">缓存</param>
        public RoadSectionsController(DeviceContext context, ILogger<RoadSectionsController> logger,IMemoryCache memoryCache)
        {
            _context = context;
            _logger = logger;
            _memoryCache = memoryCache;
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
        public PageModel<TrafficRoadSection> GetSections([FromQuery] string sectionName, [FromQuery] int sectionType, [FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            IQueryable<TrafficRoadSection> queryable = _context.RoadSections;

            if (!string.IsNullOrEmpty(sectionName))
            {
                queryable = queryable.Where(d => d.SectionName.Contains(sectionName));
            }

            if (sectionType!=0)
            {
                queryable = queryable.Where(s => s.SectionType == sectionType);
            }

            PageModel<TrafficRoadSection> sections= queryable
                .Select(s=>_memoryCache.FillSection(s))
                .Page(pageNum, pageSize);

            return sections;
        }

        /// <summary>
        /// 查询路段
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <returns>查询结果</returns>
        [HttpGet("{sectionId}")]
        public IActionResult GetSection([FromRoute] int sectionId)
        {
            TrafficRoadSection roadSection = _context.RoadSections
                .Select(s => _memoryCache.FillSection(s))
                .SingleOrDefault(c => c.SectionId == sectionId);

            if (roadSection == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(roadSection);
            }
        }

        /// <summary>
        /// 添加路段
        /// </summary>
        /// <param name="roadSection">路段</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public IActionResult PostSection([FromBody] TrafficRoadSection roadSection)
        {
            _context.RoadSections.Add(roadSection);
            _context.SaveChanges();
            _logger.LogInformation(new EventId((int)LogEvent.编辑路段, User?.Identity?.Name), $"添加路段 {roadSection}");
            return Ok(roadSection);
        }

        /// <summary>
        /// 导入路段
        /// </summary>
        /// <param name="file">文件</param>
        /// <returns>导入结果</returns>
        [HttpPost("import")]
        public async Task<IActionResult> ImportSections(IFormFile file)
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
                    if (worksheet.Dimension.Columns < 5)
                    {
                        ModelState.AddModelError("Column", "数据列少于5");
                        return BadRequest(ModelState);
                    }
                    for (int row = 2; row <= rowCount; row++)
                    {
                        if (worksheet.Cells[row, 1].Value == null)
                        {
                            ModelState.AddModelError("Value", $"{row},1 路段名称不能为空");
                            return BadRequest(ModelState);
                        }
                        if (worksheet.Cells[row, 2].Value == null
                            || !int.TryParse(worksheet.Cells[row, 2].Value.ToString(), out int direction))
                        {
                            ModelState.AddModelError("Value", $"{row},2 路段方向不能为空或数据错误");
                            return BadRequest(ModelState);
                        }
                        if (worksheet.Cells[row, 3].Value == null
                            || !int.TryParse(worksheet.Cells[row, 3].Value.ToString(), out int length))
                        {
                            ModelState.AddModelError("Value", $"{row},3 路段长度不能为空或格式不正确");
                            return BadRequest(ModelState);
                        }
                        if (worksheet.Cells[row, 4].Value == null
                            || !int.TryParse(worksheet.Cells[row, 4].Value.ToString(), out int sectionType))
                        {
                            ModelState.AddModelError("Value", $"{row},4 路段类型不能为空或格式不正确");
                            return BadRequest(ModelState);
                        }
                        if (worksheet.Cells[row, 5].Value == null
                            || !int.TryParse(worksheet.Cells[row, 5].Value.ToString(), out int speedLimit))
                        {
                            ModelState.AddModelError("Value", $"{row},5 路段限速值不能为空或格式不正确");
                            return BadRequest(ModelState);
                        }
                        _context.RoadSections.Add(
                            new TrafficRoadSection
                            {
                                SectionName = worksheet.Cells[row, 1].Value.ToString(),
                                Direction = direction,
                                Length = length,
                                SectionType = sectionType,
                                SpeedLimit = speedLimit
                            });
                    }
                    _context.SaveChanges();
                    _logger.LogInformation(new EventId((int)LogEvent.编辑路段, User?.Identity?.Name), "导入路段");
                    return Ok();
                }
            }
        }

        /// <summary>
        /// 更新路段
        /// </summary>
        /// <param name="roadSection">路段</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public IActionResult PutSection([FromBody] TrafficRoadSection roadSection)
        {

            _context.Entry(roadSection).State = EntityState.Modified;

            try
            {
                _context.SaveChanges();
                _logger.LogInformation(new EventId((int)LogEvent.编辑路段, User?.Identity?.Name), $"更新路段 {roadSection}");
                return Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (_context.RoadSections.Count(d => d.SectionId == roadSection.SectionId) == 0)
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
        /// 删除路段
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{sectionId}")]
        public IActionResult DeleteSection([FromRoute] int sectionId)
        {
            TrafficRoadSection roadSection = _context.RoadSections.SingleOrDefault(d => d.SectionId == sectionId);
            if (roadSection == null)
            {
                return NotFound();
            }
            try
            {
                _context.RoadSections.Remove(roadSection);
                _context.SaveChanges();
                _logger.LogInformation(new EventId((int)LogEvent.编辑路段, User?.Identity?.Name), $"删除路段 {roadSection}");
                return Ok();
            }
            catch (DbUpdateException)
            {
                if (_context.Channels.Count(c => c.SectionId == sectionId) > 0)
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
