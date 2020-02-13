using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
using ItsukiSumeragi.Codes.Device;

namespace ItsukiSumeragi.Controller
{
    /// <summary>
    /// 设备
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly DeviceContext _context;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<DevicesController> _logger;

        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// 数据库实例
        /// </summary>
        /// <param name="context">数据库实例</param>
        /// <param name="logger">日志</param>
        /// <param name="memoryCache">缓存</param>
        public DevicesController(DeviceContext context,ILogger<DevicesController> logger,IMemoryCache memoryCache)
        {
            _context = context;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// 查询流量设备集合
        /// </summary>
        /// <param name="deviceType">设备类型</param>
        /// <param name="deviceName">设备名称</param>
        /// <param name="deviceModel">设备型号</param>
        /// <param name="deviceStatus">设备状态</param>
        /// <param name="ip">设备ip</param>
        /// <param name="nodeUrl">所属节点</param>
        /// <param name="order">排序方式</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        public PageModel<TrafficDevice> GetDevices([FromQuery] int deviceType, [FromQuery] string deviceName, [FromQuery] int deviceModel, [FromQuery] int deviceStatus, [FromQuery] string ip, [FromQuery] string nodeUrl, [FromQuery]string order, [FromQuery] int pageNum,[FromQuery] int pageSize)
        {
            IQueryable<TrafficDevice> queryable = Include(_context.Devices);

            if (Enum.IsDefined(typeof(DeviceType), deviceType))
            {
                queryable = queryable.Where(d => d.DeviceType == (DeviceType)deviceType);
            }

            if (!string.IsNullOrEmpty(deviceName))
            {
                queryable = queryable.Where(d => d.DeviceName.Contains(deviceName));
            }

            if (deviceModel!=0)
            {
                queryable = queryable.Where(d => d.DeviceModel == deviceModel);
            }

            if (deviceStatus!=0)
            {
                queryable = queryable.Where(d => d.DeviceStatus == deviceStatus);
            }

            if (!string.IsNullOrEmpty(ip))
            {
                queryable = queryable.Where(d => d.Ip.Contains(ip));
            }

            if (!string.IsNullOrEmpty(nodeUrl))
            {
                queryable = queryable.Where(d => d.NodeUrl==nodeUrl);
            }

            PageModel<TrafficDevice> model = queryable
                .Select(d=>_memoryCache.FillDevice(d))
                .Select(d=>OrderInclude(d,order))
                .Page(pageNum, pageSize);

            return model;
        }

        /// <summary>
        /// 查询流量设备
        /// </summary>
        /// <param name="deviceId">设备编号</param>
        /// <returns>查询结果</returns>
        [HttpGet("{deviceId}")]
        public IActionResult GetDevice([FromRoute] int deviceId)
        {
            TrafficDevice device = Include(_context.Devices)
                .SingleOrDefault(c => c.DeviceId == deviceId);

            if (device == null)
            {
                return NotFound();
            }
            else
            {    _memoryCache.FillDevice(device);
                OrderInclude(device,null);
                return Ok(device);
            }
        }

        /// <summary>
        /// 查询设备关联项
        /// </summary>
        /// <param name="queryable">数据源</param>
        /// <returns>包含关联项的数据源</returns>
        private IQueryable<TrafficDevice> Include(IQueryable<TrafficDevice> queryable)
        {
            return queryable
                .Include(d => d.Device_Channels)
                    .ThenInclude(r => r.Channel)
                        .ThenInclude(c => c.Lanes)
                .Include(d => d.Device_Channels)
                    .ThenInclude(r => r.Channel)
                        .ThenInclude(c => c.Regions)
                .Include(d => d.Device_Channels)
                    .ThenInclude(r => r.Channel)
                        .ThenInclude(c => c.Shapes)
                .Include(d => d.Device_Channels)
                    .ThenInclude(r => r.Channel)
                        .ThenInclude(c => c.RoadSection)
                .Include(d => d.Device_Channels)
                    .ThenInclude(r => r.Channel)
                        .ThenInclude(c => c.RoadCrossing)
                .Include(d => d.Device_Channels)
                    .ThenInclude(r => r.Channel)
                        .ThenInclude(c => c.TrafficLocation)
                .Include(d => d.Device_Channels)
                    .ThenInclude(r => r.Channel)
                        .ThenInclude(c => c.Channel_Violations)
                            .ThenInclude(r => r.Violation)
                                .ThenInclude(v => v.Violation_Tags)
                                    .ThenInclude(r=>r.Tag)
                .Include(d => d.Device_Channels)
                    .ThenInclude(r => r.Channel)
                        .ThenInclude(c => c.Channel_Violations)
                            .ThenInclude(r => r.Violation)
                                .ThenInclude(v => v.Violation_Parameters)
                                    .ThenInclude(r => r.Parameter)
                .Include(d => d.Device_Channels)
                    .ThenInclude(r => r.Channel)
                        .ThenInclude(c => c.Channel_ViolationParameters)
                            .ThenInclude(r => r.Parameter);
        }

        /// <summary>
        /// 对设备包含的子项排序
        /// </summary>
        /// <param name="device">设备</param>
        /// <param name="order">排序方式</param>
        /// <returns>设备</returns>
        private TrafficDevice OrderInclude(TrafficDevice device,string order)
        {
            if (order == "status")
            {
                device.Device_Channels =
                    device.Device_Channels
                        .OrderBy(r => r.Channel.ChannelStatus)
                        .ThenBy(r => r.Channel.ChannelIndex)
                        .ToList();
            }
            else
            {
                device.Device_Channels =
                    device.Device_Channels
                        .OrderBy(c => c.Channel.ChannelIndex)
                        .ToList();
            }

            foreach (var relation in device.Device_Channels)
            {
                _memoryCache.FillChannel(relation.Channel);
                if (relation.Channel.RoadSection != null)
                {
                    _memoryCache.FillSection(relation.Channel.RoadSection);
                }

                relation.Channel.Lanes =
                    relation.Channel.Lanes
                        .OrderBy(l => l.LaneIndex)
                        .Select(l=>_memoryCache.FillLane(l))
                        .ToList();
                relation.Channel.Regions =
                    relation.Channel.Regions
                        .OrderBy(r => r.RegionIndex)
                        .ToList();

                foreach (var cv in relation.Channel.Channel_Violations)
                {
                    cv.Violation.Violation_Tags =
                        cv.Violation.Violation_Tags
                            .OrderBy(vt => vt.Tag.Color)
                            .ToList();
                }
            }

            return device;
        }

        /// <summary>
        /// 添加设备
        /// </summary>
        /// <param name="deviceInsert">设备信息</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public IActionResult PostDevice([FromBody] TrafficDeviceInsert deviceInsert)
        {
            TrafficDevice device = new TrafficDevice
            {
                DeviceId = 0,
                DeviceName = deviceInsert.DeviceName,
                DeviceType = deviceInsert.DeviceType,
                DeviceModel = deviceInsert.DeviceModel,
                Ip = deviceInsert.Ip,
                Port = deviceInsert.Port,
                DataPort = deviceInsert.DataPort
            };

            try
            {
                if (UpdateChannels(device, deviceInsert.Channels))
                {
                    _context.Devices.Add(device);
                    _context.SaveChanges();
                    _logger.LogInformation(new EventId((int)LogEvent.编辑设备, User?.Identity?.Name), $"添加设备 {device}");
                    return Ok(device);
                }
                else
                {
                    return BadRequest(ModelState);
                }
            }
            catch (DbUpdateException)
            {
                if (_context.Devices.Count(d => d.Ip == device.Ip) > 0)
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
        /// 导入设备
        /// </summary>
        /// <param name="file">文件</param>
        /// <returns>导入结果</returns>
        [HttpPost("import")]
        public async Task<IActionResult> ImportDevices(IFormFile file)
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
                    if (worksheet.Dimension.Columns < 7)
                    {
                        ModelState.AddModelError("Column", "数据列少于7");
                        return BadRequest(ModelState);
                    }
                    for (int row = 2; row <= rowCount; row++)
                    {
                        if (worksheet.Cells[row, 1].Value == null)
                        {
                            ModelState.AddModelError("Value", $"{row},1 设备名称不能为空");
                            return BadRequest(ModelState);
                        }
                        if (worksheet.Cells[row, 2].Value == null
                            || !int.TryParse(worksheet.Cells[row, 2].Value.ToString(), out int deviceType)
                            || !Enum.IsDefined(typeof(DeviceType), deviceType))
                        {
                            ModelState.AddModelError("Value", $"{row},2 设备类型不能为空或数据错误");
                            return BadRequest(ModelState);
                        }
                        if (worksheet.Cells[row, 3].Value == null
                            || !int.TryParse(worksheet.Cells[row, 3].Value.ToString(), out int deviceModel))
                        {
                            ModelState.AddModelError("Value", $"{row},3 设备型号不能为空或数据错误");
                            return BadRequest(ModelState);
                        }
                        if (worksheet.Cells[row, 4].Value == null || !IPAddress.TryParse(worksheet.Cells[row, 4].Value.ToString(),out var ip))
                        {
                            ModelState.AddModelError("Value", $"{row},4 设备不能为空或格式不正确");
                            return BadRequest(ModelState);
                        }
                        if (worksheet.Cells[row, 5].Value == null
                            || !int.TryParse(worksheet.Cells[row, 5].Value.ToString(),out int port)
                            || port<1
                            || port>65525)
                        {
                            ModelState.AddModelError("Value", $"{row},5 设备端口不能为空或格式不正确");
                            return BadRequest(ModelState);
                        }
                        if (worksheet.Cells[row, 6].Value == null
                            || !int.TryParse(worksheet.Cells[row, 6].Value.ToString(), out int dataPort)
                            || dataPort < 1 
                            || dataPort > 65525)
                        {
                            ModelState.AddModelError("Value", $"{row},6 设备数据端口不能为空或格式不正确");
                            return BadRequest(ModelState);
                        }
                        _context.Devices.Add(
                            new TrafficDevice
                            {
                                DeviceName = worksheet.Cells[row, 1].Value.ToString(),
                                DeviceType = (DeviceType)deviceType,
                                DeviceModel = deviceModel,
                                Ip = ip.ToString(),
                                Port = port,
                                DataPort = dataPort,
                                Location = worksheet.Cells[row, 7].Value?.ToString(),
                                Marked = !string.IsNullOrEmpty(worksheet.Cells[row, 7].Value?.ToString())
                            });
                    }

                    try
                    {
                        _context.SaveChanges();
                        _logger.LogInformation(new EventId((int)LogEvent.编辑设备,User?.Identity?.Name), "导入设备");
                        return Ok();
                    }
                    catch (DbUpdateException)
                    {
                        StringBuilder builder = new StringBuilder();
                        for (int row = 2; row <= rowCount; row++)
                        {
                            string ip = worksheet.Cells[row, 4].Value.ToString();
                            if (_context.Devices.Count(d => d.Ip == ip)>0)
                            {
                                builder.AppendLine(ip);
                            }
                        }
                        if (builder.Length > 0)
                        {
                            ModelState.AddModelError("Ip", builder.ToString());
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
        /// 更新设备
        /// </summary>
        /// <param name="deviceUpdate">设备信息</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public IActionResult PutDevice([FromBody] TrafficDeviceUpdate deviceUpdate)
        {
            TrafficDevice device = _context.Devices
                .Include(d=>d.Device_Channels)
                .SingleOrDefault(d => d.DeviceId == deviceUpdate.DeviceId);
            if (device == null)
            {
                return NotFound();
            }

            device.DeviceName = deviceUpdate.DeviceName;
            device.DeviceModel = deviceUpdate.DeviceModel;
            device.Ip = deviceUpdate.Ip;
            device.Port = deviceUpdate.Port;
            device.DataPort = deviceUpdate.DataPort;

            try
            {
                if (UpdateChannels(device, deviceUpdate.Channels))
                {
                    _context.Devices.Update(device);
                    _context.SaveChanges();
                    _logger.LogInformation(new EventId((int)LogEvent.编辑设备, User?.Identity?.Name), $"更新设备 {device}");
                    return Ok(device);
                }
                else
                {
                    return BadRequest(ModelState);
                }
            }
            catch (DbUpdateException)
            {
                if (_context.Devices.Count(d => d.DeviceId != device.DeviceId && d.Ip == device.Ip) > 0)
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
        /// 更新设备下通道集合
        /// </summary>
        /// <param name="device">设备</param>
        /// <param name="channels">通道集合</param>
        /// <returns></returns>
        private bool UpdateChannels(TrafficDevice device,List<TrafficChannel> channels)
        {
            if (channels == null)
            {
                channels = new List<TrafficChannel>();
            }

            List<int> indexes = channels.Select(c => c.ChannelIndex).Distinct().ToList();
            if (indexes.Count < channels.Count)
            {
                ModelState.AddModelError("ChannelIndexDuplicate", "通道序号重复");
                return false;
            }

            if (channels.Count > 0)
            {
                int minIndex = channels.Min(c => c.ChannelIndex);
                if (minIndex <= 0)
                {
                    ModelState.AddModelError("ChannelIndexError", "通道序号应该大于0");
                    return false;
                }
            }

            device.Device_Channels = new List<TrafficDevice_TrafficChannel>();
            foreach (var newChannel in channels)
            {
                if (!ChannelsController.CheckDependency(_context, newChannel, ModelState))
                {
                    return false;
                }
                TrafficDevice_TrafficChannel relation = new TrafficDevice_TrafficChannel
                {
                    DeviceId = device.DeviceId,
                    ChannelId = newChannel.ChannelId
                };
                device.Device_Channels.Add(relation);
                if (ChannelsController.UpdateChannel(_context, newChannel))
                {
                    if (_context.Device_Channels.Count(dc =>
                            dc.ChannelId == newChannel.ChannelId && dc.DeviceId != device.DeviceId) != 0)
                    {
                        ModelState.AddModelError("Relation", $"通道 {newChannel.ChannelId} 已经关联在其他设备");
                        return false;
                    }
                }
                else
                {
                   ChannelsController.AddChannel(_context,newChannel);
                }
            }
            return true;
        }

        /// <summary>
        /// 更新设备标注状态
        /// </summary>
        /// <param name="deviceUpdateLocation">设备标注状态</param>
        /// <returns>更新结果</returns>
        [HttpPut("location")]
        public IActionResult PutDeviceLocation([FromBody] TrafficDeviceUpdateLocation deviceUpdateLocation)
        {
            TrafficDevice device = _context.Devices.SingleOrDefault(d => d.DeviceId == deviceUpdateLocation.DeviceId);
            if (device == null)
            {
                return NotFound();
            }
            device.Location = deviceUpdateLocation.Location;
            device.Marked = true;
            _context.Devices.Update(device);
            _context.SaveChanges();
            return Ok();
        }

        /// <summary>
        /// 更新设备状态
        /// </summary>
        /// <param name="deviceUpdateStatus">设备状态</param>
        /// <returns>更新结果</returns>
        [HttpPut("status")]
        public IActionResult PutDeviceStatus([FromBody] TrafficDeviceUpdateStatus deviceUpdateStatus)
        {
            TrafficDevice device = _context.Devices.SingleOrDefault(d => d.DeviceId == deviceUpdateStatus.DeviceId);
            if (device == null)
            {
                return NotFound();
            }
            device.DeviceStatus = deviceUpdateStatus.DeviceStatus;
            device.License = deviceUpdateStatus.License;
            device.Runtime = deviceUpdateStatus.Runtime;
            device.Systime = deviceUpdateStatus.Systime;
            device.Cpu = deviceUpdateStatus.Cpu;
            device.Memory = deviceUpdateStatus.Memory;
            device.Space = deviceUpdateStatus.Space;

            _context.Devices.Update(device);
            _context.SaveChanges();
            return Ok();
        }

        /// <summary>
        /// 删除流量设备
        /// </summary>
        /// <param name="deviceId">设备编号</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{deviceId}")]
        public IActionResult DeleteDevice([FromRoute] int deviceId)
        {
            TrafficDevice device = _context.Devices.SingleOrDefault(d => d.DeviceId== deviceId);
            if (device == null)
            {
                return NotFound();
            }

            _context.Devices.Remove(device);
            _context.SaveChanges();
            _logger.LogInformation(new EventId((int)LogEvent.编辑设备,User?.Identity?.Name), $"删除设备 {device}");
            return Ok(device);
        }
    }
}