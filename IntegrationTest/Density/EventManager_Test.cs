using System;
using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MomobamiRirika.Controllers;
using MomobamiRirika.Data;
using MomobamiRirika.Models;

namespace IntegrationTest.Density
{
    [TestClass]
    public class EventManager_Test
    {
        [TestMethod]
        [DataRow(2019, 1, 1, 2019, 1, 1)]
        public void Statistics(int startYear, int startMonth, int startDay, int endYear, int endMonth, int endDay)
        {
            DateTime startDate = new DateTime(startYear, startMonth, startDay);
            DateTime endDate = new DateTime(endYear, endMonth, endDay);
            int days = Convert.ToInt32((endDate - startDate).TotalDays + 1);
            List<TrafficDevice> devices = DeviceDbSimulator.CreateDensityDevice(TestInit.ServiceProvider, 1, 6, 6, "",true);
            //创建每小时两条条数据
            EventDbSimulator.CreateData(TestInit.ServiceProvider, devices,startDate,endDate,DataCreateMode.Fixed,true);
            TestInit.RefreshDensityCache(devices);
            EventsController service = new EventsController(TestInit.ServiceProvider.GetRequiredService<DensityContext>(),
                TestInit.ServiceProvider.GetRequiredService<IMemoryCache>());

            foreach (TrafficDevice device in devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    var roadList = service.StatisticsByRoad(relation.Channel.CrossingId.Value,startDate, endDate.AddDays(1));
                    //验证按路口按小时分组统计次数
                    Assert.AreEqual(days*24, roadList.Count);

                    //验证按区域按小时分组统计次数
                    foreach (TrafficRegion region in relation.Channel.Regions)
                    {
                        var regionList=service.StatisticsByRegion(region.DataId,startDate,endDate.AddDays(1));
                        Assert.AreEqual(days * 24, regionList.Count);
                    }
                }
            }
        }

        [TestMethod]
        [DataRow(2019, 1, 1, 2019, 1, 15)]
        public void Rank(int startYear, int startMonth, int startDay, int endYear, int endMonth, int endDay)
        {
            DateTime startDate = new DateTime(startYear, startMonth, startDay);
            DateTime endDate = new DateTime(endYear, endMonth, endDay);
            List<TrafficDevice> devices = DeviceDbSimulator.CreateDensityDevice(TestInit.ServiceProvider,1, 6, 6, "",true);
            //随机出的值表示次数，每次1分钟
            Dictionary<TrafficEvent, int> trafficEvents = EventDbSimulator.CreateData(TestInit.ServiceProvider, devices, startDate,endDate,DataCreateMode.Random,true);
            TestInit.RefreshDensityCache(devices);
            EventsController service = new EventsController(TestInit.ServiceProvider.GetRequiredService<DensityContext>(),
                TestInit.ServiceProvider.GetRequiredService<IMemoryCache>());
            IMemoryCache memoryCache = TestInit.ServiceProvider.GetRequiredService<IMemoryCache>();

            //要验证的值
            var regions = trafficEvents
                .Select(p => new KeyValuePair<string, int>(p.Key.DataId, p.Value))
                .OrderByDescending(p => p.Value)
                .ToList();
            var roads = trafficEvents
                .GroupBy(p => memoryCache.GetRegion(p.Key.DataId).Channel.CrossingId)
                .Select(g => new KeyValuePair<int, int>(g.Key.Value, g.Sum(p => p.Value)))
                .OrderByDescending(p => p.Value)
                .ToList();

            //区域次数
            var countRegionTop = service.CountRankByRegion(startDate, endDate.AddDays(1));
            for(int i=0;i<countRegionTop.Count;++i)
            {
                //验证次数和区域编号
                Assert.AreEqual(countRegionTop[i].Value, regions[i].Value);
                Assert.AreEqual(countRegionTop[i].Axis, regions[i].Key);
            }

            //路口次数
            var countRoadTop = service.CountRankByRoad(startDate, endDate.AddDays(1));
            for (int i = 0; i < countRoadTop.Count; ++i)
            {
                //验证次数和路口编号
                Assert.AreEqual(countRoadTop[i].Value, roads[i].Value);
                Assert.AreEqual(countRoadTop[i].Axis, roads[i].Key);
            }

            //区域时长
            var spanRegionTop = service.SpanRankByRegion(startDate, endDate.AddDays(1));
            for (int i = 0; i < spanRegionTop.Count; ++i)
            {
                //验证时长(秒)和区域编号
                Assert.AreEqual(spanRegionTop[i].Value, regions[i].Value);
                Assert.AreEqual(spanRegionTop[i].Axis, regions[i].Key);
            }

            //路口时长
            var spanRoadTop = service.SpanRankByRoad(startDate, endDate.AddDays(1));
            for (int i = 0; i < countRoadTop.Count; ++i)
            {
                //验证时长(秒)和路口编号
                Assert.AreEqual(spanRoadTop[i].Value, roads[i].Value);
                Assert.AreEqual(spanRoadTop[i].Axis, roads[i].Key);
            }
        }

