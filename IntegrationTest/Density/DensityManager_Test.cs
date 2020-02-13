using System;
using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Data;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MomobamiRirika.Controllers;
using MomobamiRirika.Data;
using MomobamiRirika.Models;

namespace IntegrationTest.Density
{
    [TestClass]
    public class DensityManager_Test
    {
        [TestMethod]
        public void QueryList()
        {
            DateTime startDate = new DateTime(2019, 5, 10);
            DateTime endDate = new DateTime(2019,5,11);
            int days = Convert.ToInt32((endDate - startDate).TotalDays+1);
            int months = startDate.Month == endDate.Month ? 1 : 2;
            List<TrafficDevice> devices = DeviceDbSimulator.CreateDensityDevice(TestInit.ServiceProvider, 1, 1, 2, "127.0.0.1", true);
            DensityDbSimulator.CreateData(TestInit.ServiceProvider, devices, DataCreateMode.Sequence, startDate, endDate, true);
            DensitiesController controller = new DensitiesController(
                TestInit.ServiceProvider.GetRequiredService<DensityContext>(),
                TestInit.ServiceProvider.GetRequiredService<IMemoryCache>());
            int average = 720;
            foreach (TrafficDevice device in devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (TrafficRegion region in relation.Channel.Regions)
                    {
                        var oneMinuteList = controller.QueryList(region.DataId, DateTimeLevel.Minute, startDate, endDate.AddDays(1).AddMinutes(-1));
                        //验证查询数量
                        Assert.AreEqual(24 * 60 * days, oneMinuteList.Count);
                        //验证平均密度
                        Assert.AreEqual(average, Convert.ToInt32(oneMinuteList.Average(d => d.Value)));

                        var fiveMinuteList = controller.QueryList(region.DataId, DateTimeLevel.FiveMinutes, startDate, endDate.AddDays(1).AddMinutes(-5));
                        Assert.AreEqual(24 * 60 / 5 * days, fiveMinuteList.Count);
                        Assert.AreEqual(average, Convert.ToInt32(fiveMinuteList.Average(d => d.Value)));

                        var fifteenMinuteList = controller.QueryList(region.DataId, DateTimeLevel.FifteenMinutes, startDate, endDate.AddDays(1).AddMinutes(-15));
                        Assert.AreEqual(24 * 60 / 15 * days, fifteenMinuteList.Count);
                        Assert.AreEqual(average, Convert.ToInt32(fifteenMinuteList.Average(d => d.Value)));

                        var sixtyMinuteList = controller.QueryList(region.DataId, DateTimeLevel.Hour, startDate, endDate.AddDays(1).AddHours(-1));
                        Assert.AreEqual(24 * days, sixtyMinuteList.Count);
                        Assert.AreEqual(average, Convert.ToInt32(sixtyMinuteList.Average(d => d.Value)));

                        var dayList = controller.QueryList(region.DataId, DateTimeLevel.Day, startDate, endDate);
                        Assert.AreEqual(days, dayList.Count);
                        Assert.AreEqual(average, Convert.ToInt32(dayList.Average(d => d.Value)));

                        var monthList = controller.QueryList(region.DataId, DateTimeLevel.Month, TimePointConvert.CurrentTimePoint(DateTimeLevel.Month,startDate), TimePointConvert.CurrentTimePoint(DateTimeLevel.Month, endDate));
                        Assert.AreEqual(months, monthList.Count);
                        Assert.AreEqual(average, Convert.ToInt32(monthList.Average(d => d.Value)));
                    }
                }
            }
        }

