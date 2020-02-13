using System;
using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MomobamiKirari.Controllers;
using MomobamiKirari.Managers;

namespace IntegrationTest.Flow
{
    [TestClass]
    public class ChannelFlowController_Test
    {
        [TestMethod]
        public void QueryChannelDayStatus()
        {
            DateTime today = DateTime.Today;
            List<TrafficDevice> devices = DeviceDbSimulator.CreateFlowDevice(TestInit.ServiceProvider, 1, 1, 2, true);
            List<DateTime> dates = new List<DateTime>
            {
                today.AddYears(-1),
                today.AddMonths(-1),
                today.AddDays(-1),
                today
            };
            
            TestInit.RefreshFlowCache(devices);

            FlowDbSimulator.CreateData(TestInit.ServiceProvider, devices, DataCreateMode.Fixed, dates, true);

            ChannelFlowsController service = new ChannelFlowsController(TestInit.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<LaneFlowManager_Alone>()
                , TestInit.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<IMemoryCache>()
                , TestInit.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<IDistributedCache>());
            LaneFlowManager_Alone manager = TestInit.ServiceProvider.GetRequiredService<LaneFlowManager_Alone>();

            foreach (TrafficDevice device in devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    int laneCount = relation.Channel.Lanes.Count;

                    var status = service.QueryChannelDayStatus(relation.ChannelId);

                    //今日列表
                    Assert.AreEqual(laneCount + 1, status.TodayDayLanes.Count);
                    int sum = 0;
                    for (int i = 0; i < relation.Channel.Lanes.Count; ++i)
                    {
                        var v = manager.QueryList(relation.Channel.Lanes[i].DataId, DateTimeLevel.Minute, DateTime.Today, DateTime.Now);
                        int total = v.Sum(f => f.Total);
                        sum += total;
                        Assert.AreEqual(total, status.TodayDayLanes[i + 1].Total);
                        Assert.AreEqual(30, status.TodayDayLanes[i + 1].Occupancy);
                        Assert.AreEqual(40, status.TodayDayLanes[i + 1].TimeOccupancy);
                    }

                    Assert.AreEqual(sum, status.TodayDayLanes[0].Total);
                    Assert.AreEqual(30, status.TodayDayLanes[0].Occupancy);
                    Assert.AreEqual(40, status.TodayDayLanes[0].TimeOccupancy);

                    //昨天列表
                    Assert.AreEqual(laneCount + 1, status.YesterdayDayLanes.Count);
                    Assert.AreEqual(11520 * laneCount, status.YesterdayDayLanes[0].Total);
                    for (int i = 1; i < status.YesterdayDayLanes.Count; ++i)
                    {
                        Assert.AreEqual(11520, status.YesterdayDayLanes[i].Total);
                        Assert.AreEqual(30, status.YesterdayDayLanes[i].Occupancy);
                        Assert.AreEqual(40, status.YesterdayDayLanes[i].TimeOccupancy);

                    }

                    //上月列表
                    Assert.AreEqual(laneCount + 1, status.LastMonthDayLanes.Count);
                    Assert.AreEqual(11520 * laneCount, status.LastMonthDayLanes[0].Total);
                    for (int i = 1; i < status.LastMonthDayLanes.Count; ++i)
                    {
                        Assert.AreEqual(11520, status.LastMonthDayLanes[i].Total);
                        Assert.AreEqual(30, status.LastMonthDayLanes[i].Occupancy);
                        Assert.AreEqual(40, status.LastMonthDayLanes[i].TimeOccupancy);
                    }

                    //去年列表
                    Assert.AreEqual(laneCount + 1, status.LastYearDayLanes.Count);
                    Assert.AreEqual(11520 * laneCount, status.LastYearDayLanes[0].Total);
                    for (int i = 1; i < status.LastYearDayLanes.Count; ++i)
                    {
                        Assert.AreEqual(11520, status.LastYearDayLanes[i].Total);
                        Assert.AreEqual(30, status.LastYearDayLanes[i].Occupancy);
                        Assert.AreEqual(40, status.LastYearDayLanes[i].TimeOccupancy);
                    }

                    //今日图表
                    Assert.AreEqual(laneCount + 1, status.TodayDayCharts.Count);
                    Dictionary<int, int> sumValue = new Dictionary<int, int>();
                    for (int i = 0; i < relation.Channel.Lanes.Count; ++i)
                    {
                        for (int h = 0; h < DateTime.Now.Hour + 1; ++h)
                        {
                            var v = manager.QueryList(relation.Channel.Lanes[i].DataId, DateTimeLevel.Hour, DateTime.Today.AddHours(h), DateTime.Today.AddHours(h));
                            int value = v.Sum(f => f.Total);
                            //今日每小时图表
                            Assert.AreEqual(value, status.TodayDayCharts[i + 1][h].Value);
                            if (sumValue.ContainsKey(h))
                            {
                                sumValue[h] += value;
                            }
                            else
                            {
                                sumValue.Add(h, value);
                            }
                        }
                    }
                    //今日每小时图表总和
                    for (int h = 0; h < DateTime.Now.Hour + 1; ++h)
                    {
                        Assert.AreEqual(sumValue[h], status.TodayDayCharts[0][h].Value);
                    }

                    //昨天图表
                    Assert.AreEqual(laneCount + 1, status.YesterdayDayCharts.Count);
                    //昨天总和图表横轴数量
                    Assert.AreEqual(24, status.YesterdayDayCharts[0].Count);
                    for (int h = 0; h < 24; ++h)
                    {
                        //昨天总和每个横轴的value和remart
                        Assert.AreEqual(480 * laneCount, status.YesterdayDayCharts[0][h].Value);
                        Assert.AreEqual(today.AddDays(-1).AddHours(h).ToString("yyyy-MM-dd HH"), status.YesterdayDayCharts[0][h].Remark);
                    }

                    for (int i = 1; i < status.YesterdayDayCharts.Count; ++i)
                    {
                        //各个车道图表横轴数量
                        Assert.AreEqual(24, status.YesterdayDayCharts[i].Count);
                        for (int h = 0; h < 24; ++h)
                        {
                            //每个横轴的value和remart
                            Assert.AreEqual(480, status.YesterdayDayCharts[i][h].Value);
                            Assert.AreEqual(today.AddDays(-1).AddHours(h).ToString("yyyy-MM-dd HH"), status.YesterdayDayCharts[i][h].Remark);
                        }
                    }

                    //上月图表
                    Assert.AreEqual(laneCount + 1, status.LastMonthDayCharts.Count);
                    Assert.AreEqual(24, status.LastMonthDayCharts[0].Count);
                    for (int h = 0; h < 24; ++h)
                    {
                        Assert.AreEqual(480 * laneCount, status.LastMonthDayCharts[0][h].Value);
                        Assert.AreEqual(today.AddMonths(-1).AddHours(h).ToString("yyyy-MM-dd HH"), status.LastMonthDayCharts[0][h].Remark);
                    }
                    for (int i = 1; i < status.LastMonthDayCharts.Count; ++i)
                    {
                        Assert.AreEqual(24, status.LastMonthDayCharts[i].Count);
                        for (int h = 0; h < 24; ++h)
                        {
                            Assert.AreEqual(480, status.LastMonthDayCharts[i][h].Value);
                            Assert.AreEqual(today.AddMonths(-1).AddHours(h).ToString("yyyy-MM-dd HH"), status.LastMonthDayCharts[i][h].Remark);
                        }
                    }

                    //去年图表
                    Assert.AreEqual(laneCount + 1, status.LastYearDayCharts.Count);
                    Assert.AreEqual(24, status.LastYearDayCharts[0].Count);
                    for (int h = 0; h < 24; ++h)
                    {
                        Assert.AreEqual(480 * laneCount, status.LastYearDayCharts[0][h].Value);
                        Assert.AreEqual(today.AddYears(-1).AddHours(h).ToString("yyyy-MM-dd HH"), status.LastYearDayCharts[0][h].Remark);
                    }
                    for (int i = 1; i < status.LastYearDayCharts.Count; ++i)
                    {
                        Assert.AreEqual(24, status.LastYearDayCharts[i].Count);
                        for (int h = 0; h < 24; ++h)
                        {
                            Assert.AreEqual(480, status.LastYearDayCharts[i][h].Value);
                            Assert.AreEqual(today.AddYears(-1).AddHours(h).ToString("yyyy-MM-dd HH"), status.LastYearDayCharts[i][h].Remark);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void QueryChannelMinuteStatus()
        {
            List<TrafficDevice> devices = DeviceDbSimulator.CreateFlowDevice(TestInit.ServiceProvider, 1, 1, 2,true);

            TestInit.RefreshFlowCache(devices);

            FlowDbSimulator.CreateData(TestInit.ServiceProvider, devices, DataCreateMode.Fixed, DateTime.Today, true);

            ChannelFlowsController service = new ChannelFlowsController(TestInit.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<LaneFlowManager_Alone>()
                , TestInit.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<IMemoryCache>()
                , TestInit.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<IDistributedCache>());
            LaneFlowManager_Alone manager = TestInit.ServiceProvider.GetRequiredService<LaneFlowManager_Alone>();

            foreach (TrafficDevice device in devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    int laneCount = relation.Channel.Lanes.Count;

                    var hourStatus = service.QueryChannelHourStatus(relation.ChannelId);

                    //本小时列表

                    Assert.AreEqual(30, hourStatus.TodayHourLanes[0].Occupancy);

                    Assert.AreEqual(40, hourStatus.TodayHourLanes[0].TimeOccupancy);

                    for (int i = 1; i < hourStatus.TodayHourLanes.Count; ++i)
                    {

                        Assert.AreEqual(30, hourStatus.TodayHourLanes[i].Occupancy);

                        Assert.AreEqual(40, hourStatus.TodayHourLanes[i].TimeOccupancy);

                    }

                    //本小时图表
                    for (int i = 0; i < 55; ++i)
                    {
                        Assert.AreEqual(8 * laneCount, hourStatus.TodayHourCharts[0][i].Value);

                        for (int j = 1; j < hourStatus.TodayHourCharts.Count; ++j)
                        {
                            Assert.AreEqual(8, hourStatus.TodayHourCharts[j][i].Value);
                        }
                    }

                }
            }
        }
    }
}