        /// <summary>
        /// 添加一个持续时长拥堵事件
        /// </summary>
        /// <param name="context"></param>
        /// <param name="region"></param>
        /// <param name="time"></param>
        /// <param name="durationMinutes"></param>
        /// <returns></returns>
        private DateTime AddDuration(DensityContext context,TrafficRegion region,DateTime time,int durationMinutes)
        {
            TrafficEvent trafficEvent = new TrafficEvent
            {
                DataId = region.DataId,
                DateTime = time,
                EndTime = time.AddMinutes(durationMinutes)
            };
            context.Events.Add(trafficEvent);
            return time.AddMinutes(durationMinutes);
        }

        /// <summary>
        /// 添加若干个相同间隔时间和持续时间的拥堵事件
        /// </summary>
        /// <param name="context"></param>
        /// <param name="region"></param>
        /// <param name="time"></param>
        /// <param name="durationMinutes"></param>
        /// <param name="count"></param>
        /// <param name="intervalMinutes"></param>
        /// <returns></returns>
        private DateTime AddInterval(DensityContext context, TrafficRegion region, DateTime time, int durationMinutes,int count,int intervalMinutes)
        {
            DateTime temp = time;
            for (int i = 0; i < count; ++i)
            {
                AddDuration(context, region, temp, durationMinutes);
                temp = temp.AddMinutes(durationMinutes+intervalMinutes);
            }
            return temp.AddMinutes(-intervalMinutes);
        }

        [TestMethod]
        [DataRow(2019, 1, 1)]
        public void Incidence(int startYear, int startMonth, int startDay)
        {
            DateTime startDate = new DateTime(startYear, startMonth, startDay);
            List<TrafficDevice> devices = DeviceDbSimulator.CreateDensityDevice(TestInit.ServiceProvider, 1, 1, 1, "", true);

            TestInit.RefreshDensityCache(devices);
            EventsController service = new EventsController(TestInit.ServiceProvider.GetRequiredService<DensityContext>(),
                TestInit.ServiceProvider.GetRequiredService<IMemoryCache>());
            EventDbSimulator.ResetDatabase(TestInit.ServiceProvider);

            List<TrafficItem> items = new List<TrafficItem>();
            foreach (TrafficDevice device in devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    foreach (TrafficRegion region in relation.Channel.Regions)
                    {
                        items.Add(region);
                    }
                }
            }

            using (DensityContext context = TestInit.ServiceProvider.GetRequiredService<DensityContext>())
            {
                foreach (TrafficDevice device in devices)
                {
                    foreach (var relation in device.Device_Channels)
                    {
                        foreach (TrafficRegion region in relation.Channel.Regions)
                        {
                            DateTime time = startDate;
                            //添加一个持续长度满足10分钟
                            AddDuration(context, region, time, 10);

                            //添加连续3次 总长
                            time = startDate.AddHours(1);
                            AddInterval(context, region, time, 5, 3, 3);

                            //10分钟关联前后5分钟
                            time = startDate.AddHours(2);
                            time = AddDuration(context, region, time, 5);
                            time = time.AddMinutes(3);
                            time = AddDuration(context, region, time, 10);
                            time = time.AddMinutes(3);
                            AddDuration(context, region, time, 5);

                            //交集 5分钟持续三次+10分钟
                            time = startDate.AddHours(3);
                            time = AddInterval(context, region, time, 5, 3, 3);
                            time = time.AddMinutes(3);
                            AddDuration(context, region, time, 10);
                        }
                    }
                }
                context.BulkSaveChanges(options=>options.BatchSize=1000);
                foreach (TrafficDevice device in devices)
                {
                    foreach (var relation in device.Device_Channels)
                    {
                        foreach (TrafficRegion region in relation.Channel.Regions)
                        {
                            var list = service.AnalysisIncidence(region.DataId, startDate, startDate);
                            Assert.AreEqual(startDate, list[0][0].DateTime);
                            Assert.AreEqual(startDate.AddMinutes(10), list[0][0].EndTime);

                            Assert.AreEqual(startDate.AddHours(1), list[0][1].DateTime);
                            Assert.AreEqual(startDate.AddHours(1).AddMinutes(5 * 3 + 3 * 2), list[0][1].EndTime);

                            Assert.AreEqual(startDate.AddHours(2), list[0][2].DateTime);
                            Assert.AreEqual(startDate.AddHours(2).AddMinutes(5 + 3 + 10 + 3 + 5), list[0][2].EndTime);

                            Assert.AreEqual(startDate.AddHours(3), list[0][3].DateTime);
                            Assert.AreEqual(startDate.AddHours(3).AddMinutes(5 * 3 + 3 * 2 + 3 + 10), list[0][3].EndTime);
                        }
                    }
                }
            }

                
        }


        [TestMethod]
        public void Last10()
        {
            List<TrafficDevice> devices = DeviceDbSimulator.CreateDensityDevice(TestInit.ServiceProvider, 1, 1, 10, "", true);
            TestInit.RefreshDensityCache(devices);
            EventDbSimulator.CreateData(TestInit.ServiceProvider,devices,DateTime.Today,true);
            EventsController service = new EventsController(TestInit.ServiceProvider.GetRequiredService<DensityContext>(),
                TestInit.ServiceProvider.GetRequiredService<IMemoryCache>());
            var v=service.QueryLast10();
            int itemCount = devices[0].Device_Channels[0].Channel.Regions.Count;
            for (int i = 0; i < 10; ++i)
            {
                Assert.AreEqual(v[i].DataId, devices[0].Device_Channels[0].Channel.Regions[itemCount - 1-i].DataId);
            }
        }
    }
}
