using System;
using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using MomobamiKirari.Cache;
using MomobamiKirari.Models;

namespace MomobamiKirari.Managers
{
    /// <summary>
    /// 通道流量数据库操作
    /// </summary>
    public class ChannelFlowsManager
    {
        /// <summary>
        /// 车道流量数据库
        /// </summary>
        private readonly LaneFlowManager _flowManager;

        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// redis缓存
        /// </summary>
        private readonly IDistributedCache _distributedCache;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="flowManager">数据库查询实例</param>
        /// <param name="memoryCache">缓存</param>
        /// <param name="distributedCache">缓存</param>
        public ChannelFlowsManager(LaneFlowManager flowManager, IMemoryCache memoryCache, IDistributedCache distributedCache)
        {
            _flowManager = flowManager;
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
        }

        /// <summary>
        /// 查询通道当天流量状态
        /// </summary>
        /// <param name="channelId">通道编号</param>
        /// <returns>通道流量状态</returns>
        public ChannelDayFlow QueryChannelDayStatus(string channelId)
        {
            channelId = Uri.UnescapeDataString(channelId);

            List<Lane> lanes = _memoryCache.GetLanes()
                .Where(l => l.ChannelId == channelId)
                .OrderBy(l => l.LaneIndex)
                .ToList();
            HashSet<string> dataIds = lanes.Select(l => l.DataId).ToHashSet();

            ChannelDayFlow dayFlow = new ChannelDayFlow
            {
                ChannelId = channelId,
                TodayDayLanes = new List<LaneFlowItem>(),
                YesterdayDayLanes = new List<LaneFlowItem>(),
                LastMonthDayLanes = new List<LaneFlowItem>(),
                LastYearDayLanes = new List<LaneFlowItem>(),
                TodayDayCharts = new List<List<TrafficChart<DateTime, int, LaneFlow>>>(),
                YesterdayDayCharts = new List<List<TrafficChart<DateTime, int, LaneFlow>>>(),
                LastMonthDayCharts = new List<List<TrafficChart<DateTime, int, LaneFlow>>>(),
                LastYearDayCharts = new List<List<TrafficChart<DateTime, int, LaneFlow>>>()
            };

            DateTime now = DateTime.Now;
            DateTime today = now.Date;

            ChannelDayFlow flowCache = _memoryCache.GetChannelDayFlow(channelId, today);
            if (flowCache == null)
            {
                //上月和去年天
                var totalDayCharts = _flowManager.QueryCharts(dataIds, DateTimeLevel.Hour
                    , new[]
                    {
                        today.AddMonths(-1),
                        today.AddYears(-1)
                    }
                    , new[]
                    {
                        today.AddMonths(-1).AddDays(1).AddMinutes(-1),
                        today.AddYears(-1).AddDays(1).AddMinutes(-1)
                    }
                    , today);
                dayFlow.LastMonthDayCharts.Add(totalDayCharts[0]);
                dayFlow.LastYearDayCharts.Add(totalDayCharts[1]);

                var totalLastMonthDayList = _flowManager.QueryList(dataIds, DateTimeLevel.Day, today.AddMonths(-1), today.AddMonths(-1).AddDays(1).AddMinutes(-1));
                dayFlow.LastMonthDayLanes.Add(
                   totalLastMonthDayList.Count == 0
                        ? new LaneFlowItem
                        {
                            LaneName = "全部"
                        }
                        : new LaneFlowItem
                        {
                            LaneName = "全部",
                            Total = totalLastMonthDayList[0].Total,
                            Vehicle = totalLastMonthDayList[0].Vehicle,
                            Bike = totalLastMonthDayList[0].Bike,
                            Person = totalLastMonthDayList[0].Persons,
                            Occupancy = totalLastMonthDayList[0].Occupancy / totalLastMonthDayList[0].Count,
                            TimeOccupancy = totalLastMonthDayList[0].TimeOccupancy / totalLastMonthDayList[0].Count
                        });

                var totalLastYearDayList = _flowManager.QueryList(dataIds, DateTimeLevel.Day, today.AddYears(-1), today.AddYears(-1).AddDays(1).AddMinutes(-1));
                dayFlow.LastYearDayLanes.Add(
                    totalLastYearDayList.Count == 0
                        ? new LaneFlowItem
                        {
                            LaneName = "全部"
                        }
                        : new LaneFlowItem
                        {
                            LaneName = "全部",
                            Total = totalLastYearDayList[0].Total,
                            Vehicle = totalLastYearDayList[0].Vehicle,
                            Bike = totalLastYearDayList[0].Bike,
                            Person = totalLastYearDayList[0].Persons,
                            Occupancy = totalLastYearDayList[0].Occupancy / totalLastYearDayList[0].Count,
                            TimeOccupancy = totalLastYearDayList[0].TimeOccupancy / totalLastYearDayList[0].Count
                        });

                foreach (Lane lane in lanes.OrderBy(l => l.LaneIndex))
                {
                    //上月和去年天从数据库查询
                    var dayCharts = _flowManager.QueryCharts(lane.DataId, DateTimeLevel.Hour
                        , new[]
                        {
                        today.AddMonths(-1),
                        today.AddYears(-1)
                        }
                        , new[]
                        {
                        today.AddMonths(-1).AddDays(1).AddMinutes(-1),
                        today.AddYears(-1).AddDays(1).AddMinutes(-1)
                        }
                        , today);

                    dayFlow.LastMonthDayCharts.Add(dayCharts[0]);
                    dayFlow.LastYearDayCharts.Add(dayCharts[1]);

                    var lastMonthDayList = _flowManager.QueryList(lane.DataId, DateTimeLevel.Day, today.AddMonths(-1), today.AddMonths(-1).AddDays(1).AddMinutes(-1));
                    dayFlow.LastMonthDayLanes.Add(
                        lastMonthDayList.Count == 0
                            ? new LaneFlowItem
                            {
                                DataId = lane.DataId,
                                LaneName = lane.LaneName
                            }
                            : new LaneFlowItem
                            {
                                DataId = lane.DataId,
                                LaneName = lane.LaneName,
                                Total = lastMonthDayList[0].Total,
                                Vehicle = lastMonthDayList[0].Vehicle,
                                Bike = lastMonthDayList[0].Bike,
                                Person = lastMonthDayList[0].Persons,
                                Occupancy = lastMonthDayList[0].Occupancy / lastMonthDayList[0].Count,
                                TimeOccupancy = lastMonthDayList[0].TimeOccupancy / lastMonthDayList[0].Count
                            });

                    var lastYearDayList = _flowManager.QueryList(lane.DataId, DateTimeLevel.Day, today.AddYears(-1), today.AddYears(-1).AddDays(1).AddMinutes(-1));
                    dayFlow.LastYearDayLanes.Add(
                        lastYearDayList.Count == 0
                            ? new LaneFlowItem
                            {
                                DataId = lane.DataId,
                                LaneName = lane.LaneName
                            }
                            : new LaneFlowItem
                            {
                                DataId = lane.DataId,
                                LaneName = lane.LaneName,
                                Total = lastYearDayList[0].Total,
                                Vehicle = lastYearDayList[0].Vehicle,
                                Bike = lastYearDayList[0].Bike,
                                Person = lastYearDayList[0].Persons,
                                Occupancy = lastYearDayList[0].Occupancy / lastMonthDayList[0].Count,
                                TimeOccupancy = lastYearDayList[0].TimeOccupancy / lastMonthDayList[0].Count
                            });
                }

                for (int h = 0; h < 24; ++h)
                {
                    for (int i = 0; i < dataIds.Count + 1; ++i)
                    {
                        if (h > dayFlow.LastMonthDayCharts[i].Count - 1 || dayFlow.LastMonthDayCharts[i][h].Axis != today.AddHours(h))
                        {
                            dayFlow.LastMonthDayCharts[i].Insert(h, new TrafficChart<DateTime, int, LaneFlow>
                            {
                                Axis = today.AddHours(h),
                                Value = 0,
                                Remark = today.AddMonths(-1).AddHours(h).ToString("yyyy-MM-dd HH")
                            });
                        }

                        if (h > dayFlow.LastYearDayCharts[i].Count - 1 || dayFlow.LastYearDayCharts[i][h].Axis != today.AddHours(h))
                        {
                            dayFlow.LastYearDayCharts[i].Insert(h, new TrafficChart<DateTime, int, LaneFlow>
                            {
                                Axis = today.AddHours(h),
                                Value = 0,
                                Remark = today.AddYears(-1).AddHours(h).ToString("yyyy-MM-dd HH")
                            });
                        }
                    }
                }

                _memoryCache.SetChannelDayFlow(dayFlow, today);
            }
            else
            {
                dayFlow.LastMonthDayCharts = flowCache.LastMonthDayCharts;
                dayFlow.LastYearDayCharts = flowCache.LastYearDayCharts;
                dayFlow.LastMonthDayLanes = flowCache.LastMonthDayLanes;
                dayFlow.LastYearDayLanes = flowCache.LastYearDayLanes;
            }

            for (int i = 0; i < lanes.Count + 1; ++i)
            {
                dayFlow.TodayDayCharts.Add(new List<TrafficChart<DateTime, int, LaneFlow>>());
                dayFlow.YesterdayDayCharts.Add(new List<TrafficChart<DateTime, int, LaneFlow>>());

                dayFlow.TodayDayLanes.Add(new LaneFlowItem());
                dayFlow.YesterdayDayLanes.Add(new LaneFlowItem());
            }

            dayFlow.TodayDayLanes[0].LaneName = "全部";
            dayFlow.YesterdayDayLanes[0].LaneName = "全部";

            for (int h = 0; h <= now.Hour; ++h)
            {
                TrafficChart<DateTime, int, LaneFlow> totalChart = new TrafficChart<DateTime, int, LaneFlow>
                {
                    Axis = today.AddHours(h),
                    Remark = today.AddHours(h).ToString("yyyy-MM-dd HH")
                };

                dayFlow.TodayDayCharts[0].Add(totalChart);

                int index = 1;
                foreach (Lane lane in lanes)
                {
                    LaneFlow flow = _distributedCache.GetLaneHourFlow(lane.DataId, today.AddHours(h));
                    if (flow == null)
                    {
                        dayFlow.TodayDayCharts[index].Add(new TrafficChart<DateTime, int, LaneFlow>
                        {
                            Axis = today.AddHours(h),
                            Remark = today.AddHours(h).ToString("yyyy-MM-dd HH"),
                            Value = 0
                        });
                        totalChart.Value += 0;

                        dayFlow.TodayDayLanes[index].DataId = lane.DataId;
                        dayFlow.TodayDayLanes[index].LaneName = lane.LaneName;
                        dayFlow.TodayDayLanes[index].Vehicle += 0;
                        dayFlow.TodayDayLanes[index].Bike += 0;
                        dayFlow.TodayDayLanes[index].Person += 0;
                        dayFlow.TodayDayLanes[index].Total += 0;
                        dayFlow.TodayDayLanes[index].Occupancy += 0;
                        dayFlow.TodayDayLanes[index].TimeOccupancy += 0;
                        dayFlow.TodayDayLanes[index].Count += 0;
                        dayFlow.TodayDayLanes[0].Vehicle += 0;
                        dayFlow.TodayDayLanes[0].Bike += 0;
                        dayFlow.TodayDayLanes[0].Person += 0;
                        dayFlow.TodayDayLanes[0].Total += 0;
                        dayFlow.TodayDayLanes[0].Occupancy += 0;
                        dayFlow.TodayDayLanes[0].TimeOccupancy += 0;
                        dayFlow.TodayDayLanes[0].Count += 0;
                    }
                    else
                    {
                        dayFlow.TodayDayCharts[index].Add(new TrafficChart<DateTime, int, LaneFlow>
                        {
                            Axis = today.AddHours(h),
                            Remark = today.AddHours(h).ToString("yyyy-MM-dd HH"),
                            Value = flow.Total
                        });
                        totalChart.Value += flow.Total;

                        dayFlow.TodayDayLanes[index].DataId = lane.DataId;
                        dayFlow.TodayDayLanes[index].LaneName = lane.LaneName;
                        dayFlow.TodayDayLanes[index].Vehicle += flow.Vehicle;
                        dayFlow.TodayDayLanes[index].Bike += flow.Bike;
                        dayFlow.TodayDayLanes[index].Person += flow.Persons;
                        dayFlow.TodayDayLanes[index].Total += flow.Total;
                        dayFlow.TodayDayLanes[index].Occupancy += flow.Occupancy;
                        dayFlow.TodayDayLanes[index].TimeOccupancy += flow.TimeOccupancy;
                        dayFlow.TodayDayLanes[index].Count += flow.Count;
                        dayFlow.TodayDayLanes[0].Vehicle += flow.Vehicle;
                        dayFlow.TodayDayLanes[0].Bike += flow.Bike;
                        dayFlow.TodayDayLanes[0].Person += flow.Persons;
                        dayFlow.TodayDayLanes[0].Total += flow.Total;
                        dayFlow.TodayDayLanes[0].Occupancy += flow.Occupancy;
                        dayFlow.TodayDayLanes[0].TimeOccupancy += flow.TimeOccupancy;
                        dayFlow.TodayDayLanes[0].Count += flow.Count;
                    }
                    index += 1;
                }
            }

            for (int h = 0; h < 24; ++h)
            {
                TrafficChart<DateTime, int, LaneFlow> totalChart = new TrafficChart<DateTime, int, LaneFlow>
                {
                    Axis = today.AddHours(h),
                    Remark = today.AddDays(-1).AddHours(h).ToString("yyyy-MM-dd HH")
                };

                dayFlow.YesterdayDayCharts[0].Add(totalChart);

                int index = 1;
                foreach (Lane lane in lanes)
                {
                    LaneFlow laneFlow = _distributedCache.GetLaneHourFlow(lane.DataId, today.AddDays(-1).AddHours(h));
                    if (laneFlow == null)
                    {
                        dayFlow.YesterdayDayCharts[index].Add(new TrafficChart<DateTime, int, LaneFlow>
                        {
                            Axis = today.AddHours(h),
                            Remark = today.AddDays(-1).AddHours(h).ToString("yyyy-MM-dd HH"),
                            Value = 0
                        });
                        totalChart.Value += 0;

                        dayFlow.YesterdayDayLanes[index].DataId = lane.DataId;
                        dayFlow.YesterdayDayLanes[index].LaneName = lane.LaneName;
                        dayFlow.YesterdayDayLanes[index].Vehicle += 0;
                        dayFlow.YesterdayDayLanes[index].Bike += 0;
                        dayFlow.YesterdayDayLanes[index].Person += 0;
                        dayFlow.YesterdayDayLanes[index].Total += 0;
                        dayFlow.YesterdayDayLanes[index].Occupancy += 0;
                        dayFlow.YesterdayDayLanes[index].TimeOccupancy += 0;
                        dayFlow.YesterdayDayLanes[index].Count += 0;
                        dayFlow.YesterdayDayLanes[0].Vehicle += 0;
                        dayFlow.YesterdayDayLanes[0].Bike += 0;
                        dayFlow.YesterdayDayLanes[0].Person += 0;
                        dayFlow.YesterdayDayLanes[0].Total += 0;
                        dayFlow.YesterdayDayLanes[0].Occupancy += 0;
                        dayFlow.YesterdayDayLanes[0].TimeOccupancy += 0;
                        dayFlow.YesterdayDayLanes[0].Count += 0;
                    }
                    else
                    {
                        dayFlow.YesterdayDayCharts[index].Add(new TrafficChart<DateTime, int, LaneFlow>
                        {
                            Axis = today.AddHours(h),
                            Remark = today.AddDays(-1).AddHours(h).ToString("yyyy-MM-dd HH"),
                            Value = laneFlow.Total
                        });
                        totalChart.Value += laneFlow.Total;

                        dayFlow.YesterdayDayLanes[index].DataId = lane.DataId;
                        dayFlow.YesterdayDayLanes[index].LaneName = lane.LaneName;
                        dayFlow.YesterdayDayLanes[index].Vehicle += laneFlow.Vehicle;
                        dayFlow.YesterdayDayLanes[index].Bike += laneFlow.Bike;
                        dayFlow.YesterdayDayLanes[index].Person += laneFlow.Persons;
                        dayFlow.YesterdayDayLanes[index].Total += laneFlow.Total;
                        dayFlow.YesterdayDayLanes[index].Occupancy += laneFlow.Occupancy;
                        dayFlow.YesterdayDayLanes[index].TimeOccupancy += laneFlow.TimeOccupancy;
                        dayFlow.YesterdayDayLanes[index].Count += laneFlow.Count;
                        dayFlow.YesterdayDayLanes[0].Vehicle += laneFlow.Vehicle;
                        dayFlow.YesterdayDayLanes[0].Bike += laneFlow.Bike;
                        dayFlow.YesterdayDayLanes[0].Person += laneFlow.Persons;
                        dayFlow.YesterdayDayLanes[0].Total += laneFlow.Total;
                        dayFlow.YesterdayDayLanes[0].Occupancy += laneFlow.Occupancy;
                        dayFlow.YesterdayDayLanes[0].TimeOccupancy += laneFlow.TimeOccupancy;
                        dayFlow.YesterdayDayLanes[0].Count += laneFlow.Count;
                    }
                    index += 1;
                }
            }

            for (int i = 0; i < lanes.Count + 1; ++i)
            {
                dayFlow.TodayDayLanes[i].Occupancy = dayFlow.TodayDayLanes[i].Count == 0
                    ? 0
                    : dayFlow.TodayDayLanes[i].Occupancy / dayFlow.TodayDayLanes[i].Count;
                dayFlow.YesterdayDayLanes[i].Occupancy = dayFlow.YesterdayDayLanes[i].Count == 0
                    ? 0
                    : dayFlow.YesterdayDayLanes[i].Occupancy / dayFlow.YesterdayDayLanes[i].Count;

                dayFlow.TodayDayLanes[i].TimeOccupancy = dayFlow.TodayDayLanes[i].Count == 0
                    ? 0
                    : dayFlow.TodayDayLanes[i].TimeOccupancy / dayFlow.TodayDayLanes[i].Count;
                dayFlow.YesterdayDayLanes[i].TimeOccupancy = dayFlow.YesterdayDayLanes[i].Count == 0
                    ? 0
                    : dayFlow.YesterdayDayLanes[i].TimeOccupancy / dayFlow.YesterdayDayLanes[i].Count;

            }

            return dayFlow;
        }

