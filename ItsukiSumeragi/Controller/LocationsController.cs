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
    /// 地点
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController:ControllerBase
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly DeviceContext _context;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<LocationsController> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库实例</param>
        /// <param name="logger">日志</param>
        public LocationsController(DeviceContext context, ILogger<LocationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 查询地点集合
        /// </summary>
        /// <param name="locationCode">地点编号</param>
        /// <param name="locationName">地点名称</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        public PageModel<TrafficLocation> GetLocations([FromQuery] string locationCode, [FromQuery] string locationName, [FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            IQueryable<TrafficLocation> queryable = _context.Locations;

            if (!string.IsNullOrEmpty(locationCode))
            {
                queryable = queryable.Where(d => d.LocationCode.Contains(locationCode));
            }

            if (!string.IsNullOrEmpty(locationName))
            {
                queryable = queryable.Where(d => d.LocationName.Contains(locationName));
            }

            return queryable.Page(pageNum, pageSize);

        }

        /// <summary>
        /// 查询地点
        /// </summary>
        /// <param name="locationId">地点编号</param>
        /// <returns>查询结果</returns>
        [HttpGet("{locationId}")]
        public IActionResult GetLocation([FromRoute] int locationId)
        {
            TrafficLocation location = _context.Locations.SingleOrDefault(c => c.LocationId == locationId);

            if (location == null)
            {
                return NotFound();
            }

            return Ok(location);
        }

        /// <summary>
        /// 添加地点
        /// </summary>
        /// <param name="location">地点</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public IActionResult PostLocation([FromBody] TrafficLocation location)
        {
            try
            {
                _context.Locations.Add(location);
                _context.SaveChanges();
                _logger.LogInformation(new EventId((int)LogEvent.编辑地点, User?.Identity?.Name), $"添加地点 {location}");
                return Ok(location);
            }
            catch (DbUpdateException)
            {
                if (_context.Locations.Count(l => l.LocationCode == location.LocationCode) > 0)
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }
    
        }

        /// <summary>
        /// 导入地点
        /// </summary>
        /// <param name="file">文件</param>
        /// <returns>导入结果</returns>
        [HttpPost("import")]
        public async Task<IActionResult> ImportLocations(IFormFile file)
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
                    if (worksheet.Dimension.Columns < 2)
                    {
                        ModelState.AddModelError("Column", "数据列少于2");
                        return BadRequest(ModelState);
                    }
                    for (int row = 2; row <= rowCount; row++)
                    {
                        if (worksheet.Cells[row, 1].Value == null)
                        {
                            ModelState.AddModelError("Value", $"{row},1 地点编号不能为空");
                            return BadRequest(ModelState);
                        }

                        if (worksheet.Cells[row, 2].Value == null)
                        {
                            ModelState.AddModelError("Value", $"{row},2 地点名称不能为空");
                            return BadRequest(ModelState);
                        }

                        string locationCode = worksheet.Cells[row, 1].Value.ToString();

                        TrafficLocation location =
                            _context.Locations.SingleOrDefault(l => l.LocationCode == locationCode);
                        if (location == null)
                        {
                            _context.Locations.Add(new TrafficLocation
                            {
                                LocationCode = locationCode,
                                LocationName = worksheet.Cells[row, 2].Value.ToString()
                            });
                        }
                        else
                        {
                            location.LocationName= worksheet.Cells[row, 2].Value.ToString();
                            _context.Locations.Update(location);
                        }

                    }
                    _context.SaveChanges();
                    _logger.LogInformation(new EventId((int)LogEvent.编辑地点, User?.Identity?.Name), "导入地点");
                    return Ok();
                }
            }
        }

        /// <summary>
        /// 更新地点
        /// </summary>
        /// <param name="location">地点</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public IActionResult PutLocation([FromBody] TrafficLocation location)
        {
            _context.Entry(location).State = EntityState.Modified;

            try
            {
                _context.SaveChanges();
                _logger.LogInformation(new EventId((int)LogEvent.编辑地点, User?.Identity?.Name), $"更新地点 {location}");
                return Ok();
            }
            catch (DbUpdateException)
            {
                if (_context.Locations.Count(l => l.LocationId == location.LocationId) == 0)
                {
                    return NotFound();
                }
                else if (_context.Locations.Count(l => l.LocationId != location.LocationId && l.LocationCode == location.LocationCode) > 0)
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// 删除地点
        /// </summary>
        /// <param name="locationId">地点编号</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{locationId}")]
        public IActionResult DeleteLocation([FromRoute] int locationId)
        {
            TrafficLocation location = _context.Locations.SingleOrDefault(d => d.LocationId == locationId);
            if (location == null)
            {
                return NotFound();
            }

            try
            {
                _context.Locations.Remove(location);
                _context.SaveChanges();
                _logger.LogInformation(new EventId((int)LogEvent.编辑地点, User?.Identity?.Name), $"删除地点 {location}");
                return Ok(location);
            }
            catch (DbUpdateException)
            {
                if (_context.Channels.Count(c => c.LocationId == locationId) > 0)
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
