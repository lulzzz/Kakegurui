using System;
using System.Collections.Generic;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Microsoft.AspNetCore.Mvc;
using MomobamiRirika.Managers;
using MomobamiRirika.Models;

namespace MomobamiRirika.Controllers
{
    /// <summary>
    /// 高点密度
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class DensitiesController : Controller
    {
        /// <summary>
        /// 密度查询实例
        /// </summary>
        private readonly DensitiesManager _manager;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="manager">密度查询实例</param>
        public DensitiesController(DensitiesManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// 按区域查询密度集合
        /// </summary>
        /// <param name="dataId">数据编号</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>高点密度数据集合</returns>
        [HttpGet("regions/{dataId}")]
        public List<TrafficDensity> QueryList([FromRoute]string dataId, [FromQuery]DateTime startTime, [FromQuery]DateTime endTime)
        {
            return _manager.QueryList(dataId, DateTimeLevel.Minute, startTime, endTime);
        }

        /// <summary>
        /// 按区域查询密度同比环比分析
        /// </summary>
        /// <param name="dataId">数据编号</param>
        /// <param name="level">密度时间粒度</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>高点密度数据集合</returns>
        [HttpGet("analysis/regions/{dataId}")]
        public List<List<TrafficChart<DateTime,int>>> QueryComparison([FromRoute]string dataId, [FromQuery]DateTimeLevel level, [FromQuery]DateTime startTime, [FromQuery]DateTime endTime)
        {
            return _manager.QueryComparison(dataId, level, startTime, endTime);
        }

        /// <summary>
        /// 今日密度top10
        /// </summary>
        /// <returns>密度数据集合</returns>
        [HttpGet("top10/day")]
        public List<TrafficDensity> QueryDayTop10()
        {
            return _manager.QueryTop10(DateTime.Today);
        }

        /// <summary>
        /// 最近一小时密度top10
        /// </summary>
        /// <returns>密度数据集合</returns>
        [HttpGet("top10/hour")]
        public List<TrafficDensity> QueryHourTop10()
        {
            DateTime time = DateTime.Now.AddHours(-1);
            return _manager.QueryTop10(time);
        }

        /// <summary>
        /// 今日变化密度top10
        /// </summary>
        /// <returns>密度数据集合</returns>
        [HttpGet("changetop10/day")]
        public List<TrafficDensity> QueryChangeDayTop10()
        {
            DateTime today = DateTime.Today;
            DateTime now = DateTime.Now;
            return _manager.QueryChangeTop10(today, today.AddDays(-1), today.AddDays(-1).Add(now.TimeOfDay));
        }

        /// <summary>
        /// 最近一小时变化密度top10
        /// </summary>
        /// <returns>密度数据集合</returns>
        [HttpGet("changetop10/hour")]
        public List<TrafficDensity> QueryChangeHourTop10()
        {
            DateTime now = DateTime.Now;
            return _manager.QueryChangeTop10(now.AddHours(-1), now.AddHours(-2), now.AddHours(-1));
        }

        /// <summary>
        /// 重点区域密度
        /// </summary>
        /// <returns>密度数据集合</returns>
        [HttpGet("vipRegions")]
        public List<TrafficDensity> QueryVipRegions()
        {
            return _manager.QueryVipRegions();
        }
    }
}