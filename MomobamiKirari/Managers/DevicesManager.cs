using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using ItsukiSumeragi.Codes;
using Kakegurui.Log;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MomobamiKirari.Cache;
using MomobamiKirari.Data;
using MomobamiKirari.Models;
using OfficeOpenXml;
namespace MomobamiKirari.Managers
{
    /// <summary>
    /// 设备数据库操作
    /// </summary>
    public class DevicesManager
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly FlowContext _context;

        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<DevicesManager> _logger;

        /// <summary>
        /// 数据库实例
        /// </summary>
        /// <param name="context">数据库实例</param>
        /// <param name="memoryCache">缓存</param>
        /// <param name="logger">日志</param>
        public DevicesManager(FlowContext context, IMemoryCache memoryCache, ILogger<DevicesManager> logger)
        {
            _context = context;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        /// <summary>
        /// 查询流量设备集合
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <param name="deviceModel">设备型号</param>
        /// <param name="deviceStatus">设备状态</param>
        /// <param name="ip">设备ip</param>
        /// <param name="nodeUrl">所属节点</param>
        /// <param name="order">排序方式</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        public PageModel<FlowDevice> GetList(string deviceName, int deviceModel, int deviceStatus, string ip, string nodeUrl, string order, int pageNum, int pageSize)
        {
            IQueryable<FlowDevice> queryable = Include(_context.Devices);

            if (!string.IsNullOrEmpty(deviceName))
            {
                queryable = queryable.Where(d => d.DeviceName.Contains(deviceName));
            }

            if (deviceModel != 0)
            {
                queryable = queryable.Where(d => d.DeviceModel == deviceModel);
            }

            if (deviceStatus != 0)
            {
                queryable = queryable.Where(d => d.DeviceStatus == deviceStatus);
            }

            if (!string.IsNullOrEmpty(ip))
            {
                queryable = queryable.Where(d => d.Ip.Contains(ip));
            }

            if (!string.IsNullOrEmpty(nodeUrl))
            {
                queryable = queryable.Where(d => d.NodeUrl == nodeUrl);
            }

            PageModel<FlowDevice> model = queryable
                .Select(d => _memoryCache.FillDevice(d))
                .Select(d => OrderInclude(d, order))
                .Page(pageNum, pageSize);

            return model;
        }

        /// <summary>
        /// 查询流量设备
        /// </summary>
        /// <param name="deviceId">设备编号</param>
        /// <returns>查询结果</returns>
        public IStatusCodeActionResult Get(int deviceId)
        {
            FlowDevice device = Include(_context.Devices)
                .SingleOrDefault(c => c.DeviceId == deviceId);

            if (device == null)
            {
                return new NotFoundResult();
            }
            else
            {
                _memoryCache.FillDevice(device);
                OrderInclude(device, null);
                return new OkObjectResult(device);
            }
        }

        /// <summary>
        /// 添加设备
        /// </summary>
        /// <param name="deviceInsert">设备信息</param>
        /// <param name="userName">用户名</param>
        /// <returns>添加结果</returns>
        public ObjectResult Add(FlowDeviceInsert deviceInsert,string userName=null)
        {
            FlowDevice device = new FlowDevice
            {
                DeviceId = 0,
                DeviceName = deviceInsert.DeviceName,
                DeviceModel = deviceInsert.DeviceModel,
                DeviceStatus = (int)DeviceStatus.异常,
                Ip = deviceInsert.Ip,
                Port = deviceInsert.Port
            };

            try
            {
                UpdateChannels(device, deviceInsert.Channels);
                _context.Devices.Add(device);
                _context.SaveChanges();
                _logger.LogInformation(new EventId((int)LogEvent.编辑设备, userName), $"添加设备 {device}");
                return new OkObjectResult(device);
            }
            catch (Exception)
            {
                ModelStateDictionary modelState = CheckError(device, deviceInsert.Channels);
                if (modelState.IsValid)
                {
                    throw;
                }
                else
                {
                    return new BadRequestObjectResult(modelState);
                }
            }
        }

        /// <summary>
        /// 导入设备
        /// </summary>
        /// <param name="file">文件</param>
        /// <param name="userName">用户名</param>
        /// <returns>导入结果</returns>
        public ObjectResult Import(IFormFile file,string userName=null)
        {
            try
            {
                string filePath = Path.GetTempFileName();
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                FileInfo fileinfo = new FileInfo(filePath);
                using (ExcelPackage package = new ExcelPackage(fileinfo))
                {
                    List<FlowDevice> devices = new List<FlowDevice>();
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                    {

                        devices.Add(
                            new FlowDevice
                            {
                                DeviceName = worksheet.Cells[row, 1].Value.ToString(),
                                DeviceModel = Convert.ToInt32(worksheet.Cells[row, 3].Value),
                                Ip = worksheet.Cells[row, 4].Value.ToString(),
                                Port = Convert.ToInt32(worksheet.Cells[row, 5].Value),
                                Location = worksheet.Cells[row, 7].Value?.ToString(),
                                Marked = !string.IsNullOrEmpty(worksheet.Cells[row, 7].Value?.ToString())
                            });
                    }

                    _context.Devices.AddRange(devices);
                    _context.SaveChanges();
                    _logger.LogInformation(new EventId((int)LogEvent.编辑设备, userName), "导入设备");
                    return new OkObjectResult(devices);
                }
            }
            catch (Exception)
            {
                ModelStateDictionary modelState = CheckFileError(file);
                if (modelState.IsValid)
                {
                    throw;
                }
                else
                {
                    return new BadRequestObjectResult(modelState);
                }
            }

        }

        /// <summary>
        /// 更新设备
        /// </summary>
        /// <param name="deviceUpdate">设备信息</param>
        /// <param name="userName">用户名</param>
        /// <returns>更新结果</returns>
        public IStatusCodeActionResult Update(FlowDeviceUpdate deviceUpdate,string userName=null)
        {
            FlowDevice device = _context.Devices
                .Include(d => d.FlowDevice_FlowChannels)
                .SingleOrDefault(d => d.DeviceId == deviceUpdate.DeviceId);
            if (device == null)
            {
                return new NotFoundResult();
            }

            device.DeviceName = deviceUpdate.DeviceName;
            device.DeviceModel = deviceUpdate.DeviceModel;
            device.Ip = deviceUpdate.Ip;
            device.Port = deviceUpdate.Port;

            try
            {
                UpdateChannels(device, deviceUpdate.Channels);
                _context.Devices.Update(device);
                _context.SaveChanges();
                _logger.LogInformation(new EventId((int)LogEvent.编辑设备, userName), $"更新设备 {device}");
                return new OkResult();
            }
            catch (Exception)
            {
                ModelStateDictionary modelState = CheckError(device, deviceUpdate.Channels);
                if (modelState.IsValid)
                {
                    throw;
                }
                else
                {
                    return new BadRequestObjectResult(modelState);
                }
            }
        }

        /// <summary>
        /// 更新设备标注状态
        /// </summary>
        /// <param name="deviceUpdateLocation">设备标注状态</param>
        /// <returns>更新结果</returns>
        public IStatusCodeActionResult UpdateLocation(FlowDeviceUpdateLocation deviceUpdateLocation)
        {
            FlowDevice device = _context.Devices.SingleOrDefault(d => d.DeviceId == deviceUpdateLocation.DeviceId);
            if (device == null)
            {
                return new NotFoundResult();
            }
            device.Location = deviceUpdateLocation.Location;
            device.Marked = true;
            _context.Devices.Update(device);
            _context.SaveChanges();
            return new OkResult();
        }

        /// <summary>
        /// 更新设备状态
        /// </summary>
        /// <param name="deviceUpdateStatus">设备状态</param>
        /// <returns>更新结果</returns>
        public IStatusCodeActionResult UpdateStatus(FlowDeviceUpdateStatus deviceUpdateStatus)
        {
            FlowDevice device = _context.Devices.SingleOrDefault(d => d.DeviceId == deviceUpdateStatus.DeviceId);
            if (device == null)
            {
                return new NotFoundResult();
            }
            device.DeviceStatus = deviceUpdateStatus.DeviceStatus;
            device.License = deviceUpdateStatus.License;
            device.Runtime = deviceUpdateStatus.Runtime;
            device.Systime = deviceUpdateStatus.Systime;
            device.Space = deviceUpdateStatus.Space;

            _context.Devices.Update(device);
            _context.SaveChanges();
            return new OkResult();
        }

        /// <summary>
        /// 删除流量设备
        /// </summary>
        /// <param name="deviceId">设备编号</param>
        /// <param name="userName">用户名</param>
        /// <returns>删除结果</returns>
        public IStatusCodeActionResult Remove(int deviceId,string userName=null)
        {
            FlowDevice device = _context.Devices.SingleOrDefault(d => d.DeviceId == deviceId);
            if (device == null)
            {
                return new NotFoundResult();
            }
            _context.Devices.Remove(device);
            _context.SaveChanges();
            _logger.LogInformation(new EventId((int)LogEvent.编辑设备, userName), $"删除设备 {device}");
            return new OkResult();
        }

        /// <summary>
        /// 查询设备关联项
        /// </summary>
        /// <param name="queryable">数据源</param>
        /// <returns>包含关联项的数据源</returns>
        protected IQueryable<FlowDevice> Include(IQueryable<FlowDevice> queryable)
        {
            return queryable
                .Include(d => d.FlowDevice_FlowChannels)
                    .ThenInclude(r => r.Channel)
                        .ThenInclude(c => c.Lanes)
                .Include(d => d.FlowDevice_FlowChannels)
                    .ThenInclude(r => r.Channel)
                        .ThenInclude(c => c.RoadSection)
                .Include(d => d.FlowDevice_FlowChannels)
                    .ThenInclude(r => r.Channel)
                        .ThenInclude(c => c.RoadCrossing);
        }

        /// <summary>
        /// 对设备包含的子项排序
        /// </summary>
        /// <param name="device">设备</param>
        /// <param name="order">排序方式</param>
        /// <returns>设备</returns>
        protected FlowDevice OrderInclude(FlowDevice device, string order)
        {
            if (order == "status")
            {
                device.FlowDevice_FlowChannels =
                    device.FlowDevice_FlowChannels
                        .OrderBy(r => r.Channel.ChannelStatus)
                        .ThenBy(r => r.Channel.ChannelIndex)
                        .ToList();
            }
            else
            {
                device.FlowDevice_FlowChannels =
                    device.FlowDevice_FlowChannels
                        .OrderBy(c => c.Channel.ChannelIndex)
                        .ToList();
            }

            foreach (var relation in device.FlowDevice_FlowChannels)
            {
                _memoryCache.FillChannel(relation.Channel);
                if (relation.Channel.RoadSection != null)
                {
                    _memoryCache.FillSection(relation.Channel.RoadSection);
                }

                relation.Channel.Lanes =
                    relation.Channel.Lanes
                        .OrderBy(l => l.LaneIndex)
                        .Select(l => _memoryCache.FillLane(l))
                        .ToList();
            }

            return device;
        }

        /// <summary>
        /// 检查导入错误原因
        /// </summary>
        /// <param name="file">文件</param>
        /// <returns>数据校验结果</returns>
        private ModelStateDictionary CheckFileError(IFormFile file)
        {
            ModelStateDictionary modelState = new ModelStateDictionary();
            if (file == null || file.Length == 0)
            {
                modelState.AddModelError("File", "文件为空");
            }
            else
            {
                string filePath = Path.GetTempFileName();
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                FileInfo fileinfo = new FileInfo(filePath);
                using (ExcelPackage package = new ExcelPackage(fileinfo))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        modelState.AddModelError("File", "数据页少于1");
                    }
                    else
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                        if (worksheet.Dimension.Columns < 7)
                        {
                            modelState.AddModelError("File", "数据列少于7");
                        }
                        else
                        {
                            HashSet<string> ips = new HashSet<string>();
                            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                            {
                                if (worksheet.Cells[row, 1].Value == null)
                                {
                                    modelState.AddModelError("DeviceName", $"{row},1 设备名称不能为空");
                                }

                                if (worksheet.Cells[row, 3].Value == null
                                    || !int.TryParse(worksheet.Cells[row, 3].Value.ToString(), out _))
                                {
                                    modelState.AddModelError("DeviceModel", $"{row},3 设备型号格式不正确");
                                }

                                if (worksheet.Cells[row, 4].Value == null || !IPAddress.TryParse(worksheet.Cells[row, 4].Value.ToString(), out _))
                                {
                                    modelState.AddModelError("Ip", $"{row},4 设备IP格式不正确");
                                }
                                else
                                {
                                    if (ips.Contains(worksheet.Cells[row, 4].Value.ToString()))
                                    {
                                        modelState.AddModelError("Ip", $"{row},4 设备IP重复");
                                    }
                                    else
                                    {
                                        ips.Add(worksheet.Cells[row, 4].Value.ToString());
                                    }
                                }

                                if (worksheet.Cells[row, 5].Value == null
                                    || !int.TryParse(worksheet.Cells[row, 5].Value.ToString(), out int port)
                                    || port < 1
                                    || port > 65525)
                                {
                                    modelState.AddModelError("Port", $"{row},5 设备端口格式不正确");
                                }
                            }

                        }
                    }
                }
            }

            return modelState;
        }

        /// <summary>
        /// 更新设备下的通道集合
        /// </summary>
        /// <param name="device">设备</param>
        /// <param name="channels">通道集合</param>
        private void UpdateChannels(FlowDevice device, List<FlowChannel> channels)
        {
            device.FlowDevice_FlowChannels = new List<FlowDevice_FlowChannel>();
            if (channels != null)
            {
                foreach (var channel in channels)
                {
                    FlowDevice_FlowChannel relation = new FlowDevice_FlowChannel
                    {
                        DeviceId = device.DeviceId,
                        ChannelId = channel.ChannelId
                    };
                    device.FlowDevice_FlowChannels.Add(relation);
                    if (!ChannelsManager.UpdateChannel(_context, channel))
                    {
                        ChannelsManager.AddChannel(_context, channel);
                    }
                }
            }
        }

        /// <summary>
        /// 检查设备添加或更新时的错误原因
        /// </summary>
        /// <param name="device">设备</param>
        /// <param name="channels">通道集合</param>
        /// <returns>数据校验结果</returns>
        private ModelStateDictionary CheckError(FlowDevice device, List<FlowChannel> channels)
        {
            ModelStateDictionary modelState = new ModelStateDictionary();

            if (_context.Devices.Count(d => d.Ip == device.Ip) > 0)
            {
                modelState.AddModelError("Ip", "设备IP重复");
            }

            if (channels != null)
            {
                List<int> indexes = channels.Select(c => c.ChannelIndex).Distinct().ToList();
                if (indexes.Count < channels.Count)
                {
                    modelState.AddModelError("ChannelIndex", "通道序号重复");
                }

                if (channels.Any(c => c.ChannelIndex <= 0))
                {
                    modelState.AddModelError("ChannelIndex", "通道序号应该大于0");
                }

                foreach (FlowChannel newChannel in channels)
                {
                    ModelStateDictionary channelModelState = ChannelsManager.CheckUpdateError(_context, newChannel);
                    if (channelModelState.IsValid)
                    {
                        if (_context.Device_Channels.Count(dc =>
                                dc.ChannelId == newChannel.ChannelId && dc.DeviceId != device.DeviceId) != 0)
                        {
                            modelState.AddModelError("ChannelId", $"通道 {newChannel.ChannelId} 已经关联在其他设备");
                        }
                    }
                    else
                    {
                        foreach (var (key, value) in channelModelState)
                        {
                            foreach (var error in value.Errors)
                            {
                                modelState.AddModelError(key, error.ErrorMessage);
                            }
                        }
                    }
                }
            }
            return modelState;
        }
    }
}
