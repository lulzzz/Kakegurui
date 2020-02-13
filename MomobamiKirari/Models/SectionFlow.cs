using System;
using ItsukiSumeragi.Models;
using ItsukiSumeragi.Codes.Flow;

namespace MomobamiKirari.Models
{
    /// <summary>
    /// 路段流量
    /// vkt比例=vkt/所有类型的vkt和
    /// 平均速度=sum(vkt/fla*vkt比例)
    /// 拥堵指数=sum(TravelTimeProportion/TravelTimeProportionCount*vkt比例)
    /// </summary>
    public class SectionFlow : TrafficData
    {
        /// <summary>
        /// 路段编号
        /// </summary>
        public int SectionId { get; set; }

        /// <summary>
        /// 路段名称
        /// </summary>
        public string SectionName { get; set; }

        /// <summary>
        /// 路段类型
        /// </summary>
        public int SectionType { get; set; }

        /// <summary>
        /// 路段长度(米)
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 路段车道检测长度(米)
        /// </summary>
        public int Distance { get; set; }

        /// <summary>
        /// 行程时间(秒)
        /// </summary>
        public double TravelTime { get; set; }

        /// <summary>
        /// 路段平均速度(千米/小时)
        /// </summary>
        public double AverageSpeed { get; set; }

        /// <summary>
        /// 自由流速度(千米/小时)
        /// </summary>
        public double FreeSpeed { get; set; }

        /// <summary>
        /// 车头时距(秒)
        /// </summary>
        public double HeadDistance { get; set; }

        /// <summary>
        /// 车头间距(米)
        /// </summary>
        public double HeadSpace => HeadDistance * AverageSpeed;

        /// <summary>
        /// 时间占有率(%)
        /// </summary>
        public int TimeOccupancy { get; set; }

        /// <summary>
        /// 空间占用率(%)
        /// </summary>
        public int Occupancy { get; set; }

        /// <summary>
        /// 交通状态
        /// </summary>
        public TrafficStatus TrafficStatus { get; set; }

        /// <summary>
        /// 数据数量
        /// </summary>
        public int Count { get; set; }

        #region 计算参数
        /// <summary>
        /// vkt=机动车流量*路段长度(米)
        /// </summary>
        public double Vkt { get; set; }

        /// <summary>
        /// sum(机动车流量*(路段长度(米)/路段平均速度(米/秒))）
        /// </summary>
        public double Fls { get; set; }

        /// <summary>
        /// 行程时间比
        /// </summary>
        public double TravelTimeProportion { get; set; }

        /// <summary>
        /// 总流量
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 机动车总流量
        /// </summary>
        public int Vehicle { get; set; }

        #endregion

        #region 拥堵数据
        /// <summary>
        /// 拥堵开始时间
        /// </summary>
        public DateTime CongestionStartTime { get; set; }

        /// <summary>
        /// 拥堵时长
        /// </summary>
        public int CongestionSpan { get; set; }

        /// <summary>
        /// 当前拥堵时长
        /// </summary>
        public int CurrentCongestionSpan { get; set; }

        #endregion
    }
}
