using System;
using System.Collections.Generic;
using System.Linq;
using Kakegurui.Log;
using Kakegurui.Monitor;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MomobamiKirari.Cache;
using MomobamiKirari.Codes;
using MomobamiKirari.Data;
using MomobamiKirari.Models;

namespace MomobamiKirari.Monitor
{
    /// <summary>
    /// 路段流量计算
    /// </summary>
    public class SectionFlowMonitor : IFixedJob
    {
        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IDistributedCache _distributedCache;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<SectionFlowMonitor> _logger;

        /// <summary>
        /// 实例工厂
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 时间偏移
        /// </summary>
        private readonly TimeSpan _span;

        /// <summary>
        /// 路段小时状态集合
        /// </summary>
        private readonly Dictionary<int, SectionStatus> _sectionStatuses = new Dictionary<int, SectionStatus>();

        /// <summary>
        /// 当前小时
        /// </summary>
        private int _currentHour;

        /// <summary>
        /// 城市交通状态
        /// </summary>
        public static CityStatus Status { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="distributedCache">缓存</param>
        /// <param name="logger">日志</param>
        /// <param name="serviceProvider">实例工厂</param>
        /// <param name="configuration">配置项</param>
        public SectionFlowMonitor(IMemoryCache memoryCache, IDistributedCache distributedCache, ILogger<SectionFlowMonitor> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
            _span = TimeSpan.FromMinutes(configuration.GetValue("DbSpan", 2));
            _logger = logger;
            _serviceProvider = serviceProvider;

            _currentHour = DateTime.Now.Hour;
        }

        public void Handle(DateTime lastTime, DateTime current, DateTime nextTime)
        {
            SaveSectionHourStatus(current);

            CalculateSectionMinuteFlow(current);

            CalculateSectionDayFlow();
        }

        /// <summary>
        /// 保存路段小时状态
        /// </summary>
        /// <param name="time">当前计算时间</param>
        private void SaveSectionHourStatus(DateTime time)
        {
            if (time.Hour != _currentHour)
            {
                _logger.LogInformation((int)LogEvent.路段状态, $"保存路段小时状态 {_currentHour}");
                using (IServiceScope serviceScope = _serviceProvider.CreateScope())
                {
                    using (FlowContext context = serviceScope.ServiceProvider.GetRequiredService<FlowContext>())
                    {
                        try
                        {
                            context.SectionStatuses.AddRange(_sectionStatuses.Values);
                            context.BulkSaveChanges(options => options.BatchSize = _sectionStatuses.Count);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError((int)LogEvent.路段状态, ex, "路段小时状态存储异常");
                        }
                    }
                }
                _sectionStatuses.Clear();
                _currentHour = time.Hour;
            }
        }

        /// <summary>
        /// 计算所有路段分钟流量
        /// </summary>
        /// <param name="time"></param>
        private void CalculateSectionMinuteFlow(DateTime time)
        {
            var totalLanes = _memoryCache.GetLanes();
            time = time.Add(-_span);
            foreach (RoadSection section in _memoryCache.GetSections())
            {
                List<Lane> sectionLanes = totalLanes
                    .Where(l => l.Channel.SectionId.HasValue && l.Channel.SectionId.Value == section.SectionId)
                    .ToList();
                SectionFlow sectionFlow = new SectionFlow
                {
                    SectionId = section.SectionId,
                    DateTime = time,
                    SectionType = section.SectionType,
                    FreeSpeed = section.FreeSpeed,
                    Length = section.Length
                };
                int availableLanesCount = 0;
                foreach (var lane in sectionLanes)
                {
                    LaneFlow laneFlow = _distributedCache.GetLaneMinuteFlow(lane.DataId, time);
                    if (laneFlow != null)
                    {
                        sectionFlow.Total += laneFlow.Total;
                        sectionFlow.Vehicle += laneFlow.Vehicle;
                        sectionFlow.HeadDistance += laneFlow.HeadDistance;
                        sectionFlow.TimeOccupancy += laneFlow.TimeOccupancy;
                        sectionFlow.Occupancy += laneFlow.Occupancy;
                        sectionFlow.Distance += laneFlow.Distance;
                        sectionFlow.TravelTime += laneFlow.TravelTime;
                        availableLanesCount += 1;
                    }
                }

                if (availableLanesCount > 0)
                {
                    //路段平均速度(千米/小时)
                    double sectionAverageSpeed = sectionFlow.TravelTime > 0 ? (sectionFlow.Distance / sectionFlow.TravelTime * 3600 / 1000) : 0;
                    //路段行程时间(秒)
                    double sectionTravelTime = sectionAverageSpeed > 0 ? sectionFlow.Length / (sectionAverageSpeed * 1000 / 3600) : 0;
                    //路段自由流行程时间(秒)
                    double sectionFreeTravelTime = sectionFlow.FreeSpeed > 0 ? sectionFlow.Length / (sectionFlow.FreeSpeed * 1000 / 3600) : 0;
                    //路段行程时间比
                    double sectionTravelTimeProportion =
                        sectionTravelTime < sectionFreeTravelTime
                            ? 1
                            : sectionTravelTime / sectionFreeTravelTime;

                    //路段交通状态
                    TrafficStatus trafficStatus;
                    if (sectionAverageSpeed > sectionFlow.FreeSpeed * 0.7)
                    {
                        trafficStatus = TrafficStatus.通畅;
                    }
                    else if (sectionAverageSpeed <= sectionFlow.FreeSpeed * 0.7
                             && sectionAverageSpeed > sectionFlow.FreeSpeed * 0.5)
                    {
                        trafficStatus = TrafficStatus.基本通畅;
                    }
                    else if (sectionAverageSpeed <= sectionFlow.FreeSpeed * 0.5
                             && sectionAverageSpeed > sectionFlow.FreeSpeed * 0.4)
                    {
                        trafficStatus = TrafficStatus.轻度拥堵;
                    }
                    else if (sectionAverageSpeed <= sectionFlow.FreeSpeed * 0.4
                             && sectionAverageSpeed > sectionFlow.FreeSpeed * 0.3)
                    {
                        trafficStatus = TrafficStatus.中度拥堵;
                    }
                    else
                    {
                        trafficStatus = TrafficStatus.严重拥堵;
                    }

                    sectionFlow.Count = 1;

                    sectionFlow.AverageSpeed = sectionAverageSpeed;
                    sectionFlow.HeadDistance = sectionFlow.HeadDistance / availableLanesCount;
                    sectionFlow.Occupancy = sectionFlow.Occupancy / availableLanesCount;
                    sectionFlow.TimeOccupancy = sectionFlow.TimeOccupancy / availableLanesCount;
                    sectionFlow.TrafficStatus = trafficStatus;

                    sectionFlow.Total = sectionFlow.Total;
                    sectionFlow.Vehicle = sectionFlow.Vehicle;
                    sectionFlow.Vkt = sectionFlow.Vehicle * sectionFlow.Length;
                    sectionFlow.Fls = sectionFlow.Vehicle * sectionTravelTime;
                    sectionFlow.TravelTimeProportion = sectionTravelTimeProportion;

                    CaculateSectionHourStatus(sectionFlow);

                    SaveSectionMinuteFlow(sectionFlow);

                    _logger.LogDebug((int)LogEvent.路段流量, $"路段流量 id:{sectionFlow.SectionId} time:{sectionFlow.DateTime} total lanes:{sectionLanes.Count} available lanes:{availableLanesCount} type:{sectionFlow.SectionType} length:{sectionFlow.Length} free speed:{sectionFlow.FreeSpeed} average speed:{sectionFlow.AverageSpeed} head distance:{sectionFlow.HeadDistance} occ:{sectionFlow.Occupancy} tocc {sectionFlow.TimeOccupancy} status:{sectionFlow.TrafficStatus} total:{sectionFlow.Total} vehicle:{sectionFlow.Vehicle} vkt:{sectionFlow.Vkt} fls:{sectionFlow.Fls} ttp:{sectionFlow.TravelTimeProportion}");
                }
                else
                {
                    _logger.LogDebug((int)LogEvent.路段流量, $"路段流量计算失败 id:{sectionFlow.SectionId} time:{sectionFlow.DateTime} total lanes:{sectionLanes.Count} available lanes:{availableLanesCount}");
                }
                

            }
        }

        /// <summary>
        /// 计算路段小时状态
        /// </summary>
        /// <param name="sectionFlow">路段流量</param>
        private void CaculateSectionHourStatus(SectionFlow sectionFlow)
        {
            if (!_sectionStatuses.ContainsKey(sectionFlow.SectionId))
            {
                _sectionStatuses.Add(sectionFlow.SectionId, new SectionStatus
                {
                    SectionId = sectionFlow.SectionId,
                    DateTime = new DateTime(sectionFlow.DateTime.Year, sectionFlow.DateTime.Month, sectionFlow.DateTime.Day, sectionFlow.DateTime.Hour, 0, 0)
                });
            }

            if (sectionFlow.TrafficStatus == TrafficStatus.通畅)
            {
                _sectionStatuses[sectionFlow.SectionId].Good += 1;
            }
            else if (sectionFlow.TrafficStatus == TrafficStatus.基本通畅)
            {
                _sectionStatuses[sectionFlow.SectionId].Normal += 1;
            }
            else if (sectionFlow.TrafficStatus == TrafficStatus.轻度拥堵)
            {
                _sectionStatuses[sectionFlow.SectionId].Warning += 1;
            }
            else if (sectionFlow.TrafficStatus == TrafficStatus.中度拥堵)
            {
                _sectionStatuses[sectionFlow.SectionId].Bad += 1;
            }
            else if (sectionFlow.TrafficStatus == TrafficStatus.严重拥堵)
            {
                _sectionStatuses[sectionFlow.SectionId].Dead += 1;
            }
        }

        /// <summary>
        /// 保存路算分钟流量
        /// </summary>
        /// <param name="sectionFlow">路段流量</param>
        private void SaveSectionMinuteFlow(SectionFlow sectionFlow)
        {
            _memoryCache.SetSectionLastFlow(sectionFlow);
            _distributedCache.SetSectionHourFlow(sectionFlow);
            _distributedCache.SetSectionDayFlow(sectionFlow);
        }

        /// <summary>
        /// 计算路段当天流量
        /// </summary>
        private void CalculateSectionDayFlow()
        {
            DateTime now = DateTime.Now;
            CityStatus cityStatus = new CityStatus
            {
                CongestionDatas = new Dictionary<int, double>(),
                SectionStatuses = Enum.GetValues(typeof(TrafficStatus))
                    .Cast<TrafficStatus>()
                    .ToDictionary(trafficStatus => (int)trafficStatus, trafficStatus => new SectionsSpeed())
            };
            List<RoadSection> sections = _memoryCache.GetSections();
            //计算当前分钟路段交通状态
            Dictionary<TrafficStatus, List<SectionFlow>> trafficStatusFlows = Enum.GetValues(typeof(TrafficStatus)).Cast<TrafficStatus>().ToDictionary(trafficStatus => trafficStatus, trafficStatus => new List<SectionFlow>());
            foreach (var section in sections)
            {
                SectionFlow flow = _memoryCache.GetSectionLastFlow(section.SectionId);
                if (flow != null)
                {
                    trafficStatusFlows[flow.TrafficStatus].Add(flow);
                }
            }
            foreach (var pair in trafficStatusFlows)
            {
                //状态下路网总vkt
                double totalStatusVkts = pair.Value.Sum(f => f.Vkt);
                //状态下路网平均速度
                double totalStatusAverageSpeed = 0.0;
                if (totalStatusVkts > 0)
                {
                    //状态下路网平均速度
                    totalStatusAverageSpeed =
                        (pair.Value.Sum(f => f.Fls) > 0
                        ? pair.Value.Sum(f => f.Vkt) / pair.Value.Sum(f => f.Fls) * (pair.Value.Sum(f => f.Vkt) / totalStatusVkts)
                        : 0)
                        * 3600 / 1000;
                }

                //当前交通状态
                cityStatus.SectionStatuses[(int)pair.Key].AverageSpeed = Convert.ToInt32(totalStatusAverageSpeed);
                cityStatus.SectionStatuses[(int)pair.Key].SectionCount = pair.Value.Count;
            }

            //城市小时拥堵指数
            DateTime today = now.Date;
            for (int h = 0; h < now.Hour; ++h)
            {
                double? congestionData = _memoryCache.GetCityHourCongestionData(today.AddHours(h));
                if (!congestionData.HasValue)
                {
                    congestionData = GetHourCongestionData(today.AddHours(h));
                    _memoryCache.SetCityHourCongestionData(today.AddHours(h), congestionData.Value);
                }

                cityStatus.CongestionDatas.Add(h, Math.Round(congestionData.Value, 1));
            }
            cityStatus.CongestionDatas.Add(now.Hour, Math.Round(GetHourCongestionData(today.AddHours(now.Hour)), 1));

            //今日总流量
            foreach (var lane in _memoryCache.GetLanes())
            {
                LaneFlow flow = _distributedCache.GetLaneDayFlow(lane.DataId, today);
                if (flow != null)
                {
                    cityStatus.TotalFlow += flow.Total;
                }
            }
            //今日平均速度
            Dictionary<int, List<SectionFlow>> sectionTypeDayFlows = Enum.GetValues(typeof(SectionType)).Cast<SectionType>().ToDictionary(s => (int)s, s => new List<SectionFlow>());
            foreach (var section in sections)
            {
                SectionFlow flow = _distributedCache.GetSectionDayFlow(section.SectionId, today);
                if (flow != null)
                {
                    _memoryCache.FillSectionFlowCache(flow);
                    sectionTypeDayFlows[flow.SectionType].Add(flow);
                }
            }
            //路网总vkt
            double totalVkts = sectionTypeDayFlows.Sum(p => p.Value.Sum(f => f.Vkt));
            if (totalVkts > 0)
            {
                cityStatus.AverageSpeed = Convert.ToInt64(
                    sectionTypeDayFlows
                        .Sum(p => p.Value.Sum(f => f.Fls) > 0
                            ? p.Value.Sum(f => f.Vkt) / p.Value.Sum(f => f.Fls) * (p.Value.Sum(f => f.Vkt) / totalVkts)
                            : 0) * 3600 / 1000);
            }
            else
            {
                cityStatus.AverageSpeed = 0;
            }
            //拥堵排名
            cityStatus.SectionCongestionRank = sectionTypeDayFlows
                .SelectMany(p => p.Value)
                .Where(f => f.CongestionSpan > 0)
                .OrderByDescending(f => f.CongestionSpan)
                .Take(10)
                .ToList();

            //当前拥堵路段列表
            cityStatus.SectionCongestions = sectionTypeDayFlows
                .SelectMany(p => p.Value)
                .Where(f => f.TrafficStatus >= TrafficStatus.轻度拥堵)
                .OrderByDescending(f => f.CongestionStartTime)
                .ToList();

            Status = cityStatus;
        }

        /// <summary>
        /// 获取城市小时拥堵指数
        /// </summary>
        /// <param name="hour">时间</param>
        /// <returns>拥堵指数</returns>
        private double GetHourCongestionData(DateTime hour)
        {
            Dictionary<int, List<SectionFlow>> sectionTypeHourFlows = Enum.GetValues(typeof(SectionType)).Cast<SectionType>().ToDictionary(s => (int)s, s => new List<SectionFlow>());
            foreach (var section in _memoryCache.GetSections())
            {
                SectionFlow flow = _distributedCache.GetSectionHourFlow(section.SectionId, hour);
                if (flow != null)
                {
                    sectionTypeHourFlows[flow.SectionType].Add(flow);
                }
            }

            double totalHourVkts = sectionTypeHourFlows.Sum(p => p.Value.Sum(f => f.Vkt));
            //本小时拥堵指数
            if (totalHourVkts > 0)
            {
                double totalTravelTimeProportion = sectionTypeHourFlows
                    .Sum(p => p.Value.Sum(f => f.Count) == 0
                        ? 0
                        : p.Value.Sum(f => f.TravelTimeProportion) / p.Value.Sum(f => f.Count) * (p.Value.Sum(f => f.Vkt) / totalHourVkts));
                if (totalTravelTimeProportion < 1)
                {
                    totalTravelTimeProportion = 1;
                }

                double congestionData = (totalTravelTimeProportion - 1) / 0.3 * 2;

                if (totalTravelTimeProportion < 1.3)
                {
                    congestionData += 0;
                }
                else if (totalTravelTimeProportion >= 1.3 && totalTravelTimeProportion < 1.6)
                {
                    congestionData += 2;
                }
                else if (totalTravelTimeProportion >= 1.6 && totalTravelTimeProportion < 1.9)
                {
                    congestionData += 4;
                }
                else if (totalTravelTimeProportion >= 1.9 && totalTravelTimeProportion < 2.2)
                {
                    congestionData += 6;
                }
                else if (totalTravelTimeProportion >= 2.2 && totalTravelTimeProportion < 2.5)
                {
                    congestionData += 8;
                }
                else
                {
                    congestionData = 10;
                }

                return congestionData;

            }
            else
            {
                return 0;
            }
        }
    }
}
