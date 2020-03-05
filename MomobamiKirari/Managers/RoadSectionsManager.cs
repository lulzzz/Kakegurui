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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MomobamiKirari.Cache;
using MomobamiKirari.Controllers;
using MomobamiKirari.Data;
using MomobamiKirari.Models;
using OfficeOpenXml;

namespace MomobamiKirari.Managers
{
    /// <summary>
    /// 路段数据库操作
    /// </summary>
    public class RoadSectionsManager
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
        private readonly ILogger<RoadSectionsController> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库实例</param>
        /// <param name="memoryCache">缓存</param>
        /// <param name="logger">日志</param>
        public RoadSectionsManager(FlowContext context, IMemoryCache memoryCache, ILogger<RoadSectionsController> logger)
        {
            _context = context;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        /// <summary>
        /// 查询路段集合
        /// </summary>
        /// <param name="sectionName">路段名称</param>
        /// <param name="sectionType">路段类型</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        public PageModel<RoadSection> GetList(string sectionName, int sectionType, int pageNum, int pageSize)
        {
            IQueryable<RoadSection> queryable = _context.RoadSections;

            if (!string.IsNullOrEmpty(sectionName))
            {
                queryable = queryable.Where(d => d.SectionName.Contains(sectionName));
            }

            if (sectionType != 0)
            {
                queryable = queryable.Where(s => s.SectionType == sectionType);
            }

            PageModel<RoadSection> sections = queryable
                .Select(s => _memoryCache.FillSection(s))
                .Page(pageNum, pageSize);

            return sections;
        }

        /// <summary>
        /// 查询路段
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <returns>查询结果</returns>
        public IStatusCodeActionResult Get(int sectionId)
        {
            RoadSection roadSection = _context.RoadSections.SingleOrDefault(c => c.SectionId == sectionId);

            if (roadSection == null)
            {
                return new NotFoundObjectResult(null);
            }
            else
            {
                _memoryCache.FillSection(roadSection);
                return new OkObjectResult(roadSection);
            }
        }

        /// <summary>
        /// 添加路段
        /// </summary>
        /// <param name="roadSection">路段</param>
        /// <param name="userName">用户名</param>
        /// <returns>添加结果</returns>
        public ObjectResult Add([FromBody] RoadSection roadSection,string userName=null)
        {
            _context.RoadSections.Add(roadSection);
            _context.SaveChanges();
            _logger.LogInformation(new EventId((int)LogEvent.编辑路段, userName), $"添加路段 {roadSection}");
            return new OkObjectResult(roadSection);
        }

        /// <summary>
        /// 导入路段
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
                    List<RoadSection> roadSections = new List<RoadSection>();
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                    {
                        roadSections.Add(
                            new RoadSection
                            {
                                SectionName = worksheet.Cells[row, 1].Value.ToString(),
                                Direction = Convert.ToInt32(worksheet.Cells[row, 2].Value),
                                Length = Convert.ToInt32(worksheet.Cells[row, 3].Value),
                                SectionType = Convert.ToInt32(worksheet.Cells[row, 4].Value),
                                SpeedLimit = Convert.ToInt32(worksheet.Cells[row, 5].Value)
                            });
                    }
                    _context.RoadSections.AddRange(roadSections);
                    _context.SaveChanges();
                    _logger.LogInformation(new EventId((int)LogEvent.编辑路段, userName), "导入路段");
                    return new OkObjectResult(roadSections);
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
        /// 更新路段
        /// </summary>
        /// <param name="updateRoadSection">路段</param>
        /// <param name="userName">用户名</param>
        /// <returns>更新结果</returns>
        public IStatusCodeActionResult Update([FromBody] RoadSection updateRoadSection,string userName=null)
        {
            RoadSection roadSection = _context.RoadSections.SingleOrDefault(d => d.SectionId == updateRoadSection.SectionId);
            if (roadSection == null)
            {
                return new NotFoundResult();
            }
            roadSection.SectionName = updateRoadSection.SectionName;
            roadSection.SectionType = updateRoadSection.SectionType;
            roadSection.Length = updateRoadSection.Length;
            roadSection.Direction = updateRoadSection.Direction;
            roadSection.SpeedLimit = updateRoadSection.SpeedLimit;
            _context.SaveChanges();
            _logger.LogInformation(new EventId((int)LogEvent.编辑路段, userName), $"更新路段 {roadSection}");
            return new OkResult();
        }

        /// <summary>
        /// 删除路段
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <param name="userName">用户名</param>
        /// <returns>删除结果</returns>
        public IStatusCodeActionResult Remove(int sectionId,string userName=null)
        {
            RoadSection roadSection = _context.RoadSections.SingleOrDefault(d => d.SectionId == sectionId);
            if (roadSection == null)
            {
                return new NotFoundResult();
            }
            try
            {
                _context.RoadSections.Remove(roadSection);
                _context.SaveChanges();
                _logger.LogInformation(new EventId((int)LogEvent.编辑路段, userName), $"删除路段 {roadSection}");
                return new OkResult();
            }
            catch (DbUpdateException)
            {
                if (_context.Channels.Count(c => c.SectionId == sectionId) > 0)
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
                        int rowCount = worksheet.Dimension.Rows;
                        if (worksheet.Dimension.Columns < 5)
                        {
                            modelState.AddModelError("File", "数据列少于5");
                        }
                        else
                        {
                            for (int row = 2; row <= rowCount; row++)
                            {
                                if (worksheet.Cells[row, 1].Value == null)
                                {
                                    modelState.AddModelError("SectionName", $"{row},1 路段名称不能为空");
                                }
                                if (worksheet.Cells[row, 2].Value == null
                                    || !int.TryParse(worksheet.Cells[row, 2].Value.ToString(), out _))
                                {
                                    modelState.AddModelError("Direction", $"{row},2 路段方向格式不正确");
                                }
                                if (worksheet.Cells[row, 3].Value == null
                                    || !int.TryParse(worksheet.Cells[row, 3].Value.ToString(), out _))
                                {
                                    modelState.AddModelError("Length", $"{row},3 路段长度格式不正确");
                                }
                                if (worksheet.Cells[row, 4].Value == null
                                    || !int.TryParse(worksheet.Cells[row, 4].Value.ToString(), out _))
                                {
                                    modelState.AddModelError("SectionType", $"{row},4 路段类型格式不正确");
                                }
                                if (worksheet.Cells[row, 5].Value == null
                                    || !int.TryParse(worksheet.Cells[row, 5].Value.ToString(), out _))
                                {
                                    modelState.AddModelError("SpeedLimit", $"{row},5 路段限速值格式不正确");
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
