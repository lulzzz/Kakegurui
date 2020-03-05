using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using Newtonsoft.Json;

namespace MomobamiKirari.Models
{
    /// <summary>
    /// 路口
    /// </summary>
    public class RoadCrossing
    {
        /// <summary>
        /// 路口编号
        /// </summary>
        [Column("CrossingId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CrossingId { get; set; }

        /// <summary>
        /// 路口名称
        /// </summary>
        [Column("CrossingName", TypeName = "VARCHAR(100)")]
        [MySqlCharset("utf8")]
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string CrossingName { get; set; }

        /// <summary>
        /// 通道
        /// </summary>
        [JsonIgnore]
        public List<FlowChannel> Channels { get; set; }

        public override string ToString()
        {
            return $"路口编号：{CrossingId} 路口名称：{CrossingName}";
        }
    }
}
