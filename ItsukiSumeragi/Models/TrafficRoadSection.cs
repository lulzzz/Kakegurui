using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using Newtonsoft.Json;

namespace ItsukiSumeragi.Models
{
    /// <summary>
    /// 路段
    /// </summary>
    public class TrafficRoadSection
    {
        /// <summary>
        /// 路口编号
        /// </summary>
        [Column("SectionId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SectionId { get; set; }

        /// <summary>
        /// 路口名称
        /// </summary>
        [Column("SectionName", TypeName = "VARCHAR(100)")]
        [MySqlCharset("utf8")]
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string SectionName { get; set; }

        /// <summary>
        /// 路段类型
        /// </summary>
        [Column("SectionType", TypeName = "INT")]
        [Required]
        public int SectionType { get; set; }

        /// <summary>
        /// 路段类型描述
        /// </summary>
        [NotMapped]
        public string SectionType_Desc { get; set; }

        /// <summary>
        /// 路段方向
        /// </summary>
        [Column("Direction", TypeName = "INT")]
        [Required]
        public int Direction { get; set; }

        /// <summary>
        /// 路段方向描述
        /// </summary>
        [NotMapped]
        public string Direction_Desc { get; set; }

        /// <summary>
        /// 路段长度
        /// </summary>
        [Column("Length", TypeName = "INT")]
        [Required]
        [Range(1,2000)]
        public int Length { get; set; }

        /// <summary>
        /// 限速值
        /// </summary>
        [Column("SpeedLimit", TypeName = "INT")]
        [Required]
        [Range(1,200)]
        public int SpeedLimit { get; set; }

        /// <summary>
        /// 自由流速度
        /// </summary>
        [NotMapped]
        public int FreeSpeed
        {
            get
            {
                switch (SectionType)
                {
                    case (int)ItsukiSumeragi.Codes.Device.SectionType.快速路:
                        return 80;
                    case (int)ItsukiSumeragi.Codes.Device.SectionType.主干路:
                        return 70;
                    default:
                        return 50;
                }
            }
        }

        /// <summary>
        /// 通道
        /// </summary>
        [JsonIgnore]
        public List<TrafficChannel> Channels { get; set; }

        public override string ToString()
        {
            return $"路段编号：{SectionId} 路段名称：{SectionName}";
        }
    }
}
