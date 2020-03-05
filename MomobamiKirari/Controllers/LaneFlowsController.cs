using System;
using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Mvc;
using MomobamiKirari.Managers;
using MomobamiKirari.Models;
using MomobamiKirari.Codes;

namespace MomobamiKirari.Controllers
{
    /// <summary>
    /// 车道流量
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class LaneFlowsController : Controller
    {
        /// <summary>
        /// 车道流量数据库操作实例
        /// </summary>
        private readonly LaneFlowManager _manager;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="manager">车道流量数据库操作实例</param>
        public LaneFlowsController(LaneFlowManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// 按路口查询流量数据
        /// </summary>
        /// <param name="crossingId">路口编号</param>
        /// <param name="flowTypes">流量密度数据</param>
        /// <param name="level">时间粒度</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <returns>流量数据集合</returns>
        [DateTimeUrl("startTimes")]
        [DateTimeUrl("endTimes")]
        [HttpGet("crossings/{crossingId}")]
        public List<List<TrafficChart<DateTime,int, LaneFlow>>> QueryChartsByCrossing([FromRoute]int crossingId, [FromQuery]DateTimeLevel level, [FromQuery]FlowType[] flowTypes, [FromQuery]DateTime[] startTimes, [FromQuery]DateTime[] endTimes)
        {
            return _manager.QueryChartsByCrossing(crossingId, level, flowTypes, startTimes, endTimes);
        }

        /// <summary>
        /// 按路口方向查询流量数据
        /// </summary>
        /// <param name="crossingId">路口编号</param>
        /// <param name="directions">路口方向集合</param>
        /// <param name="level">时间粒度</param>
        /// <param name="flowTypes">流量密度数据</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <returns>流量数据集合</returns>
        [IntegersUrl("directions")]
        [DateTimeUrl("startTimes")]
        [DateTimeUrl("endTimes")]
        [HttpGet("crossings/{crossingId}/directions/{directions}")]
        public List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryChartsByCrossing([FromRoute]int crossingId, [FromRoute]int[] directions, [FromQuery]DateTimeLevel level, [FromQuery]FlowType[] flowTypes, [FromQuery]DateTime[] startTimes, [FromQuery]DateTime[] endTimes)
        {
            return _manager.QueryChartsByCrossing(crossingId, directions, level, flowTypes, startTimes, endTimes);
        }

        /// <summary>
        /// 按路口流向查询流量数据
        /// </summary>
        /// <param name="crossingId">路口编号</param>
        /// <param name="direction">路口方向集</param>
        /// <param name="flowDirections">路口流向合</param>
        /// <param name="level">时间粒度</param>
        /// <param name="flowTypes">流量密度数据</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <returns>流量数据集合</returns>
        [IntegersUrl("flowDirections")]
        [DateTimeUrl("startTimes")]
        [DateTimeUrl("endTimes")]
        [HttpGet("crossings/{crossingId}/directions/{direction}/flowDirections/{flowDirections}")]
        public List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryChartsByCrossing([FromRoute]int crossingId, [FromRoute]int direction, [FromRoute]int[] flowDirections, [FromQuery]DateTimeLevel level, [FromQuery]FlowType[] flowTypes, [FromQuery]DateTime[] startTimes, [FromQuery]DateTime[] endTimes)
        {
            return _manager.QueryChartsByCrossing(crossingId, direction,flowDirections, level, flowTypes, startTimes, endTimes);
        }

        /// <summary>
        /// 按路口和车道查询流量数据
        /// </summary>
        /// <param name="crossingId">路口编号</param>
        /// <param name="dataIds">车道数据编号集合</param>
        /// <param name="level">时间粒度</param>
        /// <param name="flowTypes">流量密度数据</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <returns>流量数据集合</returns>
        [StringsUrl("dataIds")]
        [DateTimeUrl("startTimes")]
        [DateTimeUrl("endTimes")]
        [HttpGet("crossings/{crossingId}/lanes/{dataIds}")]
        public List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryChartsByCrossing([FromRoute]int crossingId, [FromRoute]string[] dataIds, [FromQuery]DateTimeLevel level, [FromQuery]FlowType[] flowTypes, [FromQuery]DateTime[] startTimes, [FromQuery]DateTime[] endTimes)
        {
            return _manager.QueryCharts(dataIds.Select(Uri.UnescapeDataString).ToHashSet(), level, startTimes, endTimes, flowTypes);
        }

        /// <summary>
        /// 按路段查询流量数据
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <param name="flowTypes">流量密度数据</param>
        /// <param name="level">时间粒度</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <returns>流量数据集合</returns>
        [DateTimeUrl("startTimes")]
        [DateTimeUrl("endTimes")]
        [HttpGet("sections/{sectionId}")]
        public List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryChartsBySection([FromRoute]int sectionId, [FromQuery]DateTimeLevel level, [FromQuery]FlowType[] flowTypes, [FromQuery]DateTime[] startTimes, [FromQuery]DateTime[] endTimes)
        {
            return _manager.QueryChartsBySection(sectionId, level, flowTypes, startTimes, endTimes);
        }

        /// <summary>
        /// 按路段流向查询流量数据
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <param name="flowDirections">路口流向合</param>
        /// <param name="level">时间粒度</param>
        /// <param name="flowTypes">流量密度数据</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <returns>流量数据集合</returns>
        [IntegersUrl("flowDirections")]
        [DateTimeUrl("startTimes")]
        [DateTimeUrl("endTimes")]
        [HttpGet("sections/{sectionId}/flowDirections/{flowDirections}")]
        public List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryChartsBySection([FromRoute]int sectionId, [FromRoute]int[] flowDirections, [FromQuery]DateTimeLevel level, [FromQuery]FlowType[] flowTypes, [FromQuery]DateTime[] startTimes, [FromQuery]DateTime[] endTimes)
        {
            return _manager.QueryChartsBySection(sectionId,flowDirections, level, flowTypes, startTimes, endTimes);
        }

        /// <summary>
        /// 按路口和车道查询流量数据
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <param name="dataIds">车道数据编号集合</param>
        /// <param name="level">时间粒度</param>
        /// <param name="flowTypes">流量密度数据</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <returns>流量数据集合</returns>
        [StringsUrl("dataIds")]
        [DateTimeUrl("startTimes")]
        [DateTimeUrl("endTimes")]
        [HttpGet("sections/{sectionId}/lanes/{dataIds}")]
        public List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryChartsBySection([FromRoute]int sectionId, [FromRoute]string[] dataIds, [FromQuery]DateTimeLevel level, [FromQuery]FlowType[] flowTypes, [FromQuery]DateTime[] startTimes, [FromQuery]DateTime[] endTimes)
        {
            return _manager.QueryCharts(dataIds.Select(Uri.UnescapeDataString).ToHashSet(), level, startTimes, endTimes, flowTypes);
        }

        /// <summary>
        /// 按车道查询多组流量数据集合
        /// </summary>
        /// <param name="dataIds">车道数据编号集合</param>
        /// <param name="level">时间粒度</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <returns>流量数据集合</returns>
        [StringsUrl("dataIds")]
        [DateTimeUrl("startTimes")]
        [DateTimeUrl("endTimes")]
        [HttpGet("{dataIds}")]
        public List<List<LaneFlow>> QueryList([FromRoute]string[] dataIds, [FromQuery]DateTimeLevel level, [FromQuery]DateTime[] startTimes, [FromQuery]DateTime[] endTimes)
        {
            return _manager.QueryList(dataIds.Select(Uri.UnescapeDataString).ToHashSet(), level, startTimes, endTimes);
        }
    }
}