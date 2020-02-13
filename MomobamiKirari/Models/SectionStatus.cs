using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MomobamiKirari.Models
{
    /// <summary>
    /// 路段状态小时统计
    /// </summary>
    public class SectionStatus
    {
        /// <summary>
        /// 路段编号
        /// </summary>
        [Column("SectionId", TypeName = "INT")]
        [Required]
        public int SectionId { get; set; }

        /// <summary>
        /// 路段名称
        /// </summary>
        [NotMapped]
        public string SectionName { get; set; }

        /// <summary>
        /// 数据时间
        /// </summary>
        [Column("DateTime", TypeName = "DATETIME")]
        [Required]
        public DateTime DateTime { get; set; }

        /// <summary>
        /// 通畅
        /// </summary>
        [Column("Good", TypeName = "INT")]
        [Required]
        public int Good { get; set; }

        /// <summary>
        /// 基本通畅
        /// </summary>
        [Column("Normal", TypeName = "INT")]
        [Required]
        public int Normal { get; set; }

        /// <summary>
        /// 轻度拥堵
        /// </summary>
        [Column("Warning", TypeName = "INT")]
        [Required]
        public int Warning { get; set; }

        /// <summary>
        /// 一般拥堵
        /// </summary>
        [Column("Bad", TypeName = "INT")]
        [Required]
        public int Bad { get; set; }

        /// <summary>
        /// 严重拥堵
        /// </summary>
        [Column("Dead", TypeName = "INT")]
        [Required]
        public int Dead { get; set; }

    }
}
