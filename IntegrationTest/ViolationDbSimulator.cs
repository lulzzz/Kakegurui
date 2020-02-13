using System;
using System.Collections.Generic;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Microsoft.Extensions.DependencyInjection;
using NishinotouinYuriko.Data;
using NishinotouinYuriko.DataFlow;
using NishinotouinYuriko.Models;
using ItsukiSumeragi.Codes.Device;
using ItsukiSumeragi.Codes.Flow;
using ItsukiSumeragi.Codes.Violation;

namespace IntegrationTest
{
    /// <summary>
    /// smo流量模拟器
    /// </summary>
    public class ViolationDbSimulator
    {
        public static void CreateData(IServiceProvider serviceProvider, List<TrafficDevice> devices,List<TrafficViolation> violations,List<DateTime> startTimes, List<DateTime> endTimes, bool initDatabase = false)
        {
            if (initDatabase)
            {
                using (ViolationContext context = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<ViolationContext>())
                {
                    context.Database.EnsureDeleted();
                    context.Database.EnsureCreated();
                }
            }

            for (int i = 0; i < startTimes.Count; ++i)
            {
                DateTime minTime = TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel, startTimes[i]);
                DateTime maxTime = TimePointConvert.NextTimePoint(BranchDbConvert.DateLevel, minTime);
                ViolationBranchBlock branch = new ViolationBranchBlock(serviceProvider);
                branch.Open(minTime, maxTime);
                for (DateTime date = startTimes[i]; date <= endTimes[i]; date = date.AddDays(1))
                {
                    for (int h = 0; h < 24; h += 1)
                    {
                        DateTime startTime = startTimes[i].AddHours(h);
                        foreach (TrafficDevice device in devices)
                        {
                            foreach (var relation in device.Device_Channels)
                            {
                                foreach (TrafficViolation violation in violations.GetRange(0,4))
                                {
                                    foreach(int targetType in Enum.GetValues(typeof(TargetType)))
                                    {
                                        if (targetType == (int) TargetType.小型车)
                                        {
                                            foreach (int carType in new List<int>
                                            {
                                                (int)CarType.轿车,
                                                (int)CarType.微面
                                            })
                                            {
                                                foreach (int direction in Enum.GetValues(typeof(ChannelDirection)))
                                                {
                                                    ViolationStruct violationStruct = new ViolationStruct
                                                    {
                                                        DataId = relation.ChannelId,
                                                        DateTime = new DateTime(startTime.Year, startTime.Month, startTime.Day,
                                                            startTime.Hour, startTime.Minute, 0),
                                                        LocationId = relation.Channel.LocationId.Value,
                                                        ViolationId = violation.ViolationId,
                                                        CarType = carType,
                                                        Direction = direction,
                                                        PlateNumber = "京A00001",
                                                        TargetType = targetType
                                                    };
                                                    branch.Post(violationStruct);
                                                }
                                            }
                                        }
                                        else if (targetType == (int) TargetType.大型车)
                                        {
                                            foreach (int carType in new List<int>
                                            {
                                                (int)CarType.大型客车,
                                                (int)CarType.大型货车
                                            })
                                            {
                                                foreach (int direction in Enum.GetValues(typeof(ChannelDirection)))
                                                {
                                                    ViolationStruct violationStruct = new ViolationStruct
                                                    {
                                                        DataId = relation.ChannelId,
                                                        DateTime = new DateTime(startTime.Year, startTime.Month, startTime.Day,
                                                            startTime.Hour, startTime.Minute, 0),
                                                        LocationId = relation.Channel.LocationId.Value,
                                                        ViolationId = violation.ViolationId,
                                                        CarType = carType,
                                                        Direction = direction,
                                                        PlateNumber = "京A00001",
                                                        TargetType = targetType
                                                    };
                                                    branch.Post(violationStruct);
                                                }
                                            }
                                        }
                                        else if (targetType == (int)TargetType.非机动车)
                                        {
                                            foreach (int carType in new List<int>
                                            {
                                                (int)CarType.三轮车,
                                                (int)CarType.随车吊
                                            })
                                            {
                                                foreach (int direction in Enum.GetValues(typeof(ChannelDirection)))
                                                {
                                                    ViolationStruct violationStruct = new ViolationStruct
                                                    {
                                                        DataId = relation.ChannelId,
                                                        DateTime = new DateTime(startTime.Year, startTime.Month, startTime.Day,
                                                            startTime.Hour, startTime.Minute, 0),
                                                        LocationId = relation.Channel.LocationId.Value,
                                                        ViolationId = violation.ViolationId,
                                                        CarType = carType,
                                                        Direction = direction,
                                                        PlateNumber = "京A00001",
                                                        TargetType = targetType
                                                    };
                                                    branch.Post(violationStruct);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                }
                branch.Close();

            }

        }



    }
}
