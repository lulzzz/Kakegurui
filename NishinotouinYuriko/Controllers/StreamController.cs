using Microsoft.AspNetCore.Mvc;
using NishinotouinYuriko.Monitor;

namespace NishinotouinYuriko.Controllers
{
    /// <summary>
    /// 流媒体
    /// </summary>
    [Produces("application/json")]
    [Route("api/media/mss/stream/pushers")]
    public class StreamController : ControllerBase
    {
        /// <summary>
        /// 流媒体监控
        /// </summary>
        private readonly StreamMonitor _streamMonitor;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="streamMonitor">流媒体监控</param>
        public StreamController(StreamMonitor streamMonitor)
        {
            _streamMonitor = streamMonitor;
        }

        /// <summary>
        /// 添加流媒体
        /// </summary>
        /// <param name="url">视频地址</param>
        /// <param name="path">流媒体路径</param>
        /// <returns>添加结果</returns>
        [HttpGet("add")]
        public IActionResult AddStream([FromQuery] string url,[FromQuery]string path)
        {
            return Ok(_streamMonitor.Add(url, path));
        }

        /// <summary>
        /// 更新流媒体心跳时间
        /// </summary>
        /// <param name="path">流媒体路径</param>
        /// <returns>更新结果</returns>
        [HttpGet("update")]
        public IActionResult UpdateStream([FromQuery]string path)
        {
            _streamMonitor.Update(path);
            return Ok();
        }
    }
}