        [TestMethod]
        public void QueryComparison()
        {
            //创建某天的模拟数据
            DateTime startDate = new DateTime(2019,7,1);
            List<TrafficDevice> devices = DeviceDbSimulator.CreateDensityDevice(TestInit.ServiceProvider, 1, 1, 2, "", true);
            List<DateTime> dates = new List<DateTime>
            {
                startDate.AddYears(-1),
                startDate.AddMonths(-1),
                startDate.AddDays(-1),
                startDate
            };
            DensityDbSimulator.CreateData(TestInit.ServiceProvider, devices, DataCreateMode.Sequence, dates, true);
            DensitiesController controller = new DensitiesController(
                TestInit.ServiceProvider.GetRequiredService<DensityContext>(),
                TestInit.ServiceProvider.GetRequiredService<IMemoryCache>());
            foreach (TrafficDevice device in devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (TrafficRegion region in relation.Channel.Regions)
                    {
                        //验证5分钟
                        var fiveCharts = controller.QueryComparison(region.DataId, DateTimeLevel.FiveMinutes, startDate, startDate.AddDays(1).AddMinutes(-5));
                        for(int i=0;i<fiveCharts.Count;++i)
                        {
                            fiveCharts[i] = fiveCharts[i].OrderBy(c => c.Axis).ToList();
                        }
                        //验证同比环比的图表时间对其到第一个时间段
                        Assert.AreEqual(startDate, fiveCharts[0][0].Axis);
                        Assert.AreEqual(startDate, fiveCharts[1][0].Axis);
                        Assert.AreEqual(startDate, fiveCharts[2][0].Axis);

                        //验证同比环比的remark是真实的时间
                        Assert.AreEqual(startDate.ToString("yyyy-MM-dd HH:mm"), fiveCharts[0][0].Remark);
                        Assert.AreEqual(startDate.AddDays(-1).ToString("yyyy-MM-dd HH:mm"), fiveCharts[1][0].Remark);
                        Assert.AreEqual(startDate.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm"), fiveCharts[2][0].Remark);
                        //验证图表中的数据数量
                        foreach (List<TrafficChart<DateTime, int>> charts in fiveCharts)
                        {
                            Assert.AreEqual(24 * 60 / 5, charts.Count);
                        }

                        var fifteenCharts = controller.QueryComparison(region.DataId, DateTimeLevel.FifteenMinutes, startDate, startDate.AddDays(1).AddMinutes(-15));
                        for (int i = 0; i < fifteenCharts.Count; ++i)
                        {
                            fifteenCharts[i] = fifteenCharts[i].OrderBy(c => c.Axis).ToList();
                        }
                        Assert.AreEqual(startDate, fifteenCharts[0][0].Axis);
                        Assert.AreEqual(startDate, fifteenCharts[1][0].Axis);
                        Assert.AreEqual(startDate, fifteenCharts[2][0].Axis);

                        Assert.AreEqual(startDate.ToString("yyyy-MM-dd HH:mm"), fifteenCharts[0][0].Remark);
                        Assert.AreEqual(startDate.AddDays(-1).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[1][0].Remark);
                        Assert.AreEqual(startDate.AddMinutes(-15).ToString("yyyy-MM-dd HH:mm"), fifteenCharts[2][0].Remark);

                        foreach (List<TrafficChart<DateTime, int>> charts in fifteenCharts)
                        {
                            Assert.AreEqual(24 * 60 / 15, charts.Count);
                        }

                        var hourCharts = controller.QueryComparison(region.DataId, DateTimeLevel.Hour, startDate, startDate.AddDays(1).AddHours(-1));
                        for (int i = 0; i < hourCharts.Count; ++i)
                        {
                            hourCharts[i] = hourCharts[i].OrderBy(c => c.Axis).ToList();
                        }
                        Assert.AreEqual(startDate, hourCharts[0][0].Axis);
                        Assert.AreEqual(startDate, hourCharts[1][0].Axis);
                        Assert.AreEqual(startDate, hourCharts[2][0].Axis);

                        Assert.AreEqual(startDate.ToString("yyyy-MM-dd HH"), hourCharts[0][0].Remark);
                        Assert.AreEqual(startDate.AddDays(-1).ToString("yyyy-MM-dd HH"), hourCharts[1][0].Remark);
                        Assert.AreEqual(startDate.AddHours(-1).ToString("yyyy-MM-dd HH"), hourCharts[2][0].Remark);

                        foreach (List<TrafficChart<DateTime, int>> charts in hourCharts)
                        {
                            Assert.AreEqual(24, charts.Count);
                        }

                        var dayCharts = controller.QueryComparison(region.DataId, DateTimeLevel.Day, startDate, startDate);
                                  
                        for (int i = 0; i < dayCharts.Count; ++i)
                        {
                            dayCharts[i] = dayCharts[i].OrderBy(c => c.Axis).ToList();
                        }
                        Assert.AreEqual(startDate, dayCharts[0][0].Axis);
                        Assert.AreEqual(startDate, dayCharts[1][0].Axis);
                        Assert.AreEqual(startDate, dayCharts[2][0].Axis);

                        Assert.AreEqual(startDate.ToString("yyyy-MM-dd"), dayCharts[0][0].Remark);
                        Assert.AreEqual(startDate.AddMonths(-1).ToString("yyyy-MM-dd"), dayCharts[1][0].Remark);
                        Assert.AreEqual(startDate.AddDays(-1).ToString("yyyy-MM-dd"), dayCharts[2][0].Remark);

                        foreach (List<TrafficChart<DateTime, int>> charts in dayCharts)
                        {
                            Assert.AreEqual(1, charts.Count);
                        }

                        var monthCharts = controller.QueryComparison(region.DataId, DateTimeLevel.Month,TimePointConvert.CurrentTimePoint(DateTimeLevel.Month,startDate), TimePointConvert.CurrentTimePoint(DateTimeLevel.Month, startDate));
                        for (int i = 0; i < monthCharts.Count; ++i)
                        {
                            monthCharts[i] = monthCharts[i].OrderBy(c => c.Axis).ToList();
                        }
                        Assert.AreEqual(TimePointConvert.CurrentTimePoint(DateTimeLevel.Month, startDate), monthCharts[0][0].Axis);
                        Assert.AreEqual(TimePointConvert.CurrentTimePoint(DateTimeLevel.Month, startDate), monthCharts[1][0].Axis);
                        Assert.AreEqual(TimePointConvert.CurrentTimePoint(DateTimeLevel.Month, startDate), monthCharts[2][0].Axis);

                        Assert.AreEqual(startDate.ToString("yyyy-MM"), monthCharts[0][0].Remark);
                        Assert.AreEqual(startDate.AddYears(-1).ToString("yyyy-MM"), monthCharts[1][0].Remark);
                        Assert.AreEqual(startDate.AddMonths(-1).ToString("yyyy-MM"), monthCharts[2][0].Remark);

                        foreach (List<TrafficChart<DateTime, int>> charts in monthCharts)
                        {
                            Assert.AreEqual(1, charts.Count);
                        }
                    }
                }
            }

        }

