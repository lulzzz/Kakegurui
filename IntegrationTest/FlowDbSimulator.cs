﻿using System;
using System.Collections.Generic;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Microsoft.Extensions.DependencyInjection;
using MomobamiKirari.Data;
using MomobamiKirari.DataFlow;
using MomobamiKirari.Models;
using ItsukiSumeragi.Codes.Flow;
using Kakegurui.Log;
using Microsoft.Extensions.Logging;

namespace IntegrationTest
{
    /// <summary>
    /// smo流量模拟器
    /// </summary>
    public class FlowDbSimulator
    {
        public static void ResetDatabase(IServiceProvider serviceProvider)
        {
            using (FlowContext context = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<FlowContext>())
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }
        }

        public static Dictionary<string, List<LaneFlow>> CreateData(IServiceProvider serviceProvider, List<TrafficDevice> devices, List<DataCreateMode> modes, List<DateTime> startTimes, List<DateTime> endTimes, bool initDatabase = false)
        {
            if (initDatabase)
            {
                ResetDatabase(serviceProvider);
            }
            Dictionary<string, List<LaneFlow>> datas = new Dictionary<string, List<LaneFlow>>();
            foreach (TrafficDevice device in devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (TrafficLane lane in relation.Channel.Lanes)
                    {
                        datas.Add(lane.DataId, new List<LaneFlow>());
                    }
                }
            }
            for (int i = 0; i < startTimes.Count; ++i)
            {
                DateTime minTime = TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel, startTimes[i]);
                DateTime maxTime = TimePointConvert.NextTimePoint(BranchDbConvert.DateLevel, minTime);
                FlowBranchBlock branch = new FlowBranchBlock(serviceProvider);
                branch.Open(devices, minTime, maxTime);
                Random random = new Random();
                foreach (TrafficDevice device in devices)
                {
                    foreach (var relation in device.Device_Channels)
                    {
                        foreach (TrafficLane lane in relation.Channel.Lanes)
                        {
                            int value = 1;
                            for (int m = 0; m < 1440; ++m)
                            {
                                DateTime dataTime = startTimes[i].AddMinutes(m);
                                if (dataTime > DateTime.Now)
                                {
                                    break;
                                }
                                if (dataTime >= startTimes[i] && dataTime < endTimes[i])
                                {
                                    LaneFlow laneFlow;
                                    if (modes[i] == DataCreateMode.Fixed)
                                    {
                                        laneFlow = new LaneFlow
                                        {
                                            DataId = lane.DataId,
                                            DateTime = new DateTime(dataTime.Year, dataTime.Month, dataTime.Day, dataTime.Hour, dataTime.Minute, 0),
                                            Cars = 1,
                                            Buss = 1,
                                            Trucks = 1,
                                            Vans = 1,
                                            Tricycles = 1,
                                            Motorcycles = 1,
                                            Bikes = 1,
                                            Persons = 1,
                                            AverageSpeedData = 50,
                                            HeadDistance = 10,
                                            Occupancy = 30,
                                            TimeOccupancy = 40,
                                            TrafficStatus = TrafficStatus.轻度拥堵,
                                            Count = 1

                                        };
                                    }
                                    else if (modes[i] == DataCreateMode.Sequence)
                                    {
                                        laneFlow = new LaneFlow
                                        {
                                            DataId = lane.DataId,
                                            DateTime = new DateTime(dataTime.Year, dataTime.Month, dataTime.Day, dataTime.Hour, dataTime.Minute, 0),
                                            Cars = value++,
                                            Buss = 2,
                                            Trucks = 3,
                                            Vans = 4,
                                            Tricycles = 5,
                                            Motorcycles = 6,
                                            Bikes = 7,
                                            Persons = 8,
                                            AverageSpeedData = 9,
                                            HeadDistance = 10,
                                            Occupancy = 12,
                                            TimeOccupancy = 13,
                                            TrafficStatus = TrafficStatus.通畅,
                                            Count = 1
                                        };
                                    }
                                    else
                                    {
                                        laneFlow = new LaneFlow
                                        {
                                            DataId = lane.DataId,
                                            DateTime = new DateTime(dataTime.Year, dataTime.Month, dataTime.Day, dataTime.Hour, dataTime.Minute, 0),
                                            Cars = random.Next(1, 10),
                                            Buss = random.Next(1, 10),
                                            Trucks = random.Next(1, 10),
                                            Vans = random.Next(1, 10),
                                            Tricycles = random.Next(1, 10),
                                            Motorcycles = random.Next(1, 10),
                                            Bikes = random.Next(1, 10),
                                            Persons = random.Next(1, 10),
                                            AverageSpeedData = random.Next(1,10),
                                            HeadDistance = random.Next(1, 10),
                                            Occupancy = random.Next(1, 10),
                                            TimeOccupancy = random.Next(1, 10),
                                            TrafficStatus = (TrafficStatus)random.Next(1,6),
                                            Count = 1
                                        };
                                    }
                                    
                                    laneFlow.SectionId = lane.Channel.RoadSection.SectionId;
                                    laneFlow.SectionType = lane.Channel.RoadSection.SectionType;
                                    laneFlow.SectionLength = lane.Channel.RoadSection.Length;
                                    laneFlow.FreeSpeed = lane.Channel.RoadSection.FreeSpeed;
                                    laneFlow.Distance = laneFlow.Vehicle * lane.Length;
                                    laneFlow.TravelTime = laneFlow.AverageSpeedData > 0
                                        ? laneFlow.Vehicle * lane.Length / Convert.ToDouble(laneFlow.AverageSpeedData * 1000 / 3600)
                                        : 0;
                                    datas[lane.DataId].Add(laneFlow);
                                    branch.Post(laneFlow);
                                }
                                ++value;
                            }
                        }
                    }

                }

