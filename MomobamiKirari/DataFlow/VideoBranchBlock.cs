using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using ItsukiSumeragi.DataFlow;
using Kakegurui.Log;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MomobamiKirari.Models;

namespace MomobamiKirari.DataFlow
{
    /// <summary>
    /// 视频结构化分支数据块
    /// </summary>
    public class VideoBranchBlock : TrafficBranchBlock<VideoStruct, FlowDevice>
    {
        /// <summary>
        /// 机动车数据库数据块
        /// </summary>
        private VehicleDbBlock _vehicleDbBlock;

        /// <summary>
        /// 机动车批处理数据块
        /// </summary>
        private BatchBlock<VideoVehicle> _vehicleBatchBlock;

        /// <summary>
        /// 非机动车数据库数据块
        /// </summary>
        private BikeDbBlock _bikeDbBlock;

        /// <summary>
        /// 非机动车批处理数据块
        /// </summary>
        private BatchBlock<VideoBike> _bikeBatchBlock;

        /// <summary>
        /// 行人数据库数据块
        /// </summary>
        private PedestrainDbBlock _pedestrainDbBlock;

        /// <summary>
        /// 行人批处理数据块
        /// </summary>
        private BatchBlock<VideoPedestrain> _pedestrainBatchBlock;

        /// <summary>
        /// 机动车总数
        /// </summary>
        private int _vehicleTotal;

        /// <summary>
        /// 非机动车总数
        /// </summary>
        private int _bikeTotal;

        /// <summary>
        /// 行人总数
        /// </summary>
        private int _pedestrainTotal;

        /// <summary>
        /// 未知类型总数
        /// </summary>
        private int _unknown;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">实例工厂</param>
        public VideoBranchBlock(IServiceProvider serviceProvider)
            : base(serviceProvider, LogEvent.视频数据块)
        {
        }

        protected override void OpenCore()
        {
            _vehicleTotal = 0;
            _bikeTotal = 0;
            _pedestrainTotal = 0;
            _unknown = 0;

            _vehicleBatchBlock = new BatchBlock<VideoVehicle>(BatchSize);
            _vehicleDbBlock = new VehicleDbBlock(ThreadCount, _serviceProvider);
            _vehicleBatchBlock.LinkTo(_vehicleDbBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });

            _bikeBatchBlock = new BatchBlock<VideoBike>(BatchSize);
            _bikeDbBlock = new BikeDbBlock(ThreadCount, _serviceProvider);
            _bikeBatchBlock.LinkTo(_bikeDbBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });

            _pedestrainBatchBlock = new BatchBlock<VideoPedestrain>(BatchSize);
            _pedestrainDbBlock = new PedestrainDbBlock(ThreadCount, _serviceProvider);
            _pedestrainBatchBlock.LinkTo(_pedestrainDbBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
        }

        protected override void Handle(VideoStruct t)
        {
            if (t is VideoVehicle vehicle)
            {
                _vehicleBatchBlock.Post(vehicle);
                _vehicleTotal += 1;
            }
            else if (t is VideoBike bike)
            {
                _bikeBatchBlock.Post(bike);
                _bikeTotal += 1;
            }
            else if (t is VideoPedestrain pedestrain)
            {
                _pedestrainBatchBlock.Post(pedestrain);
                _pedestrainTotal += 1;
            }
            else
            {
                _unknown += 1;
            }
        }

        protected override void CloseCore()
        {
            _vehicleBatchBlock.Complete();
            _bikeBatchBlock.Complete();
            _pedestrainBatchBlock.Complete();

            _vehicleDbBlock.WaitCompletion();
            _bikeDbBlock.WaitCompletion();
            _pedestrainDbBlock.WaitCompletion();
        }

        public override void TriggerSave()
        {
            _vehicleBatchBlock.TriggerBatch();
            _bikeBatchBlock.TriggerBatch();
            _pedestrainBatchBlock.TriggerBatch();
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
                {"未知类型总数", _unknown},
                {"机动车总数", _vehicleTotal},
                {"机动车缓存滞留数量", _vehicleBatchBlock.OutputCount * BatchSize},
                {"机动车入库滞留数量", _vehicleDbBlock.InputCount * BatchSize},
                {"机动车存储成功数量", _vehicleDbBlock.Success},
                {"机动车存储失败数量", _vehicleDbBlock.Failed},
                {"非机动车总数", _bikeTotal},
                {"非机动车缓存滞留数量", _bikeBatchBlock.OutputCount * BatchSize},
                {"非机动车入库滞留数量", _bikeDbBlock.InputCount * BatchSize},
                {"非机动车存储成功数量", _bikeDbBlock.Success},
                {"非机动车存储失败数量", _bikeDbBlock.Failed},
                {"行人总数", _pedestrainTotal},
                {"行人缓存滞留数量", _pedestrainBatchBlock.OutputCount * BatchSize},
                {"行人入库滞留数量", _pedestrainDbBlock.InputCount * BatchSize},
                {"行人存储成功数量", _pedestrainDbBlock.Success},
                {"行人存储失败数量", _pedestrainDbBlock.Failed}
            };

            return Task.FromResult(
                _vehicleDbBlock.InputCount == 0
                    ? HealthCheckResult.Healthy("视频结构化数据存储正常", data)
                    : HealthCheckResult.Unhealthy("视频结构化数据存储已经超过阀值", null, data));
        }
        #endregion
    }
}