        [TestMethod]
        public void QueryDayTop10()
        {
            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            
            List<TrafficDevice> devices = DeviceDbSimulator.CreateDensityDevice(TestInit.ServiceProvider, 1, 1, 12, "", true);
            DensityDbSimulator.ResetDatabase(TestInit.ServiceProvider);
            TestInit.RefreshDensityCache(devices);
            DensitiesController controller = new DensitiesController(
                TestInit.ServiceProvider.GetRequiredService<DensityContext>(),
                TestInit.ServiceProvider.GetRequiredService<IMemoryCache>());

            var regions = DensityDbSimulator.CreateData(TestInit.ServiceProvider,devices
                ,new List<DataCreateMode>{DataCreateMode.Fixed,DataCreateMode.Fixed,DataCreateMode.Random}
                ,new List<DateTime>{today.AddDays(-1),now.AddHours(-2).AddMinutes(30),now.AddHours(-1).AddMinutes(30)}
                ,new List<DateTime>{today.AddDays(-1).AddMinutes(1),now.AddHours(-2).AddMinutes(31), now.AddHours(-1).AddMinutes(31) }
                ,true);

            var densities = regions.OrderByDescending(p => p.Value).ToList();
    
            var dayTop10 = controller.QueryDayTop10();
            for (int i = 0; i < dayTop10.Count; ++i)
            {
                Assert.AreEqual(densities[i].Key.DataId,dayTop10[i].DataId);
                //今天
                Assert.AreEqual(Math.Ceiling((densities[i].Value + 1) / 2.0), dayTop10[i].Value);
            }

            var hourTop10 = controller.QueryHourTop10();
            for (int i = 0; i < hourTop10.Count; ++i)
            {
                Assert.AreEqual(densities[i].Key.DataId, hourTop10[i].DataId);
                //本小时
                Assert.AreEqual(densities[i].Value, hourTop10[i].Value);
            }

            var dayChangeTop10 = controller.QueryChangeDayTop10();
            for (int i = 0; i < dayChangeTop10.Count; ++i)
            {
                Assert.AreEqual(densities[i].Key.DataId, dayChangeTop10[i].DataId);
                //今天
                Assert.AreEqual(Math.Ceiling((densities[i].Value + 1) / 2.0), dayChangeTop10[i].Value);
                //昨天
                Assert.AreEqual(1, dayChangeTop10[i].LastValue);
            }

            var hourChangeTop10 = controller.QueryChangeHourTop10();
            for (int i = 0; i < hourChangeTop10.Count; ++i)
            {
                Assert.AreEqual(densities[i].Key.DataId, hourChangeTop10[i].DataId);
                //本小时
                Assert.AreEqual(hourTop10[i].Value, hourChangeTop10[i].Value);
                //上小时
                Assert.AreEqual(1, hourChangeTop10[i].LastValue);
            }
        }

