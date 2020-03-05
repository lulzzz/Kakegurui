using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YumekoJabami.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库实例</param>
        public ParametersController(SystemContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 按节点查询参数集合
        /// </summary>
        /// <returns>查询结果</returns>
        [HttpGet]
        public IEnumerable<Parameter> GetParameters()
        {
            return _context.Parameters;
        }

        /// <summary>
        /// 添加参数
        /// </summary>
        /// <param name="parameter">参数</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public async Task<IActionResult> PostParameter([FromBody] Parameter parameter)
        {
            _context.Parameters.Add(parameter);

            try
            {
                await _context.SaveChangesAsync();
            }

            catch (DbUpdateException)
            {
                if (_context.Parameters.Count(p => p.Key == parameter.Key) > 0)
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return Ok(parameter);
        }

        /// <summary>
        /// 更新参数
        /// </summary>
        /// <param name="parameter">参数</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public async Task<IActionResult> PutParameter([FromBody] Parameter parameter)
        {

            _context.Entry(parameter).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (_context.Parameters.Count(p => p.Key == parameter.Key) == 0)
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
        /// <param name="key">参数键</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{key}")]
        public async Task<IActionResult> DeleteParameter([FromRoute]string key)
        {
            Parameter parameter = await _context.Parameters.SingleOrDefaultAsync(p =>p.Key == key);
            if (parameter == null)
            {
                return NotFound();
            }

            _context.Parameters.Remove(parameter);
            await _context.SaveChangesAsync();
            return Ok(parameter);
        }

    }
}
