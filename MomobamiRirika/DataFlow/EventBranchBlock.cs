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
    /// 交通事件数据分支数据块
    /// </summary>
    public class EventBranchBlock : TrafficBranchBlock<TrafficEvent>
    {
        /// <summary>
        /// 区域数据块项
        /// </summary>
        private class Item
        {
            public string DataId { get; set; }
            public EventRegionBlock Block { get; set; }
            public int Total { get; set; }
        }

        /// <summary>
        /// 车道或区域集合
        /// </summary>
        private readonly ConcurrentDictionary<string, Item> _regionBlocks = new ConcurrentDictionary<string, Item>();

        /// <summary>
        /// 广播数据块
        /// </summary>
        private BroadcastBlock<TrafficEvent> _broadcastBlock;

        /// <summary>
        /// 数据库新增数据块
        /// </summary>
        private EventInsertDbBlock _insertDbBlock;

        /// <summary>
        /// 数据库更新数据块
        /// </summary>
        private EventUpdateDbBlock _updateDbBlock;

        /// <summary>
        /// 缓存数据块
        /// </summary>
        private EventCacheBlock _cacheBlock;

        /// <summary>
        /// ws数据块
        /// </summary>
        private EventWebSocketBlock _webSocketBlock;

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
        public EventBranchBlock(IServiceProvider serviceProvider)
            : base(serviceProvider,LogEvent.事件数据块)
        {

        }

        protected override void OpenCore()
        {
            _ok = 0;
            _unknown = 0;

            _broadcastBlock = new BroadcastBlock<TrafficEvent>(e => e);
            _insertDbBlock = new EventInsertDbBlock(ThreadCount, _serviceProvider);
            _updateDbBlock = new EventUpdateDbBlock(ThreadCount, _serviceProvider);
            _cacheBlock = new EventCacheBlock();
            _webSocketBlock = new EventWebSocketBlock(_serviceProvider);

            _regionBlocks.Clear();
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (TrafficRegion region in relation.Channel.Regions)
                    {
                        EventRegionBlock regionBlock = new EventRegionBlock(_serviceProvider);
                        regionBlock.LinkTo(_broadcastBlock, _updateDbBlock.InputBlock);
                        _regionBlocks.TryAdd(region.MatchId, new Item
                        {
                            Block = regionBlock,
                            DataId = region.DataId
                        });
                    }
                }
            }

            _broadcastBlock.LinkTo(_insertDbBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
            _broadcastBlock.LinkTo(_cacheBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
            _broadcastBlock.LinkTo(_webSocketBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });

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
                            EventRegionBlock regionBlock = new EventRegionBlock(_serviceProvider);
                            regionBlock.LinkTo(_broadcastBlock, _updateDbBlock.InputBlock);
                            _regionBlocks.TryAdd(region.MatchId, new Item
                            {
                                Block = regionBlock,
                                DataId = region.DataId
                            });
                            _logger.LogInformation((int)LogEvent.事件数据块, $"事件添加区域 {region.MatchId}");

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
                            _logger.LogInformation((int)LogEvent.高点数据块, $"事件删除区域 {region.MatchId}");
                        }
                    }
                }
            }
        }

        protected override void Handle(TrafficEvent trafficEvent)
        {
            if (_regionBlocks.ContainsKey(trafficEvent.MatchId))
            {
                Item item = _regionBlocks[trafficEvent.MatchId];
                trafficEvent.DataId = item.DataId;
                item.Block.InputBlock.Post(trafficEvent);
                item.Total += 1;
                _ok += 1;
            }
            else
            {
                _unknown += 1;
                _logger.LogWarning((int)LogEvent.事件数据块, $"未配置的区域 {trafficEvent.MatchId}");
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
            _cacheBlock.WaitCompletion();
            _webSocketBlock.WaitCompletion();
            _insertDbBlock.WaitCompletion();

            _updateDbBlock.InputBlock.Complete();
            _updateDbBlock.WaitCompletion();
        }

        #region 实现 IHealthCheck
        public override Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (_insertDbBlock == null || _updateDbBlock == null)
            {
                return Task.FromResult(HealthCheckResult.Healthy("事件数据存储"));
            }
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                {"成功解析数量", _ok},
                {"未知数据数量", _unknown},
                {"下个时间段数量", _next},
                {"时间范围外数量", _over},
                {"新增数据滞留数量", _insertDbBlock.InputCount},
                {"新增数据成功数量", _insertDbBlock.Success},
                {"新增数据失败数量", _insertDbBlock.Failed},
                {"更新数据滞留数量", _updateDbBlock.InputCount},
                {"更新数据成功数量", _updateDbBlock.Success},
                {"更新数据失败数量", _updateDbBlock.Failed}
            };

            foreach (var pair in _regionBlocks)
            {
                data.Add(pair.Key, pair.Value.Total);
            }

            return Task.FromResult(_insertDbBlock.InputCount==0? HealthCheckResult.Healthy("事件数据存储正常", data):HealthCheckResult.Unhealthy("事件数据存储已经超过阀值", null, data));
        }
        #endregion
    }
}
