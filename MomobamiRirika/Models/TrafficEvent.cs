using System;
using System.ComponentModel.DataAnnotations.Schema;
using ItsukiSumeragi.Models;

namespace MomobamiRirika.Models
{
    /// <summary>
    /// 交通事件
    /// </summary>
    public class TrafficEvent:TrafficData
    {
        /// <summary>
        /// 结束时间
        /// </summary>
        [Column("EndTime", TypeName = "DATETIME")]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 路口编号
        /// </summary>
        [NotMapped]
        public int CrossingId { get; set; }

        /// <summary>
        /// 路口名称
        /// </summary>
        [NotMapped]
        public string CrossingName { get; set; }

        /// <summary>
        /// 区域名称
        /// </summary>
        [NotMapped]
        public string RegionName { get; set; }

        /// <summary>
        /// 数据匹配编号
        /// </summary>
        [NotMapped]
        public string MatchId { get; set; }
    }
}
