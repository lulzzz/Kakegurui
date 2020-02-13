using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using Newtonsoft.Json;

namespace ItsukiSumeragi.Models
{
    /// <summary>
    /// 违法行为
    /// </summary>
    public class TrafficViolation
    {
        /// <summary>
        /// 违法编号
        /// </summary>
        [Column("ViolationId", TypeName = "INT")]
        [Required]
        public int ViolationId { get; set; }

        /// <summary>
        /// 违法名称
        /// </summary>
        [Column("ViolationName", TypeName = "VARCHAR(100)")]
        [Required]
        [MySqlCharset("utf8")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string ViolationName { get; set; }

        /// <summary>
        /// 国标编号
        /// </summary>
        [Column("GbCode", TypeName = "VARCHAR(100)")]
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string GbCode { get; set; }

        /// <summary>
        /// 国标名称
        /// </summary>
        [Column("GbName", TypeName = "VARCHAR(100)")]
        [Required]
        [MySqlCharset("utf8")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string GbName { get; set; }

        /// <summary>
        /// 标签集合
        /// </summary>
        public List<TrafficViolation_TrafficTag> Violation_Tags { get; set; }

        /// <summary>
        /// 违法参数集合
        /// </summary>
        public List<TrafficViolation_TrafficViolationParameter> Violation_Parameters { get; set; }

        /// <summary>
        /// 通道关联集合
        /// </summary>
        [JsonIgnore]
        public List<TrafficChannel_TrafficViolation> Channel_Violations { get; set; }

    }

    /// <summary>
    /// 违法行为osd参数更新
    /// </summary>
    public class TrafficViolationUpdateOsdParameters
    {
        /// <summary>
        /// 违法编号
        /// </summary>
        [Column("ViolationId", TypeName = "INT")]
        [Required]
        public int ViolationId { get; set; }

        /// <summary>
        /// 违法参数集合
        /// </summary>
        public List<TrafficViolation_TrafficViolationParameter> Violation_OsdParameters { get; set; }

        /// <summary>
        /// 违法参数集合
        /// </summary>
        public List<TrafficViolation_TrafficViolationParameter> Violation_FontParameters { get; set; }

    }

    /// <summary>
    /// 违法行为文件参数更新
    /// </summary>
    public class TrafficViolationUpdateFileParameters
    {
        /// <summary>
        /// 违法编号
        /// </summary>
        [Column("ViolationId", TypeName = "INT")]
        [Required]
        public int ViolationId { get; set; }

        /// <summary>
        /// 违法参数集合
        /// </summary>
        public List<TrafficViolation_TrafficViolationParameter> Violation_FileParameters { get; set; }

        /// <summary>
        /// 违法参数集合
        /// </summary>
        public List<TrafficViolation_TrafficViolationParameter> Violation_FileNameParameters { get; set; }

    }
}
