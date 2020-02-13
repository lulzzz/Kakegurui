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
using MomobamiRirika.Models;

namespace MomobamiRirika.DataFlow
{
    /// <summary>
    /// 密度分支数据块
    /// </summary>
    public class DensityBranchBlock : TrafficBranchBlock<TrafficDensity>
    {
        /// <summary>
        /// 区域数据块项
        /// </summary>
        private class Item
        {
            public string DataId { get; set; }
            public DensityRegionBlock Block { get; set; }
            public int Total { get; set; }
        }

        /// <summary>
        /// 车道或区域集合
        /// </summary>
        private readonly ConcurrentDictionary<string, Item> _regionBlocks = new ConcurrentDictionary<string, Item>();

        /// <summary>
        /// 一分钟批处理数据块
        /// </summary>
        private BatchBlock<TrafficDensity> _oneBatchBlock;
        /// <summary>
        /// 五分钟批处理数据块
        /// </summary>
        private BatchBlock<TrafficDensity> _fiveBatchBlock;
        /// <summary>
        /// 十五分钟批处理数据块
        /// </summary>
        private BatchBlock<TrafficDensity> _fifteenBatchBlock;
        /// <summary>
        /// 六十分钟批处理数据块
        /// </summary>
        private BatchBlock<TrafficDensity> _sixtyBatchBlock;

        /// <summary>
        /// 广播数据块
        /// </summary>
        private BroadcastBlock<TrafficDensity> _broadcastBlock;

        /// <summary>
        /// 数据库数据块
        /// </summary>
        private DensityDbBlock _dbBlock;
        /// <summary>
        /// 缓存数据块
        /// </summary>
        private DensityCacheBlock _cacheBlock;
        /// <summary>
        /// websocket数据块
        /// </summary>
        private DensityWebSocketBlock _webSocketBlock;

        /// <summary>
        /// 正确总数
        /// </summary>
        private int _ok;

        /// <summary>
        /// 未知总数
        /// </summary>
        public int _unknown;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">实例工厂</param>
        public DensityBranchBlock(IServiceProvider serviceProvider)
            : base(serviceProvider, LogEvent.高点数据块)
        {
        
        }

        protected override void OpenCore()
        {
            _ok = 0;
            _unknown = 0;

            _broadcastBlock = new BroadcastBlock<TrafficDensity>(d => d);
            _fiveBatchBlock = new BatchBlock<TrafficDensity>(BatchSize);
            _fifteenBatchBlock = new BatchBlock<TrafficDensity>(BatchSize);
            _sixtyBatchBlock = new BatchBlock<TrafficDensity>(BatchSize);

            _oneBatchBlock = new BatchBlock<TrafficDensity>(BatchSize);
            _cacheBlock = new DensityCacheBlock();
            _webSocketBlock = new DensityWebSocketBlock();

            _dbBlock = new DensityDbBlock(ThreadCount, _serviceProvider);

            _regionBlocks.Clear();
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (TrafficRegion region in relation.Channel.Regions)
                    {
                        DensityRegionBlock block = new DensityRegionBlock(_serviceProvider);
                        block.LinkTo(_broadcastBlock, _fiveBatchBlock, _fifteenBatchBlock, _sixtyBatchBlock);
                        _regionBlocks.TryAdd(region.MatchId, new Item
                        {
                            Block = block,
                            DataId =region.DataId
                        });
                    }
                }
            }

            _broadcastBlock.LinkTo(_oneBatchBlock, new DataflowLinkOptions { PropagateCompletion = true });
            _broadcastBlock.LinkTo(_cacheBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
            _broadcastBlock.LinkTo(_webSocketBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });

