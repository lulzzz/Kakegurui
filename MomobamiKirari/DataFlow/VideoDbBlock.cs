using System;
using ItsukiSumeragi.DataFlow;
using Kakegurui.Log;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MomobamiKirari.Data;
using MomobamiKirari.Models;

namespace MomobamiKirari.DataFlow
{
    /// <summary>
    /// 机动车视频结构化数据块
    /// </summary>
    public class VehicleDbBlock : TrafficArrayActionBlock<VideoVehicle>
    {
        /// <summary>
        /// 实例工厂
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<VehicleDbBlock> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="threadCount">入库线程数</param>
        /// <param name="serviceProvider">实例工厂</param>
        public VehicleDbBlock(int threadCount, IServiceProvider serviceProvider)
            : base(threadCount)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<VehicleDbBlock>>();
        }

        /// <summary>
        /// 入库成功个数
        /// </summary>
        public int Success { get; private set; }

        /// <summary>
        /// 入库失败个数
        /// </summary>
        public int Failed { get; private set; }

        protected override void Handle(VideoVehicle[] datas)
        {
            _logger.LogDebug((int)LogEvent.视频数据块, "开始保存视频结构化数据");
            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                using (FlowContext context = serviceScope.ServiceProvider.GetRequiredService<FlowContext>())
                {
                    try
                    {
                        context.Vehicles.AddRange(datas);
                        context.BulkSaveChanges(options => options.BatchSize = datas.Length);
                        Success += datas.Length;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError((int)LogEvent.视频数据块, ex, "机动车数据入库异常");
                        Failed += datas.Length;
                    }
                }
            }
            _logger.LogDebug((int)LogEvent.视频数据块, "保存视频结构化数据完成");
        }
    }

    /// <summary>
    /// 非机动车数据库数据块
    /// </summary>
    public class BikeDbBlock : TrafficArrayActionBlock<VideoBike>
    {
        /// <summary>
        /// 实例工厂
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<BikeDbBlock> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">实例工厂</param>
        /// <param name="threadCount">入库线程数</param>
        public BikeDbBlock(int threadCount, IServiceProvider serviceProvider)
            : base(threadCount)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<BikeDbBlock>>();
        }

        /// <summary>
        /// 入库成功个数
        /// </summary>
        public int Success { get; private set; }

        /// <summary>
        /// 入库失败个数
        /// </summary>
        public int Failed { get; private set; }

        protected override void Handle(VideoBike[] datas)
        {
            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                using (FlowContext context = serviceScope.ServiceProvider.GetRequiredService<FlowContext>())
                {
                    try
                    {
                        context.Bikes.AddRange(datas);
                        context.BulkSaveChanges(options => options.BatchSize = datas.Length);
                        Success += datas.Length;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError((int)LogEvent.视频数据块, ex, "非机动车数据入库异常");
                        Failed += datas.Length;

                    }
                }
            }


        }
    }

    /// <summary>
    /// 行人视频结构化数据块
    /// </summary>
    public class PedestrainDbBlock : TrafficArrayActionBlock<VideoPedestrain>
    {
        /// <summary>
        /// 实例工厂
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<PedestrainDbBlock> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">实例工厂</param>
        /// <param name="threadCount">入库线程数</param>
        public PedestrainDbBlock(int threadCount, IServiceProvider serviceProvider)
            : base(threadCount)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<PedestrainDbBlock>>();
        }

        /// <summary>
        /// 入库成功个数
        /// </summary>
        public int Success { get; private set; }

        /// <summary>
        /// 入库失败个数
        /// </summary>
        public int Failed { get; private set; }

        protected override void Handle(VideoPedestrain[] datas)
        {
            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                using (FlowContext context = serviceScope.ServiceProvider.GetRequiredService<FlowContext>())
                {
                    try
                    {
                        context.Pedestrains.AddRange(datas);
                        context.BulkSaveChanges(options => options.BatchSize = datas.Length);
                        Success += datas.Length;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError((int)LogEvent.视频数据块, ex, "行人数据入库异常");
                        Failed += datas.Length;
                    }
                }
            }


        }
    }
}