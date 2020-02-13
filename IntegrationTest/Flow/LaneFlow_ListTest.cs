using System;
using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MomobamiKirari.Managers;
using MomobamiKirari.Models;

namespace IntegrationTest.Flow
{
    [TestClass]
    public class LaneFlow_ListTest
    {
        private static List<TrafficDevice> _devices;
        private static Dictionary<string, List<LaneFlow>> _datas;
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

            _datas = FlowDbSimulator.CreateData(TestInit.ServiceProvider, _devices, DataCreateMode.Fixed, dates, true);
        }

        [TestMethod]
        public void QueryList_ByLane()
        {
            LaneFlowManager_Alone service = TestInit.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<LaneFlowManager_Alone>();

            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (TrafficLane lane in relation.Channel.Lanes)
                    {
                        var list1 = service.QueryList(lane.DataId, DateTimeLevel.Minute, _startDate, _startDate.AddDays(_days).AddMinutes(-1));
                        Assert.AreEqual(24 * 60 * _days, list1.Count);
                        for (int i = 0; i < list1.Count; ++i)
                        {
                            Assert.AreEqual(_datas[lane.DataId][i].Total, list1[i].Total);
                            Assert.IsTrue(Math.Abs(_datas[lane.DataId][i].AverageSpeed - list1[i].AverageSpeed) < 0.1);
                            Assert.IsTrue(Math.Abs(_datas[lane.DataId][i].HeadDistance - list1[i].HeadDistance) < 0.1);
                            Assert.IsTrue(Math.Abs(_datas[lane.DataId][i].HeadSpace - list1[i].HeadSpace) < 0.1);
                            Assert.AreEqual(_datas[lane.DataId][i].Occupancy, list1[i].Occupancy);
                            Assert.AreEqual(_datas[lane.DataId][i].TimeOccupancy, list1[i].TimeOccupancy);

                        }

                        var list5 = service.QueryList(lane.DataId, DateTimeLevel.FiveMinutes, _startDate, _startDate.AddDays(_days).AddMinutes(-1));
                        var datas5 = _datas[lane.DataId]
                            .GroupBy(f => TimePointConvert.CurrentTimePoint(DateTimeLevel.FiveMinutes, f.DateTime))
                            .Select(g => new LaneFlow
                            {
                                Cars = g.Sum(f => f.Cars),
                                Vans = g.Sum(f => f.Vans),
                                Tricycles = g.Sum(f => f.Tricycles),
                                Trucks = g.Sum(f => f.Trucks),
                                Buss = g.Sum(f => f.Buss),
                                Motorcycles = g.Sum(f => f.Motorcycles),
                                Bikes = g.Sum(f => f.Bikes),
                                Persons = g.Sum(f => f.Persons),
                                Occupancy = g.Sum(f => f.Occupancy),
                                TimeOccupancy = g.Sum(f => f.TimeOccupancy),
                                HeadDistance = g.Sum(f => f.HeadDistance),
                                TravelTime = g.Sum(f => f.TravelTime),
                                Distance = g.Sum(f => f.Distance),
                                Count = g.Count()

                            }).ToList();
                        for (int i = 0; i < list5.Count; ++i)
                        {
                            Assert.AreEqual(datas5[i].Total, list5[i].Total);
                            Assert.IsTrue(Math.Abs(datas5[i].AverageSpeed - list5[i].AverageSpeed) < 0.1);
                            Assert.IsTrue(Math.Abs(datas5[i].HeadDistance - list5[i].HeadDistance) < 0.1);
                            Assert.IsTrue(Math.Abs(datas5[i].HeadSpace - list5[i].HeadSpace) < 0.1);
                            Assert.AreEqual(datas5[i].Occupancy, list5[i].Occupancy);
                            Assert.AreEqual(datas5[i].TimeOccupancy, list5[i].TimeOccupancy);
                        }

                        var list15 = service.QueryList(lane.DataId, DateTimeLevel.FifteenMinutes, _startDate, _startDate.AddDays(_days).AddMinutes(-1));

                        var datas15 = _datas[lane.DataId]
                            .GroupBy(f => TimePointConvert.CurrentTimePoint(DateTimeLevel.FifteenMinutes, f.DateTime))
                            .Select(g => new LaneFlow
                            {
                                Cars = g.Sum(f => f.Cars),
                                Vans = g.Sum(f => f.Vans),
                                Tricycles = g.Sum(f => f.Tricycles),
                                Trucks = g.Sum(f => f.Trucks),
                                Buss = g.Sum(f => f.Buss),
                                Motorcycles = g.Sum(f => f.Motorcycles),
                                Bikes = g.Sum(f => f.Bikes),
                                Persons = g.Sum(f => f.Persons),
                                Occupancy = g.Sum(f => f.Occupancy),
                                TimeOccupancy = g.Sum(f => f.TimeOccupancy),
                                HeadDistance = g.Sum(f => f.HeadDistance),
                                TravelTime = g.Sum(f => f.TravelTime),
                                Distance = g.Sum(f => f.Distance),
                                Count = g.Count()
                            }).ToList();
                        for (int i = 0; i < list15.Count; ++i)
                        {
                            Assert.AreEqual(datas15[i].Total, list15[i].Total);
                            Assert.IsTrue(Math.Abs(datas15[i].AverageSpeed - list15[i].AverageSpeed) < 0.1);
                            Assert.IsTrue(Math.Abs(datas15[i].HeadDistance - list15[i].HeadDistance) < 0.1);
                            Assert.IsTrue(Math.Abs(datas15[i].HeadSpace - list15[i].HeadSpace) < 0.1);
                            Assert.AreEqual(datas15[i].Occupancy, list15[i].Occupancy);
                            Assert.AreEqual(datas15[i].TimeOccupancy, list15[i].TimeOccupancy);
                        }

                        var list60 = service.QueryList(lane.DataId, DateTimeLevel.Hour, _startDate, _startDate.AddDays(_days).AddMinutes(-1));

                        var datas60 = _datas[lane.DataId]
                            .GroupBy(f => TimePointConvert.CurrentTimePoint(DateTimeLevel.Hour, f.DateTime))
                            .Select(g => new LaneFlow
                            {
                                Cars = g.Sum(f => f.Cars),
                                Vans = g.Sum(f => f.Vans),
                                Tricycles = g.Sum(f => f.Tricycles),
                                Trucks = g.Sum(f => f.Trucks),
                                Buss = g.Sum(f => f.Buss),
                                Motorcycles = g.Sum(f => f.Motorcycles),
                                Bikes = g.Sum(f => f.Bikes),
                                Persons = g.Sum(f => f.Persons),
                                Occupancy = g.Sum(f => f.Occupancy),
                                TimeOccupancy = g.Sum(f => f.TimeOccupancy),
                                HeadDistance = g.Sum(f => f.HeadDistance),
                                TravelTime = g.Sum(f => f.TravelTime),
                                Distance = g.Sum(f => f.Distance),
                                Count = g.Count()
                            }).ToList();
                        for (int i = 0; i < list60.Count; ++i)
                        {
                            Assert.AreEqual(datas60[i].Total, list60[i].Total);
                            Assert.IsTrue(Math.Abs(datas60[i].AverageSpeed - list60[i].AverageSpeed) < 0.1);
                            Assert.IsTrue(Math.Abs(datas60[i].HeadDistance - list60[i].HeadDistance) < 0.1);
                            Assert.IsTrue(Math.Abs(datas60[i].HeadSpace - list60[i].HeadSpace) < 0.1);
                            Assert.AreEqual(datas60[i].Occupancy, list60[i].Occupancy);
                            Assert.AreEqual(datas60[i].TimeOccupancy, list60[i].TimeOccupancy);
                        }

                        var listDay = service.QueryList(lane.DataId, DateTimeLevel.Day, _startDate, _startDate.AddDays(_days).AddMinutes(-1));

                        var datasDay = _datas[lane.DataId]
                            .GroupBy(f => TimePointConvert.CurrentTimePoint(DateTimeLevel.Day, f.DateTime))
                            .Select(g => new LaneFlow
                            {
                                Cars = g.Sum(f => f.Cars),
                                Vans = g.Sum(f => f.Vans),
                                Tricycles = g.Sum(f => f.Tricycles),
                                Trucks = g.Sum(f => f.Trucks),
                                Buss = g.Sum(f => f.Buss),
                                Motorcycles = g.Sum(f => f.Motorcycles),
                                Bikes = g.Sum(f => f.Bikes),
                                Persons = g.Sum(f => f.Persons),
                                Occupancy = g.Sum(f => f.Occupancy),
                                TimeOccupancy = g.Sum(f => f.TimeOccupancy),
                                HeadDistance = g.Sum(f => f.HeadDistance),
                                TravelTime = g.Sum(f => f.TravelTime),
                                Distance = g.Sum(f => f.Distance),
                                Count = g.Count()
                            }).ToList();
                        for (int i = 0; i < listDay.Count; ++i)
                        {
                            Assert.AreEqual(datasDay[i].Total, listDay[i].Total);
                            Assert.IsTrue(Math.Abs(datasDay[i].AverageSpeed - listDay[i].AverageSpeed) < 0.1);
                            Assert.IsTrue(Math.Abs(datasDay[i].HeadDistance - listDay[i].HeadDistance) < 0.1);
                            Assert.IsTrue(Math.Abs(datasDay[i].HeadSpace - listDay[i].HeadSpace) < 0.1);
                            Assert.AreEqual(datasDay[i].Occupancy, listDay[i].Occupancy);
                            Assert.AreEqual(datasDay[i].TimeOccupancy, listDay[i].TimeOccupancy);
                        }

                        var listMonth = service.QueryList(lane.DataId, DateTimeLevel.Month, _startDate, _startDate.AddDays(_days).AddMinutes(-1));

                        var datasMonth = _datas[lane.DataId]
                            .GroupBy(f => TimePointConvert.CurrentTimePoint(DateTimeLevel.Month, f.DateTime))
                            .Select(g => new LaneFlow
                            {
                                Cars = g.Sum(f => f.Cars),
                                Vans = g.Sum(f => f.Vans),
                                Tricycles = g.Sum(f => f.Tricycles),
                                Trucks = g.Sum(f => f.Trucks),
                                Buss = g.Sum(f => f.Buss),
                                Motorcycles = g.Sum(f => f.Motorcycles),
                                Bikes = g.Sum(f => f.Bikes),
                                Persons = g.Sum(f => f.Persons),
                                Occupancy = g.Sum(f => f.Occupancy),
                                TimeOccupancy = g.Sum(f => f.TimeOccupancy),
                                HeadDistance = g.Sum(f => f.HeadDistance),
                                TravelTime = g.Sum(f => f.TravelTime),
                                Distance = g.Sum(f => f.Distance),
                                Count = g.Count()
                            }).ToList();
                        for (int i = 0; i < listMonth.Count; ++i)
                        {
                            Assert.AreEqual(datasMonth[i].Total, listMonth[i].Total);
                            Assert.IsTrue(Math.Abs(datasMonth[i].AverageSpeed - datasMonth[i].AverageSpeed) < 0.1);
                            Assert.IsTrue(Math.Abs(datasMonth[i].HeadDistance - listMonth[i].HeadDistance) < 0.1);
                            Assert.IsTrue(Math.Abs(datasMonth[i].HeadSpace - listMonth[i].HeadSpace) < 0.1);
                            Assert.AreEqual(datasMonth[i].Occupancy, listMonth[i].Occupancy);
                            Assert.AreEqual(datasMonth[i].TimeOccupancy, listMonth[i].TimeOccupancy);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void QueryList_BySection()
        {
            LaneFlowManager_Alone service = TestInit.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<LaneFlowManager_Alone>();
            IMemoryCache memoryCache = TestInit.ServiceProvider.GetRequiredService<IMemoryCache>();
            foreach (TrafficDevice device in _devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    HashSet<string> laneIds = new HashSet<string>(
                        memoryCache.GetLanes()
                        .Where(l => l.Channel.SectionId == relation.Channel.SectionId.Value)
                        .Select(l => l.DataId));
                    var list1 = service.QueryList(laneIds, DateTimeLevel.Minute, _startDate, _startDate.AddDays(_days).AddMinutes(-1));
                    for (int i = 0; i < list1.Count; ++i)
                    {
                        int totalFlow = 0;
                        int totalDistance = 0;
                        double totalTravelTime = 0.0;
                        int totalOccupancy = 0;
                        int totalTimeOccupancy = 0;
                        double totalHeadDistance = 0.0;
                        int totalCount = 0;
                        foreach (string laneId in laneIds)
                        {
                            totalFlow += _datas[laneId][i].Total;
                            totalDistance += _datas[laneId][i].Distance;
                            totalTravelTime += _datas[laneId][i].TravelTime;
                            totalOccupancy += _datas[laneId][i].Occupancy;
                            totalTimeOccupancy += _datas[laneId][i].TimeOccupancy;
                            totalHeadDistance += _datas[laneId][i].HeadDistance;
                            totalCount += 1;
                        }
                        Assert.AreEqual(totalFlow, list1[i].Total);
                        Assert.AreEqual(totalOccupancy, list1[i].Occupancy);
                        Assert.AreEqual(totalTimeOccupancy, list1[i].TimeOccupancy);
                        Assert.AreEqual(totalHeadDistance, list1[i].HeadDistance);

                        Assert.IsTrue(Math.Abs(totalDistance / totalTravelTime * 3600 / 1000 - list1[i].AverageSpeed) < 0.1);
                        Assert.IsTrue(Math.Abs(totalHeadDistance / totalCount * (totalDistance / totalTravelTime) - list1[i].HeadSpace) < 0.1);
                    }

                    var list5 = service.QueryList(laneIds, DateTimeLevel.FiveMinutes, _startDate, _startDate.AddDays(_days).AddMinutes(-1));
                    for (int i = 0; i < list5.Count; ++i)
                    {
                        int totalFlow = 0;
                        int totalDistance = 0;
                        double totalTravelTime = 0.0;
                        int totalOccupancy = 0;
                        int totalTimeOccupancy = 0;
                        double totalHeadDistance = 0.0;
                        int totalCount = 0;
                        foreach (string laneId in laneIds)
                        {
                            var list = _datas[laneId].Where(f =>
                                f.DateTime >= _startDate.AddMinutes(5 * i) &&
                                f.DateTime < _startDate.AddMinutes(5 * (i + 1))).ToList();
                            totalFlow += list.Sum(f => f.Total);
                            totalDistance += list.Sum(f => f.Distance);
                            totalTravelTime += list.Sum(f => f.TravelTime);
                            totalOccupancy += list.Sum(f => f.Occupancy);
                            totalTimeOccupancy += list.Sum(f => f.TimeOccupancy);
                            totalHeadDistance += list.Sum(f => f.HeadDistance);
                            totalCount += list.Count;
                        }

                        Assert.AreEqual(totalFlow, list5[i].Total);
                        Assert.AreEqual(totalOccupancy, list5[i].Occupancy);
                        Assert.AreEqual(totalTimeOccupancy, list5[i].TimeOccupancy);
                        Assert.AreEqual(totalHeadDistance, list5[i].HeadDistance);

                        Assert.IsTrue(Math.Abs(totalDistance / totalTravelTime * 3600 / 1000 - list5[i].AverageSpeed) < 0.1);
                        Assert.IsTrue(Math.Abs(totalHeadDistance / totalCount * (totalDistance / totalTravelTime) - list5[i].HeadSpace) < 0.1);
                    }

                    var list15 = service.QueryList(laneIds, DateTimeLevel.FifteenMinutes, _startDate, _startDate.AddDays(_days).AddMinutes(-1));
                    for (int i = 0; i < list15.Count; ++i)
                    {
                        int totalFlow = 0;
                        int totalDistance = 0;
                        double totalTravelTime = 0.0;
                        int totalOccupancy = 0;
                        int totalTimeOccupancy = 0;
                        double totalHeadDistance = 0.0;
                        int totalCount = 0;
                        foreach (string laneId in laneIds)
                        {
                            var list = _datas[laneId].Where(f =>
                                f.DateTime >= _startDate.AddMinutes(15 * i) &&
                                f.DateTime < _startDate.AddMinutes(15 * (i + 1))).ToList();
                            totalFlow += list.Sum(f => f.Total);
                            totalDistance += list.Sum(f => f.Distance);
                            totalTravelTime += list.Sum(f => f.TravelTime);
                            totalOccupancy += list.Sum(f => f.Occupancy);
                            totalTimeOccupancy += list.Sum(f => f.TimeOccupancy);
                            totalHeadDistance += list.Sum(f => f.HeadDistance);
                            totalCount += list.Count;
                        }
                        Assert.AreEqual(totalFlow, list15[i].Total);
                        Assert.AreEqual(totalOccupancy, list15[i].Occupancy);
                        Assert.AreEqual(totalTimeOccupancy, list15[i].TimeOccupancy);
                        Assert.AreEqual(totalHeadDistance, list15[i].HeadDistance);

                        Assert.IsTrue(Math.Abs(totalDistance / totalTravelTime * 3600 / 1000 - list15[i].AverageSpeed) < 0.1);
                        Assert.IsTrue(Math.Abs(totalHeadDistance / totalCount * (totalDistance / totalTravelTime) - list15[i].HeadSpace) < 0.1);
                    }


                    var list60 = service.QueryList(laneIds, DateTimeLevel.Hour, _startDate, _startDate.AddDays(_days).AddMinutes(-1));
                    for (int i = 0; i < list60.Count; ++i)
                    {
                        int totalFlow = 0;
                        int totalDistance = 0;
                        double totalTravelTime = 0.0;
                        int totalOccupancy = 0;
                        int totalTimeOccupancy = 0;
                        double totalHeadDistance = 0.0;
                        int totalCount = 0;
                        foreach (string laneId in laneIds)
                        {
                            var list = _datas[laneId].Where(f =>
                                f.DateTime >= _startDate.AddMinutes(60 * i) &&
                                f.DateTime < _startDate.AddMinutes(60 * (i + 1))).ToList();
                            totalFlow += list.Sum(f => f.Total);
                            totalDistance += list.Sum(f => f.Distance);
                            totalTravelTime += list.Sum(f => f.TravelTime);
                            totalOccupancy += list.Sum(f => f.Occupancy);
                            totalTimeOccupancy += list.Sum(f => f.TimeOccupancy);
                            totalHeadDistance += list.Sum(f => f.HeadDistance);
                            totalCount += list.Count;
                        }
                        Assert.AreEqual(totalFlow, list60[i].Total);
                        Assert.AreEqual(totalOccupancy, list60[i].Occupancy);
                        Assert.AreEqual(totalTimeOccupancy, list60[i].TimeOccupancy);
                        Assert.AreEqual(totalHeadDistance, list60[i].HeadDistance);

                        Assert.IsTrue(Math.Abs(totalDistance / totalTravelTime * 3600 / 1000 - list60[i].AverageSpeed) < 0.1);
                        Assert.IsTrue(Math.Abs(totalHeadDistance / totalCount * (totalDistance / totalTravelTime) - list60[i].HeadSpace) < 0.1);
                    }


                    var listDay = service.QueryList(laneIds, DateTimeLevel.Day, _startDate, _startDate.AddDays(_days).AddMinutes(-1));
                    for (int i = 0; i < listDay.Count; ++i)
                    {
                        int totalFlow = 0;
                        int totalDistance = 0;
                        double totalTravelTime = 0.0;
                        int totalOccupancy = 0;
                        int totalTimeOccupancy = 0;
                        double totalHeadDistance = 0.0;
                        int totalCount = 0;
                        foreach (string laneId in laneIds)
                        {
                            var list = _datas[laneId].Where(f =>
                                f.DateTime >= _startDate.AddDays(i) &&
                                f.DateTime < _startDate.AddDays(i + 1)).ToList();
                            totalFlow += list.Sum(f => f.Total);
                            totalDistance += list.Sum(f => f.Distance);
                            totalTravelTime += list.Sum(f => f.TravelTime);
                            totalOccupancy += list.Sum(f => f.Occupancy);
                            totalTimeOccupancy += list.Sum(f => f.TimeOccupancy);
                            totalHeadDistance += list.Sum(f => f.HeadDistance);
                            totalCount += list.Count;
                        }
                        Assert.AreEqual(totalFlow, listDay[i].Total);
                        Assert.AreEqual(totalOccupancy, listDay[i].Occupancy);
                        Assert.AreEqual(totalTimeOccupancy, listDay[i].TimeOccupancy);
                        Assert.AreEqual(totalHeadDistance, listDay[i].HeadDistance);

                        Assert.IsTrue(Math.Abs(totalDistance / totalTravelTime * 3600 / 1000 - listDay[i].AverageSpeed) < 0.1);
                        Assert.IsTrue(Math.Abs(totalHeadDistance / totalCount * (totalDistance / totalTravelTime) - listDay[i].HeadSpace) < 0.1);
                    }


                    var listMonth = service.QueryList(laneIds, DateTimeLevel.Month, _startDate, _startDate.AddDays(_days).AddMinutes(-1));
                    for (int i = 0; i < listMonth.Count; ++i)
                    {
                        int totalFlow = 0;
                        int totalDistance = 0;
                        double totalTravelTime = 0.0;
                        int totalOccupancy = 0;
                        int totalTimeOccupancy = 0;
                        double totalHeadDistance = 0.0;
                        int totalCount = 0;
                        foreach (string laneId in laneIds)
                        {
                            var list = _datas[laneId].Where(f =>
                                f.DateTime >= _startDate.AddMonths(i) &&
                                f.DateTime < _startDate.AddMonths(i + 1)).ToList();
                            totalFlow += list.Sum(f => f.Total);
                            totalDistance += list.Sum(f => f.Distance);
                            totalTravelTime += list.Sum(f => f.TravelTime);
                            totalOccupancy += list.Sum(f => f.Occupancy);
                            totalTimeOccupancy += list.Sum(f => f.TimeOccupancy);
                            totalHeadDistance += list.Sum(f => f.HeadDistance);
                            totalCount += list.Count;
                        }
                        Assert.AreEqual(totalFlow, listMonth[i].Total);
                        Assert.AreEqual(totalOccupancy, listMonth[i].Occupancy);
                        Assert.AreEqual(totalTimeOccupancy, listMonth[i].TimeOccupancy);
                        Assert.AreEqual(totalHeadDistance, listMonth[i].HeadDistance);

                        Assert.IsTrue(Math.Abs(totalDistance / totalTravelTime * 3600 / 1000 - listMonth[i].AverageSpeed) < 0.1);
                        Assert.IsTrue(Math.Abs(totalHeadDistance / totalCount * (totalDistance / totalTravelTime) - listMonth[i].HeadSpace) < 0.1);
                    }
                }
            }
        }
    }
}
