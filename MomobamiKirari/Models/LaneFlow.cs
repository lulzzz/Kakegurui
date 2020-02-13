using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using ItsukiSumeragi.Codes.Flow;

namespace MomobamiKirari.Models
{
    /// <summary>
    /// 车道流量
    /// </summary>
    public class LaneFlow:TrafficData
    {
        /// <summary>
        /// 轿车流量
        /// </summary>
        [Column("Cars", TypeName = "INT")]
        [Required]
        public int Cars { get; set; }

        /// <summary>
        /// 客车流量
        /// </summary>
        [Column("Buss", TypeName = "INT")]
        [Required]
        public int Buss { get; set; }

        /// <summary>
        /// 卡车流量
        /// </summary>
        [Column("Trucks", TypeName = "INT")]
        [Required]
        public int Trucks { get; set; }

        /// <summary>
        /// 面包车流量
        /// </summary>
        [Column("Vans", TypeName = "INT")]
        [Required]
        public int Vans { get; set; }

        /// <summary>
        /// 三轮车流量
        /// </summary>
        [Column("Tricycles", TypeName = "INT")]
        [Required]
        public int Tricycles { get; set; }

        /// <summary>
        /// 摩托车流量
        /// </summary>
        [Column("Motorcycles", TypeName = "INT")]
        [Required]
        public int Motorcycles { get; set; }

        /// <summary>
        /// 自行车流量
        /// </summary>
        [Column("Bikes", TypeName = "INT")]
        [Required]
        public int Bikes { get; set; }

        /// <summary>
        /// 行人流量
        /// </summary>
        [Column("Persons", TypeName = "INT")]
        [Required]
        public int Persons { get; set; }

        /// <summary>
        /// 车道检测距离(米)=机动车流量*车道检测长度(米)
        /// </summary>
        [Column("Distance", TypeName = "INT")]
        public int Distance { get; set; }

        /// <summary>
        /// 车道行程时间=车道检测距离(米)/车道平均速度(米/秒)
        /// </summary>
        [Column("TravelTime", TypeName = "DOUBLE")]
        public double TravelTime { get; set; }

        /// <summary>
        /// 车道时距(秒)
        /// </summary>
        [Column("HeadDistance", TypeName = "DOUBLE")]
        [Required]
        public double HeadDistance { get; set; }

        /// <summary>
        /// 时间占有率(%)
        /// </summary>
        [Column("TimeOccupancy", TypeName = "INT")]
        [Required]
        public int TimeOccupancy { get; set; }

        /// <summary>
        /// 空间占用率(%)
        /// </summary>
        [Column("Occupancy", TypeName = "INT")]
        [Required]
        public int Occupancy { get; set; }

        /// <summary>
        /// 数据数量
        /// </summary>
        [Column("Count", TypeName = "INT")]
        [Required]
        public int Count { get; set; }

        /// <summary>
        /// 交通状态
        /// </summary>
        [NotMapped]
        public TrafficStatus TrafficStatus { get; set; }

        /// <summary>
        /// 路口名称
        /// </summary>
        [NotMapped]
        public string CrossingName { get; set; }

        /// <summary>
        /// 车道名称
        /// </summary>
        [NotMapped]
        public string LaneName { get; set; }

        /// <summary>
        /// 车道方向
        /// </summary>
        [NotMapped]
        public int Direction { get; set; }

        /// <summary>
        /// 车道方向描述
        /// </summary>
        [NotMapped]
        public string Direction_Desc { get; set; }

        /// <summary>
        /// 车道流向
        /// </summary>
        [NotMapped]
        public int FlowDirection { get; set; }

        /// <summary>
        /// 车道流向描述
        /// </summary>
        [NotMapped]
        public string FlowDirection_Desc { get; set; }

        /// <summary>
        /// 流量总和
        /// </summary>
        [NotMapped]
        public int Total => Cars + Bikes + Buss + Persons + Tricycles + Trucks + Motorcycles + Vans;

        /// <summary>
        /// 机动车流量总和
        /// </summary>
        [NotMapped]
        public int Vehicle => Cars + Buss + Tricycles + Trucks + Vans;

        /// <summary>
        /// 机动车流量总和
        /// </summary>
        [NotMapped]
        public int Bike => Bikes + Motorcycles;

        /// <summary>
        /// 当前数据的时间粒度
        /// </summary>
        [NotMapped]
        public DateTimeLevel DateLevel { get; set; }

        /// <summary>
        /// 平均速度(km/h)
        /// </summary>
        [NotMapped]
        public double AverageSpeed => TravelTime>0?Distance / TravelTime*3600/1000:0;

        /// <summary>
        /// 车头间距(米)
        /// </summary>
        [NotMapped]
        public double HeadSpace => Count==0?0:HeadDistance / Count * (Distance / TravelTime);

        /// <summary>
        /// 数据上传的平均速度(km/h)
        /// </summary>
        [NotMapped]
        public double AverageSpeedData { get; set; }

        /// <summary>
        /// 路段长度
        /// </summary>
        [NotMapped]
        public int SectionId { get; set; }

        /// <summary>
        /// 路段长度
        /// </summary>
        [NotMapped]
        public int SectionType{ get; set; }

        /// <summary>
        /// 路段长度
        /// </summary>
        [NotMapped]
        public int SectionLength { get; set; }

        /// <summary>
        /// 路段自由流速度
        /// </summary>
        [NotMapped]
        public double FreeSpeed { get; set; }

    }

    /// <summary>
    /// 一分钟流量
    /// </summary>
    public class LaneFlow_One: LaneFlow
    {
        
    }

    /// <summary>
    /// 5分钟流量
    /// </summary>
    public class LaneFlow_Five: LaneFlow
    {
      
    }

    /// <summary>
    /// 15分钟流量
    /// </summary>
    public class LaneFlow_Fifteen : LaneFlow
    {
       
    }

    /// <summary>
    /// 60分钟流量
    /// </summary>
    public class LaneFlow_Hour : LaneFlow
    {
       
    }
}