                branch.Close();
                if (i == startTimes.Count - 1)
                {
                    DateTime currentTime = TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel);
                    if (minTime != currentTime)
                    {
                        using (FlowContext context = serviceProvider.GetRequiredService<FlowContext>())
                        {
                            context.ChangeDatabase(BranchDbConvert.GetTableName(minTime));
                        }
                        branch.SwitchBranch(maxTime, TimePointConvert.NextTimePoint(BranchDbConvert.DateLevel, maxTime));
                    }
                }
                else
                {
                    DateTime nextItem = TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel, startTimes[i + 1]);
                    if (minTime != nextItem)
                    {
                        using (FlowContext context = serviceProvider.GetRequiredService<FlowContext>())
                        {
                            context.ChangeDatabase(BranchDbConvert.GetTableName(minTime));
                        }
                        branch.SwitchBranch(maxTime, TimePointConvert.NextTimePoint(BranchDbConvert.DateLevel, maxTime));
                    }
                }
            }

            return datas;
        }

        public static Dictionary<string, List<LaneFlow>> CreateData(IServiceProvider serviceProvider, List<TrafficDevice> devices, DataCreateMode mode, List<DateTime> dates, bool initDatabase = false)
        {
            List<DateTime> startTimes = new List<DateTime>();
            List<DateTime> endTimes = new List<DateTime>();
            List<DataCreateMode> modes = new List<DataCreateMode>();
            foreach (DateTime date in dates)
            {
                startTimes.Add(date);
                endTimes.Add(date.AddDays(1));
                modes.Add(mode);
            }

            return CreateData(serviceProvider, devices, modes, startTimes, endTimes, initDatabase);
        }

        public static Dictionary<string, List<LaneFlow>> CreateData(IServiceProvider serviceProvider, List<TrafficDevice> devices, DataCreateMode mode, DateTime startDate, DateTime endDate, bool initDatabase = false)
        {
            List<DateTime> startTimes = new List<DateTime>();
            List<DateTime> endTimes = new List<DateTime>();
            List<DataCreateMode> modes = new List<DataCreateMode>();
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                startTimes.Add(date);
                endTimes.Add(date.AddDays(1));
                modes.Add(mode);
            }

            return CreateData(serviceProvider, devices, modes, startTimes, endTimes, initDatabase);
        }

        public static Dictionary<string, List<LaneFlow>> CreateData(IServiceProvider serviceProvider, List<TrafficDevice> devices, DataCreateMode mode, DateTime day, bool initDatabase = false)
        {
            List<DateTime> startTimes = new List<DateTime> { day };
            List<DateTime> endTimes = new List<DateTime> { day.AddDays(1) };
            List<DataCreateMode> modes = new List<DataCreateMode> { mode };
            return CreateData(serviceProvider, devices, modes, startTimes, endTimes, initDatabase);
        }

        public static void CreateData(IServiceProvider serviceProvider, List<TrafficDevice> devices, DateTime startDate,
            int minutes)
        {
            using (FlowContext context =
                serviceProvider.CreateScope().ServiceProvider.GetRequiredService<FlowContext>())
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }

            for (int m = 0; m < minutes; ++m)
            {
                DateTime dataTime = startDate.AddMinutes(m);
                using (FlowContext context =
                    serviceProvider.CreateScope().ServiceProvider.GetRequiredService<FlowContext>())
                {
                    foreach (TrafficDevice device in devices)
                    {
                        foreach (var relation in device.Device_Channels)
                        {
                            foreach (TrafficLane lane in relation.Channel.Lanes)
                            {
                        
                                LaneFlow_One laneFlow = new LaneFlow_One
                                {
                                    DataId = lane.DataId,
                                    DateTime = new DateTime(dataTime.Year, dataTime.Month, dataTime.Day, dataTime.Hour,
                                        dataTime.Minute, 0),
                                    Cars = 1,
                                    Buss = 1,
                                    Trucks = 1,
                                    Vans = 1,
                                    Tricycles = 1,
                                    Motorcycles = 1,
                                    Bikes = 1,
                                    Persons = 1,
                                    AverageSpeedData = 50,
                                    HeadDistance = 10,
                                    Occupancy = 30,
                                    TimeOccupancy = 40,
                                    TrafficStatus = TrafficStatus.轻度拥堵,
                                    Count = 1
                                };
                                context.LaneFlows_One.Add(laneFlow);

                                if ((m+1) % 5 == 0)
                                {
                                    LaneFlow_Five laneFlow5 = new LaneFlow_Five
                                    {
                                        DataId = lane.DataId,
                                        DateTime = new DateTime(dataTime.Year, dataTime.Month, dataTime.Day, dataTime.Hour,
                                            dataTime.Minute, 0),
                                        Cars = 5,
                                        Buss = 5,
                                        Trucks = 5,
                                        Vans = 5,
                                        Tricycles = 5,
                                        Motorcycles = 5,
                                        Bikes = 5,
                                        Persons = 5,
                                        AverageSpeedData = 50,
                                        HeadDistance = 10,
                                        Occupancy = 30,
                                        TimeOccupancy = 40,
                                        TrafficStatus = TrafficStatus.轻度拥堵,
                                        Count = 5
                                    };
                                    context.LaneFlows_Five.Add(laneFlow5);
                                }

                                if ((m + 1) % 15 == 0)
                                {
                                    LaneFlow_Fifteen laneFlow15 = new LaneFlow_Fifteen
                                    {
                                        DataId = lane.DataId,
                                        DateTime = new DateTime(dataTime.Year, dataTime.Month, dataTime.Day, dataTime.Hour,
                                            dataTime.Minute, 0),
                                        Cars = 15,
                                        Buss = 15,
                                        Trucks = 15,
                                        Vans = 15,
                                        Tricycles = 15,
                                        Motorcycles = 15,
                                        Bikes = 15,
                                        Persons = 15,
                                        AverageSpeedData = 50,
                                        HeadDistance = 10,
                                        Occupancy = 30,
                                        TimeOccupancy = 40,
                                        TrafficStatus = TrafficStatus.轻度拥堵,
                                        Count = 15
                                    };
                                    context.LaneFlows_Fifteen.Add(laneFlow15);
                                }

                                if ((m + 1) % 60 == 0)
                                {
                                    LaneFlow_Hour laneFlow60 = new LaneFlow_Hour
                                    {
                                        DataId = lane.DataId,
                                        DateTime = new DateTime(dataTime.Year, dataTime.Month, dataTime.Day, dataTime.Hour,
                                            0, 0),
                                        Cars = 60,
                                        Buss = 60,
                                        Trucks = 60,
                                        Vans = 60,
                                        Tricycles = 60,
                                        Motorcycles = 60,
                                        Bikes = 60,
                                        Persons = 60,
                                        AverageSpeedData = 50,
                                        HeadDistance = 10,
                                        Occupancy = 30,
                                        TimeOccupancy = 40,
                                        TrafficStatus = TrafficStatus.轻度拥堵,
                                        Count = 60
                                    };
                                    context.LaneFlows_Hour.Add(laneFlow60);
                                }
                            }
                        }
                    }
                    context.BulkSaveChanges(options => options.BatchSize = 1000);
                }
                LogPool.Logger.LogInformation($"{dataTime:yyyy-MM-dd HH:mm:ss}");
            }
        }
    }
}