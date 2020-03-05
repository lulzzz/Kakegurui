using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using MomobamiRirika.Data;
using MomobamiRirika.DataFlow;
using MomobamiRirika.Models;

namespace IntegrationTest
{
    public class EventDbSimulator
    {
        public static void ResetDatabase(IServiceProvider serviceProvider)
        {
            using (DensityContext context = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DensityContext>())
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }
        }

        public static Dictionary<TrafficEvent, int> CreateData(IServiceProvider serviceProvider,List<DensityDevice> devices, DateTime startDate, DateTime endDate,DataCreateMode mode,bool initDatabase=false)
        {
            if (initDatabase)
            {
                ResetDatabase(serviceProvider);
            }

            using (IServiceScope serviceScope = serviceProvider.CreateScope())
            {
                using (DensityContext context = serviceScope.ServiceProvider.GetRequiredService<DensityContext>())
                {

                    Dictionary<TrafficEvent, int> result = new Dictionary<TrafficEvent, int>();
                    Random random = new Random();

                    int hours = Convert.ToInt32((endDate - startDate).TotalHours + 24);
                    foreach (DensityDevice device in devices)
                    {
                        foreach (var relation in device.DensityDevice_DensityChannels)
                        {
                            foreach (TrafficRegion region in relation.Channel.Regions)
                            {
                                if (mode == DataCreateMode.Fixed)
                                {
                                    for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                                    {
                                        for (int h = 0; h < 24; ++h)
                                        {
                                            TrafficEvent trafficEvent1 = new TrafficEvent
                                            {
                                                DataId = region.DataId,
                                                DateTime = date.AddHours(h),
                                                EndTime = date.AddHours(h).AddMinutes(1)
                                            };
                                            context.Events.Add(trafficEvent1);
                                            TrafficEvent trafficEvent2 = new TrafficEvent
                                            {
                                                DataId = region.DataId,
                                                DateTime = date.AddHours(h).AddMinutes(30),
                                                EndTime = date.AddHours(h).AddMinutes(30).AddMinutes(1)
                                            };
                                            context.Events.Add(trafficEvent2);
                                        }
                                    }
                                }
                                else if (mode == DataCreateMode.Random)
                                {
                                    int value = random.Next(1, hours);
                                    result.Add(new TrafficEvent
                                    {
                                        DataId = region.DataId
                                    }, value);
                                    for (int h = 0; h < value; ++h)
                                    {
                                        TrafficEvent trafficEvent = new TrafficEvent
                                        {
                                            DataId = region.DataId,
                                            DateTime = startDate.AddHours(h),
                                            EndTime = startDate.AddHours(h).AddMinutes(1)
                                        };
                                        context.Events.Add(trafficEvent);
                                    }
                                }
                            }
                        }
                    }

                    context.BulkSaveChanges(options => options.BatchSize = 1000);
                    return result;
                }
            }
        }


        public static void CreateData(IServiceProvider serviceProvider, List<DensityDevice> devices, DateTime startDate, bool initDatabase = false)
        {
            if (initDatabase)
            {
                ResetDatabase(serviceProvider);
            }

            using (IServiceScope serviceScope = serviceProvider.CreateScope())
            {
                EventBranchBlock branch = new EventBranchBlock(serviceScope.ServiceProvider);
                branch.Open(devices);
                int h = 0;
                foreach (DensityDevice device in devices)
                {
                    foreach (var relation in device.DensityDevice_DensityChannels)
                    {
                        foreach (TrafficRegion region in relation.Channel.Regions)
                        {
                            branch.Post(new TrafficEvent
                            {
                                MatchId = $"{device.Ip}_{relation.Channel.ChannelIndex}_{region.RegionIndex}",
                                DateTime = startDate.AddHours(h)
                            });
                            ++h;
                        }
                    }
                }
                
                branch.Close();

            }
        }

    }
}
