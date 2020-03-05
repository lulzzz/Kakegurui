using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomobamiRirika.Managers;
using MomobamiRirika.Models;

namespace MomobamiRirika.Controllers
{
    /// <summary>
    /// 通道
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ChannelsController : ControllerBase
    {
        /// <summary>
        /// 通道数据库操作实例
        /// </summary>
        private readonly ChannelsManager _manager;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="manager">通道数据库操作实例</param>
        public ChannelsController(ChannelsManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// 查询通道集合
        /// </summary>
        /// <param name="channelName">通道名称</param>
        /// <param name="crossingId">路口编号</param>
        /// <param name="alone">是否查询未关联通道</param>
        /// <param name="pageNum">分页页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        public PageModel<DensityChannel> GetList([FromQuery] string channelName, [FromQuery] int crossingId, [FromQuery] bool alone, [FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            return _manager.GetList(channelName, crossingId, alone, pageNum, pageSize);
        }

        /// <summary>
        /// 查询通道
        /// </summary>
        /// <param name="channelId"/>通道编号/param>
        /// <returns>查询结果</returns>
        [HttpGet("{channelId}")]
        public IActionResult Get([FromRoute] string channelId)
        {
            return _manager.Get(channelId);
        }

        /// <summary>
        /// 添加通道
        /// </summary>
        /// <param name="channel">通道</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public IActionResult Add([FromBody] DensityChannel channel)
        {
            return _manager.Add(channel, User?.Identity?.Name);
        }

        /// <summary>
        /// 导入通道
        /// </summary>
        /// <param name="file">文件</param>
        /// <returns>导入结果</returns>
        [HttpPost("import")]
        public IActionResult Import(IFormFile file)
        {
            return _manager.Import(file, User?.Identity?.Name);
        }

        /// <summary>
        /// 更新通道
        /// </summary>
        /// <param name="updateChannel">通道</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public IActionResult Update([FromBody] DensityChannel updateChannel)
        {
            return _manager.Update(updateChannel);
        }

        /// <summary>
        /// 更新通道标注状态
        /// </summary>
        /// <param name="channelUpdateLocation">通道标注状态</param>
        /// <returns>更新结果</returns>
        [HttpPut("location")]
        public IActionResult UpdateLocation([FromBody] DensityChannelUpdateLocation channelUpdateLocation)
        {
            return _manager.UpdateLocation(channelUpdateLocation);
        }

        /// <summary>
        /// 更新通道状态
        /// </summary>
        /// <param name="channelUpdateStatus">通道状态</param>
        /// <returns>更新结果</returns>
        [HttpPut("status")]
        public IActionResult UpdateStatus([FromBody] DensityChannelUpdateStatus channelUpdateStatus)
        {
            return _manager.UpdateStatus(channelUpdateStatus);
        }

        /// <summary>
        /// 删除通道
        /// </summary>
        /// <param name="channelId">通道编号</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{channelId}")]
        public IActionResult Remove([FromRoute]string channelId)
        {
            return _manager.Remove(channelId);
        }
    }
}
