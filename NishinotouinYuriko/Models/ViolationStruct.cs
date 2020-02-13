using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ItsukiSumeragi.Models;
using MySql.Data.EntityFrameworkCore.DataAnnotations;

namespace NishinotouinYuriko.Models
{
    /// <summary>
    /// 违法行为数据
    /// </summary>
    public class ViolationStruct : TrafficData
    {
        /// <summary>
        /// 违法编号
        /// </summary>
        [Required]
        [Column("ViolationId", TypeName = "INT")]
        public int ViolationId { get; set; }

        /// <summary>
        /// 违法名称
        /// </summary>
        [NotMapped]
        public string ViolationName { get; set; }

        /// <summary>
        /// 地点编号
        /// </summary>
        [Required]
        [Column("LocationId", TypeName = "INT")]
        public int LocationId { get; set; }

        /// <summary>
        /// 地点名称
        /// </summary>
        [NotMapped]
        public string LocationName { get; set; }

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
        /// 目标类型
        /// </summary>
        [Required]
        [Column("TargetType", TypeName = "INT")]
        public int TargetType { get; set; }

        /// <summary>
        /// 目标类型描述
        /// </summary>
        [NotMapped]
        public string TargetType_Desc { get; set; }
        /// <summary>
        /// 方向
        /// </summary>
        [Required]
        [Column("Direction", TypeName = "INT")]
        public int Direction { get; set; }

        /// <summary>
        /// 方向描述
        /// </summary>
        [NotMapped]
        public string Direction_Desc { get; set; }

        /// <summary>
        /// 车牌号
        /// </summary>
        [Required]
        [Column("PlateNumber", TypeName = "VARCHAR(100)")]
        [MySqlCharset("utf8")]
        public string PlateNumber { get; set; }

        /// <summary>
        /// 图片1链接
        /// </summary>
        [Column("ImageLink1", TypeName = "VARCHAR(100)")]
        public string ImageLink1 { get; set; }

        /// <summary>
        /// 图片2链接
        /// </summary>
        [Column("ImageLink2", TypeName = "VARCHAR(100)")]
        public string ImageLink2 { get; set; }

        /// <summary>
        /// 图片3链接
        /// </summary>
        [Column("ImageLink3", TypeName = "VARCHAR(100)")]
        public string ImageLink3 { get; set; }

        /// <summary>
        /// 图片4链接
        /// </summary>
        [Column("ImageLink4", TypeName = "VARCHAR(100)")]
        public string ImageLink4 { get; set; }

        /// <summary>
        /// 图片5链接
        /// </summary>
        [Column("ImageLink5", TypeName = "VARCHAR(100)")]
        public string ImageLink5 { get; set; }

        /// <summary>
        /// 视频链接
        /// </summary>
        [Column("VideoLink", TypeName = "VARCHAR(100)")]
        public string VideoLink { get; set; }

        /// <summary>
        /// 图片1
        /// </summary>
        [NotMapped]
        public string Image1 { get; set; }

        /// <summary>
        /// 图片2
        /// </summary>
        [NotMapped]
        public string Image2 { get; set; }

        /// <summary>
        /// 图片3
        /// </summary>
        [NotMapped]
        public string Image3 { get; set; }

        /// <summary>
        /// 图片4
        /// </summary>
        [NotMapped]
        public string Image4 { get; set; }

        /// <summary>
        /// 图片5
        /// </summary>
        [NotMapped]
        public string Image5 { get; set; }

        /// <summary>
        /// 视频
        /// </summary>
        [NotMapped]
        public string Video { get; set; }

        /// <summary>
        /// 总和
        /// </summary>
        [NotMapped]
        public int Count { get; set; }
    }
}
