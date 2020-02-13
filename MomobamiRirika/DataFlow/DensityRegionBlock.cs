using System;
using System.Threading.Tasks.Dataflow;
using ItsukiSumeragi.DataFlow;
using Kakegurui.Core;
using MomobamiRirika.Models;

namespace MomobamiRirika.DataFlow
{
    /// <summary>
    /// 密度区域数据块
    /// </summary>
    public class DensityRegionBlock : ITrafficBlock<TrafficDensity>
    {
        /// <summary>
        /// 广播数据块
        /// </summary>
        private readonly BroadcastBlock<TrafficDensity> _regionBroadcastBlock;

        /// <summary>
        /// 1分钟统计数据块
        /// </summary>
        private readonly DensityTimeSpanBlock _oneTimeSpanBlock;

        /// <summary>
        /// 5分钟统计数据块
        /// </summary>
        private readonly DensityTimeSpanBlock _fiveTimeSpanBlock;

        /// <summary>
        /// 15分钟统计数据块
        /// </summary>
        private readonly DensityTimeSpanBlock _fifteenTimeSpanBlock;

        /// <summary>
        /// 1小时统计数据块
        /// </summary>
        private readonly DensityTimeSpanBlock _sixtyTimeSpanBlock;

        /// <summary>
        /// 数据块入口
        /// </summary>
        public ITargetBlock<TrafficDensity> InputBlock => _regionBroadcastBlock;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">实例工厂</param>
        public DensityRegionBlock(IServiceProvider serviceProvider)
        {
            _regionBroadcastBlock = new BroadcastBlock<TrafficDensity>(d => d);
            _oneTimeSpanBlock = new DensityTimeSpanBlock(DateTimeLevel.Minute, serviceProvider);
            _fiveTimeSpanBlock = new DensityTimeSpanBlock(DateTimeLevel.FiveMinutes, serviceProvider);
            _fifteenTimeSpanBlock = new DensityTimeSpanBlock(DateTimeLevel.FifteenMinutes, serviceProvider);
            _sixtyTimeSpanBlock = new DensityTimeSpanBlock(DateTimeLevel.Hour, serviceProvider);

            _regionBroadcastBlock.LinkTo(_oneTimeSpanBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
            _regionBroadcastBlock.LinkTo(_fiveTimeSpanBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
            _regionBroadcastBlock.LinkTo(_fifteenTimeSpanBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
            _regionBroadcastBlock.LinkTo(_sixtyTimeSpanBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });

        }

        /// <summary>
        /// 链接数据块
        /// </summary>
        /// <param name="oneBlock">1分钟统计</param>
        /// <param name="fiveBlock">5分钟统计</param>
        /// <param name="fifteenBlock">15分钟统计</param>
        /// <param name="hourBlock">1小时统计</param>
        public void LinkTo(ITargetBlock<TrafficDensity> oneBlock, ITargetBlock<TrafficDensity> fiveBlock, ITargetBlock<TrafficDensity> fifteenBlock, ITargetBlock<TrafficDensity> hourBlock)
        {
            _oneTimeSpanBlock.LinkTo(oneBlock);
            _fiveTimeSpanBlock.LinkTo(fiveBlock);
            _fifteenTimeSpanBlock.LinkTo(fifteenBlock);
            _sixtyTimeSpanBlock.LinkTo(hourBlock);
        }

        /// <summary>
        /// 等待数据块完成
        /// </summary>
        public void WaitCompletion()
        {
            _oneTimeSpanBlock.WaitCompletion();
            _fiveTimeSpanBlock.WaitCompletion();
            _fifteenTimeSpanBlock.WaitCompletion();
            _sixtyTimeSpanBlock.WaitCompletion();
        }
    }
}
