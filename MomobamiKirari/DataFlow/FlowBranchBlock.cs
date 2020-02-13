using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using ItsukiSumeragi.DataFlow;
using ItsukiSumeragi.Models;
using Kakegurui.Log;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using MomobamiKirari.Models;

namespace MomobamiKirari.DataFlow
{
    /// <summary>
    /// 流量分支数据块
    /// </summary>
    public class FlowBranchBlock : TrafficBranchBlock<LaneFlow>
    {
        /// <summary>
        /// 车道数据块项
        /// </summary>
        private class LaneItem
        {
            /// <summary>
            /// 车道数据块
            /// </summary>
            public LaneFlowStatisticsBlock LaneBlock { get; set; }

            /// <summary>
            /// 路段
            /// </summary>
            public TrafficRoadSection RoadSection { get; set; }

            /// <summary>
            /// 车道检测长度
            /// </summary>
            public int Length { get; set; }

            /// <summary>
            /// 数据块总数
            /// </summary>
            public int Total { get; set; }
        }

        /// <summary>
        /// 车道或区域集合
        /// </summary>
        private readonly ConcurrentDictionary<string, LaneItem> _laneBlocks = new ConcurrentDictionary<string, LaneItem>();

        /// <summary>
        /// 车道缓存数据块
        /// </summary>
        private LaneFlowCacheBlock _laneFlowCacheBlock;

        /// <summary>
        /// 一分钟批处理数据块
        /// </summary>
        private BatchBlock<LaneFlow> _oneBatchBlock;
        /// <summary>
        /// 五分钟批处理数据块
        /// </summary>
        private BatchBlock<LaneFlow> _fiveBatchBlock;
        /// <summary>
        /// 十五分钟批处理数据块
        /// </summary>
        private BatchBlock<LaneFlow> _fifteenBatchBlock;
        /// <summary>
        /// 六十分钟批处理数据块
        /// </summary>
        private BatchBlock<LaneFlow> _sixtyBatchBlock;

        /// <summary>
        /// 车道数据库数据块
        /// </summary>
        private LaneFlowDbBlock _laneFlowDbBlock;


        /// <summary>
        /// 正确总数
        /// </summary>
        private int _ok;

        /// <summary>
        /// 未知总数
        /// </summary>
        private int _unknown;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">实例工厂</param>
        public FlowBranchBlock(IServiceProvider serviceProvider)
            : base(serviceProvider,LogEvent.流量数据块)
        {

        }

