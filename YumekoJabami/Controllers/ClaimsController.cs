using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using YumekoJabami.Data;
using Claim = YumekoJabami.Models.Claim;

namespace YumekoJabami.Controllers
{
    /// <summary>
    /// 权限
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ClaimsController : ControllerBase
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly SystemContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库实例</param>
        public ClaimsController(SystemContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 查询权限集合
        /// </summary>
        /// <returns>查询结果</returns>
        [HttpGet]
        public IEnumerable<Claim> GetCustomClaims()
        {
            return _context.TrafficClaims;
        }
    }
}
