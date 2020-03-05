using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ItsukiSumeragi.Models;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using MomobamiKirari.Codes;

namespace MomobamiKirari.Models
{
    /// <summary>
    /// 视频结构化数据
    /// </summary>
    public class VideoStruct : TrafficData
    {
        /// <summary>
        /// 图片
        /// </summary>
        [Column("Image", TypeName = "MEDIUMTEXT")]
        [Required]
        public string Image { get; set; }

        /// <summary>
        /// 特征码
        /// </summary>
        [Column("Feature", TypeName = "TEXT")]
        [Required]
        public string Feature { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        [Column("CountIndex", TypeName = "INT")]
        [Required]
        public int CountIndex { get; set; }

        /// <summary>
        /// 通道名称
        /// </summary>
        [NotMapped]
        public string ChannelName { get; set; }

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
        /// 视频结构化数据类型
        /// </summary>
        [NotMapped]
        public VideoStructType StructType;

    }

    /// <summary>
    /// 机动车视频结构化
    /// </summary>
    public class VideoVehicle : VideoStruct
    {
        /// <summary>
        /// 车辆类型
        /// </summary>
        [Required]
        [Column("CarType", TypeName = "INT")]
        public int CarType { get; set; }

        /// <summary>
        /// 车辆类型描述
        /// </summary>
        [NotMapped]
        public string CarType_Desc { get; set; }

        /// <summary>
        /// 车辆品牌
        /// </summary>
        [Required]
        [Column("CarBrand", TypeName = "VARCHAR(100)")]
        [MySqlCharset("utf8")]
        public string CarBrand { get; set; }

        /// <summary>
        /// 车辆颜色
        /// </summary>
        [Required]
        [Column("CarColor", TypeName = "INT")]
        public int CarColor { get; set; }

        /// <summary>
        /// 车辆颜色描述
        /// </summary>
        [NotMapped]
        public string CarColor_Desc { get; set; }

        /// <summary>
        /// 车牌号
        /// </summary>
        [Required]
        [Column("PlateNumber", TypeName = "VARCHAR(100)")]
        [MySqlCharset("utf8")]
        public string PlateNumber { get; set; }

        /// <summary>
        /// 车牌类型
        /// </summary>
        [Required]
        [Column("PlateType", TypeName = "INT")]
        public int PlateType { get; set; }

        /// <summary>
        /// 车牌类型描述
        /// </summary>
        [NotMapped]
        public string PlateType_Desc { get; set; }
    }

    /// <summary>
    /// 非机动车视频结构化
    /// </summary>
    public class VideoBike : VideoStruct
    {
        /// <summary>
        /// 非机动车类型
        /// </summary>
        [Required]
        [Column("BikeType", TypeName = "INT")]
        public int BikeType { get; set; }

        /// <summary>
        /// 非机动车类型描述
        /// </summary>
        [NotMapped]
        public string BikeType_Desc { get; set; }
    }

    /// <summary>
    /// 行人视频结构化
    /// </summary>
    public class VideoPedestrain : VideoStruct
    {
        /// <summary>
        /// 行人性别
        /// </summary>
        [Required]
        [Column("Sex", TypeName = "INT")]
        public int Sex { get; set; }

        /// <summary>
        /// 行人性别描述
        /// </summary>
        [NotMapped]
        public string Sex_Desc { get; set; }

        /// <summary>
        /// 行人年龄
        /// </summary>
        [Required]
        [Column("Age", TypeName = "INT")]
        public int Age { get; set; }

        /// <summary>
        /// 行人年龄描述
        /// </summary>
        [NotMapped]
        public string Age_Desc { get; set; }

        /// <summary>
        /// 行人上半身颜色
        /// </summary>
        [Required]
        [Column("UpperColor", TypeName = "INT")]
        public int UpperColor { get; set; }

        /// <summary>
        /// 行人上半身颜色描述
        /// </summary>
        [NotMapped]
        public string UpperColor_Desc { get; set; }
    }
}
