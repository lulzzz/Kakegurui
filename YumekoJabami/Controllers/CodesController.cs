using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YumekoJabami.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YumekoJabami.Codes;
using YumekoJabami.Data;

namespace YumekoJabami.Controllers
{
    /// <summary>
    /// 字典
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class CodesController : ControllerBase
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly SystemContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库实例</param>
        public CodesController(SystemContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 按系统查询字典集合
        /// </summary>
        /// <returns>查询结果</returns>
        [HttpGet]
        public IEnumerable<TrafficCode> GetCodes()
        {
            return _context.Codes;
        }

        /// <summary>
        /// 按系统查询字典集合
        /// </summary>
        /// <param name="system">系统编号</param>
        /// <returns>查询结果</returns>
        [HttpGet("systems/{system}")]
        public IEnumerable<TrafficCode> GetCodes([FromRoute] SystemType system)
        {
            return _context.Codes
                .Where(c => c.System == system);
        }

        /// <summary>
        /// 按系统查询字典键集合
        /// </summary>
        /// <param name="system">系统编号</param>
        /// <returns>查询结果</returns>
        [HttpGet("systems/{system}/keys")]
        public IEnumerable<TrafficCode> GetCodeKeys([FromRoute] SystemType system)
        {

            return _context.Codes
                .Where(c => c.System == system)
                .Select(c => c.Key)
                //这里如果不加ToList,在筛选重复的时候会忽略大小写
                .ToList()
                .Distinct()
                .Select(k=>new TrafficCode{Key=k});
        }

        /// <summary>
        /// 按系统和键查询字典集合
        /// </summary>
        /// <param name="system">系统编号</param>
        /// <param name="key">字典键</param>
        /// <returns>查询结果</returns>
        [HttpGet("systems/{system}/keys/{key}")]
        public IEnumerable<TrafficCode> GetCodes([FromRoute] SystemType system, [FromRoute] string key)
        {
            return _context.Codes
                .Where(c => c.System == system && c.Key == key)
                .OrderBy(c=>c.Value);
        }

        /// <summary>
        /// 添加字典
        /// </summary>
        /// <param name="trafficCode">字典</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public async Task<IActionResult> PostCode([FromBody] TrafficCode trafficCode)
        {
            _context.Codes.Add(trafficCode);

            try
            {
                await _context.SaveChangesAsync();
            }

            catch (DbUpdateException)
            {
                if (_context.Codes.Count(c => c.System == trafficCode.System && c.Key == trafficCode.Key && c.Value == trafficCode.Value)>0)
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }
            return Ok(trafficCode);
        }

        /// <summary>
        /// 更新字典
        /// </summary>
        /// <param name="trafficCode">字典</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public async Task<IActionResult> PutCode([FromBody] TrafficCode trafficCode)
        {
            _context.Entry(trafficCode).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (_context.Codes.Count(c => c.System == trafficCode.System && c.Key == trafficCode.Key && c.Value == trafficCode.Value) ==0)
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
        /// 删除字典
        /// </summary>
        /// <param name="system">系统编号</param>
        /// <param name="key">字典键</param>
        /// <param name="value">字典值</param>
        /// <returns>删除结果</returns>
        [HttpDelete("systems/{system}/keys/{key}/values/{value}")]
        public async Task<IActionResult> DeleteCode([FromRoute] SystemType system, [FromRoute]string key, [FromRoute]int value)
        {
            var code = await _context.Codes.SingleOrDefaultAsync(c => c.System == system && c.Key == key && c.Value == value);
            if (code == null)
            {
                return NotFound();
            }

            _context.Codes.Remove(code);
            await _context.SaveChangesAsync();
            return Ok(code);
        }

    }
}
