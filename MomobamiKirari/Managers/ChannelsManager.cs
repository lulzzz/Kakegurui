using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// 通道数据库操作
    /// </summary>
    public class ChannelsManager
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
        private readonly ILogger<ChannelsManager> _logger;

        /// <summary>
        /// 数据库实例
        /// </summary>
        /// <param name="context">数据库实例</param>
        /// <param name="memoryCache">缓存</param>
        /// <param name="logger">日志</param>
        public ChannelsManager(FlowContext context, IMemoryCache memoryCache, ILogger<ChannelsManager> logger)
        {
            _context = context;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        /// <summary>
        /// 查询通道集合
        /// </summary>
        /// <param name="channelName">通道名称</param>
        /// <param name="crossingId">路口编号</param>
        /// <param name="sectionId">路段编号</param>
        /// <param name="alone">是否查询未关联通道</param>
        /// <param name="pageNum">分页页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        public PageModel<FlowChannel> GetList(string channelName, int crossingId, int sectionId, bool alone, int pageNum, int pageSize)
        {
            IQueryable<FlowChannel> queryable = Include(_context.Channels);

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
                queryable = queryable.Where(c => c.FlowDevice_FlowChannel == null);
            }

            PageModel<FlowChannel> channels = queryable
                .Select(c => _memoryCache.FillChannel(c))
                .Page(pageNum, pageSize);

            return channels;
        }

        /// <summary>
        /// 查询通道
        /// </summary>
        /// <param name="channelId"/>通道编号/param>
        /// <returns>查询结果</returns>
        public IStatusCodeActionResult Get(string channelId)
        {
            FlowChannel channel = Include(_context.Channels)
                .SingleOrDefault(c => c.ChannelId == channelId);
            if (channel == null)
            {
                return new NotFoundObjectResult(null);
            }
            else
            {
                _memoryCache.FillChannel(channel);
                return new OkObjectResult(channel);
            }
        }

        /// <summary>
        /// 按路段查询车道集合
        /// </summary>
        /// <param name="sectionName">路段名称</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        public PageModel<object> GetGroupBySection(string sectionName, int pageNum, int pageSize)
        {
            IQueryable<IGrouping<int, FlowChannel>> queryable =
                _context.Channels
                    .Where(c => c.SectionId.HasValue && c.Lanes.Count > 0)
                    .Include(c => c.FlowDevice_FlowChannel)
                        .ThenInclude(r => r.Device)
                    .Include(c => c.RoadSection)
                    .Include(c => c.Lanes)
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
        /// <param name="userName">用户名</param>
        /// <returns>添加结果</returns>
        public ObjectResult Add(FlowChannel channel,string userName=null)
        {
            try
            {
                AddChannel(_context, channel);
                _context.SaveChanges();
                _logger.LogInformation(new EventId((int)LogEvent.编辑通道,userName), $"添加通道 {channel}");
                return new OkObjectResult(channel);
            }
            catch (Exception)
            {
                ModelStateDictionary modelState = CheckInsertError(_context, channel);
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
        /// 导入通道
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

                List<FlowChannel> channels=new List<FlowChannel>();
                FileInfo fileinfo = new FileInfo(filePath);
                using (ExcelPackage package = new ExcelPackage(fileinfo))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                    {
                        int? sectionId = Convert.ToInt32(worksheet.Cells[row, 4].Value);
                        int? crossingId = Convert.ToInt32(worksheet.Cells[row, 5].Value);
                        if (_context.RoadCrossings.Count(r => r.CrossingId == crossingId) == 0)
                        {
                            crossingId = null;
                        }

                        if (_context.RoadSections.Count(r => r.SectionId == sectionId) == 0)
                        {
                            sectionId = null;
                        }

                        channels.Add(
                            new FlowChannel
                            {
                                ChannelId = worksheet.Cells[row, 2].Value.ToString(),
                                ChannelName = worksheet.Cells[row, 1].Value.ToString(),
                                ChannelType = Convert.ToInt32(worksheet.Cells[row, 3].Value),
                                SectionId = sectionId,
                                CrossingId = crossingId,
                                RtspUser = worksheet.Cells[row, 6].Value?.ToString(),
                                RtspPassword = worksheet.Cells[row, 7].Value?.ToString(),
                                RtspProtocol = Convert.ToInt32(worksheet.Cells[row, 8].Value),
                                Location = worksheet.Cells[row, 9].Value?.ToString(),
                                Marked = !string.IsNullOrEmpty(worksheet.Cells[row, 9].Value?.ToString())
                            });
                    }
                    _context.Channels.AddRange(channels);
                    _context.SaveChanges();
                    _logger.LogInformation(new EventId((int)LogEvent.编辑通道, userName), "导入通道");
                    return new OkObjectResult(channels);
                }
            }
            catch (Exception)
            {
                ModelStateDictionary modelState = CheckFileError(_context, file);
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
        /// 更新通道
        /// </summary>
        /// <param name="updateChannel">通道</param>
        /// <param name="userName">用户名</param>
        /// <returns>更新结果</returns>
        public IStatusCodeActionResult Update([FromBody] FlowChannel updateChannel,string userName=null)
        {
            try
            {
                if (UpdateChannel(_context, updateChannel))
                {
                    _context.SaveChanges();
                    _logger.LogInformation(new EventId((int)LogEvent.编辑通道, userName), $"更新通道 {updateChannel}");
                    return new OkResult();
                }
                else
                {
                    return new NotFoundResult();
                }
            }
            catch (Exception)
            {
                ModelStateDictionary modelState = CheckUpdateError(_context,updateChannel);
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
        /// 更新通道标注状态
        /// </summary>
        /// <param name="channelUpdateLocation">通道标注状态</param>
        /// <returns>更新结果</returns>
        public IStatusCodeActionResult UpdateLocation([FromBody] FlowChannelUpdateLocation channelUpdateLocation)
        {
            FlowChannel channel = _context.Channels.SingleOrDefault(d => d.ChannelId == channelUpdateLocation.ChannelId);
            if (channel == null)
            {
                return new NotFoundResult();
            }

            channel.Location = channelUpdateLocation.Location;
            channel.Marked = true;
            _context.Channels.Update(channel);
            _context.SaveChanges();
            return new OkResult();
        }

        /// <summary>
        /// 更新通道状态
        /// </summary>
        /// <param name="channelUpdateStatus">通道状态</param>
        /// <returns>更新结果</returns>
        public IStatusCodeActionResult UpdateStatus(FlowChannelUpdateStatus channelUpdateStatus)
        {
            FlowChannel channel = _context.Channels.SingleOrDefault(c => c.ChannelId == channelUpdateStatus.ChannelId);
            if (channel == null)
            {
                return new NotFoundResult();
            }
            channel.ChannelStatus = channelUpdateStatus.ChannelStatus;
            _context.Channels.Update(channel);
            _context.SaveChanges();
            return new OkResult();
        }

        /// <summary>
        /// 删除通道
        /// </summary>
        /// <param name="channelId">通道编号</param>
        /// <param name="userName">用户名</param>
        /// <returns>删除结果</returns>
        public IStatusCodeActionResult Remove([FromRoute]string channelId,string userName=null)
        {
            channelId = Uri.UnescapeDataString(channelId);
            FlowChannel channel = _context.Channels.SingleOrDefault(c => c.ChannelId == channelId);
            if (channel == null)
            {
                return new NotFoundResult();
            }
            _context.Channels.Remove(channel);
            _context.SaveChanges();
            _logger.LogInformation(new EventId((int)LogEvent.编辑通道, userName), $"删除通道 {channel}");
            return new OkResult();
        }

        /// <summary>
        /// 查询通道关联项
        /// </summary>
        /// <param name="queryable">数据源</param>
        /// <returns>包含关联项的数据源</returns>
        private IQueryable<FlowChannel> Include(IQueryable<FlowChannel> queryable)
        {
            return queryable
                .Include(c => c.FlowDevice_FlowChannel)
                .ThenInclude(r => r.Device)
                .Include(c => c.Lanes)
                .Include(c => c.RoadSection)
                .Include(c => c.RoadCrossing);
        }

        /// <summary>
        /// 检查通道更新错误原因
        /// </summary>
        /// <param name="deviceContext">数据库实例</param>
        /// <param name="channel">通道</param>
        /// <returns>数据校验结果</returns>
        public static ModelStateDictionary CheckUpdateError(FlowContext deviceContext, FlowChannel channel)
        {
            ModelStateDictionary modelState = new ModelStateDictionary();
            if (channel.CrossingId.HasValue)
            {
                if (deviceContext.RoadCrossings.Count(d => d.CrossingId == channel.CrossingId) == 0)
                {
                    modelState.AddModelError("CrossingId", $"不存在路口编号 {channel.CrossingId}");
                }
            }

            if (channel.SectionId.HasValue)
            {
                if (deviceContext.RoadSections.Count(d => d.SectionId == channel.SectionId) == 0)
                {
                    modelState.AddModelError("SectionId", $"不存在路段编号 {channel.SectionId}");
                }
            }

            return modelState;
        }

        /// <summary>
        /// 检查通道添加错误原因
        /// </summary>
        /// <param name="deviceContext">数据库实例</param>
        /// <param name="channel">通道</param>
        /// <returns>数据校验结果</returns>
        private ModelStateDictionary CheckInsertError(FlowContext deviceContext, FlowChannel channel)
        {
            ModelStateDictionary modelState = CheckUpdateError(deviceContext, channel);

            if (deviceContext.Channels.Count(c => c.ChannelId == channel.ChannelId) > 0)
            {
                modelState.AddModelError("ChannelId", $"通道编号重复 {channel.ChannelId}");
            }

            return modelState;
        }

        /// <summary>
        /// 检查通道导入错误原因
        /// </summary>
        /// <param name="deviceContext">数据库实例</param>
        /// <param name="file">文件</param>
        /// <returns>数据校验结果</returns>
        private ModelStateDictionary CheckFileError(FlowContext deviceContext, IFormFile file)
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
                        modelState.AddModelError("File", "没有数据页");
                    }
                    else
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension.Rows;
                        if (worksheet.Dimension.Columns < 13)
                        {
                            modelState.AddModelError("File", "数据列少于14");
                        }
                        else
                        {
                            for (int row = 2; row <= rowCount; row++)
                            {
                                if (worksheet.Cells[row, 1].Value == null)
                                {
                                    modelState.AddModelError("ChannelName", $"{row},1 通道名称不能为空");
                                }

                                if (worksheet.Cells[row, 2].Value == null)
                                {
                                    modelState.AddModelError("ChannelId", $"{row},2 通道地址不能为空");
                                }
                                else
                                {
                                    if (deviceContext.Channels.Count(c => c.ChannelId == worksheet.Cells[row, 2].Value.ToString()) > 0)
                                    {
                                        modelState.AddModelError("ChannelId", $"通道编号重复{worksheet.Cells[row, 2].Value}");
                                    }
                                }

                                if (worksheet.Cells[row, 3].Value == null
                                    || !int.TryParse(worksheet.Cells[row, 3].Value.ToString(), out _))
                                {
                                    modelState.AddModelError("ChannelType", $"{row},3 通道类型格式不正确");
                                }

                                if (worksheet.Cells[row, 4].Value != null && !int.TryParse(worksheet.Cells[row, 4].Value.ToString(), out _))
                                {
                                    modelState.AddModelError("SectionId", $"{row},4 路段编号格式不正确");
                                }

                                if (worksheet.Cells[row, 5].Value != null && !int.TryParse(worksheet.Cells[row, 5].Value.ToString(), out _))
                                {
                                    modelState.AddModelError("CrossingId", $"{row},5 路口编号格式不正确");
                                }

                                if (worksheet.Cells[row, 8].Value!=null&&!int.TryParse(worksheet.Cells[row, 8].Value.ToString(), out _))
                                {
                                    modelState.AddModelError("RtspProtocol", $"{row},8 Rtsp协议类型数据格式错误");
                                }
                            }
                        }
                    }
                }
            }

            return modelState;
        }

        /// <summary>
        /// 添加通道
        /// </summary>
        /// <param name="deviceContext">数据库上下文</param>
        /// <param name="newChannel">新通道</param>
        public static void AddChannel(FlowContext deviceContext, FlowChannel newChannel)
        {
            newChannel.ChannelStatus = (int)DeviceStatus.异常;
            newChannel.RoadSection = null;
            newChannel.RoadCrossing = null;
            deviceContext.Channels.Add(newChannel);
        }

        /// <summary>
        /// 更新通道
        /// </summary>
        /// <param name="deviceContext">数据库实例</param>
        /// <param name="updateChannel">更新的通道</param>
        /// <returns>更新成功时返回true，如果未找到通道返回false</returns>
        public static bool UpdateChannel(FlowContext deviceContext, FlowChannel updateChannel)
        {
            FlowChannel channel = deviceContext.Channels
                .Include(c => c.Lanes)
                .SingleOrDefault(c => c.ChannelId == updateChannel.ChannelId);
            if (channel == null)
            {
                return false;
            }
            else
            {
                channel.ChannelName = updateChannel.ChannelName;
                channel.ChannelType = updateChannel.ChannelType;
                channel.ChannelIndex = updateChannel.ChannelIndex;
                channel.RtspUser = updateChannel.RtspUser;
                channel.RtspPassword = updateChannel.RtspPassword;
                channel.RtspProtocol = updateChannel.RtspProtocol;
                channel.IsLoop = updateChannel.IsLoop;
                channel.SectionId = updateChannel.SectionId;
                channel.RoadSection = null;
                channel.CrossingId = updateChannel.CrossingId;
                channel.RoadCrossing = null;
                channel.Lanes = updateChannel.Lanes;

                deviceContext.Channels.Update(channel);
                return true;
            }

        }

    }
}
