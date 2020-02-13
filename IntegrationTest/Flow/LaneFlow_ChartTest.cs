using System;
using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MomobamiKirari.Controllers;
using MomobamiKirari.Managers;
using MomobamiKirari.Models;
using ItsukiSumeragi.Codes.Flow;

namespace IntegrationTest.Flow
{
    [TestClass]
    public class LaneFlow_ChartTest
    {
        private static List<TrafficDevice> _devices;
        private static int _days;
        private static DateTime _startDate;
        private static List<DateTime> _startMonths;
        private static List<DateTime> _startDates;
        private static int _months;

        [ClassInitialize]
        public static void CreateData(TestContext testContext)
        {
            _startDate = new DateTime(2019, 1, 1);
            _days = 1;
            List<DateTime> dates = new List<DateTime>();
            for (DateTime date = _startDate; date <= _startDate.AddDays(_days - 1); date = date.AddDays(1))
            {
                dates.Add(date);
            }

            _startDates = new List<DateTime>();
            for (int d = 0; d < _days; ++d)
            {
                _startDates.Add(_startDate.AddDays(d));
            }

            _months = 1;
            _startMonths = new List<DateTime>();
            for (int m = 0; m < _months; ++m)
            {
                _startMonths.Add(TimePointConvert.CurrentTimePoint(DateTimeLevel.Month, _startDate).AddMonths(m));
            }

            _devices = DeviceDbSimulator.CreateFlowDevice(TestInit.ServiceProvider, 1, 1, 2, true);
            TestInit.RefreshFlowCache(_devices);

            FlowDbSimulator.CreateData(TestInit.ServiceProvider, _devices, DataCreateMode.Fixed, dates, true);
        }

