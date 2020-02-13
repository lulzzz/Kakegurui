using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Data;
using ItsukiSumeragi.Models;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ItsukiSumeragi.Codes.Violation;

namespace ItsukiSumeragi.Controller
{
    /// <summary>
    /// 违法行为
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ViolationsController : ControllerBase
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly DeviceContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库实例</param>
        public ViolationsController(DeviceContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 查询违法行为集合
        /// </summary>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        public PageModel<TrafficViolation> GetViolations([FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            return _context.Violations
                .Include(v=>v.Violation_Tags)
                    .ThenInclude(r => r.Tag)
                .Include(v => v.Violation_Parameters)
                    .ThenInclude(r => r.Parameter)
                .Page(pageNum, pageSize);
        }

        /// <summary>
        /// 查询违法行为
        /// </summary>
        /// <param name="violationId">违法行为编号</param>
        /// <returns>查询结果</returns>
        [HttpGet("{violationId}")]
        public IActionResult GetViolation([FromRoute] int violationId)
        {
            TrafficViolation violation = _context.Violations
                .Include(v => v.Violation_Tags)
                    .ThenInclude(r => r.Tag)
                .Include(v => v.Violation_Parameters)
                    .ThenInclude(r => r.Parameter)
                .SingleOrDefault(t => t.ViolationId == violationId);
            if (violation == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(violation);
            }
        }

        /// <summary>
        /// 更新违法行为
        /// </summary>
        /// <param name="updateViolation">违法行为</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public IActionResult PutViolation([FromBody] TrafficViolation updateViolation)
        {
            TrafficViolation violation = _context.Violations
                .Include(v=>v.Violation_Tags)
                .SingleOrDefault(v => v.ViolationId == updateViolation.ViolationId);
            if (violation == null)
            {
                return NotFound();
            }
            if (updateViolation.Violation_Tags != null)
            {
                foreach (var relation in violation.Violation_Tags)
                {
                    relation.Tag = null;
                    if (_context.Tags.Count(t => t.TagName == relation.TagName) == 0)
                    {
                        ModelState.AddModelError("Tag", $"不存在标签 {relation.TagName}");
                        return BadRequest(ModelState);
                    }
                }
            }
            violation.ViolationName = updateViolation.ViolationName;
            violation.GbCode = updateViolation.GbCode;
            violation.GbName = updateViolation.GbName;
            violation.Violation_Tags = updateViolation.Violation_Tags;
            _context.Violations.Update(violation);
            _context.SaveChanges();
            return Ok();
        }

        /// <summary>
        /// 更新违法行为osd参数
        /// </summary>
        /// <param name="updateViolation">违法行为</param>
        /// <returns>更新结果</returns>
        [HttpPut("osd")]
        public IActionResult PutViolationOsdParameters([FromBody] TrafficViolationUpdateOsdParameters updateViolation)
        {
            TrafficViolation violation = _context.Violations
                .Include(v => v.Violation_Parameters)
                    .ThenInclude(r => r.Parameter)
                .SingleOrDefault(v => v.ViolationId == updateViolation.ViolationId);
            List<TrafficViolationParameter> parameters = _context.ViolationParameters.ToList();
            List<TrafficViolationParameter> osdParameters =
                parameters.Where(p => p.ParameterType == ViolationParameterType.Osd).ToList();
            List<TrafficViolationParameter> fontParameters =
                parameters.Where(p => p.ParameterType == ViolationParameterType.Font).ToList();
            if (violation == null)
            {
                return NotFound();
            }
            if (updateViolation.Violation_OsdParameters == null)
            {
                updateViolation.Violation_OsdParameters = new List<TrafficViolation_TrafficViolationParameter>();
            }
            else
            {
                foreach (var relation in updateViolation.Violation_OsdParameters)
                {
                    relation.Parameter = null;
                    if (osdParameters.Count(p => p.Key == relation.Key) == 0)
                    {
                        ModelState.AddModelError("Parameter", $"不存在OSD参数 {relation.Key}");
                        return BadRequest(ModelState);
                    }
                }
            }

            violation.Violation_Parameters.RemoveAll(p => p.Parameter.ParameterType == ViolationParameterType.Osd);
            violation.Violation_Parameters.AddRange(updateViolation.Violation_OsdParameters);

            if (updateViolation.Violation_FontParameters != null)
            {
                foreach (var relation in updateViolation.Violation_FontParameters)
                {
                    relation.Parameter = null;
                    TrafficViolationParameter fontParamter = fontParameters.SingleOrDefault(p => p.Key == relation.Key);
                    if (fontParamter != null)
                    {
                        fontParamter.Value = relation.Value;
                        _context.ViolationParameters.Update(fontParamter);
                    }
                }
            }
            

            _context.Violations.Update(violation);
            _context.SaveChanges();
            return Ok();
        }

        /// <summary>
        /// 更新违法行为文件参数
        /// </summary>
        /// <param name="updateViolation">违法行为</param>
        /// <returns>更新结果</returns>
        [HttpPut("file")]
        public IActionResult PutViolationFileParameters([FromBody] TrafficViolationUpdateFileParameters updateViolation)
        {
            TrafficViolation violation = _context.Violations
                .Include(v => v.Violation_Parameters)
                    .ThenInclude(r => r.Parameter)
                .SingleOrDefault(v => v.ViolationId == updateViolation.ViolationId);
            List<TrafficViolationParameter> parameters = _context.ViolationParameters.ToList();
            List<TrafficViolationParameter> fileParameters =
                parameters.Where(p => p.ParameterType == ViolationParameterType.File).ToList();
            List<TrafficViolationParameter> fileNameParameters =
                parameters.Where(p => p.ParameterType == ViolationParameterType.FileName).ToList();
            if (violation == null)
            {
                return NotFound();
            }
            if (updateViolation.Violation_FileParameters == null)
            {
                updateViolation.Violation_FileParameters = new List<TrafficViolation_TrafficViolationParameter>();
            }
            else
            {
                foreach (var relation in updateViolation.Violation_FileParameters)
                {
                    relation.Parameter = null;
                    if (fileParameters.Count(p => p.Key == relation.Key) == 0)
                    {
                        ModelState.AddModelError("Parameter", $"不存在文件参数 {relation.Key}");
                        return BadRequest(ModelState);
                    }
                }
            }

            violation.Violation_Parameters.RemoveAll(p => p.Parameter.ParameterType == ViolationParameterType.File);
            violation.Violation_Parameters.AddRange(updateViolation.Violation_FileParameters);

            if (updateViolation.Violation_FileNameParameters != null)
            {
                foreach (var relation in updateViolation.Violation_FileNameParameters)
                {
                    relation.Parameter = null;
                    TrafficViolationParameter fileNameParameter = fileNameParameters.SingleOrDefault(p => p.Key == relation.Key);
                    if (fileNameParameter != null)
                    {
                        fileNameParameter.Value = relation.Value;
                        _context.ViolationParameters.Update(fileNameParameter);
                    }
                }
            }
            

            _context.Violations.Update(violation);
            _context.SaveChanges();
            return Ok();
        }

    }
}
