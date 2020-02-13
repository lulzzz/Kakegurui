using System.Collections.Generic;

namespace MomobamiKirari.Models
{
    /// <summary>
    /// 路网流量状态
    /// </summary>
    public class CityStatus
    {
        /// <summary>
        /// 当天总流量
        /// </summary>
        public int TotalFlow { get; set; }

        /// <summary>
        /// 当天平均速度
        /// </summary>
        public long AverageSpeed { get; set; }

        /// <summary>
        /// 当天拥堵指数
        /// </summary>
        public Dictionary<int, double> CongestionDatas { get; set; }

        /// <summary>
        /// 路段当前交通状态
        /// </summary>
        public Dictionary<int, SectionsSpeed> SectionStatuses { get; set; }

        /// <summary>
        /// 拥堵路段列表
        /// </summary>
        public List<SectionFlow> SectionCongestions { get; set; }

        /// <summary>
        /// 路段拥堵排名
        /// </summary>
        public List<SectionFlow> SectionCongestionRank { get; set; }

    }

}
