using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using YumekoJabami.Codes;

namespace YumekoJabami.Controllers
{
    /// <summary>
    /// 系统
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class SystemsController : ControllerBase
    {
        /// <summary>
        /// 查询系统集合
        /// </summary>
        /// <returns>查询结果</returns>
        [HttpGet]
        public IActionResult GetSystems()
        {
            return Ok(Enum.GetValues(typeof(SystemType))
                .Cast<SystemType>()
                .Select(e => new {Id = (int) e, Name = e.ToString()}));
        }
    }
}
