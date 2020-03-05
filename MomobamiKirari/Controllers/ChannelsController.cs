using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomobamiKirari.Managers;
using MomobamiKirari.Models;

namespace MomobamiKirari.Controllers
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
        private readonly ChannelsManager _channelsManager;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="channelsManager">通道数据库操作实例</param>
        public ChannelsController(ChannelsManager channelsManager)
        {
            _channelsManager = channelsManager;
        }

        /// <summary>
        /// 查询通道集合
        /// </summary>
        /// <param name="channelName">通道名称</param>
        /// <param name="crossingId">路口编号</param>
        /// <param name="sectionId">路段编号</param>
        /// <param name="alone">是否查询未关联通道</param>
        /// <param name="pageNum">分页页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        public PageModel<FlowChannel> GetList([FromQuery] string channelName, [FromQuery] int crossingId, [FromQuery] int sectionId, [FromQuery] bool alone, [FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            return _channelsManager.GetList(channelName,crossingId,sectionId,alone,pageNum,pageSize);
        }

        /// <summary>
        /// 查询通道
        /// </summary>
        /// <param name="channelId"/>通道编号/param>
        /// <returns>查询结果</returns>
        [HttpGet("{channelId}")]
        public IActionResult Get([FromRoute] string channelId)
        {
            return _channelsManager.Get(channelId);
        }

        /// <summary>
        /// 按路段查询车道集合
        /// </summary>
        /// <param name="sectionName">路段名称</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet("Group/Section")]
        public PageModel<object> GetGroupBySection([FromQuery] string sectionName,[FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            return _channelsManager.GetGroupBySection(sectionName, pageNum, pageSize);
        }

        /// <summary>
        /// 添加通道
        /// </summary>
        /// <param name="channel">通道</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public IActionResult Add([FromBody] FlowChannel channel)
        {
            return _channelsManager.Add(channel, User?.Identity?.Name);
        }

        /// <summary>
        /// 导入通道
        /// </summary>
        /// <param name="file">文件</param>
        /// <returns>导入结果</returns>
        [HttpPost("import")]
        public IActionResult Import(IFormFile file)
        {
            return _channelsManager.Import(file, User?.Identity?.Name);
        }

        /// <summary>
        /// 更新通道
        /// </summary>
        /// <param name="updateChannel">通道</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public IActionResult Update([FromBody] FlowChannel updateChannel)
        {
            return _channelsManager.Update(updateChannel);
        }

        /// <summary>
        /// 更新通道标注状态
        /// </summary>
        /// <param name="channelUpdateLocation">通道标注状态</param>
        /// <returns>更新结果</returns>
        [HttpPut("location")]
        public IActionResult UpdateLocation([FromBody] FlowChannelUpdateLocation channelUpdateLocation)
        {
            return _channelsManager.UpdateLocation(channelUpdateLocation);
        }

        /// <summary>
        /// 更新通道状态
        /// </summary>
        /// <param name="channelUpdateStatus">通道状态</param>
        /// <returns>更新结果</returns>
        [HttpPut("status")]
        public IActionResult UpdateStatus([FromBody] FlowChannelUpdateStatus channelUpdateStatus)
        {
            return _channelsManager.UpdateStatus(channelUpdateStatus);
        }

        /// <summary>
        /// 删除通道
        /// </summary>
        /// <param name="channelId">通道编号</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{channelId}")]
        public IActionResult Remove([FromRoute]string channelId)
        {
            return _channelsManager.Remove(channelId, User?.Identity?.Name);
        }
    }
}
