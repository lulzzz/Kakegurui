using Microsoft.AspNetCore.Mvc;
using MomobamiKirari.Managers;
using MomobamiKirari.Models;

namespace MomobamiKirari.Controllers
{
    /// <summary>
    /// 通道流量
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ChannelFlowsController
    {
        /// <summary>
        /// 通道流量数据库操作实例
        /// </summary>
        private readonly ChannelFlowsManager _manager;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="manager">通道流量数据库操作实例</param>
        public ChannelFlowsController(ChannelFlowsManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// 查询通道当天流量状态
        /// </summary>
        /// <param name="channelId">通道编号</param>
        /// <returns>通道流量状态</returns>
        [HttpGet("day/channels/{channelId}")]
        public ChannelDayFlow QueryChannelDayStatus([FromRoute]string channelId)
        {
            return _manager.QueryChannelDayStatus(channelId);
        }

        /// <summary>
        /// 查询通道小时流量状态
        /// </summary>
        /// <param name="channelId">通道编号</param>
        /// <returns>通道流量状态</returns>
        [HttpGet("hour/channels/{channelId}")]
        public ChannelHourFlow QueryChannelHourStatus([FromRoute]string channelId)
        {
            return _manager.QueryChannelHourStatus(channelId);
        }

        /// <summary>
        /// 查询通道分钟流量状态
        /// </summary>
        /// <param name="channelId">通道编号</param>
        /// <returns>通道流量状态</returns>
        [HttpGet("minute/channels/{channelId}")]
        public ChannelMinuteFlow QueryChannelMuniteStatus([FromRoute]string channelId)
        {
            return _manager.QueryChannelMuniteStatus(channelId);
        }
    }
}