        [TestMethod]
        public void QueryFlowCharts()
        {
            LaneFlowManager_Alone service = TestInit.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<LaneFlowManager_Alone>();
            LaneFlowsController flowsController = new LaneFlowsController(service);

            //按车道查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (TrafficLane lane in relation.Channel.Lanes)
                    {
                        List<List<TrafficChart<DateTime, int,LaneFlow>>> minuteCharts = service.QueryCharts(new HashSet<string>{ lane.DataId }, DateTimeLevel.Minute, _startDates.ToArray() , _startDates.Select(t=>t.AddDays(1).AddMinutes(-1)).ToArray());
                        //检查时间段数量
                        Assert.AreEqual(_days, minuteCharts.Count);
                        for(int i=0;i<minuteCharts.Count;++i)
                        {
                            //检查每个时间段的数据数量
                            Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                            for (int m = 0; m < 24*60; ++m)
                            {
                                //检查每个数据的各项值
                                Assert.AreEqual(8, minuteCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int,LaneFlow>>> fiveCharts = service.QueryCharts(new HashSet<string> { lane.DataId }, DateTimeLevel.FiveMinutes, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                        Assert.AreEqual(_days, fiveCharts.Count);
                        for (int i = 0; i < fiveCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 5; ++m)
                            {
                                Assert.AreEqual(8*5, fiveCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int,LaneFlow>>> fifteenCharts = service.QueryCharts(new HashSet<string> { lane.DataId }, DateTimeLevel.FifteenMinutes, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                        Assert.AreEqual(_days, fifteenCharts.Count);
                        for (int i = 0; i < fifteenCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 15; ++m)
                            {
                                Assert.AreEqual(8 * 15, fifteenCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int,LaneFlow>>> hourCharts = service.QueryCharts(new HashSet<string> { lane.DataId }, DateTimeLevel.Hour, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                        Assert.AreEqual(_days, hourCharts.Count);
                        for (int i = 0; i < hourCharts.Count; ++i)
                        {
                            Assert.AreEqual(24, hourCharts[i].Count);
                            for (int h = 0; h < 24; ++h)
                            {
                                Assert.AreEqual(8 * 60, hourCharts[i][h].Value);
                                Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                                Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int,LaneFlow>>> dayCharts = service.QueryCharts(new HashSet<string> { lane.DataId }, DateTimeLevel.Day, _startDates.ToArray(), _startDates.ToArray());
                        Assert.AreEqual(_days, dayCharts.Count);
                        for (int i = 0; i < dayCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, dayCharts[i].Count);
                            Assert.AreEqual(8 * 60*24, dayCharts[i][0].Value);
                            Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                            Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                        }

                        List<List<TrafficChart<DateTime, int,LaneFlow>>> monthCharts = service.QueryCharts(new HashSet<string> { lane.DataId }, DateTimeLevel.Month, _startMonths.ToArray(), _startMonths.ToArray());
                        Assert.AreEqual(_months, monthCharts.Count);
                        for (int i = 0; i < monthCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, monthCharts[i].Count);
                            int total = service.QueryList(lane.DataId, DateTimeLevel.Month, _startMonths[i], _startMonths[i])
                                .Sum(f => f.Total);
                            Assert.AreEqual(total, monthCharts[i][0].Value);
                            Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                            Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                        }
                    }
                }
            }

            //按路口查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    int laneCount = relation.Channel.Lanes.Count;
                    List<List<TrafficChart<DateTime, int,LaneFlow>>> minuteCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, DateTimeLevel.Minute, null, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                    //检查时间段数量
                    Assert.AreEqual(_days, minuteCharts.Count);
                    for (int i = 0; i < minuteCharts.Count; ++i)
                    {
                        //检查每个时间段的数据数量
                        Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                        for (int m = 0; m < 24 * 60; ++m)
                        {
                            //检查每个数据的各项值
                            Assert.AreEqual(8 * laneCount, minuteCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int,LaneFlow>>> fiveCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, DateTimeLevel.FiveMinutes,null, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                    Assert.AreEqual(_days, fiveCharts.Count);
                    for (int i = 0; i < fiveCharts.Count; ++i)
                    {
                        Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                        for (int m = 0; m < 24 * 60 / 5; ++m)
                        {
                            Assert.AreEqual(8 * 5 * laneCount, fiveCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int,LaneFlow>>> fifteenCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, DateTimeLevel.FifteenMinutes, null, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                    Assert.AreEqual(_days, fifteenCharts.Count);
                    for (int i = 0; i < fifteenCharts.Count; ++i)
                    {
                        Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                        for (int m = 0; m < 24 * 60 / 15; ++m)
                        {
                            Assert.AreEqual(8 * 15 * laneCount, fifteenCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int,LaneFlow>>> hourCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, DateTimeLevel.Hour, null, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                    Assert.AreEqual(_days, hourCharts.Count);
                    for (int i = 0; i < hourCharts.Count; ++i)
                    {
                        Assert.AreEqual(24, hourCharts[i].Count);
                        for (int h = 0; h < 24; ++h)
                        {
                            Assert.AreEqual(8 * 60 * laneCount, hourCharts[i][h].Value);
                            Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                            Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int,LaneFlow>>> dayCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, DateTimeLevel.Day, null, _startDates.ToArray(), _startDates.ToArray());
                    Assert.AreEqual(_days, dayCharts.Count);
                    for (int i = 0; i < dayCharts.Count; ++i)
                    {
                        Assert.AreEqual(1, dayCharts[i].Count);
                        Assert.AreEqual(8 * 60 * 24 * laneCount, dayCharts[i][0].Value);
                        Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                        Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                    }

                    List<List<TrafficChart<DateTime, int,LaneFlow>>> monthCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, DateTimeLevel.Month, null, _startMonths.ToArray(), _startMonths.ToArray());
                    Assert.AreEqual(_months, monthCharts.Count);
                    for (int i = 0; i < monthCharts.Count; ++i)
                    {
                        Assert.AreEqual(1, monthCharts[i].Count);
                        int total = 0;
                        foreach (TrafficLane lane in relation.Channel.Lanes)
                        {
                            total += service.QueryList(lane.DataId, DateTimeLevel.Month, _startMonths[i], _startMonths[i])
                                .Sum(f => f.Total);
                        }
                        Assert.AreEqual(total, monthCharts[i][0].Value);
                        Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                        Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                    }
                }
            }

            //按路口方向查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    int[] directions = relation.Channel.Lanes.Select(l => l.Direction).Distinct().ToArray();
                    foreach (int direction in directions)
                    {
                        int laneCount = relation.Channel.Lanes.Count(l => l.Direction == direction);
                        List<List<TrafficChart<DateTime, int,LaneFlow>>> minuteCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, new[] { direction }, DateTimeLevel.Minute,null, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                        //检查时间段数量
                        Assert.AreEqual(_days, minuteCharts.Count);
                        for (int i = 0; i < minuteCharts.Count; ++i)
                        {
                            //检查每个时间段的数据数量
                            Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                            for (int m = 0; m < 24 * 60; ++m)
                            {
                                //检查每个数据的各项值
                                Assert.AreEqual(8 * laneCount, minuteCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int,LaneFlow>>> fiveCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, new[] { direction }, DateTimeLevel.FiveMinutes, null, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                        Assert.AreEqual(_days, fiveCharts.Count);
                        for (int i = 0; i < fiveCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 5; ++m)
                            {
                                Assert.AreEqual(8 * 5 * laneCount, fiveCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int,LaneFlow>>> fifteenCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, new[] { direction }, DateTimeLevel.FifteenMinutes, null, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                        Assert.AreEqual(_days, fifteenCharts.Count);
                        for (int i = 0; i < fifteenCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 15; ++m)
                            {
                                Assert.AreEqual(8 * 15 * laneCount, fifteenCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int,LaneFlow>>> hourCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, new[] { direction }, DateTimeLevel.Hour, null, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                        Assert.AreEqual(_days, hourCharts.Count);
                        for (int i = 0; i < hourCharts.Count; ++i)
                        {
                            Assert.AreEqual(24, hourCharts[i].Count);
                            for (int h = 0; h < 24; ++h)
                            {
                                Assert.AreEqual(8 * 60 * laneCount, hourCharts[i][h].Value);
                                Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                                Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int,LaneFlow>>> dayCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, new[] { direction }, DateTimeLevel.Day, null, _startDates.ToArray(), _startDates.ToArray());
                        Assert.AreEqual(_days, dayCharts.Count);
                        for (int i = 0; i < dayCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, dayCharts[i].Count);
                            Assert.AreEqual(8 * 60 * 24 * laneCount, dayCharts[i][0].Value);
                            Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                            Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                        }

                        List<List<TrafficChart<DateTime, int,LaneFlow>>> monthCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, new[] { direction }, DateTimeLevel.Month, null, _startMonths.ToArray(), _startMonths.ToArray());
                        Assert.AreEqual(_months, monthCharts.Count);
                        for (int i = 0; i < monthCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, monthCharts[i].Count);
                            int total = 0;
                            foreach (TrafficLane lane in relation.Channel.Lanes.Where(l => l.Direction == direction))
                            {
                                total += service.QueryList(lane.DataId, DateTimeLevel.Month, _startMonths[i], _startMonths[i])
                                    .Sum(f => f.Total);
                            }
                            Assert.AreEqual(total, monthCharts[i][0].Value);
                            Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                            Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                        }
                    }
                }
            }

            //按路口流向查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    int[] directions = relation.Channel.Lanes.Select(l => l.Direction).Distinct().ToArray();
                    foreach (int direction in directions)
                    {
                        int[] flowDirections = relation.Channel.Lanes.Where(l => l.Direction == direction)
                            .Select(l => l.FlowDirection).Distinct().ToArray();
                        int laneCount = relation.Channel.Lanes.Count(l => l.Direction == direction && flowDirections.Contains(l.FlowDirection));

                        List<List<TrafficChart<DateTime, int,LaneFlow>>> minuteCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, direction, flowDirections, DateTimeLevel.Minute, null, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                        //检查时间段数量
                        Assert.AreEqual(_days, minuteCharts.Count);
                        for (int i = 0; i < minuteCharts.Count; ++i)
                        {
                            //检查每个时间段的数据数量
                            Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                            for (int m = 0; m < 24 * 60; ++m)
                            {
                                //检查每个数据的各项值
                                Assert.AreEqual(8 * laneCount, minuteCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int,LaneFlow>>> fiveCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, direction, flowDirections, DateTimeLevel.FiveMinutes, null, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                        Assert.AreEqual(_days, fiveCharts.Count);
                        for (int i = 0; i < fiveCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 5; ++m)
                            {
                                Assert.AreEqual(8 * 5 * laneCount, fiveCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int,LaneFlow>>> fifteenCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, direction, flowDirections, DateTimeLevel.FifteenMinutes, null, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                        Assert.AreEqual(_days, fifteenCharts.Count);
                        for (int i = 0; i < fifteenCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 15; ++m)
                            {
                                Assert.AreEqual(8 * 15 * laneCount, fifteenCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int,LaneFlow>>> hourCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, direction, flowDirections, DateTimeLevel.Hour, null, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                        Assert.AreEqual(_days, hourCharts.Count);
                        for (int i = 0; i < hourCharts.Count; ++i)
                        {
                            Assert.AreEqual(24, hourCharts[i].Count);
                            for (int h = 0; h < 24; ++h)
                            {
                                Assert.AreEqual(8 * 60 * laneCount, hourCharts[i][h].Value);
                                Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                                Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int,LaneFlow>>> dayCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, direction, flowDirections, DateTimeLevel.Day, null, _startDates.ToArray(), _startDates.ToArray());
                        Assert.AreEqual(_days, dayCharts.Count);
                        for (int i = 0; i < dayCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, dayCharts[i].Count);
                            Assert.AreEqual(8 * 60 * 24 * laneCount, dayCharts[i][0].Value);
                            Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                            Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                        }

                        List<List<TrafficChart<DateTime, int,LaneFlow>>> monthCharts = flowsController.QueryChartsByCrossing(relation.Channel.CrossingId.Value, direction, flowDirections, DateTimeLevel.Month, null, _startMonths.ToArray(), _startMonths.ToArray());
                        Assert.AreEqual(_months, monthCharts.Count);
                        for (int i = 0; i < monthCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, monthCharts[i].Count);
                            int total = 0;
                            foreach (TrafficLane lane in relation.Channel.Lanes.Where(l => l.Direction == direction && flowDirections.Contains(l.FlowDirection)))
                            {
                                total += service.QueryList(lane.DataId, DateTimeLevel.Month, _startMonths[i], _startMonths[i])
                                    .Sum(f => f.Total);
                            }
                            Assert.AreEqual(total, monthCharts[i][0].Value);
                            Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                            Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void QueryAverageSpeedCharts()
        {
            LaneFlowsController controller = new LaneFlowsController(TestInit.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<LaneFlowManager_Alone>());
            //按车道查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (TrafficLane lane in relation.Channel.Lanes)
                    {
                        List<List<TrafficChart<DateTime, int, LaneFlow>>> minuteCharts = controller.QueryChartsBySection(1,new [] { lane.DataId }, DateTimeLevel.Minute, new[] { FlowType.平均速度 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                        //检查时间段数量
                        Assert.AreEqual(_days, minuteCharts.Count);
                        for (int i = 0; i < minuteCharts.Count; ++i)
                        {
                            //检查每个时间段的数据数量
                            Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                            for (int m = 0; m < 24 * 60; ++m)
                            {
                                //检查每个数据的各项值
                                Assert.AreEqual(50, minuteCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fiveCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.FiveMinutes, new[] { FlowType.平均速度 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                        Assert.AreEqual(_days, fiveCharts.Count);
                        for (int i = 0; i < fiveCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 5; ++m)
                            {
                                Assert.AreEqual(50, fiveCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fifteenCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.FifteenMinutes, new[] { FlowType.平均速度 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                        Assert.AreEqual(_days, fifteenCharts.Count);
                        for (int i = 0; i < fifteenCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 15; ++m)
                            {
                                Assert.AreEqual(50, fifteenCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> hourCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Hour, new[] { FlowType.平均速度 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                        Assert.AreEqual(_days, hourCharts.Count);
                        for (int i = 0; i < hourCharts.Count; ++i)
                        {
                            Assert.AreEqual(24, hourCharts[i].Count);
                            for (int h = 0; h < 24; ++h)
                            {
                                Assert.AreEqual(50, hourCharts[i][h].Value);
                                Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                                Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> dayCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Day, new[] { FlowType.平均速度 }, _startDates.ToArray(), _startDates.ToArray());
                        Assert.AreEqual(_days, dayCharts.Count);
                        for (int i = 0; i < dayCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, dayCharts[i].Count);
                            Assert.AreEqual(50, dayCharts[i][0].Value);
                            Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                            Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> monthCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Month, new[] { FlowType.平均速度 }, _startMonths.ToArray(), _startMonths.ToArray());
                        Assert.AreEqual(_months, monthCharts.Count);
                        for (int i = 0; i < monthCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, monthCharts[i].Count);
                            Assert.AreEqual(50, monthCharts[i][0].Value);
                            Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                            Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                        }
                    }
                }
            }

            //按路段查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    List<List<TrafficChart<DateTime, int, LaneFlow>>> minuteCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Minute, new[] { FlowType.平均速度 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                    //检查时间段数量
                    Assert.AreEqual(_days, minuteCharts.Count);
                    for (int i = 0; i < minuteCharts.Count; ++i)
                    {
                        //检查每个时间段的数据数量
                        Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                        for (int m = 0; m < 24 * 60; ++m)
                        {
                            //检查每个数据的各项值
                            Assert.AreEqual(50, minuteCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> fiveCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.FiveMinutes, new[] { FlowType.平均速度 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                    Assert.AreEqual(_days, fiveCharts.Count);
                    for (int i = 0; i < fiveCharts.Count; ++i)
                    {
                        Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                        for (int m = 0; m < 24 * 60 / 5; ++m)
                        {
                            Assert.AreEqual(50, fiveCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> fifteenCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.FifteenMinutes, new[] { FlowType.平均速度 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                    Assert.AreEqual(_days, fifteenCharts.Count);
                    for (int i = 0; i < fifteenCharts.Count; ++i)
                    {
                        Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                        for (int m = 0; m < 24 * 60 / 15; ++m)
                        {
                            Assert.AreEqual(50, fifteenCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> hourCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Hour, new[] { FlowType.平均速度 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                    Assert.AreEqual(_days, hourCharts.Count);
                    for (int i = 0; i < hourCharts.Count; ++i)
                    {
                        Assert.AreEqual(24, hourCharts[i].Count);
                        for (int h = 0; h < 24; ++h)
                        {
                            Assert.AreEqual(50, hourCharts[i][h].Value);
                            Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                            Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> dayCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Day, new[] { FlowType.平均速度 }, _startDates.ToArray(), _startDates.ToArray());
                    Assert.AreEqual(_days, dayCharts.Count);
                    for (int i = 0; i < dayCharts.Count; ++i)
                    {
                        Assert.AreEqual(1, dayCharts[i].Count);
                        Assert.AreEqual(50, dayCharts[i][0].Value);
                        Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                        Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> monthCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Month, new[] { FlowType.平均速度 }, _startMonths.ToArray(), _startMonths.ToArray());
                    Assert.AreEqual(_months, monthCharts.Count);
                    for (int i = 0; i < monthCharts.Count; ++i)
                    {
                        Assert.AreEqual(1, monthCharts[i].Count);
                        Assert.AreEqual(50, monthCharts[i][0].Value);
                        Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                        Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                    }
                }
            }

            //按路口流向查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (int flowDirection in relation.Channel.Lanes.Select(l => l.FlowDirection).Distinct())
                    {
                        List<List<TrafficChart<DateTime, int, LaneFlow>>> minuteCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Minute, new[] { FlowType.平均速度 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                        //检查时间段数量
                        Assert.AreEqual(_days, minuteCharts.Count);
                        for (int i = 0; i < minuteCharts.Count; ++i)
                        {
                            //检查每个时间段的数据数量
                            Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                            for (int m = 0; m < 24 * 60; ++m)
                            {
                                //检查每个数据的各项值
                                Assert.AreEqual(50, minuteCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fiveCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.FiveMinutes, new[] { FlowType.平均速度 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                        Assert.AreEqual(_days, fiveCharts.Count);
                        for (int i = 0; i < fiveCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 5; ++m)
                            {
                                Assert.AreEqual(50, fiveCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fifteenCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.FifteenMinutes, new[] { FlowType.平均速度 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                        Assert.AreEqual(_days, fifteenCharts.Count);
                        for (int i = 0; i < fifteenCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 15; ++m)
                            {
                                Assert.AreEqual(50, fifteenCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> hourCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Hour, new[] { FlowType.平均速度 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                        Assert.AreEqual(_days, hourCharts.Count);
                        for (int i = 0; i < hourCharts.Count; ++i)
                        {
                            Assert.AreEqual(24, hourCharts[i].Count);
                            for (int h = 0; h < 24; ++h)
                            {
                                Assert.AreEqual(50, hourCharts[i][h].Value);
                                Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                                Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> dayCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Day, new[] { FlowType.平均速度 }, _startDates.ToArray(), _startDates.ToArray());
                        Assert.AreEqual(_days, dayCharts.Count);
                        for (int i = 0; i < dayCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, dayCharts[i].Count);
                            Assert.AreEqual(50, dayCharts[i][0].Value);
                            Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                            Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> monthCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Month, new[] { FlowType.平均速度 }, _startMonths.ToArray(), _startMonths.ToArray());
                        Assert.AreEqual(_months, monthCharts.Count);
                        for (int i = 0; i < monthCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, monthCharts[i].Count);
                            Assert.AreEqual(50, monthCharts[i][0].Value);
                            Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                            Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void QueryHeadDistanceCharts()
        {
            LaneFlowsController controller = new LaneFlowsController(TestInit.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<LaneFlowManager_Alone>());
            //按车道查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (TrafficLane lane in relation.Channel.Lanes)
                    {
                        List<List<TrafficChart<DateTime, int, LaneFlow>>> minuteCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Minute, new[] { FlowType.车头时距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                        //检查时间段数量
                        Assert.AreEqual(_days, minuteCharts.Count);
                        for (int i = 0; i < minuteCharts.Count; ++i)
                        {
                            //检查每个时间段的数据数量
                            Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                            for (int m = 0; m < 24 * 60; ++m)
                            {
                                //检查每个数据的各项值
                                Assert.AreEqual(10, minuteCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fiveCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.FiveMinutes, new[] { FlowType.车头时距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                        Assert.AreEqual(_days, fiveCharts.Count);
                        for (int i = 0; i < fiveCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 5; ++m)
                            {
                                Assert.AreEqual(10, fiveCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fifteenCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.FifteenMinutes, new[] { FlowType.车头时距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                        Assert.AreEqual(_days, fifteenCharts.Count);
                        for (int i = 0; i < fifteenCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 15; ++m)
                            {
                                Assert.AreEqual(10, fifteenCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> hourCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Hour, new[] { FlowType.车头时距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                        Assert.AreEqual(_days, hourCharts.Count);
                        for (int i = 0; i < hourCharts.Count; ++i)
                        {
                            Assert.AreEqual(24, hourCharts[i].Count);
                            for (int h = 0; h < 24; ++h)
                            {
                                Assert.AreEqual(10, hourCharts[i][h].Value);
                                Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                                Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> dayCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Day, new[] { FlowType.车头时距 }, _startDates.ToArray(), _startDates.ToArray());
                        Assert.AreEqual(_days, dayCharts.Count);
                        for (int i = 0; i < dayCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, dayCharts[i].Count);
                            Assert.AreEqual(10, dayCharts[i][0].Value);
                            Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                            Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> monthCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Month, new[] { FlowType.车头时距 }, _startMonths.ToArray(), _startMonths.ToArray());
                        Assert.AreEqual(_months, monthCharts.Count);
                        for (int i = 0; i < monthCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, monthCharts[i].Count);
                            Assert.AreEqual(10, monthCharts[i][0].Value);
                            Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                            Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                        }
                    }
                }
            }

            //按路段查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    List<List<TrafficChart<DateTime, int, LaneFlow>>> minuteCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Minute, new[] { FlowType.车头时距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                    //检查时间段数量
                    Assert.AreEqual(_days, minuteCharts.Count);
                    for (int i = 0; i < minuteCharts.Count; ++i)
                    {
                        //检查每个时间段的数据数量
                        Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                        for (int m = 0; m < 24 * 60; ++m)
                        {
                            //检查每个数据的各项值
                            Assert.AreEqual(10, minuteCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> fiveCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.FiveMinutes, new[] { FlowType.车头时距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                    Assert.AreEqual(_days, fiveCharts.Count);
                    for (int i = 0; i < fiveCharts.Count; ++i)
                    {
                        Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                        for (int m = 0; m < 24 * 60 / 5; ++m)
                        {
                            Assert.AreEqual(10, fiveCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> fifteenCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.FifteenMinutes, new[] { FlowType.车头时距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                    Assert.AreEqual(_days, fifteenCharts.Count);
                    for (int i = 0; i < fifteenCharts.Count; ++i)
                    {
                        Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                        for (int m = 0; m < 24 * 60 / 15; ++m)
                        {
                            Assert.AreEqual(10, fifteenCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> hourCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Hour, new[] { FlowType.车头时距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                    Assert.AreEqual(_days, hourCharts.Count);
                    for (int i = 0; i < hourCharts.Count; ++i)
                    {
                        Assert.AreEqual(24, hourCharts[i].Count);
                        for (int h = 0; h < 24; ++h)
                        {
                            Assert.AreEqual(10, hourCharts[i][h].Value);
                            Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                            Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> dayCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Day, new[] { FlowType.车头时距 }, _startDates.ToArray(), _startDates.ToArray());
                    Assert.AreEqual(_days, dayCharts.Count);
                    for (int i = 0; i < dayCharts.Count; ++i)
                    {
                        Assert.AreEqual(1, dayCharts[i].Count);
                        Assert.AreEqual(10, dayCharts[i][0].Value);
                        Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                        Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> monthCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Month, new[] { FlowType.车头时距 }, _startMonths.ToArray(), _startMonths.ToArray());
                    Assert.AreEqual(_months, monthCharts.Count);
                    for (int i = 0; i < monthCharts.Count; ++i)
                    {
                        Assert.AreEqual(1, monthCharts[i].Count);
                        Assert.AreEqual(10, monthCharts[i][0].Value);
                        Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                        Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                    }
                }
            }

            //按路口流向查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (int flowDirection in relation.Channel.Lanes.Select(l => l.FlowDirection).Distinct())
                    {
                        List<List<TrafficChart<DateTime, int, LaneFlow>>> minuteCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Minute, new[] { FlowType.车头时距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                        //检查时间段数量
                        Assert.AreEqual(_days, minuteCharts.Count);
                        for (int i = 0; i < minuteCharts.Count; ++i)
                        {
                            //检查每个时间段的数据数量
                            Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                            for (int m = 0; m < 24 * 60; ++m)
                            {
                                //检查每个数据的各项值
                                Assert.AreEqual(10, minuteCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fiveCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.FiveMinutes, new[] { FlowType.车头时距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                        Assert.AreEqual(_days, fiveCharts.Count);
                        for (int i = 0; i < fiveCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 5; ++m)
                            {
                                Assert.AreEqual(10, fiveCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fifteenCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.FifteenMinutes, new[] { FlowType.车头时距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                        Assert.AreEqual(_days, fifteenCharts.Count);
                        for (int i = 0; i < fifteenCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 15; ++m)
                            {
                                Assert.AreEqual(10, fifteenCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> hourCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Hour, new[] { FlowType.车头时距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                        Assert.AreEqual(_days, hourCharts.Count);
                        for (int i = 0; i < hourCharts.Count; ++i)
                        {
                            Assert.AreEqual(24, hourCharts[i].Count);
                            for (int h = 0; h < 24; ++h)
                            {
                                Assert.AreEqual(10, hourCharts[i][h].Value);
                                Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                                Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> dayCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Day, new[] { FlowType.车头时距 }, _startDates.ToArray(), _startDates.ToArray());
                        Assert.AreEqual(_days, dayCharts.Count);
                        for (int i = 0; i < dayCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, dayCharts[i].Count);
                            Assert.AreEqual(10, dayCharts[i][0].Value);
                            Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                            Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> monthCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Month, new[] { FlowType.车头时距 }, _startMonths.ToArray(), _startMonths.ToArray());
                        Assert.AreEqual(_months, monthCharts.Count);
                        for (int i = 0; i < monthCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, monthCharts[i].Count);
                            Assert.AreEqual(10, monthCharts[i][0].Value);
                            Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                            Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void QueryOccupancyCharts()
        {
            LaneFlowsController controller = new LaneFlowsController(TestInit.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<LaneFlowManager_Alone>());
            //按车道查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (TrafficLane lane in relation.Channel.Lanes)
                    {
                        List<List<TrafficChart<DateTime, int, LaneFlow>>> minuteCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Minute, new[] { FlowType.空间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                        //检查时间段数量
                        Assert.AreEqual(_days, minuteCharts.Count);
                        for (int i = 0; i < minuteCharts.Count; ++i)
                        {
                            //检查每个时间段的数据数量
                            Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                            for (int m = 0; m < 24 * 60; ++m)
                            {
                                //检查每个数据的各项值
                                Assert.AreEqual(30, minuteCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fiveCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.FiveMinutes, new[] { FlowType.空间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                        Assert.AreEqual(_days, fiveCharts.Count);
                        for (int i = 0; i < fiveCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 5; ++m)
                            {
                                Assert.AreEqual(30, fiveCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fifteenCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.FifteenMinutes, new[] { FlowType.空间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                        Assert.AreEqual(_days, fifteenCharts.Count);
                        for (int i = 0; i < fifteenCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 15; ++m)
                            {
                                Assert.AreEqual(30, fifteenCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> hourCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Hour, new[] { FlowType.空间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                        Assert.AreEqual(_days, hourCharts.Count);
                        for (int i = 0; i < hourCharts.Count; ++i)
                        {
                            Assert.AreEqual(24, hourCharts[i].Count);
                            for (int h = 0; h < 24; ++h)
                            {
                                Assert.AreEqual(30, hourCharts[i][h].Value);
                                Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                                Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> dayCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Day, new[] { FlowType.空间占有率 }, _startDates.ToArray(), _startDates.ToArray());
                        Assert.AreEqual(_days, dayCharts.Count);
                        for (int i = 0; i < dayCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, dayCharts[i].Count);
                            Assert.AreEqual(30, dayCharts[i][0].Value);
                            Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                            Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> monthCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Month, new[] { FlowType.空间占有率 }, _startMonths.ToArray(), _startMonths.ToArray());
                        Assert.AreEqual(_months, monthCharts.Count);
                        for (int i = 0; i < monthCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, monthCharts[i].Count);
                            Assert.AreEqual(30, monthCharts[i][0].Value);
                            Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                            Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                        }
                    }
                }
            }

            //按路段查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    List<List<TrafficChart<DateTime, int, LaneFlow>>> minuteCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Minute, new[] { FlowType.空间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                    //检查时间段数量
                    Assert.AreEqual(_days, minuteCharts.Count);
                    for (int i = 0; i < minuteCharts.Count; ++i)
                    {
                        //检查每个时间段的数据数量
                        Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                        for (int m = 0; m < 24 * 60; ++m)
                        {
                            //检查每个数据的各项值
                            Assert.AreEqual(30, minuteCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> fiveCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.FiveMinutes, new[] { FlowType.空间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                    Assert.AreEqual(_days, fiveCharts.Count);
                    for (int i = 0; i < fiveCharts.Count; ++i)
                    {
                        Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                        for (int m = 0; m < 24 * 60 / 5; ++m)
                        {
                            Assert.AreEqual(30, fiveCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> fifteenCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.FifteenMinutes, new[] { FlowType.空间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                    Assert.AreEqual(_days, fifteenCharts.Count);
                    for (int i = 0; i < fifteenCharts.Count; ++i)
                    {
                        Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                        for (int m = 0; m < 24 * 60 / 15; ++m)
                        {
                            Assert.AreEqual(30, fifteenCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> hourCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Hour, new[] { FlowType.空间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                    Assert.AreEqual(_days, hourCharts.Count);
                    for (int i = 0; i < hourCharts.Count; ++i)
                    {
                        Assert.AreEqual(24, hourCharts[i].Count);
                        for (int h = 0; h < 24; ++h)
                        {
                            Assert.AreEqual(30, hourCharts[i][h].Value);
                            Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                            Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> dayCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Day, new[] { FlowType.空间占有率 }, _startDates.ToArray(), _startDates.ToArray());
                    Assert.AreEqual(_days, dayCharts.Count);
                    for (int i = 0; i < dayCharts.Count; ++i)
                    {
                        Assert.AreEqual(1, dayCharts[i].Count);
                        Assert.AreEqual(30, dayCharts[i][0].Value);
                        Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                        Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> monthCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Month, new[] { FlowType.空间占有率 }, _startMonths.ToArray(), _startMonths.ToArray());
                    Assert.AreEqual(_months, monthCharts.Count);
                    for (int i = 0; i < monthCharts.Count; ++i)
                    {
                        Assert.AreEqual(1, monthCharts[i].Count);
                        Assert.AreEqual(30, monthCharts[i][0].Value);
                        Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                        Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                    }
                }
            }

            //按路口流向查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (int flowDirection in relation.Channel.Lanes.Select(l => l.FlowDirection).Distinct())
                    {
                        List<List<TrafficChart<DateTime, int, LaneFlow>>> minuteCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Minute, new[] { FlowType.空间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                        //检查时间段数量
                        Assert.AreEqual(_days, minuteCharts.Count);
                        for (int i = 0; i < minuteCharts.Count; ++i)
                        {
                            //检查每个时间段的数据数量
                            Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                            for (int m = 0; m < 24 * 60; ++m)
                            {
                                //检查每个数据的各项值
                                Assert.AreEqual(30, minuteCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fiveCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.FiveMinutes, new[] { FlowType.空间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                        Assert.AreEqual(_days, fiveCharts.Count);
                        for (int i = 0; i < fiveCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 5; ++m)
                            {
                                Assert.AreEqual(30, fiveCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fifteenCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.FifteenMinutes, new[] { FlowType.空间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                        Assert.AreEqual(_days, fifteenCharts.Count);
                        for (int i = 0; i < fifteenCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 15; ++m)
                            {
                                Assert.AreEqual(30, fifteenCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> hourCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Hour, new[] { FlowType.空间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                        Assert.AreEqual(_days, hourCharts.Count);
                        for (int i = 0; i < hourCharts.Count; ++i)
                        {
                            Assert.AreEqual(24, hourCharts[i].Count);
                            for (int h = 0; h < 24; ++h)
                            {
                                Assert.AreEqual(30, hourCharts[i][h].Value);
                                Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                                Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> dayCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Day, new[] { FlowType.空间占有率 }, _startDates.ToArray(), _startDates.ToArray());
                        Assert.AreEqual(_days, dayCharts.Count);
                        for (int i = 0; i < dayCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, dayCharts[i].Count);
                            Assert.AreEqual(30, dayCharts[i][0].Value);
                            Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                            Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> monthCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Month, new[] { FlowType.空间占有率 }, _startMonths.ToArray(), _startMonths.ToArray());
                        Assert.AreEqual(_months, monthCharts.Count);
                        for (int i = 0; i < monthCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, monthCharts[i].Count);
                            Assert.AreEqual(30, monthCharts[i][0].Value);
                            Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                            Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void QueryTimeOccupancyCharts()
        {
            LaneFlowsController controller = new LaneFlowsController(TestInit.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<LaneFlowManager_Alone>());
            //按车道查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (TrafficLane lane in relation.Channel.Lanes)
                    {
                        List<List<TrafficChart<DateTime, int, LaneFlow>>> minuteCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Minute, new[] { FlowType.时间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                        //检查时间段数量
                        Assert.AreEqual(_days, minuteCharts.Count);
                        for (int i = 0; i < minuteCharts.Count; ++i)
                        {
                            //检查每个时间段的数据数量
                            Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                            for (int m = 0; m < 24 * 60; ++m)
                            {
                                //检查每个数据的各项值
                                Assert.AreEqual(40, minuteCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fiveCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.FiveMinutes, new[] { FlowType.时间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                        Assert.AreEqual(_days, fiveCharts.Count);
                        for (int i = 0; i < fiveCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 5; ++m)
                            {
                                Assert.AreEqual(40, fiveCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fifteenCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.FifteenMinutes, new[] { FlowType.时间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                        Assert.AreEqual(_days, fifteenCharts.Count);
                        for (int i = 0; i < fifteenCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 15; ++m)
                            {
                                Assert.AreEqual(40, fifteenCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> hourCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Hour, new[] { FlowType.时间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                        Assert.AreEqual(_days, hourCharts.Count);
                        for (int i = 0; i < hourCharts.Count; ++i)
                        {
                            Assert.AreEqual(24, hourCharts[i].Count);
                            for (int h = 0; h < 24; ++h)
                            {
                                Assert.AreEqual(40, hourCharts[i][h].Value);
                                Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                                Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> dayCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Day, new[] { FlowType.时间占有率 }, _startDates.ToArray(), _startDates.ToArray());
                        Assert.AreEqual(_days, dayCharts.Count);
                        for (int i = 0; i < dayCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, dayCharts[i].Count);
                            Assert.AreEqual(40, dayCharts[i][0].Value);
                            Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                            Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> monthCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Month, new[] { FlowType.时间占有率 }, _startMonths.ToArray(), _startMonths.ToArray());
                        Assert.AreEqual(_months, monthCharts.Count);
                        for (int i = 0; i < monthCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, monthCharts[i].Count);
                            Assert.AreEqual(40, monthCharts[i][0].Value);
                            Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                            Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                        }
                    }
                }
            }

            //按路段查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    List<List<TrafficChart<DateTime, int, LaneFlow>>> minuteCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Minute, new[] { FlowType.时间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                    //检查时间段数量
                    Assert.AreEqual(_days, minuteCharts.Count);
                    for (int i = 0; i < minuteCharts.Count; ++i)
                    {
                        //检查每个时间段的数据数量
                        Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                        for (int m = 0; m < 24 * 60; ++m)
                        {
                            //检查每个数据的各项值
                            Assert.AreEqual(40, minuteCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> fiveCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.FiveMinutes, new[] { FlowType.时间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                    Assert.AreEqual(_days, fiveCharts.Count);
                    for (int i = 0; i < fiveCharts.Count; ++i)
                    {
                        Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                        for (int m = 0; m < 24 * 60 / 5; ++m)
                        {
                            Assert.AreEqual(40, fiveCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> fifteenCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.FifteenMinutes, new[] { FlowType.时间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                    Assert.AreEqual(_days, fifteenCharts.Count);
                    for (int i = 0; i < fifteenCharts.Count; ++i)
                    {
                        Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                        for (int m = 0; m < 24 * 60 / 15; ++m)
                        {
                            Assert.AreEqual(40, fifteenCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> hourCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Hour, new[] { FlowType.时间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                    Assert.AreEqual(_days, hourCharts.Count);
                    for (int i = 0; i < hourCharts.Count; ++i)
                    {
                        Assert.AreEqual(24, hourCharts[i].Count);
                        for (int h = 0; h < 24; ++h)
                        {
                            Assert.AreEqual(40, hourCharts[i][h].Value);
                            Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                            Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> dayCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Day, new[] { FlowType.时间占有率 }, _startDates.ToArray(), _startDates.ToArray());
                    Assert.AreEqual(_days, dayCharts.Count);
                    for (int i = 0; i < dayCharts.Count; ++i)
                    {
                        Assert.AreEqual(1, dayCharts[i].Count);
                        Assert.AreEqual(40, dayCharts[i][0].Value);
                        Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                        Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> monthCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Month, new[] { FlowType.时间占有率 }, _startMonths.ToArray(), _startMonths.ToArray());
                    Assert.AreEqual(_months, monthCharts.Count);
                    for (int i = 0; i < monthCharts.Count; ++i)
                    {
                        Assert.AreEqual(1, monthCharts[i].Count);
                        Assert.AreEqual(40, monthCharts[i][0].Value);
                        Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                        Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                    }
                }
            }

            //按路口流向查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (int flowDirection in relation.Channel.Lanes.Select(l => l.FlowDirection).Distinct())
                    {
                        List<List<TrafficChart<DateTime, int, LaneFlow>>> minuteCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Minute, new[] { FlowType.时间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                        //检查时间段数量
                        Assert.AreEqual(_days, minuteCharts.Count);
                        for (int i = 0; i < minuteCharts.Count; ++i)
                        {
                            //检查每个时间段的数据数量
                            Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                            for (int m = 0; m < 24 * 60; ++m)
                            {
                                //检查每个数据的各项值
                                Assert.AreEqual(40, minuteCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fiveCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.FiveMinutes, new[] { FlowType.时间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                        Assert.AreEqual(_days, fiveCharts.Count);
                        for (int i = 0; i < fiveCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 5; ++m)
                            {
                                Assert.AreEqual(40, fiveCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fifteenCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.FifteenMinutes, new[] { FlowType.时间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                        Assert.AreEqual(_days, fifteenCharts.Count);
                        for (int i = 0; i < fifteenCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 15; ++m)
                            {
                                Assert.AreEqual(40, fifteenCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> hourCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Hour, new[] { FlowType.时间占有率 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                        Assert.AreEqual(_days, hourCharts.Count);
                        for (int i = 0; i < hourCharts.Count; ++i)
                        {
                            Assert.AreEqual(24, hourCharts[i].Count);
                            for (int h = 0; h < 24; ++h)
                            {
                                Assert.AreEqual(40, hourCharts[i][h].Value);
                                Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                                Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> dayCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Day, new[] { FlowType.时间占有率 }, _startDates.ToArray(), _startDates.ToArray());
                        Assert.AreEqual(_days, dayCharts.Count);
                        for (int i = 0; i < dayCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, dayCharts[i].Count);
                            Assert.AreEqual(40, dayCharts[i][0].Value);
                            Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                            Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> monthCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Month, new[] { FlowType.时间占有率 }, _startMonths.ToArray(), _startMonths.ToArray());
                        Assert.AreEqual(_months, monthCharts.Count);
                        for (int i = 0; i < monthCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, monthCharts[i].Count);
                            Assert.AreEqual(40, monthCharts[i][0].Value);
                            Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                            Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void QueryTimeHeadSpaceCharts()
        {
            LaneFlowsController controller = new LaneFlowsController(TestInit.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<LaneFlowManager_Alone>());
            //按车道查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (TrafficLane lane in relation.Channel.Lanes)
                    {
                        List<List<TrafficChart<DateTime, int, LaneFlow>>> minuteCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Minute, new[] { FlowType.车头间距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                        //检查时间段数量
                        Assert.AreEqual(_days, minuteCharts.Count);
                        for (int i = 0; i < minuteCharts.Count; ++i)
                        {
                            //检查每个时间段的数据数量
                            Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                            for (int m = 0; m < 24 * 60; ++m)
                            {
                                //检查每个数据的各项值
                                Assert.AreEqual(139, minuteCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fiveCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.FiveMinutes, new[] { FlowType.车头间距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                        Assert.AreEqual(_days, fiveCharts.Count);
                        for (int i = 0; i < fiveCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 5; ++m)
                            {
                                Assert.AreEqual(139, fiveCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fifteenCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.FifteenMinutes, new[] { FlowType.车头间距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                        Assert.AreEqual(_days, fifteenCharts.Count);
                        for (int i = 0; i < fifteenCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 15; ++m)
                            {
                                Assert.AreEqual(139, fifteenCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> hourCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Hour, new[] { FlowType.车头间距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                        Assert.AreEqual(_days, hourCharts.Count);
                        for (int i = 0; i < hourCharts.Count; ++i)
                        {
                            Assert.AreEqual(24, hourCharts[i].Count);
                            for (int h = 0; h < 24; ++h)
                            {
                                Assert.AreEqual(139, hourCharts[i][h].Value);
                                Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                                Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> dayCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Day, new[] { FlowType.车头间距 }, _startDates.ToArray(), _startDates.ToArray());
                        Assert.AreEqual(_days, dayCharts.Count);
                        for (int i = 0; i < dayCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, dayCharts[i].Count);
                            Assert.AreEqual(139, dayCharts[i][0].Value);
                            Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                            Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> monthCharts = controller.QueryChartsBySection(1, new[] { lane.DataId }, DateTimeLevel.Month, new[] { FlowType.车头间距 }, _startMonths.ToArray(), _startMonths.ToArray());
                        Assert.AreEqual(_months, monthCharts.Count);
                        for (int i = 0; i < monthCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, monthCharts[i].Count);
                            Assert.AreEqual(139, monthCharts[i][0].Value);
                            Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                            Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                        }
                    }
                }
            }

            //按路段查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    List<List<TrafficChart<DateTime, int, LaneFlow>>> minuteCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Minute, new[] { FlowType.车头间距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                    //检查时间段数量
                    Assert.AreEqual(_days, minuteCharts.Count);
                    for (int i = 0; i < minuteCharts.Count; ++i)
                    {
                        //检查每个时间段的数据数量
                        Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                        for (int m = 0; m < 24 * 60; ++m)
                        {
                            //检查每个数据的各项值
                            Assert.AreEqual(139, minuteCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> fiveCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.FiveMinutes, new[] { FlowType.车头间距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                    Assert.AreEqual(_days, fiveCharts.Count);
                    for (int i = 0; i < fiveCharts.Count; ++i)
                    {
                        Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                        for (int m = 0; m < 24 * 60 / 5; ++m)
                        {
                            Assert.AreEqual(139, fiveCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> fifteenCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.FifteenMinutes, new[] { FlowType.车头间距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                    Assert.AreEqual(_days, fifteenCharts.Count);
                    for (int i = 0; i < fifteenCharts.Count; ++i)
                    {
                        Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                        for (int m = 0; m < 24 * 60 / 15; ++m)
                        {
                            Assert.AreEqual(139, fifteenCharts[i][m].Value);
                            Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                            Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> hourCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Hour, new[] { FlowType.车头间距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                    Assert.AreEqual(_days, hourCharts.Count);
                    for (int i = 0; i < hourCharts.Count; ++i)
                    {
                        Assert.AreEqual(24, hourCharts[i].Count);
                        for (int h = 0; h < 24; ++h)
                        {
                            Assert.AreEqual(139, hourCharts[i][h].Value);
                            Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                            Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                        }
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> dayCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Day, new[] { FlowType.车头间距 }, _startDates.ToArray(), _startDates.ToArray());
                    Assert.AreEqual(_days, dayCharts.Count);
                    for (int i = 0; i < dayCharts.Count; ++i)
                    {
                        Assert.AreEqual(1, dayCharts[i].Count);
                        Assert.AreEqual(139, dayCharts[i][0].Value);
                        Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                        Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                    }

                    List<List<TrafficChart<DateTime, int, LaneFlow>>> monthCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, DateTimeLevel.Month, new[] { FlowType.车头间距 }, _startMonths.ToArray(), _startMonths.ToArray());
                    Assert.AreEqual(_months, monthCharts.Count);
                    for (int i = 0; i < monthCharts.Count; ++i)
                    {
                        Assert.AreEqual(1, monthCharts[i].Count);
                        Assert.AreEqual(139, monthCharts[i][0].Value);
                        Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                        Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                    }
                }
            }

            //按路口流向查询图表
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (int flowDirection in relation.Channel.Lanes.Select(l => l.FlowDirection).Distinct())
                    {
                        List<List<TrafficChart<DateTime, int, LaneFlow>>> minuteCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Minute, new[] { FlowType.车头间距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-1)).ToArray());
                        //检查时间段数量
                        Assert.AreEqual(_days, minuteCharts.Count);
                        for (int i = 0; i < minuteCharts.Count; ++i)
                        {
                            //检查每个时间段的数据数量
                            Assert.AreEqual(24 * 60, minuteCharts[i].Count);
                            for (int m = 0; m < 24 * 60; ++m)
                            {
                                //检查每个数据的各项值
                                Assert.AreEqual(139, minuteCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m), minuteCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m).ToString("yyyy-MM-dd HH:mm"), minuteCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fiveCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.FiveMinutes, new[] { FlowType.车头间距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-5)).ToArray());
                        Assert.AreEqual(_days, fiveCharts.Count);
                        for (int i = 0; i < fiveCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 5, fiveCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 5; ++m)
                            {
                                Assert.AreEqual(139, fiveCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 5), fiveCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> fifteenCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.FifteenMinutes, new[] { FlowType.车头间距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddMinutes(-15)).ToArray());
                        Assert.AreEqual(_days, fifteenCharts.Count);
                        for (int i = 0; i < fifteenCharts.Count; ++i)
                        {
                            Assert.AreEqual(24 * 60 / 15, fifteenCharts[i].Count);
                            for (int m = 0; m < 24 * 60 / 15; ++m)
                            {
                                Assert.AreEqual(139, fifteenCharts[i][m].Value);
                                Assert.AreEqual(_startDates[0].AddMinutes(m * 15), fifteenCharts[i][m].Axis);
                                Assert.AreEqual(_startDates[i].AddMinutes(m * 15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[i][m].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> hourCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Hour, new[] { FlowType.车头间距 }, _startDates.ToArray(), _startDates.Select(t => t.AddDays(1).AddHours(-1)).ToArray());
                        Assert.AreEqual(_days, hourCharts.Count);
                        for (int i = 0; i < hourCharts.Count; ++i)
                        {
                            Assert.AreEqual(24, hourCharts[i].Count);
                            for (int h = 0; h < 24; ++h)
                            {
                                Assert.AreEqual(139, hourCharts[i][h].Value);
                                Assert.AreEqual(_startDates[0].AddHours(h), hourCharts[i][h].Axis);
                                Assert.AreEqual(_startDates[i].AddHours(h).ToString("yyyy-MM-dd HH"), hourCharts[i][h].Remark);
                            }
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> dayCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Day, new[] { FlowType.车头间距 }, _startDates.ToArray(), _startDates.ToArray());
                        Assert.AreEqual(_days, dayCharts.Count);
                        for (int i = 0; i < dayCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, dayCharts[i].Count);
                            Assert.AreEqual(139, dayCharts[i][0].Value);
                            Assert.AreEqual(_startDates[0], dayCharts[i][0].Axis);
                            Assert.AreEqual(_startDates[i].ToString("yyyy-MM-dd"), dayCharts[i][0].Remark);
                        }

                        List<List<TrafficChart<DateTime, int, LaneFlow>>> monthCharts = controller.QueryChartsBySection(relation.Channel.SectionId.Value, new[] { flowDirection }, DateTimeLevel.Month, new[] { FlowType.车头间距 }, _startMonths.ToArray(), _startMonths.ToArray());
                        Assert.AreEqual(_months, monthCharts.Count);
                        for (int i = 0; i < monthCharts.Count; ++i)
                        {
                            Assert.AreEqual(1, monthCharts[i].Count);
                            Assert.AreEqual(139, monthCharts[i][0].Value);
                            Assert.AreEqual(_startMonths[0], monthCharts[i][0].Axis);
                            Assert.AreEqual(_startMonths[i].ToString("yyyy-MM"), monthCharts[i][0].Remark);
                        }
                    }
                }
            }
        }

    }
}
