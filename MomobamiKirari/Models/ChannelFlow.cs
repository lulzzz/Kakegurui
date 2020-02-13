using System;
using System.Collections.Generic;
using ItsukiSumeragi.Models;

namespace MomobamiKirari.Models
{
    /// <summary>
    /// 车道流量
    /// </summary>
    public class LaneFlowItem
    {
        /// <summary>
        /// 数据编号
        /// </summary>
        public string DataId { get; set; }

        /// <summary>
        /// 车道名称
        /// </summary>
        public string LaneName { get; set; }

        /// <summary>
        /// 流量总和
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 机动车流量总和
        /// </summary>
        public int Vehicle { get; set; }

        /// <summary>
        /// 机动车流量总和
        /// </summary>
        public int Bike { get; set; }

        /// <summary>
        /// 行人流量总和
        /// </summary>
        public int Person { get; set; }

        /// <summary>
        /// 空间占有率
        /// </summary>
        public int Occupancy { get; set; }

        /// <summary>
        /// 时间占有率
        /// </summary>
        public int TimeOccupancy { get; set; }

        /// <summary>
        /// 数据数量
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// 通道当天流量状态
    /// </summary>
    public class ChannelDayFlow
    {
        /// <summary>
        /// 通道编号
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// 今天车道流量集合
        /// </summary>
        public List<LaneFlowItem> TodayDayLanes { get; set; }
        /// <summary>
        /// 昨天车道流量集合
        /// </summary>
        public List<LaneFlowItem> YesterdayDayLanes { get; set; }
        /// <summary>
        /// 上月今天车道流量集合
        /// </summary>
        public List<LaneFlowItem> LastMonthDayLanes { get; set; }
        /// <summary>
        /// 去年今天车道流量集合
        /// </summary>
        public List<LaneFlowItem> LastYearDayLanes { get; set; }

        /// <summary>
        /// 今天车道图表数据
        /// </summary>
        public List<List<TrafficChart<DateTime, int,LaneFlow>>> TodayDayCharts { get; set; }
        /// <summary>
        /// 昨天车道图表数据
        /// </summary>
        public List<List<TrafficChart<DateTime, int,LaneFlow>>> YesterdayDayCharts { get; set; }
        /// <summary>
        /// 上月今天车道图表数据
        /// </summary>
        public List<List<TrafficChart<DateTime, int,LaneFlow>>> LastMonthDayCharts { get; set; }
        /// <summary>
        /// 去年今天车道图表数据
        /// </summary>
        public List<List<TrafficChart<DateTime, int,LaneFlow>>> LastYearDayCharts { get; set; }

    }

    /// <summary>
    /// 通道小时流量
    /// </summary>
    public class ChannelHourFlow
    {
        /// <summary>
        /// 通道编号
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// 今天车道流量集合
        /// </summary>
        public List<LaneFlowItem> TodayHourLanes { get; set; }

        /// <summary>
        /// 今天车道图表数据
        /// </summary>
        public List<List<TrafficChart<DateTime, int, LaneFlow>>> TodayHourCharts { get; set; }

    }

    /// <summary>
    /// 通道分钟流量状态
    /// </summary>
    public class ChannelMinuteFlow
    {
        /// <summary>
        /// 路段流量
        /// </summary>
        public SectionFlow SectionFlow { get; set; }

        /// <summary>
        /// 车道流量集合
        /// </summary>
        public List<LaneFlow> LanesFlow { get; set; }
    }
}