        [TestMethod]
        public void QueryVipRegions()
        {
            List<TrafficDevice> devices = new List<TrafficDevice>();
            int deviceCount = 1;
            int channelCount = 1;
            int regionCount = 12;
            HashSet<string> vips = new HashSet<string>();
            //随机创建重点区域
            Random random = new Random();
            using (IServiceScope serviceScope = TestInit.ServiceProvider.CreateScope())
            {
                using (DeviceContext context = serviceScope.ServiceProvider.GetRequiredService<DeviceContext>())
                {
                    context.Database.EnsureDeleted();
                    context.Database.EnsureCreated();

                    int deviceId = 1;
                    int crossingId = 1;
                    int sectionId = 1;
                    int regionId = 1;
                    int channelId = 1;
                    for (int i = 0; i < deviceCount; ++i)
                    {
                        TrafficDevice densityDevice = new TrafficDevice
                        {
                            DeviceId = deviceId++,
                            Ip = "192.168.200.204",
                            Port = 18000 + i
                        };
                        densityDevice.DeviceName = "设备" + densityDevice.Port;
                        densityDevice.Device_Channels = new List<TrafficDevice_TrafficChannel>();
                        for (int j = 0; j < channelCount; ++j)
                        {
                            TrafficRoadCrossing roadCrossing = new TrafficRoadCrossing
                            {
                                CrossingId = crossingId,
                                CrossingName = "路口" + crossingId
                            };
                            TrafficRoadSection roadSection = new TrafficRoadSection
                            {
                                SectionId = sectionId,
                                SectionName = "路段" + sectionId
                            };

                            TrafficChannel channel = new TrafficChannel()
                            {
                                ChannelId = channelId.ToString(),
                                ChannelName = $"通道 {densityDevice.DeviceId} {j+1}",
                                ChannelIndex = j + 1,
                                CrossingId = crossingId,
                                SectionId = sectionId,
                                Regions = new List<TrafficRegion>(),
                                RoadCrossing = roadCrossing,
                                RoadSection = roadSection,

                            };
                            TrafficDevice_TrafficChannel relation = new TrafficDevice_TrafficChannel
                            {
                                ChannelId = channel.ChannelId,
                                DeviceId = densityDevice.DeviceId,
                                Channel = channel
                            };
                            channelId++;
                            crossingId++;
                            sectionId++;
                            densityDevice.Device_Channels.Add(relation);

                            for (int k = 0; k < regionCount; ++k)
                            {
                                int value = random.Next(1, 2);
                                TrafficRegion region=new TrafficRegion
                                {
                                    ChannelId = channel.ChannelId,
                                    Channel = channel,
                                    Region = "1",
                                    RegionIndex = k + 1,
                                    RegionName = "区域" + regionId++,
                                    IsVip = value == 1
                                };
                                if (value == 1)
                                {
                                    vips.Add(region.DataId);
                                }
                                channel.Regions.Add(region);
                            }
                        }
                        context.Devices.Add(densityDevice);
                        devices.Add(densityDevice);
                    }
                    context.SaveChanges();
                }
                DensityDbSimulator.CreateData(TestInit.ServiceProvider, devices, DataCreateMode.Fixed, DateTime.Today, DateTime.Today);
                TestInit.RefreshDensityCache(devices);
                DensitiesController controller = new DensitiesController(
                    TestInit.ServiceProvider.GetRequiredService<DensityContext>(),
                    TestInit.ServiceProvider.GetRequiredService<IMemoryCache>());
                var v = controller.QueryVipRegions();
                foreach (TrafficDensity density in v)
                {
                    Assert.IsTrue(vips.Contains(density.DataId));
                }
            }
           
        }
    }

}