        /// <summary>
        /// 查询通道小时流量状态
        /// </summary>
        /// <param name="channelId">通道编号</param>
        /// <returns>通道流量状态</returns>
        public ChannelHourFlow QueryChannelHourStatus([FromRoute]string channelId)
        {
            channelId = Uri.UnescapeDataString(channelId);
            List<Lane> lanes = _memoryCache.GetLanes()
                .Where(l => l.ChannelId == channelId)
                .OrderBy(l => l.LaneIndex)
                .ToList();

            ChannelHourFlow hourFlow = new ChannelHourFlow
            {
                ChannelId = channelId,
                TodayHourLanes = new List<LaneFlowItem>(),
                TodayHourCharts = new List<List<TrafficChart<DateTime, int, LaneFlow>>>()
            };

            DateTime now = DateTime.Now;

            for (int i = 0; i < lanes.Count + 1; ++i)
            {
                hourFlow.TodayHourCharts.Add(new List<TrafficChart<DateTime, int, LaneFlow>>());
                hourFlow.TodayHourLanes.Add(new LaneFlowItem());
            }

            hourFlow.TodayHourLanes[0].LaneName = "全部";
            for (int m = 0; m < 60; ++m)
            {
                TrafficChart<DateTime, int, LaneFlow> totalChart = new TrafficChart<DateTime, int, LaneFlow>
                {
                    Axis = now.AddHours(-1).AddMinutes(m),
                    Remark = now.AddHours(-1).AddMinutes(m).ToString("yyyy-MM-dd HH:mm")
                };

                hourFlow.TodayHourCharts[0].Add(totalChart);

                int index = 1;
                foreach (Lane lane in lanes)
                {
                    LaneFlow laneFlow = _distributedCache.GetLaneMinuteFlow(lane.DataId, now.AddHours(-1).AddMinutes(m));
                    if (laneFlow == null)
                    {
                        hourFlow.TodayHourCharts[index].Add(new TrafficChart<DateTime, int, LaneFlow>
                        {
                            Axis = now.AddHours(-1).AddMinutes(m),
                            Remark = now.AddHours(-1).AddMinutes(m).ToString("yyyy-MM-dd HH:mm"),
                            Value = 0
                        });
                        totalChart.Value += 0;

                        hourFlow.TodayHourLanes[index].DataId = lane.DataId;
                        hourFlow.TodayHourLanes[index].LaneName = lane.LaneName;
                        hourFlow.TodayHourLanes[index].Vehicle += 0;
                        hourFlow.TodayHourLanes[index].Bike += 0;
                        hourFlow.TodayHourLanes[index].Person += 0;
                        hourFlow.TodayHourLanes[index].Total += 0;
                        hourFlow.TodayHourLanes[index].Occupancy += 0;
                        hourFlow.TodayHourLanes[index].TimeOccupancy += 0;
                        hourFlow.TodayHourLanes[index].Count += 0;
                        hourFlow.TodayHourLanes[0].Vehicle += 0;
                        hourFlow.TodayHourLanes[0].Bike += 0;
                        hourFlow.TodayHourLanes[0].Person += 0;
                        hourFlow.TodayHourLanes[0].Total += 0;
                        hourFlow.TodayHourLanes[0].Occupancy += 0;
                        hourFlow.TodayHourLanes[0].TimeOccupancy += 0;
                        hourFlow.TodayHourLanes[0].Count += 0;
                    }
                    else
                    {
                        hourFlow.TodayHourCharts[index].Add(new TrafficChart<DateTime, int, LaneFlow>
                        {
                            Axis = now.AddHours(-1).AddMinutes(m),
                            Remark = now.AddHours(-1).AddMinutes(m).ToString("yyyy-MM-dd HH:mm"),
                            Value = laneFlow.Total
                        });
                        totalChart.Value += laneFlow.Total;

                        hourFlow.TodayHourLanes[index].DataId = lane.DataId;
                        hourFlow.TodayHourLanes[index].LaneName = lane.LaneName;
                        hourFlow.TodayHourLanes[index].Vehicle += laneFlow.Vehicle;
                        hourFlow.TodayHourLanes[index].Bike += laneFlow.Bike;
                        hourFlow.TodayHourLanes[index].Person += laneFlow.Persons;
                        hourFlow.TodayHourLanes[index].Total += laneFlow.Total;
                        hourFlow.TodayHourLanes[index].Occupancy += laneFlow.Occupancy;
                        hourFlow.TodayHourLanes[index].TimeOccupancy += laneFlow.TimeOccupancy;
                        hourFlow.TodayHourLanes[index].Count += laneFlow.Count;
                        hourFlow.TodayHourLanes[0].Vehicle += laneFlow.Vehicle;
                        hourFlow.TodayHourLanes[0].Bike += laneFlow.Bike;
                        hourFlow.TodayHourLanes[0].Person += laneFlow.Persons;
                        hourFlow.TodayHourLanes[0].Total += laneFlow.Total;
                        hourFlow.TodayHourLanes[0].Occupancy += laneFlow.Occupancy;
                        hourFlow.TodayHourLanes[0].TimeOccupancy += laneFlow.TimeOccupancy;
                        hourFlow.TodayHourLanes[0].Count += 1;
                    }
                    index += 1;
                }
            }

            for (int i = 0; i < lanes.Count + 1; ++i)
            {
                hourFlow.TodayHourLanes[i].Occupancy = hourFlow.TodayHourLanes[i].Count == 0
                    ? 0
                    : hourFlow.TodayHourLanes[i].Occupancy / hourFlow.TodayHourLanes[i].Count;

                hourFlow.TodayHourLanes[i].TimeOccupancy = hourFlow.TodayHourLanes[i].Count == 0
                    ? 0
                    : hourFlow.TodayHourLanes[i].TimeOccupancy / hourFlow.TodayHourLanes[i].Count;
            }

            return hourFlow;
        }

