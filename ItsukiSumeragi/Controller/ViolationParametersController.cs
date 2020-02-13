using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Data;
using ItsukiSumeragi.Models;
using Microsoft.AspNetCore.Mvc;
using ItsukiSumeragi.Codes.Device;
using ItsukiSumeragi.Codes.Violation;

namespace ItsukiSumeragi.Controller
{
    /// <summary>
    /// 违法行为参数
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ViolationParametersController: ControllerBase
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly DeviceContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库实例</param>
        public ViolationParametersController(DeviceContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 查询参数集合
        /// </summary>
        /// <param name="parameterType">参数类型</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        public List<TrafficViolationParameter> GetParameters([FromQuery] ViolationParameterType parameterType)
        {
            return _context.ViolationParameters
                .Where(p=>p.ParameterType==parameterType)
                .ToList();
        }
    }
}
