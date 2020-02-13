using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Data;
using ItsukiSumeragi.Models;
using Kakegurui.Log;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using ItsukiSumeragi.Codes.Device;

namespace ItsukiSumeragi.Controller
{
    /// <summary>
    /// 通道
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ChannelsController : ControllerBase
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly DeviceContext _context;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<ChannelsController> _logger;

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
        public ChannelsController(DeviceContext context,ILogger<ChannelsController> logger,IMemoryCache memoryCache)
        {
            _context = context;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// 查询通道集合
        /// </summary>
        /// <param name="deviceType">设备类型</param>
        /// <param name="channelName">通道名称</param>
        /// <param name="crossingId">路口编号</param>
        /// <param name="sectionId">路段编号</param>
        /// <param name="alone">是否查询未关联通道</param>
        /// <param name="pageNum">分页页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        public PageModel<TrafficChannel> GetChannels([FromQuery] int deviceType, [FromQuery] string channelName, [FromQuery] int crossingId, [FromQuery] int sectionId, [FromQuery] bool alone, [FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            IQueryable<TrafficChannel> queryable = Include(_context.Channels);

            if (Enum.IsDefined(typeof(DeviceType), deviceType))
            {
                queryable = queryable.Where(c => c.Device_Channel==null||c.Device_Channel.Device.DeviceType == (DeviceType)deviceType);
            }

            if (!string.IsNullOrEmpty(channelName))
            {
                queryable = queryable.Where(c => c.ChannelName.Contains(channelName));
            }

            if (crossingId != 0)
            {
                queryable = queryable.Where(c => c.CrossingId == crossingId);
            }

            if (sectionId != 0)
            {
                queryable = queryable.Where(c => c.SectionId == sectionId);
            }

            if (alone)
            {
                queryable = queryable.Where(c => c.Device_Channel==null);
            }

            PageModel<TrafficChannel> channels=queryable
                .Select(c=>_memoryCache.FillChannel(c))
                .Page(pageNum, pageSize);

            return channels;
        }

        /// <summary>
        /// 查询通道
        /// </summary>
        /// <param name="channelId"/>通道编号/param>
        /// <returns>查询结果</returns>
        [HttpGet("{channelId}")]
        public IActionResult GetChannel([FromRoute] string channelId)
        {
            TrafficChannel channel = Include(_context.Channels)
                .SingleOrDefault(c=>c.ChannelId==channelId);

            if (channel == null)
            {
                return NotFound();
            }
            else
            {
                _memoryCache.FillChannel(channel);
                return Ok(channel);
            }

        }

        /// <summary>
        /// 查询通道关联项
        /// </summary>
        /// <param name="queryable">数据源</param>
        /// <returns>包含关联项的数据源</returns>
        private IQueryable<TrafficChannel> Include(IQueryable<TrafficChannel> queryable)
        {
            return queryable
                .Include(c => c.Device_Channel)
                    .ThenInclude(r => r.Device)
                .Include(c => c.Lanes)
                .Include(c => c.Regions)
                .Include(c => c.Shapes)
                .Include(c => c.RoadSection)
                .Include(c => c.RoadCrossing)
                .Include(c => c.TrafficLocation)
                .Include(c => c.Channel_Violations)
                    .ThenInclude(r => r.Violation)
                        .ThenInclude(v => v.Violation_Tags)
                            .ThenInclude(r=>r.Tag)
                .Include(c => c.Channel_Violations)
                    .ThenInclude(r => r.Violation)
                        .ThenInclude(v => v.Violation_Parameters)
                            .ThenInclude(r=>r.Parameter)
                .Include(c => c.Channel_ViolationParameters)
                    .ThenInclude(r => r.Parameter);
        }

