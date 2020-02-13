using System.Threading.Tasks.Dataflow;

namespace ItsukiSumeragi.DataFlow
{
    /// <summary>
    /// 流量数据源数据块
    /// </summary>
    public abstract class TrafficActionBlock<T>: ITrafficBlock<T>
    {
        /// <summary>
        /// 入库数据块
        /// </summary>
        protected readonly ActionBlock<T> _actionBlock;

        /// <summary>
        /// 当前等待处理的数量
        /// </summary>
        public int InputCount => _actionBlock.InputCount;

        /// <summary>
        /// 数据块入口
        /// </summary>
        public ITargetBlock<T> InputBlock => _actionBlock;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="threadCount">线程数</param>
        protected TrafficActionBlock(int threadCount = 1)
        {
            _actionBlock = new ActionBlock<T>(
                Handle,new ExecutionDataflowBlockOptions{MaxDegreeOfParallelism = threadCount});
        }

        /// <summary>
        /// 处理交通数据
        /// </summary>
        /// <param name="t">交通数据</param>
        protected abstract void Handle(T t);

        /// <summary>
        /// 等待数据块完成
        /// </summary>
        public virtual void WaitCompletion()
        {
            _actionBlock.Completion.Wait();
        }
    }

    /// <summary>
    /// 交通数据数组数据块
    /// </summary>
    /// <typeparam name="T">交通数据</typeparam>
    public abstract class TrafficArrayActionBlock<T>:ITrafficArrayBlock<T>
    {
        /// <summary>
        /// 数据库数据块
        /// </summary>
        private readonly ActionBlock<T[]> _actionBlock;

        /// <summary>
        /// 当前等待处理的数量
        /// </summary>
        public int InputCount => _actionBlock.InputCount;

        /// <summary>
        /// 数据块入口
        /// </summary>
        public ITargetBlock<T[]> InputBlock => _actionBlock;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="threadCount">线程数</param>
        protected TrafficArrayActionBlock(int threadCount = 1)
        {
            _actionBlock = new ActionBlock<T[]>(Handle, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = threadCount });
        }

        /// <summary>
        /// 数据处理
        /// </summary>
        /// <param name="datas">数据集合</param>
        protected abstract void Handle(T[] datas);

        /// <summary>
        /// 等待数据块完成
        /// </summary>
        public void WaitCompletion()
        {
            _actionBlock.Completion.Wait();
        }
    }
}
