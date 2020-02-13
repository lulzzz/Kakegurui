using System;
using System.Linq;
using ItsukiSumeragi.DataFlow;
using Kakegurui.Core;
using Kakegurui.Log;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MomobamiKirari.Data;
using MomobamiKirari.Models;

namespace MomobamiKirari.DataFlow
{
    /// <summary>
    /// 流量数据库数据块
    /// </summary>
    public class LaneFlowDbBlock : TrafficArrayActionBlock<LaneFlow>
    {
        /// <summary>
        /// 实例工厂
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<LaneFlowDbBlock> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="threadCount">入库线程数</param>
        /// <param name="serviceProvider">实例工厂</param>
        public LaneFlowDbBlock(int threadCount, IServiceProvider serviceProvider)
            : base(threadCount)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<LaneFlowDbBlock>>();
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
        public int Failed_Fifteen  { get; private set; }

        /// <summary>
        /// 六十分钟成功次数
        /// </summary>
        public int Success_Sixty { get; private set; }

        /// <summary>
        /// 六十分钟失败次数
        /// </summary>
        public int Failed_Sixty { get; private set; }

        protected override void Handle(LaneFlow[] datas)
        {
            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                using (FlowContext context = serviceScope.ServiceProvider.GetRequiredService<FlowContext>())
                {
                    if (datas[0].DateLevel == DateTimeLevel.Minute)
                    {
                        try
                        {
                            context.LaneFlows_One.AddRange(datas.Select(data => new LaneFlow_One
                            {
                                DataId = data.DataId,
                                DateTime = data.DateTime,
                                Cars = data.Cars,
                                Buss = data.Buss,
                                Vans = data.Vans,
                                Tricycles = data.Tricycles,
                                Trucks = data.Trucks,
                                Motorcycles = data.Motorcycles,
                                Bikes = data.Bikes,
                                Persons = data.Persons,
                                TravelTime = data.TravelTime,
                                Distance = data.Distance,
                                HeadDistance = data.HeadDistance,
                                Occupancy = data.Occupancy,
                                TimeOccupancy = data.TimeOccupancy,
                                Count = data.Count
                            }));
                            context.BulkSaveChanges(options => options.BatchSize = datas.Length);
                            Success_One += datas.Length;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError((int)LogEvent.流量数据块, ex, "一分钟流量数据存储异常");
                            Failed_One += datas.Length;
                        }
                    }
                    else if (datas[0].DateLevel == DateTimeLevel.FiveMinutes)
                    {
                        try
                        {
                            context.LaneFlows_Five.AddRange(datas.Select(data => new LaneFlow_Five
                            {
                                DataId = data.DataId,
                                DateTime = data.DateTime,
                                Cars = data.Cars,
                                Buss = data.Buss,
                                Vans = data.Vans,
                                Tricycles = data.Tricycles,
                                Trucks = data.Trucks,
                                Motorcycles = data.Motorcycles,
                                Bikes = data.Bikes,
                                Persons = data.Persons,
                                TravelTime = data.TravelTime,
                                Distance = data.Distance,
                                HeadDistance = data.HeadDistance,
                                Occupancy = data.Occupancy,
                                TimeOccupancy = data.TimeOccupancy,
                                Count = data.Count
                            }));
                            context.BulkSaveChanges(options => options.BatchSize = datas.Length);
                            Success_Five += datas.Length;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError((int)LogEvent.流量数据块, ex, "五分钟流量数据存储异常");
                            Failed_Five += datas.Length;
                        }
                    }
                    else if (datas[0].DateLevel == DateTimeLevel.FifteenMinutes)
                    {
                        try
                        {
                            context.LaneFlows_Fifteen.AddRange(datas.Select(data => new LaneFlow_Fifteen
                            {
                                DataId = data.DataId,
                                DateTime = data.DateTime,
                                Cars = data.Cars,
                                Buss = data.Buss,
                                Vans = data.Vans,
                                Tricycles = data.Tricycles,
                                Trucks = data.Trucks,
                                Motorcycles = data.Motorcycles,
                                Bikes = data.Bikes,
                                Persons = data.Persons,
                                TravelTime = data.TravelTime,
                                Distance = data.Distance,
                                HeadDistance = data.HeadDistance,
                                Occupancy = data.Occupancy,
                                TimeOccupancy = data.TimeOccupancy,
                                Count = data.Count
                            }));
                            context.BulkSaveChanges(options => options.BatchSize = datas.Length);
                            Success_Fifteen += datas.Length;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError((int)LogEvent.流量数据块, ex, "十五分钟流量数据存储异常");
                            Failed_Fifteen += datas.Length;
                        }
                    }
                    else if (datas[0].DateLevel == DateTimeLevel.Hour)
                    {
                        try
                        {
                            context.LaneFlows_Hour.AddRange(datas.Select(data => new LaneFlow_Hour
                            {
                                DataId = data.DataId,
                                DateTime = data.DateTime,
                                Cars = data.Cars,
                                Buss = data.Buss,
                                Vans = data.Vans,
                                Tricycles = data.Tricycles,
                                Trucks = data.Trucks,
                                Motorcycles = data.Motorcycles,
                                Bikes = data.Bikes,
                                Persons = data.Persons,
                                TravelTime = data.TravelTime,
                                Distance = data.Distance,
                                HeadDistance = data.HeadDistance,
                                Occupancy = data.Occupancy,
                                TimeOccupancy = data.TimeOccupancy,
                                Count = data.Count
                            }));
                            context.BulkSaveChanges(options => options.BatchSize = datas.Length);
                            Success_Sixty += datas.Length;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError((int)LogEvent.流量数据块, ex, "一小时流量数据存储异常");
                            Failed_Sixty += datas.Length;
                        }
                    }
                }
            }
        }
    }
}