        /// <summary>
        /// 按路段查询车道集合
        /// </summary>
        /// <param name="sectionName">路段名称</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet("Group/Section")]
        public PageModel<object> GetChannelsGroupBySection([FromQuery] string sectionName,[FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            IQueryable<IGrouping<int, TrafficChannel>> queryable = 
                _context.Channels
                    .Where(c=>c.SectionId.HasValue&&c.Lanes.Count>0)
                    .Include(c=>c.Device_Channel)
                        .ThenInclude(r=>r.Device)
                    .Include(c=>c.RoadSection)
                    .Include(c=>c.Lanes)
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
                    Channels = g.ToList()
                }).ToList(),
                Total = model.Total
            };

        }

        /// <summary>
        /// 添加通道
        /// </summary>
        /// <param name="channel">通道</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public IActionResult PostChannel([FromBody] TrafficChannel channel)
        {
            if (!CheckDependency(_context, channel, ModelState))
            {
                return BadRequest(ModelState);
            }

            try
            {
                AddChannel(_context,channel);
                _context.SaveChanges();
                _logger.LogInformation(new EventId((int)LogEvent.编辑通道, User?.Identity?.Name), $"添加通道 {channel}");
            }
            catch (DbUpdateException)
            {
                if (_context.Channels.Count(c => c.ChannelId == channel.ChannelId) > 0)
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }
       
            return Ok(channel);
        }

        /// <summary>
        /// 导入通道
        /// </summary>
        /// <param name="file">文件</param>
        /// <returns>导入结果</returns>
        [HttpPost("import")]
        public async Task<IActionResult> ImportChannels(IFormFile file)
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
                    if (worksheet.Dimension.Columns < 13)
                    {
                        ModelState.AddModelError("Column", "数据列少于14");
                        return BadRequest(ModelState);
                    }

                    for (int row = 2; row <= rowCount; row++)
                    {
                        if (worksheet.Cells[row, 1].Value == null)
                        {
                            ModelState.AddModelError("Value", $"{row},1 通道名称不能为空");
                            return BadRequest(ModelState);
                        }

                        if (worksheet.Cells[row, 2].Value == null)
                        {
                            ModelState.AddModelError("Value", $"{row},2 通道地址不能为空");
                            return BadRequest(ModelState);
                        }

                        if (worksheet.Cells[row, 3].Value == null
                            || !int.TryParse(worksheet.Cells[row, 3].Value.ToString(), out int channelType))
                        {
                            ModelState.AddModelError("Value", $"{row},3 通道类型不能为空或数据错误");
                            return BadRequest(ModelState);
                        }

                        int? sectionId;
                        if (worksheet.Cells[row, 4].Value == null)
                        {
                            sectionId = null;

                        }
                        else
                        {
                            if (int.TryParse(worksheet.Cells[row, 4].Value.ToString(), out int i))
                            {
                                sectionId = i;
                            }
                            else
                            {
                                ModelState.AddModelError("Value", $"{row},4 路段编号格式不正确");
                                return BadRequest(ModelState);
                            }
                        }

                        int? crossingId;
                        if (worksheet.Cells[row, 5].Value == null)
                        {
                            crossingId = null;

                        }
                        else
                        {
                            if (int.TryParse(worksheet.Cells[row, 5].Value.ToString(), out int i))
                            {
                                crossingId = i;
                            }
                            else
                            {
                                ModelState.AddModelError("Value", $"{row},5 路口编号格式不正确");
                                return BadRequest(ModelState);
                            }
                        }

                        int? rtspProtocol;
                        if (worksheet.Cells[row, 8].Value == null)
                        {
                            rtspProtocol = null;

                        }
                        else
                        {
                            if (int.TryParse(worksheet.Cells[row, 8].Value.ToString(), out int i))
                            {
                                rtspProtocol = i;
                            }
                            else
                            {
                                ModelState.AddModelError("Value", $"{row},8 Rtsp协议类型数据格式错误");
                                return BadRequest(ModelState);
                            }
                        }

                        int? locationId;
                        if (worksheet.Cells[row, 10].Value == null)
                        {
                            locationId = null;

                        }
                        else
                        {
                            if (int.TryParse(worksheet.Cells[row, 10].Value.ToString(), out int i))
                            {
                                locationId = i;
                            }
                            else
                            {
                                ModelState.AddModelError("Value", $"{row},10 地点编号格式不正确");
                                return BadRequest(ModelState);
                            }
                        }

                        int? direction;
                        if (worksheet.Cells[row, 11].Value == null)
                        {
                            direction = null;

                        }
                        else
                        {
                            if (int.TryParse(worksheet.Cells[row, 11].Value.ToString(), out int i))
                            {
                                direction = i;
                            }
                            else
                            {
                                ModelState.AddModelError("Value", $"{row},11 朝向数据格式错误");
                                return BadRequest(ModelState);
                            }
                        }

                        int? frameRate;
                        if (worksheet.Cells[row, 12].Value == null)
                        {
                            frameRate = null;

                        }
                        else
                        {
                            if (int.TryParse(worksheet.Cells[row, 12].Value.ToString(), out int i))
                            {
                                frameRate = i;
                            }
                            else
                            {
                                ModelState.AddModelError("Value", $"{row},12 帧率格式错误");
                                return BadRequest(ModelState);
                            }
                        }

                        int? speedLimit;
                        if (worksheet.Cells[row, 13].Value == null)
                        {
                            speedLimit = null;

                        }
                        else
                        {
                            if (int.TryParse(worksheet.Cells[row, 13].Value.ToString(), out int i))
                            {
                                speedLimit = i;
                            }
                            else
                            {
                                ModelState.AddModelError("Value", $"{row},13 限速值数据格式错误");
                                return BadRequest(ModelState);
                            }
                        }

                        if (crossingId.HasValue)
                        {
                            if (_context.RoadCrossings.Count(r =>r.CrossingId== crossingId.Value)==0)
                            {
                                crossingId = null;
                            }
                        }

                        if (sectionId.HasValue)
                        {
                            if (_context.RoadSections.Count(r => r.SectionId == sectionId.Value) == 0)
                            {
                                sectionId = null;
                            }
                        }

                        if (locationId.HasValue)
                        {
                            if (_context.Locations.Count(l => l.LocationId == locationId.Value) == 0)
                            {
                                locationId = null;
                            }
                        }
                        _context.Channels.Add(
                            new TrafficChannel
                            {
                                ChannelId = worksheet.Cells[row, 2].Value.ToString(),
                                ChannelName = worksheet.Cells[row, 1].Value.ToString(),
                                ChannelType =channelType,
                                SectionId = sectionId,
                                CrossingId = crossingId,
                                RtspUser = worksheet.Cells[row, 6].Value?.ToString(),
                                RtspPassword = worksheet.Cells[row, 7].Value?.ToString(),
                                RtspProtocol = rtspProtocol,
                                Location = worksheet.Cells[row, 9].Value?.ToString(),
                                Marked = !string.IsNullOrEmpty(worksheet.Cells[row, 9].Value?.ToString()),
                                LocationId = locationId,
                                Direction = direction,
                                FrameRate = frameRate,
                                SpeedLimit = speedLimit
                            });
                    }

                    try
                    {
                        _context.SaveChanges();
                        _logger.LogInformation(new EventId((int)LogEvent.编辑通道, User?.Identity?.Name), "导入通道");
                        return Ok();
                    }
                    catch (DbUpdateException)
                    {
                        StringBuilder builder = new StringBuilder();
                        for (int row = 2; row <= rowCount; row++)
                        {
                            string channelId = worksheet.Cells[row, 2].Value.ToString();
                            if (_context.Channels.Count(c => c.ChannelId == channelId) > 0)
                            {
                                builder.AppendLine(channelId);
                            }
                        }
                        if (builder.Length > 0)
                        {
                            ModelState.AddModelError("ChannelId", builder.ToString());
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

        /// <summary>
        /// 更新通道
        /// </summary>
        /// <param name="updateChannel">通道</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public IActionResult PutChannel([FromBody] TrafficChannel updateChannel)
        {
            if (!CheckDependency(_context, updateChannel, ModelState))
            {
                return BadRequest(ModelState);
            }

            if (UpdateChannel(_context, updateChannel))
            {
                _context.SaveChanges();
                _logger.LogInformation(new EventId((int)LogEvent.编辑通道, User?.Identity?.Name), $"更新通道 {updateChannel}");
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// 更新通道标注状态
        /// </summary>
        /// <param name="channelUpdateLocation">通道标注状态</param>
        /// <returns>更新结果</returns>
        [HttpPut("location")]
        public IActionResult PutChannelLocation([FromBody] TrafficChannelUpdateLocation channelUpdateLocation)
        {
            TrafficChannel channel = _context.Channels.SingleOrDefault(d => d.ChannelId == channelUpdateLocation.ChannelId);
            if (channel == null)
            {
                return NotFound();
            }
   
            channel.Location = channelUpdateLocation.Location;
            channel.Marked = true;
            _context.Channels.Update(channel);
            _context.SaveChanges();
            return Ok();
        }

        /// <summary>
        /// 更新通道状态
        /// </summary>
        /// <param name="channelUpdateStatus">通道状态</param>
        /// <returns>更新结果</returns>
        [HttpPut("status")]
        public IActionResult PutChannelStatus([FromBody] TrafficChannelUpdateStatus channelUpdateStatus)
        {
            TrafficChannel channel = _context.Channels.SingleOrDefault(c => c.ChannelId == channelUpdateStatus.ChannelId);
            if (channel == null)
            {
                return NotFound();
            }
            channel.ChannelStatus = channelUpdateStatus.ChannelStatus;
            _context.Channels.Update(channel);
            _context.SaveChanges();
            return Ok();
        }

        /// <summary>
        /// 更新通道违法参数状态
        /// </summary>
        /// <param name="channelUpdateParameter">通道参数</param>
        /// <returns>更新结果</returns>
        [HttpPut("parameter")]
        public IActionResult PutChannelParameter([FromBody] TrafficChannelUpdateParameter channelUpdateParameter)
        {
            TrafficChannel channel = _context.Channels
                .Include(c=>c.Channel_Violations)
                    .ThenInclude(r=>r.Violation)
                        .ThenInclude(v=>v.Violation_Parameters)
                .Include(c => c.Channel_ViolationParameters)
                .SingleOrDefault(c => c.ChannelId == channelUpdateParameter.ChannelId);
            if (channel == null)
            {
                return NotFound();
            }

            if (channelUpdateParameter.Channel_ViolationParameters == null)
            {
                channelUpdateParameter.Channel_ViolationParameters =
                    new List<TrafficChannel_TrafficViolationParameter>();
            }

            foreach (var relation in channelUpdateParameter.Channel_ViolationParameters.ToList())
            {
                TrafficChannel_TrafficViolation cv = channel.Channel_Violations
                    .SingleOrDefault(r => r.ViolationId == relation.ViolationId);
                if (cv == null)
                {
                    ModelState.AddModelError("Violation", $"通道未关联该违法行为{relation.ViolationId}");
                    return BadRequest(ModelState);
                }
                else
                {
                    TrafficViolation_TrafficViolationParameter vp =
                        cv.Violation.Violation_Parameters.SingleOrDefault(r => r.Key == relation.Key);

                    if (vp==null)
                    {
                        ModelState.AddModelError("Parameter", $"违法行为下没有该参数 {relation.Key}");
                        return BadRequest(ModelState);
                    }
                    else
                    {
                        if (relation.Value == vp.Value)
                        {
                            channelUpdateParameter.Channel_ViolationParameters.Remove(relation);
                        }
                    }
                }
            }

            channel.Channel_ViolationParameters = channelUpdateParameter.Channel_ViolationParameters;
            _context.Channels.Update(channel);
            _context.SaveChanges();
            return Ok();
        }

        /// <summary>
        /// 删除通道
        /// </summary>
        /// <param name="channelId">通道编号</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{channelId}")]
        public IActionResult DeleteChannel([FromRoute]string channelId)
        {
            channelId= Uri.UnescapeDataString(channelId);
            TrafficChannel channel = _context.Channels.SingleOrDefault(c => c.ChannelId == channelId);
            if (channel == null)
            {
                return NotFound();
            }
            _context.Channels.Remove(channel);
            _context.SaveChanges();
            _logger.LogInformation(new EventId((int)LogEvent.编辑通道, User?.Identity?.Name), $"删除通道 {channel}");
            return Ok(channel);
        }

        /// <summary>
        /// 检查通道依赖项
        /// </summary>
        /// <param name="deviceContext">数据库实例</param>
        /// <param name="channel">通道</param>
        /// <param name="modelState">数据状态</param>
        /// <returns>是否通过检查</returns>
        public static bool CheckDependency(DeviceContext deviceContext, TrafficChannel channel, ModelStateDictionary modelState)
        {
            if (channel.ChannelDeviceId!=null&&deviceContext.Channels.Count(c =>c.ChannelId!=channel.ChannelId && c.ChannelDeviceId == channel.ChannelDeviceId) > 0)
            {
                modelState.AddModelError("ChannelDeviceId", "通道设备编号重复");
                return false;
            }

            if (channel.CrossingId.HasValue)
            {
                if (deviceContext.RoadCrossings.Count(d => d.CrossingId == channel.CrossingId) == 0)
                {
                    modelState.AddModelError("Crossing", $"不存在路口编号 {channel.CrossingId}");
                    return false;
                }
            }

            if (channel.SectionId.HasValue)
            {
                if (deviceContext.RoadSections.Count(d => d.SectionId == channel.SectionId) == 0)
                {
                    modelState.AddModelError("Section", $"不存在路段编号 {channel.SectionId}");
                    return false;
                }
            }

            if (channel.LocationId.HasValue)
            {
                if (deviceContext.Locations.Count(l => l.LocationId == channel.LocationId) == 0)
                {
                    modelState.AddModelError("Location", $"不存在地点编号 {channel.LocationId}");
                    return false;
                }
            }

            if (channel.Channel_Violations!=null)
            {
                foreach (var relation in channel.Channel_Violations)
                {
                    if (deviceContext.Violations.Count(v => v.ViolationId == relation.ViolationId) == 0)
                    {
                        modelState.AddModelError("Violation", $"不存在违法行为编号 {relation.ViolationId}");
                        return false;
                    }
                }
            }

            return true;
        }

        public static void AddChannel(DeviceContext deviceContext, TrafficChannel newChannel)
        {
            newChannel.ChannelStatus = (int)DeviceStatus.异常;
            newChannel.RoadSection = null;
            newChannel.RoadCrossing = null;
            newChannel.TrafficLocation = null;
            deviceContext.Channels.Add(newChannel);

        }

        /// <summary>
        /// 更新通道
        /// </summary>
        /// <param name="deviceContext">数据库实例</param>
        /// <param name="updateChannel">更新的通道</param>
        /// <returns>返回是否存在要更新的通道</returns>
        public static bool UpdateChannel(DeviceContext deviceContext, TrafficChannel updateChannel)
        {
            TrafficChannel channel = deviceContext.Channels
                .Include(c => c.Lanes)
                .Include(c => c.Regions)
                .Include(c => c.Shapes)
                .Include(c => c.Channel_Violations)
                .Include(c => c.Channel_ViolationParameters)
                .SingleOrDefault(c => c.ChannelId == updateChannel.ChannelId);
            if (channel == null)
            {
                return false;
            }
            channel.ChannelName = updateChannel.ChannelName;
            channel.ChannelType = updateChannel.ChannelType;
            channel.ChannelIndex = updateChannel.ChannelIndex;
            channel.ChannelDeviceId = updateChannel.ChannelDeviceId;
            channel.ChannelDeviceType = updateChannel.ChannelDeviceType;
            channel.RtspUser = updateChannel.RtspUser;
            channel.RtspPassword = updateChannel.RtspPassword;
            channel.RtspProtocol = updateChannel.RtspProtocol;
            channel.IsLoop = updateChannel.IsLoop;
            channel.FrameRate = updateChannel.FrameRate;
            channel.SpeedLimit = updateChannel.SpeedLimit;
            channel.Direction = updateChannel.Direction;
            channel.RecordNumber = updateChannel.RecordNumber;
            channel.SectionId = updateChannel.SectionId;
            channel.RoadSection = null;
            channel.CrossingId = updateChannel.CrossingId;
            channel.RoadCrossing = null;
            channel.LocationId = updateChannel.LocationId;
            channel.TrafficLocation = null;
            channel.Lanes = updateChannel.Lanes;
            channel.Regions = updateChannel.Regions;
            channel.Shapes = updateChannel.Shapes;
            channel.Channel_Violations = updateChannel.Channel_Violations;
           
            if (channel.Channel_Violations == null)
            {
                channel.Channel_ViolationParameters = null;
            }
            else
            {
                foreach (var cv in channel.Channel_Violations)
                {
                    cv.Violation = null;
                }
                channel.Channel_ViolationParameters?.RemoveAll(cp =>
                    channel.Channel_Violations.All(cv => cp.ViolationId != cv.ViolationId));
            }

            if (channel.Channel_ViolationParameters != null)
            {
                foreach (var cvp in channel.Channel_ViolationParameters)
                {
                    cvp.Parameter = null;
                }
            }
            deviceContext.Channels.Update(channel);
            return true;
        }
    }
}
