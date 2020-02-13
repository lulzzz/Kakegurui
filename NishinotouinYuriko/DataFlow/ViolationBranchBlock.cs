using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using ItsukiSumeragi.DataFlow;
using Kakegurui.Log;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NishinotouinYuriko.Models;

namespace NishinotouinYuriko.DataFlow
{
    /// <summary>
    /// 违法数据分支数据块
    /// </summary>
    public class ViolationBranchBlock : TrafficBranchBlock<ViolationStruct>
    {
        /// <summary>
        /// 广播数据块
        /// </summary>
        private BroadcastBlock<ViolationStruct> _broadcastBlock;

        /// <summary>
        /// 批处理数据块
        /// </summary>
        private BatchBlock<ViolationStruct> _batchBlock;

        /// <summary>
        /// 图片数据块
        /// </summary>
        private ViolationImageBlock _imageBlock;

        /// <summary>
        /// 数据库数据块
        /// </summary>
        private ViolationDbBlock _dbBlock;

        /// <summary>
        /// 总数
        /// </summary>
        private int _total;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">实例工厂</param>
        public ViolationBranchBlock(IServiceProvider serviceProvider)
            : base(serviceProvider, LogEvent.违法数据块)
        {
        }

        protected override void OpenCore()
        {
            _total = 0;

            _broadcastBlock=new BroadcastBlock<ViolationStruct>(v=>v);
            _batchBlock = new BatchBlock<ViolationStruct>(1000);
            _imageBlock=new ViolationImageBlock(_serviceProvider);
            _dbBlock = new ViolationDbBlock(ThreadCount, _serviceProvider);
            _broadcastBlock.LinkTo(_batchBlock, new DataflowLinkOptions {PropagateCompletion = true});
            _broadcastBlock.LinkTo(_imageBlock.InputBlock, new DataflowLinkOptions {PropagateCompletion = true});

            _batchBlock.LinkTo(_dbBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
        }

        protected override void Handle(ViolationStruct t)
        {
            _broadcastBlock.Post(t);
            _total += 1;
        }

        protected override void CloseCore()
        {
            _broadcastBlock.Complete();
            _imageBlock.WaitCompletion();
            _dbBlock.WaitCompletion();
        }

        public override void TriggerSave()
        {
            _batchBlock.TriggerBatch();
        }

        #region 实现 IHealthCheck
        public override Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                {"下个时间段数量", _next},
                {"时间范围外数量", _over},
                {"数据总数", _total},
                {"缓存滞留数量", _batchBlock.OutputCount * BatchSize},
                {"入库滞留数量", _dbBlock.InputCount * BatchSize},
                {"存储成功数量", _dbBlock.Success},
                {"存储失败数量", _dbBlock.Failed}
            };

            return Task.FromResult(
                _dbBlock.InputCount == 0
                    ? HealthCheckResult.Healthy("违法数据存储", data)
                    : HealthCheckResult.Unhealthy("违法数据存储", null, data));
        }
        #endregion
    }
}
