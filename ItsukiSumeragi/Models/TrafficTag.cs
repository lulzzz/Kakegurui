
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using Newtonsoft.Json;

namespace ItsukiSumeragi.Models
{
    /// <summary>
    /// 违法标签
    /// </summary>
    public class TrafficTag
    {
        /// <summary>
        /// 标签名称
        /// </summary>
        [Column("TagName", TypeName = "VARCHAR(100)")]
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string TagName { get; set; }

        /// <summary>
        /// 标签类型
        /// </summary>
        [Column("TagType", TypeName = "INT")]
        [Required]
        public int TagType { get; set; }

        /// <summary>
        /// 标签类型描述
        /// </summary>
        [NotMapped]
        public string TagType_Desc { get; set; }

        /// <summary>
        /// 英文名
        /// </summary>
        [Column("EnglishName", TypeName = "VARCHAR(100)")]
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string EnglishName { get; set; }

        /// <summary>
        /// 中文名
        /// </summary>
        [Column("ChineseName", TypeName = "VARCHAR(100)")]
        [Required]
        [MySqlCharset("utf8")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string ChineseName { get; set; }

        /// <summary>
        /// 颜色
        /// </summary>
        [Column("Color", TypeName = "VARCHAR(100)")]
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Color { get; set; }

        /// <summary>
        /// 说明
        /// </summary>
        [Column("Mark", TypeName = "TEXT")]
        public string Mark { get; set; }

        /// <summary>
        /// 违法行为和标签关联集合
        /// </summary>
        [JsonIgnore]
        public List<TrafficViolation_TrafficTag> Violation_Tags { get; set; }

    }
}
