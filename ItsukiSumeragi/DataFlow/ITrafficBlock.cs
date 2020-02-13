using System.Threading.Tasks.Dataflow;

namespace ItsukiSumeragi.DataFlow
{
    /// <summary>
    /// 交通数据数据块
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public interface ITrafficBlock<in T>
    {
        /// <summary>
        /// 数据块入口
        /// </summary>
        ITargetBlock<T> InputBlock { get; }

        /// <summary>
        /// 等待数据块完成
        /// </summary>
        void WaitCompletion();
    }

    /// <summary>
    /// 交通数据数组数据块
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public interface ITrafficArrayBlock<in T>
    {
        /// <summary>
        /// 数据块入口
        /// </summary>
        ITargetBlock<T[]> InputBlock { get; }

        /// <summary>
        /// 等待数据块完成
        /// </summary>
        void WaitCompletion();
    }
}
