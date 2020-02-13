using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace ItsukiSumeragi.Models
{
    /// <summary>
    /// 违法行为文件参数关联
    /// </summary>
    public class TrafficViolation_TrafficViolationParameter
    {
        /// <summary>
        /// 违法编号
        /// </summary>
        [Column("ViolationId", TypeName = "INT")]
        [Required]
        public int ViolationId { get; set; }

        /// <summary>
        /// 参数键
        /// </summary>
        [Column("Key", TypeName = "VARCHAR(100)")]
        [Required]
        public string Key { get; set; }

        /// <summary>
        /// 参数值
        /// </summary>
        [Column("Value", TypeName = "VARCHAR(100)")]
        public string Value { get; set; }

        /// <summary>
        /// 违法行为
        /// </summary>
        [JsonIgnore]
        public TrafficViolation Violation { get; set; }

        /// <summary>
        /// 违法参数
        /// </summary>
        public TrafficViolationParameter Parameter { get; set; }
    }
}
