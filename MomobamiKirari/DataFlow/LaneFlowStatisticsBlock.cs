using System;
using System.Threading.Tasks.Dataflow;
using ItsukiSumeragi.DataFlow;
using Kakegurui.Core;
using MomobamiKirari.Models;

namespace MomobamiKirari.DataFlow
{
    /// <summary>
    /// 车道流量统计数据块
    /// </summary>
    public class LaneFlowStatisticsBlock: ITrafficBlock<LaneFlow>
    {
        /// <summary>
        /// 广播数据块
        /// </summary>
        private readonly BroadcastBlock<LaneFlow> _laneBroadcastBlock;

        /// <summary>
        /// 1分钟统计数据块
        /// </summary>
        private readonly LaneFlowTimeSpanBlock _oneTimeSpanBlock;

        /// <summary>
        /// 5分钟统计数据块
        /// </summary>
        private readonly LaneFlowTimeSpanBlock _fiveTimeSpanBlock;

        /// <summary>
        /// 15分钟统计数据块
        /// </summary>
        private readonly LaneFlowTimeSpanBlock _fifteenTimeSpanBlock;

        /// <summary>
        /// 1小时统计数据块
        /// </summary>
        private readonly LaneFlowTimeSpanBlock _sixtyTimeSpanBlock;

        /// <summary>
        /// 数据块入口
        /// </summary>
        public ITargetBlock<LaneFlow> InputBlock => _laneBroadcastBlock;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">实例工厂</param>
        public LaneFlowStatisticsBlock(IServiceProvider serviceProvider)
        {
            _laneBroadcastBlock = new BroadcastBlock<LaneFlow>(f => f);
            _oneTimeSpanBlock = new LaneFlowTimeSpanBlock(DateTimeLevel.Minute, serviceProvider);
            _fiveTimeSpanBlock = new LaneFlowTimeSpanBlock(DateTimeLevel.FiveMinutes, serviceProvider);
            _fifteenTimeSpanBlock = new LaneFlowTimeSpanBlock(DateTimeLevel.FifteenMinutes, serviceProvider);
            _sixtyTimeSpanBlock = new LaneFlowTimeSpanBlock(DateTimeLevel.Hour, serviceProvider);

            _laneBroadcastBlock.LinkTo(_oneTimeSpanBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
            _laneBroadcastBlock.LinkTo(_fiveTimeSpanBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
            _laneBroadcastBlock.LinkTo(_fifteenTimeSpanBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
            _laneBroadcastBlock.LinkTo(_sixtyTimeSpanBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
        }

        /// <summary>
        /// 链接数据块
        /// </summary>
        /// <param name="oneBlock">1分钟统计结果</param>
        /// <param name="fiveBlock">5分钟统计结果</param>
        /// <param name="fifteenBlock">15分钟统计结果</param>
        /// <param name="hourBlock">1小时统计结果</param>
        public void LinkTo(ITargetBlock<LaneFlow> oneBlock, ITargetBlock<LaneFlow> fiveBlock, ITargetBlock<LaneFlow> fifteenBlock, ITargetBlock<LaneFlow> hourBlock)
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
