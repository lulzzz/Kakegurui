using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kakegurui.Log;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MomobamiKirari.Controllers;
using MomobamiKirari.Data;
using MomobamiKirari.Models;
using OfficeOpenXml;

namespace MomobamiKirari.Managers
{
    /// <summary>
    /// 路口数据库操作
    /// </summary>
    public class RoadCrossingsManager
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly FlowContext _context;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<RoadCrossingsController> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库实例</param>
        /// <param name="logger">日志</param>
        public RoadCrossingsManager(FlowContext context, ILogger<RoadCrossingsController> logger)
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
        public PageModel<RoadCrossing> GetList(string crossingName, int pageNum, int pageSize)
        {
            IQueryable<RoadCrossing> queryable = _context.RoadCrossings;

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
        public IStatusCodeActionResult Get(int crossingId)
        {
            RoadCrossing roadCrossing= _context.RoadCrossings.SingleOrDefault(c => c.CrossingId == crossingId);
            if (roadCrossing == null)
            {
                return new NotFoundObjectResult(null);
            }
            else
            {
                return new OkObjectResult(roadCrossing);
            }
        }

        /// <summary>
        /// 添加路口
        /// </summary>
        /// <param name="roadCrossing">路口</param>
        /// <param name="userName">用户名</param>
        /// <returns>添加结果</returns>
        public ObjectResult Add([FromBody] RoadCrossing roadCrossing,string userName=null)
        {
            _context.RoadCrossings.Add(roadCrossing);
            _context.SaveChanges();
            _logger.LogInformation(new EventId((int)LogEvent.编辑路口, userName), $"添加路口 {roadCrossing}");
            return new OkObjectResult(roadCrossing);
        }

        /// <summary>
        /// 导入路口
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
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    List<RoadCrossing> roadCrossings = new List<RoadCrossing>();
                    for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                    {

                        roadCrossings.Add(new RoadCrossing { CrossingName = worksheet.Cells[row, 1].Value.ToString() });
                    }
                    _context.RoadCrossings.AddRange(roadCrossings);
                    _context.SaveChanges();
                    _logger.LogInformation(new EventId((int)LogEvent.编辑路口, userName), "导入路口");
                    return new OkObjectResult(roadCrossings);
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
        /// 更新路口
        /// </summary>
        /// <param name="updateRoadCrossing">路口</param>
        /// <param name="userName">用户名</param>
        /// <returns>更新结果</returns>
        public IStatusCodeActionResult Update(RoadCrossing updateRoadCrossing,string userName=null)
        {
            RoadCrossing roadCrossing = _context.RoadCrossings.SingleOrDefault(d => d.CrossingId == updateRoadCrossing.CrossingId);
            if (roadCrossing == null)
            {
                return new NotFoundResult();
            }
            roadCrossing.CrossingName = updateRoadCrossing.CrossingName;
            _context.SaveChanges();
            _logger.LogInformation(new EventId((int)LogEvent.编辑路口, userName), $"更新路口 {roadCrossing}");
            return new OkResult();
        }

        /// <summary>
        /// 删除路口
        /// </summary>
        /// <param name="crossingId">路口编号</param>
        /// <param name="userName">用户名</param>
        /// <returns>删除结果</returns>
        public IStatusCodeActionResult Remove(int crossingId,string userName=null)
        {
            RoadCrossing roadCrossing = _context.RoadCrossings.SingleOrDefault(d => d.CrossingId == crossingId);
            if (roadCrossing == null)
            {
                return new NotFoundResult();
            }

            try
            {
                _context.RoadCrossings.Remove(roadCrossing);
                _context.SaveChanges();
                _logger.LogInformation(new EventId((int)LogEvent.编辑路口, userName), $"删除路口 {roadCrossing}");
                return new OkResult();
            }
            catch (DbUpdateException)
            {
                if (_context.Channels.Count(c => c.CrossingId == crossingId) > 0)
                {
                    ModelStateDictionary modelState = new ModelStateDictionary();
                    modelState.AddModelError("Channel", "存在关联的通道");
                    return new BadRequestObjectResult(modelState);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// 检查文件导入错误原因
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

                        if (worksheet.Dimension.Columns < 1)
                        {
                            modelState.AddModelError("File", "数据列少于1");

                        }
                        else
                        {
                            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                            {
                                if (worksheet.Cells[row, 1].Value == null)
                                {
                                    modelState.AddModelError("CrossingName", $"{row},1 路口名称不能为空");
                                }
                            }
                        }
                    }
                }
            }

            return modelState;
        }

    }
}
