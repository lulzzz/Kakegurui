using System;
using System.Collections.Generic;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Microsoft.Extensions.DependencyInjection;
using MomobamiRirika.Data;
using MomobamiRirika.DataFlow;
using MomobamiRirika.Models;

namespace IntegrationTest
{
    public class DensityDbSimulator
    {
        public static void ResetDatabase(IServiceProvider serviceProvider)
        {
            using (DensityContext context = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DensityContext>())
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }
        }

        public static Dictionary<TrafficRegion, int> CreateData(IServiceProvider serviceProvider, List<TrafficDevice> devices, List<DataCreateMode> modes, List<DateTime> startTimes, List<DateTime> endTimes, bool initDatabase = false)
        {
            if (initDatabase)
            {
                ResetDatabase(serviceProvider);
            }



            Dictionary<TrafficRegion, int> regions = new Dictionary<TrafficRegion, int>();

            for(int i=0;i<startTimes.Count;++i)
            {
                DateTime minTime = TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel, startTimes[i]);
                DateTime maxTime = TimePointConvert.NextTimePoint(BranchDbConvert.DateLevel, minTime);
                DensityBranchBlock branch = new DensityBranchBlock(serviceProvider);
                branch.Open(devices, minTime, maxTime);
                Random random = new Random();
                foreach (TrafficDevice device in devices)
                {
                    foreach (var relation in device.Device_Channels)
                    {
                        foreach (TrafficRegion region in relation.Channel.Regions)
                        {
                            int randomValue = random.Next(1, 1000);
                            if (modes[i] == DataCreateMode.Random)
                            {
                                regions[region] = randomValue;
                            }

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
                                    TrafficDensity density;
                                    if (modes[i] == DataCreateMode.Fixed)
                                    {
                                        density = new TrafficDensity
                                        {
                                            MatchId = $"{device.Ip}_{relation.Channel.ChannelIndex}_{region.RegionIndex}",
                                            DateTime = new DateTime(dataTime.Year, dataTime.Month, dataTime.Day, dataTime.Hour, dataTime.Minute, 0),
                                            Value = 1
                                        };
                                    }
                                    else if (modes[i] == DataCreateMode.Sequence)
                                    {
                                        density = new TrafficDensity
                                        {
                                            MatchId = $"{device.Ip}_{relation.Channel.ChannelIndex}_{region.RegionIndex}",
                                            DateTime = new DateTime(dataTime.Year, dataTime.Month, dataTime.Day, dataTime.Hour, dataTime.Minute, 0),
                                            Value = value
                                        };
                                    }
                                    else
                                    {
                                        density = new TrafficDensity
                                        {
                                            MatchId = $"{device.Ip}_{relation.Channel.ChannelIndex}_{region.RegionIndex}",
                                            DateTime = new DateTime(dataTime.Year, dataTime.Month, dataTime.Day, dataTime.Hour, dataTime.Minute, 0),
                                            Value = randomValue
                                        };
                                    }
                                    branch.Post(density);
                                }
                                ++value;
                            }
                        }
                    }
  
                }
                
                branch.Close();
                if (i==startTimes.Count-1)
                {
                    DateTime currentTime = TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel);
                    if (minTime != currentTime)
                    {
                        using (DensityContext context = serviceProvider.GetRequiredService<DensityContext>())
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
                        using (DensityContext context = serviceProvider.GetRequiredService<DensityContext>())
                        {
                            context.ChangeDatabase(BranchDbConvert.GetTableName(minTime));
                        }
                        branch.SwitchBranch(maxTime, TimePointConvert.NextTimePoint(BranchDbConvert.DateLevel, maxTime));
                    }
                }
            }

            return regions;
        }

        public static void CreateData(IServiceProvider serviceProvider, List<TrafficDevice> devices, DataCreateMode mode, List<DateTime> dates, bool initDatabase = false)
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

            CreateData(serviceProvider, devices, modes, startTimes, endTimes, initDatabase);
        }

        public static void CreateData(IServiceProvider serviceProvider,List<TrafficDevice> devices, DataCreateMode mode, DateTime startDate, DateTime endDate, bool initDatabase = false)
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

            CreateData(serviceProvider,devices,modes,startTimes, endTimes,initDatabase);
        }

        public static void CreateData(IServiceProvider serviceProvider,List<TrafficDevice> devices, DataCreateMode mode, DateTime day, bool initDatabase = false)
        {
            List<DateTime> startTimes = new List<DateTime> {day};
            List<DateTime> endTimes = new List<DateTime> {day.AddDays(1)};
            List<DataCreateMode> modes = new List<DataCreateMode> {mode};
            CreateData(serviceProvider, devices, modes, startTimes,endTimes, initDatabase);
        }

    }
}