        protected override void OpenCore()
        {
            //重置计数
            _ok = 0;
            _unknown = 0;

            //车道
            _laneFlowCacheBlock = new LaneFlowCacheBlock(_serviceProvider);
            _oneBatchBlock = new BatchBlock<LaneFlow>(BatchSize);
            _fiveBatchBlock = new BatchBlock<LaneFlow>(BatchSize);
            _fifteenBatchBlock = new BatchBlock<LaneFlow>(BatchSize);
            _sixtyBatchBlock = new BatchBlock<LaneFlow>(BatchSize);
            _laneFlowDbBlock = new LaneFlowDbBlock(ThreadCount, _serviceProvider);

            _oneBatchBlock.LinkTo(_laneFlowDbBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = false });
            _fiveBatchBlock.LinkTo(_laneFlowDbBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = false });
            _fifteenBatchBlock.LinkTo(_laneFlowDbBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = false });
            _sixtyBatchBlock.LinkTo(_laneFlowDbBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = false });

            _laneBlocks.Clear();
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    if (relation.Channel.SectionId.HasValue)
                    {

                    }
                    foreach (TrafficLane lane in relation.Channel.Lanes)
                    {
                        LaneFlowStatisticsBlock laneBlock = new LaneFlowStatisticsBlock(_serviceProvider);
                        laneBlock.LinkTo(_oneBatchBlock, _fiveBatchBlock, _fifteenBatchBlock, _sixtyBatchBlock);
                        LaneItem laneItem = new LaneItem
                        {
                            LaneBlock = laneBlock,
                            Length = lane.Length,
                            RoadSection = relation.Channel.SectionId.HasValue
                                ? relation.Channel.RoadSection
                                : null
                        };
                        _laneBlocks.TryAdd(lane.DataId, laneItem);
                    }
                }
            }
        }

        protected override void ResetCore(List<TrafficDevice> devices)
        {
            foreach (TrafficDevice newDevice in devices)
            {
                foreach (var relation in newDevice.Device_Channels)
                {
                    foreach (TrafficLane lane in relation.Channel.Lanes)
                    {
                        if (!_laneBlocks.ContainsKey(lane.DataId))
                        {
                            LaneFlowStatisticsBlock laneBlock = new LaneFlowStatisticsBlock(_serviceProvider);
                            laneBlock.LinkTo(_oneBatchBlock, _fiveBatchBlock, _fifteenBatchBlock, _sixtyBatchBlock);
                            LaneItem laneItem = new LaneItem
                            {
                                LaneBlock = laneBlock,
                                Length = lane.Length,
                                RoadSection = relation.Channel.SectionId.HasValue
                                    ? relation.Channel.RoadSection
                                    : null
                            };
                            _laneBlocks.TryAdd(lane.DataId, laneItem);
                            _logger.LogInformation((int)LogEvent.流量数据块, $"添加车道 {lane.DataId}");

                        }
                    }
                }
            }

            foreach (TrafficDevice oldDevice in _devices)
            {
                foreach (var relation in oldDevice.Device_Channels)
                {
                    foreach (TrafficLane lane in relation.Channel.Lanes)
                    {
                        if (devices.SelectMany(d => d.Device_Channels)
                            .Select(r => r.Channel)
                            .SelectMany(c => c.Lanes)
                            .All(l => l.DataId != lane.DataId))
                        {
                            if (_laneBlocks.TryRemove(lane.DataId, out LaneItem item))
                            {
                                item.LaneBlock.InputBlock.Complete();
                                item.LaneBlock.WaitCompletion();
                            }
                            _logger.LogInformation((int)LogEvent.流量数据块, $"删除车道 {lane.DataId}");
                        }
                    }
                }
            }
        }

        protected override void Handle(LaneFlow flow)
        {
            if (_laneBlocks.ContainsKey(flow.DataId))
            {
                LaneItem laneItem = _laneBlocks[flow.DataId];
          
                if(laneItem.RoadSection!=null)
                {
                    flow.SectionId = laneItem.RoadSection.SectionId;
                    flow.SectionType = laneItem.RoadSection.SectionType;
                    flow.SectionLength = laneItem.RoadSection.Length;
                    flow.FreeSpeed = laneItem.RoadSection.FreeSpeed;
                }

                flow.Distance = flow.Vehicle * laneItem.Length;
                flow.TravelTime = flow.AverageSpeedData > 0
                    ? flow.Vehicle * laneItem.Length / Convert.ToDouble(flow.AverageSpeedData * 1000 / 3600)
                    : 0;

                laneItem.LaneBlock.InputBlock.Post(flow);
                _laneFlowCacheBlock.InputBlock.Post(flow);

                laneItem.Total += 1;
                _ok += 1;
            }
            else
            {
                _unknown += 1;
                _logger.LogWarning((int)LogEvent.流量数据块, $"未知的车道 {flow.DataId}");
            }
        }

        protected override void CloseCore()
        {
            foreach (var laneBlock in _laneBlocks)
            {
                laneBlock.Value.LaneBlock.InputBlock.Complete();
                laneBlock.Value.LaneBlock.WaitCompletion();
            }

            _laneFlowCacheBlock.InputBlock.Complete();
            _laneFlowCacheBlock.WaitCompletion();

            _oneBatchBlock.Complete();
            _fiveBatchBlock.Complete();
            _fifteenBatchBlock.Complete();
            _sixtyBatchBlock.Complete();

            _oneBatchBlock.Completion.Wait();
            _fiveBatchBlock.Completion.Wait();
            _fifteenBatchBlock.Completion.Wait();
            _sixtyBatchBlock.Completion.Wait();

            _laneFlowDbBlock.InputBlock.Complete();
            _laneFlowDbBlock.WaitCompletion();

        }

        public override void TriggerSave()
        {
            _oneBatchBlock.TriggerBatch();
            _fiveBatchBlock.TriggerBatch();
            _fifteenBatchBlock.TriggerBatch();
            _sixtyBatchBlock.TriggerBatch();
        }

        #region 实现 IHealthCheck
        public override Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                {"成功解析数量", _ok},
                {"未知数据数量", _unknown},
                {"下个时间段数量", _next},
                {"时间范围外数量", _over},
                {"数据滞留数量", _laneFlowDbBlock.InputCount* BatchSize},
                {"一分钟缓存滞留数量", _oneBatchBlock.OutputCount*BatchSize},
                {"一分钟存储成功数量", _laneFlowDbBlock.Success_One},
                {"一分钟存储失败数量", _laneFlowDbBlock.Failed_One},
                {"五分钟缓存滞留数量", _fiveBatchBlock.OutputCount*BatchSize},
                {"五分钟存储成功数量", _laneFlowDbBlock.Success_Five},
                {"五分钟存储失败数量", _laneFlowDbBlock.Failed_Five},
                {"十五分钟缓存滞留数量", _fifteenBatchBlock.OutputCount*BatchSize},
                {"十五分钟存储成功数量", _laneFlowDbBlock.Success_Fifteen},
                {"十五分钟存储失败数量", _laneFlowDbBlock.Failed_Fifteen},
                {"一小时缓存滞留数量", _oneBatchBlock.OutputCount*BatchSize},
                {"一小时入库成功数量", _laneFlowDbBlock.Success_Sixty},
                {"一小时入库失败数量", _laneFlowDbBlock.Failed_Sixty}
            };

            foreach (var pair in _laneBlocks)
            {
                data.Add(pair.Key,pair.Value.Total);
            }

            return Task.FromResult(_laneFlowDbBlock.InputCount == 0 ? HealthCheckResult.Healthy("流量数据存储正常", data) : HealthCheckResult.Unhealthy("流量数据存储已经超过阀值", null, data));
        }
        #endregion
    }
}
