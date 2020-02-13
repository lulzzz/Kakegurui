using System;
using System.Linq;
using ItsukiSumeragi.DataFlow;
using Kakegurui.Core;
using Kakegurui.Log;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MomobamiRirika.Data;
using MomobamiRirika.Models;

namespace MomobamiRirika.DataFlow
{
    /// <summary>
    /// 密度数据库数据块
    /// </summary>
    public class DensityDbBlock : TrafficArrayActionBlock<TrafficDensity>
    {
        /// <summary>
        /// 实例工厂
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<DensityDbBlock> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="threadCount">入库线程数</param>
        /// <param name="serviceProvider">实例工厂</param>
        public DensityDbBlock(int threadCount, IServiceProvider serviceProvider)
            :base(threadCount)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<DensityDbBlock>>();
        }

        /// <summary>
        /// 一分钟成功次数
        /// </summary>
        public int Success_One { get; private set; }

        /// <summary>
        /// 一分钟失败次数
        /// </summary>
        public int Failed_One { get; private set; }

        /// <summary>
        /// 五分钟成功次数
        /// </summary>
        public int Success_Five { get; private set; }

        /// <summary>
        /// 五分钟失败次数
        /// </summary>
        public int Failed_Five { get; private set; }

        /// <summary>
        /// 十五分钟成功次数
        /// </summary>
        public int Success_Fifteen { get; private set; }

        /// <summary>
        /// 十五分钟失败次数
        /// </summary>
        public int Failed_Fifteen { get; private set; }

        /// <summary>
        /// 六十分钟成功次数
        /// </summary>
        public int Success_Sixty { get; private set; }

        /// <summary>
        /// 六十分钟失败次数
        /// </summary>
        public int Failed_Sixty { get; private set; }

        protected override void Handle(TrafficDensity[] datas)
        {
            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                using (DensityContext context = serviceScope.ServiceProvider.GetRequiredService<DensityContext>())
                {
                    if (datas[0].DateLevel == DateTimeLevel.Minute)
                    {
                        try
                        {
                            context.Densities_One.AddRange(datas.Select(data => new TrafficDensity_One
                            {
                                DataId = data.DataId,
                                DateTime = data.DateTime,
                                Value = data.Value
                            }));
                            context.BulkSaveChanges(options => options.BatchSize = datas.Length);
                            Success_One += datas.Length;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError((int)LogEvent.高点数据块, ex, "一分钟密度数据入库异常");
                            Failed_One += datas.Length;
                        }
                    }
                    else if (datas[0].DateLevel == DateTimeLevel.FiveMinutes)
                    {
                        try
                        {
                            context.Densities_Five.AddRange(datas.Select(data => new TrafficDensity_Five
                            {
                                DataId = data.DataId,
                                DateTime = data.DateTime,
                                Value = data.Value
                            }));
                            context.BulkSaveChanges(options => options.BatchSize = datas.Length);
                            Success_Five += datas.Length;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError((int)LogEvent.高点数据块, ex, "五分钟密度数据入库异常");
                            Failed_Five += datas.Length;
                        }
                    }
                    else if (datas[0].DateLevel == DateTimeLevel.FifteenMinutes)
                    {
                        try
                        {
                            context.Densities_Fifteen.AddRange(datas.Select(data => new TrafficDensity_Fifteen
                            {
                                DataId = data.DataId,
                                DateTime = data.DateTime,
                                Value = data.Value
                            }));
                            context.BulkSaveChanges(options => options.BatchSize = datas.Length);
                            Success_Fifteen += datas.Length;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError((int)LogEvent.高点数据块, ex, "十五分钟密度数据入库异常");
                            Failed_Fifteen += datas.Length;
                        }
                    }
                    else if (datas[0].DateLevel == DateTimeLevel.Hour)
                    {
                        try
                        {
                            context.Densities_hour.AddRange(datas.Select(data => new TrafficDensity_Hour
                            {
                                DataId = data.DataId,
                                DateTime = data.DateTime,
                                Value = data.Value
                            }));
                            context.BulkSaveChanges(options => options.BatchSize = datas.Length);
                            Success_Sixty += datas.Length;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError((int)LogEvent.高点数据块, ex, "一小时密度数据入库异常");
                            Failed_Sixty += datas.Length;
                        }
                    }
                }
            }
        }
    }
}
