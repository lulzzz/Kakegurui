using System;
using System.Collections.Generic;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Microsoft.AspNetCore.Mvc;
using MomobamiKirari.Managers;
using MomobamiKirari.Models;

namespace MomobamiKirari.Controllers
{
    /// <summary>
    /// 路段流量
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class SectionFlowsController
    {
        /// <summary>
        /// 路段流量数据库操作实例
        /// </summary>
        private readonly SectionFlowsManager _manager;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="manager">路段流量数据库操作实例</param>
        public SectionFlowsController(SectionFlowsManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// 交通状态时间查询
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <param name="level">日期级别</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>查询结果</returns>
        [HttpGet("status/sections/{sectionId}")]
        public List<SectionStatus> QueryStatusList([FromRoute]int sectionId, [FromQuery]DateTimeLevel level, [FromQuery]DateTime startTime, [FromQuery]DateTime endTime)
        {
            return _manager.QueryStatusList(sectionId, level, startTime, endTime);
        }

        /// <summary>
        /// 拥堵时长图表查询
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <param name="level">日期级别</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>查询结果</returns>
        [HttpGet("congestion/sections/{sectionId}")]
        public List<TrafficChart<DateTime, int>> QueryCongestionChart([FromRoute]int sectionId, [FromQuery]DateTimeLevel level, [FromQuery]DateTime startTime, [FromQuery]DateTime endTime)
        {
            return _manager.QueryCongestionChart(sectionId, level, startTime, endTime);
        }
    }
}