            _oneBatchBlock.LinkTo(_dbBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = false });
            _fiveBatchBlock.LinkTo(_dbBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = false });
            _fifteenBatchBlock.LinkTo(_dbBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = false });
            _sixtyBatchBlock.LinkTo(_dbBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = false });
        }

        protected override void ResetCore(List<TrafficDevice> devices)
        {
            foreach (TrafficDevice newDevice in devices)
            {
                foreach (var relation in newDevice.Device_Channels)
                {
                    foreach (TrafficRegion region in relation.Channel.Regions)
                    {
                        if (!_regionBlocks.ContainsKey(region.MatchId))
                        {
                            DensityRegionBlock block = new DensityRegionBlock(_serviceProvider);
                            block.LinkTo(_broadcastBlock, _fiveBatchBlock, _fifteenBatchBlock, _sixtyBatchBlock);
                            _regionBlocks.TryAdd(region.MatchId, new Item
                            {
                                Block = block,
                                DataId = region.DataId
                            });
                            _logger.LogInformation((int)LogEvent.高点数据块, $"密度添加区域 {region.MatchId}");

                        }
                    }
                }
            }

            foreach (TrafficDevice oldDevice in _devices)
            {
                foreach (var relation in oldDevice.Device_Channels)
                {
                    foreach (TrafficRegion region in relation.Channel.Regions)
                    {
                        if (devices.SelectMany(d => d.Device_Channels)
                            .Select(r => r.Channel)
                            .SelectMany(c => c.Regions)
                            .All(r => r.MatchId != region.MatchId))
                        {
                            if (_regionBlocks.TryRemove(region.MatchId, out Item item))
                            {
                                item.Block.InputBlock.Complete();
                                item.Block.WaitCompletion();
                            }
                            _logger.LogInformation((int)LogEvent.高点数据块, $"密度删除区域 {region.MatchId}");
                        }
                    }
                }
            }
        }

        protected override void Handle(TrafficDensity density)
        {
            if (_regionBlocks.ContainsKey(density.MatchId))
            {
                Item item = _regionBlocks[density.MatchId];
                density.DataId = item.DataId;
                item.Block.InputBlock.Post(density);
                item.Total += 1;
                _ok += 1;
            }
            else
            {
                _unknown += 1;
                _logger.LogWarning((int)LogEvent.高点数据块, $"未配置的区域 {density.MatchId}");
            }
        }

        protected override void CloseCore()
        {
            foreach (var regionBlock in _regionBlocks)
            {
                regionBlock.Value.Block.InputBlock.Complete();
                regionBlock.Value.Block.WaitCompletion();
            }

            _broadcastBlock.Complete();
            _fiveBatchBlock.Complete();
            _fifteenBatchBlock.Complete();
            _sixtyBatchBlock.Complete();

            _cacheBlock.WaitCompletion();
            _webSocketBlock.WaitCompletion();
            _oneBatchBlock.Completion.Wait();
            _fiveBatchBlock.Completion.Wait();
            _fifteenBatchBlock.Completion.Wait();
            _sixtyBatchBlock.Completion.Wait();

            _dbBlock.InputBlock.Complete();
            _dbBlock.WaitCompletion();
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
                {"数据滞留数量", _dbBlock.InputCount* BatchSize},
                {"一分钟存储成功数量", _dbBlock.Success_One},
                {"一分钟存储失败数量", _dbBlock.Failed_One},
                {"五分钟存储成功数量", _dbBlock.Success_Five},
                {"五分钟存储失败数量", _dbBlock.Failed_Five},
                {"十五分钟存储成功数量", _dbBlock.Success_Fifteen},
                {"十五分钟存储失败数量", _dbBlock.Failed_Fifteen},
                {"一小时入库成功数量", _dbBlock.Success_Sixty},
                {"一小时入库失败数量", _dbBlock.Failed_Sixty}
            };

            foreach (var pair in _regionBlocks)
            {
                data.Add(pair.Key, pair.Value.Total);
            }

            return Task.FromResult(_dbBlock.InputCount == 0 ? HealthCheckResult.Healthy("密度数据存储正常", data) : HealthCheckResult.Unhealthy("密度数据存储已经超过阀值", null, data));
        }
        #endregion
    }
}