        /// <summary>
        /// 查询通道分钟流量状态
        /// </summary>
        /// <param name="channelId">通道编号</param>
        /// <returns>通道流量状态</returns>
        public ChannelMinuteFlow QueryChannelMuniteStatus(string channelId)
        {
            channelId = Uri.UnescapeDataString(channelId);
            ChannelMinuteFlow channelMinuteFlow = new ChannelMinuteFlow
            {
                LanesFlow = new List<LaneFlow>()
            };
            FlowChannel channel = _memoryCache.GetChannel(channelId);
            if (channel != null)
            {
                if (channel.SectionId.HasValue)
                {
                    SectionFlow sectionFlow = _memoryCache.GetSectionLastFlow(channel.SectionId.Value);
                    if (sectionFlow == null)
                    {
                        channelMinuteFlow.SectionFlow = new SectionFlow
                        {
                            SectionId = channel.SectionId.Value,
                            SectionName = channel.RoadSection.SectionName
                        };
                    }
                    else
                    {
                        _memoryCache.FillSectionFlowCache(sectionFlow);
                        channelMinuteFlow.SectionFlow = sectionFlow;
                    }
                }

                foreach (Lane lane in channel.Lanes)
                {
                    LaneFlow f = _memoryCache.GetLaneLastFlow(lane.DataId);
                    if (f == null)
                    {
                        channelMinuteFlow.LanesFlow.Add(new LaneFlow
                        {
                            DataId = lane.DataId,
                            LaneName = lane.LaneName
                        });
                    }
                    else
                    {
                        _memoryCache.FillLaneFlow(f);
                        channelMinuteFlow.LanesFlow.Add(f);
                    }
                }
            }
            return channelMinuteFlow;
        }
    }
}
