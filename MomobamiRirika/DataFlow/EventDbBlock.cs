using System;
using System.Linq;
using ItsukiSumeragi.DataFlow;
using Kakegurui.Log;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MomobamiRirika.Data;
using MomobamiRirika.Models;

namespace MomobamiRirika.DataFlow
{
    /// <summary>
    /// 交通事件数据块
    /// </summary>
    public class EventInsertDbBlock : TrafficActionBlock<TrafficEvent>
    {
        /// <summary>
        /// 实例工厂
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<EventInsertDbBlock> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="threadCount">入库线程数</param>
        /// <param name="serviceProvider">实例工厂</param>
        public EventInsertDbBlock(int threadCount, IServiceProvider serviceProvider)
            : base(threadCount)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<EventInsertDbBlock>>();
        }

        /// <summary>
        /// 成功数量
        /// </summary>
        public int Success { get; private set; }

        /// <summary>
        /// 失败数量
        /// </summary>
        public int Failed { get; private set; }


        #region 重写TrafficActionBlock
        /// <summary>
        /// 入库处理函数
        /// </summary>
        /// <param name="data">交通事件</param>
        protected override void Handle(TrafficEvent data)
        {
            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                using (DensityContext context = serviceScope.ServiceProvider.GetRequiredService<DensityContext>())
                {
                    try
                    {
                        context.Events.Add(data);
                        context.SaveChanges();
                        Success += 1;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError((int)LogEvent.事件数据块, ex, "事件数据入库异常");
                        Failed += 1;
                    }
                }
            }

           
        }
        #endregion
    }

    /// <summary>
    /// 交通事件数据块
    /// </summary>
    public class EventUpdateDbBlock : TrafficActionBlock<TrafficEvent>
    {
        /// <summary>
        /// 实例工厂
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<EventUpdateDbBlock> _logger;

        /// <summary>
        /// 成功数量
        /// </summary>
        public int Success { get; private set; }

        /// <summary>
        /// 失败数量
        /// </summary>
        public int Failed { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="threadCount">入库线程数</param>
        /// <param name="serviceProvider">实例工厂</param>
        public EventUpdateDbBlock(int threadCount, IServiceProvider serviceProvider)
            : base(threadCount)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<EventUpdateDbBlock>>();

        }

        #region 重写TrafficActionBlock
        /// <summary>
        /// 入库处理函数
        /// </summary>
        /// <param name="data">交通事件</param>
        protected override void Handle(TrafficEvent data)
        {
            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                using (DensityContext context = serviceScope.ServiceProvider.GetRequiredService<DensityContext>())
                {
                    try
                    {
                        TrafficEvent dbData=context.Events.SingleOrDefault(e =>
                            e.DataId == data.DataId && e.DateTime == data.DateTime);
                        if (dbData == null)
                        {
                            Failed += 1;
                            _logger.LogWarning((int)LogEvent.事件数据块, "交通事件更新失败");
                        }
                        else
                        {
                            dbData.EndTime = data.EndTime;
                            context.SaveChanges();
                            Success += 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError((int)LogEvent.事件数据块, ex, "事件数据更新异常");
                        Failed += 1;
                    }
                }
            }

           
        }
        #endregion
    }
}
