using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ItsukiSumeragi.Models
{
    public abstract class TrafficData
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Column("Id", TypeName = "INT")]
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// 数据编号
        /// </summary>
        [Column("DataId", TypeName = "VARCHAR(100)")]
        [Required]
        public string DataId { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        [Column("DateTime", TypeName = "DATETIME")]
        [Required]
        public DateTime DateTime { get; set; }

    }
}
