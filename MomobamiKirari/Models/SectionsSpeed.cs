
namespace MomobamiKirari.Models
{
    /// <summary>
    /// 根据交通状态计算的速度值
    /// </summary>
    public class SectionsSpeed
    {
        /// <summary>
        /// 路段数量
        /// </summary>
        public int SectionCount { get; set; }

        /// <summary>
        /// 平均速度
        /// </summary>
        public int AverageSpeed { get; set; }
    }
}
