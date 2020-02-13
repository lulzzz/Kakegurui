using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YumekoJabami.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using YumekoJabami.Data;

namespace YumekoJabami.Controllers
{
    /// <summary>
    /// 参数
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ParametersController : ControllerBase
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly SystemContext _context;

        /// <summary>
        /// 配置项
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// 默认参数类型
        /// </summary>
        public const string DefaultType = "default";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库实例</param>
        /// <param name="configuration">配置项</param>
        public ParametersController(SystemContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// 按节点查询参数集合
        /// </summary>
        /// <param name="type">单元编号</param>
        /// <returns>查询结果</returns>
        [HttpGet("types/{type}")]
        public IEnumerable<TrafficParameter> GetParameters([FromRoute] string type)
        {
            if (DefaultType == type)
            {
                return GetDefaultParameters();
            }
            return _context.Parameters.Where(p => p.Type == type);
        }

        /// <summary>
        /// 按节点查询参数集合
        /// </summary>
        /// <returns>查询结果</returns>
        [HttpGet("types")]
        public List<TrafficParameter> GetParameterTypes()
        {
            List<TrafficParameter> types= _context.Parameters
                .Select(p => p.Type)
                .Distinct()
                .Select(t => new TrafficParameter { Type = t })
                .ToList();
            types.Insert(0,new TrafficParameter{Type = DefaultType});
            return types;
        }
      
        /// <summary>
        /// 获取默认参数集合
        /// </summary>
        /// <returns>默认参数集合</returns>
        private List<TrafficParameter> GetDefaultParameters()
        {
            return new List<TrafficParameter>
            {
                new TrafficParameter
                {
                    Key = "DbSpan",
                    Value = "2",
                    Description = "分表时等待数据入库的时间(分)"
                },
                new TrafficParameter
                {
                    Key = "NodeMode",
                    Value = "1",
                    Description = "节点类型"
                },
                new TrafficParameter
                {
                    Key = "NodeUrl",
                    Value = "",
                    Description = "节点地址"
                },
                new TrafficParameter
                {
                    Key = "DbIp",
                    Value = _configuration.GetValue<string>("DbIp"),
                    Description = "数据库地址"
                },
                new TrafficParameter
                {
                    Key = "DbPort",
                    Value = _configuration.GetValue<string>("DbPort"),
                    Description = "数据库端口"
                },
                new TrafficParameter
                {
                    Key = "DbUser",
                    Value = _configuration.GetValue<string>("DbUser"),
                    Description = "数据库用户"
                },
                new TrafficParameter
                {
                    Key = "DbPassword",
                    Value = _configuration.GetValue<string>("DbPassword"),
                    Description = "数据库密码"
                },
                new TrafficParameter
                {
                    Key = "DeviceDb",
                    Value = "Traffic_Device",
                    Description = "设备数据库"
                },
                new TrafficParameter
                {
                    Key = "FlowDb",
                    Value = "Traffic_Flow",
                    Description = "流量数据库"
                },
                new TrafficParameter
                {
                    Key = "DensityDb",
                    Value = "Traffic_Density",
                    Description = "密度数据库"
                },
                new TrafficParameter
                {
                    Key = "ViolationDb",
                    Value = "Traffic_Violation",
                    Description = "违法事件数据库"
                },
                new TrafficParameter
                {
                    Key = "CacheIp",
                    Value = _configuration.GetValue<string>("CacheIp"),
                    Description = "缓存地址"
                },
                new TrafficParameter
                {
                    Key = "CachePort",
                    Value = _configuration.GetValue<string>("CachePort"),
                    Description = "缓存端口"
                }
            };
        }

        /// <summary>
        /// 新增参数类型
        /// </summary>
        /// <param name="parameterType">参数</param>
        /// <returns>添加结果</returns>
        [HttpPost("types")]
        public async Task<IActionResult> PostParameterType([FromBody] TrafficParameterType parameterType)
        {
            if (DefaultType == parameterType.Type)
            {
                return Conflict();
            }
            List<TrafficParameter> parameters = GetDefaultParameters();
            foreach (TrafficParameter parameter in parameters)
            {
                parameter.Type = parameterType.Type;
            }
            _context.Parameters.AddRange(parameters);
            try
            {
                await _context.SaveChangesAsync();
            }

            catch (DbUpdateException)
            {
                if (_context.Parameters.Count(p => p.Type == parameterType.Type ) > 0)
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return Ok(parameterType);
        }

        /// <summary>
        /// 添加参数
        /// </summary>
        /// <param name="trafficParameter">参数</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public async Task<IActionResult> PostParameter([FromBody] TrafficParameter trafficParameter)
        {
            if (DefaultType == trafficParameter.Type)
            {
                return BadRequest();
            }
            if (_context.Parameters.Count(p => p.Type == trafficParameter.Type) == 0)
            {
                ModelState.AddModelError("Type", $"不存在的参数类型 {trafficParameter.Type}");
                return BadRequest(ModelState);
            }

            _context.Parameters.Add(trafficParameter);

            try
            {
                await _context.SaveChangesAsync();
            }

            catch (DbUpdateException)
            {
                if (_context.Parameters.Count(p => p.Type == trafficParameter.Type && p.Key == trafficParameter.Key) > 0)
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return Ok(trafficParameter);
        }

        /// <summary>
        /// 更新参数
        /// </summary>
        /// <param name="trafficParameter">参数</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public async Task<IActionResult> PutParameter([FromBody] TrafficParameter trafficParameter)
        {
            if (DefaultType == trafficParameter.Type)
            {
                return BadRequest();
            }
            _context.Entry(trafficParameter).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (_context.Parameters.Count(p => p.Type == trafficParameter.Type && p.Key == trafficParameter.Key) == 0)
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok();
        }

        /// <summary>
        /// 删除参数
        /// </summary>
        /// <param name="type">系统编号</param>
        /// <returns>删除结果</returns>
        [HttpDelete("types/{type}")]
        public async Task<IActionResult> DeleteParameterType([FromRoute] string type)
        {
            if (DefaultType == type)
            {
                return BadRequest();
            }
            List<TrafficParameter> parameters = _context.Parameters.Where(p => p.Type == type).ToList();
            _context.Parameters.RemoveRange(parameters);
            await _context.SaveChangesAsync();
            return Ok(parameters);
        }

        /// <summary>
        /// 删除参数
        /// </summary>
        /// <param name="type">系统编号</param>
        /// <param name="key">参数键</param>
        /// <returns>删除结果</returns>
        [HttpDelete("types/{type}/keys/{key}")]
        public async Task<IActionResult> DeleteParameter([FromRoute] string type, string key)
        {
            if (DefaultType == type)
            {
                return BadRequest();
            }
            TrafficParameter trafficParameter = await _context.Parameters.SingleOrDefaultAsync(p => p.Type == type && p.Key == key);
            if (trafficParameter == null)
            {
                return NotFound();
            }

            _context.Parameters.Remove(trafficParameter);
            await _context.SaveChangesAsync();
            return Ok(trafficParameter);
        }

    }
}
