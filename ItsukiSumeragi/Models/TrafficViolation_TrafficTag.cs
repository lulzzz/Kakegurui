using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace ItsukiSumeragi.Models
{
    /// <summary>
    /// 违法行为标签关联
    /// </summary>
    public class TrafficViolation_TrafficTag
    {
        /// <summary>
        /// 违法编号
        /// </summary>
        [Column("ViolationId", TypeName = "INT")]
        [Required]
        public int ViolationId { get; set; }

        /// <summary>
        /// 标签名称
        /// </summary>
        [Column("TagName", TypeName = "VARCHAR(100)")]
        [Required]
        public string TagName { get; set; }

        /// <summary>
        /// 违法行为
        /// </summary>
        [JsonIgnore]
        public TrafficViolation Violation { get; set; }

        /// <summary>
        /// 违法标签
        /// </summary>
        public TrafficTag Tag { get; set; }

    }
}
