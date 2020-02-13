using Microsoft.AspNetCore.Mvc;
using MomobamiKirari.Models;
using MomobamiKirari.Monitor;

namespace MomobamiKirari.Controllers
{
    /// <summary>
    /// 城市流量
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class CityFlowsController
    {
        /// <summary>
        /// 查询城市流量状态
        /// </summary>
        /// <returns>城市流量状态</returns>
        [HttpGet("status")]
        public CityStatus QueryTrafficStatus()
        {
            return SectionFlowMonitor.Status;
        }
    }
}
