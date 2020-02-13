using System;
using ItsukiSumeragi.DataFlow;
using Kakegurui.Log;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NishinotouinYuriko.Data;
using NishinotouinYuriko.Models;


namespace NishinotouinYuriko.DataFlow
{
    /// <summary>
    /// 违法数据块
    /// </summary>
    public class ViolationDbBlock : TrafficArrayActionBlock<ViolationStruct>
    {
        /// <summary>
        /// 实例工厂
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<ViolationDbBlock> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="threadCount">入库线程数</param>
        /// <param name="serviceProvider">实例工厂</param>
        public ViolationDbBlock(int threadCount, IServiceProvider serviceProvider)
            : base(threadCount)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<ViolationDbBlock>>();
        }

        /// <summary>
        /// 入库成功个数
        /// </summary>
        public int Success { get; private set; }

        /// <summary>
        /// 入库失败个数
        /// </summary>
        public int Failed { get; private set; }

        protected override void Handle(ViolationStruct[] datas)
        {
            _logger.LogDebug((int)LogEvent.违法数据块, "开始保存违法数据");

            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                using (ViolationContext context = serviceScope.ServiceProvider.GetRequiredService<ViolationContext>())
                {
                    try
                    {
                        context.Violations.AddRange(datas);
                        context.BulkSaveChanges(options => options.BatchSize = datas.Length);
                        Success += datas.Length;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError((int)LogEvent.违法数据块, ex, "违法数据入库异常");
                        Failed += datas.Length;
                    }
                }
            }
            _logger.LogDebug((int)LogEvent.违法数据块, "保存违法数据完成");

        }
    }
}