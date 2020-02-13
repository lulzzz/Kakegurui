using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ItsukiSumeragi.Models;
using Kakegurui.Core;

namespace MomobamiRirika.Models
{
    /// <summary>
    /// 密度数据
    /// </summary>
    public class TrafficDensity:TrafficData
    {
        /// <summary>
        /// 密度值
        /// </summary>
        [Required]
        [Column("Value", TypeName = "INT")]
        public int Value { get; set; }

        /// <summary>
        /// 区域名称
        /// </summary>
        [NotMapped]
        public string RegionName { get; set; }

        /// <summary>
        /// 数据时间粒度
        /// </summary>
        [NotMapped]
        public DateTimeLevel DateLevel { get; set; }

        /// <summary>
        /// 用于计算变化时的变量
        /// </summary>
        [NotMapped]
        public int LastValue { get; set; }

        /// <summary>
        /// 数据匹配编号
        /// </summary>
        [NotMapped]
        public string MatchId { get; set; }
    }

    /// <summary>
    /// 1分钟密度数据
    /// </summary>
    public class TrafficDensity_One : TrafficDensity
    {

    }

    /// <summary>
    /// 5分钟密度数据
    /// </summary>
    public class TrafficDensity_Five : TrafficDensity
    {

    }

    /// <summary>
    /// 15分钟密度数据
    /// </summary>
    public class TrafficDensity_Fifteen : TrafficDensity
    {

    }

    /// <summary>
    /// 60分钟密度数据
    /// </summary>
    public class TrafficDensity_Hour : TrafficDensity
    {

    }
}
