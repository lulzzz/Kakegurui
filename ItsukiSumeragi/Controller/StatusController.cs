using System.Reflection;
using ItsukiSumeragi.Models;
using ItsukiSumeragi.Monitor;
using Microsoft.AspNetCore.Mvc;

namespace ItsukiSumeragi.Controller
{
    /// <summary>
    /// 系统状态
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class StatusController:ControllerBase
    {
        /// <summary>
        /// 获取系统时间戳
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetTimeStamp()
        {
            return Ok(new SystemStatus
            {
                Version= Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                TimeStamp = SystemSyncPublisher.TimeStamp
            });
        }
    }
}
