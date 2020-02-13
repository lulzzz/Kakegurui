using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using Newtonsoft.Json;
using ItsukiSumeragi.Codes.Violation;

namespace ItsukiSumeragi.Models
{

    /// <summary>
    /// 违法参数
    /// </summary>
    public class TrafficViolationParameter
    {
        /// <summary>
        /// 参数键
        /// </summary>
        [Column("Key", TypeName = "VARCHAR(100)")]
        [Required]
        public string Key { get; set; }

        /// <summary>
        /// 参数类型
        /// </summary>
        [Column("ParameterType", TypeName = "INT")]
        [Required]
        public ViolationParameterType ParameterType { get; set; }

        /// <summary>
        /// 参数值
        /// </summary>
        [Column("Value", TypeName = "VARCHAR(100)")]
        [MySqlCharset("utf8")]
        public string Value { get; set; }

        /// <summary>
        /// 参数值类型
        /// </summary>
        [Column("ValueType", TypeName = "INT")]
        [Required]
        public ViolationValueType ValueType { get; set; }

        /// <summary>
        /// 参数最小值，数字和浮点类型有效
        /// </summary>
        [Column("MinValue", TypeName = "VARCHAR(100)")]
        public int? MinValue { get; set; }

        /// <summary>
        /// 参数最大值，数字和浮点类型有效
        /// </summary>
        [Column("MaxValue", TypeName = "VARCHAR(100)")]
        public int? MaxValue { get; set; }

        /// <summary>
        /// 参数描述
        /// </summary>
        [Column("Description", TypeName = "VARCHAR(100)")]
        [MySqlCharset("utf8")]
        public string Description { get; set; }

        /// <summary>
        /// 参数单位
        /// </summary>
        [Column("Unit", TypeName = "VARCHAR(100)")]
        [MySqlCharset("utf8")]
        public string Unit { get; set; }

        /// <summary>
        /// 枚举键集合
        /// </summary>
        [Column("Keys", TypeName = "VARCHAR(100)")]
        public string Keys { get; set; }

        /// <summary>
        /// 枚举值集合
        /// </summary>
        [Column("Values", TypeName = "VARCHAR(100)")]
        public string Values { get; set; }

        /// <summary>
        /// 违法行为参数关联
        /// </summary>
        [JsonIgnore]
        public List<TrafficViolation_TrafficViolationParameter> Violation_Parameters { get; set; }

        /// <summary>
        /// 通道参数关联
        /// </summary>
        [JsonIgnore]
        public List<TrafficChannel_TrafficViolationParameter> Channel_Parameters { get; set; }
    }
}
