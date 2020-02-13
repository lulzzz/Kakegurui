using System;
using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MomobamiKirari.Controllers;
using MomobamiKirari.Data;
using ItsukiSumeragi.Codes.Flow;
using MomobamiKirari.Managers;

namespace IntegrationTest.Flow
{
    [TestClass]
    public class VideoManager_Test
    {
        [TestMethod]
        [DataRow(2019, 4, 29, 2019, 4, 29)]
        public void QueryList()
        {
            DateTime startDate = new DateTime(2019, 4, 29);
            DateTime endDate = new DateTime(2019, 4, 29);
            int days = Convert.ToInt32((endDate - startDate).TotalDays+1);
            List<TrafficDevice> devices = DeviceDbSimulator.CreateFlowDevice(TestInit.ServiceProvider, 1, 1, 2, true);
            TestInit.RefreshFlowCache(devices);
            VideoStructDbSimulator.CreateData(TestInit.ServiceProvider,devices,DataCreateMode.Fixed, startDate, endDate, true);
            VideoStructsController controller = new VideoStructsController(
                TestInit.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<VideoStructManager_Alone>());

            //按路口查询
            foreach (TrafficDevice device in devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    int laneCount = relation.Channel.Lanes.Count;
                    var vehicles = controller.QueryByCrossing(relation.Channel.CrossingId.Value, VideoStructType.机动车, startDate, endDate.AddDays(1), 0, 0, true);
                    Assert.AreEqual(days * 24 * 60 * laneCount, vehicles.Datas.Count);
                    var bikes = controller.QueryByCrossing(relation.Channel.CrossingId.Value, VideoStructType.非机动车, startDate, endDate.AddDays(1), 0, 0, true);
                    Assert.AreEqual(days * 24 * 60 * laneCount, bikes.Datas.Count);
                    var pedestrains = controller.QueryByCrossing(relation.Channel.CrossingId.Value, VideoStructType.行人, startDate, endDate.AddDays(1), 0, 0, true);
                    Assert.AreEqual(days * 24 * 60 * laneCount, pedestrains.Datas.Count);
                }
            }

            //按路口方向查询
            foreach (TrafficDevice device in devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    int[] directions = relation.Channel.Lanes.Select(l => l.Direction).Distinct().ToArray();
                    foreach (int direction in directions)
                    {
                        int laneCount = relation.Channel.Lanes.Count(l => l.Direction == direction);
                        var vehicles = controller.QueryByCrossing(relation.Channel.CrossingId.Value, new[] { direction }, VideoStructType.机动车, startDate, endDate.AddDays(1), 0, 0, true);
                        Assert.AreEqual(days * 24 * 60 * laneCount, vehicles.Datas.Count);
                        var bikes = controller.QueryByCrossing(relation.Channel.CrossingId.Value, new[] { direction }, VideoStructType.非机动车, startDate, endDate.AddDays(1), 0, 0, true);
                        Assert.AreEqual(days * 24 * 60 * laneCount, bikes.Datas.Count);
                        var pedestrains = controller.QueryByCrossing(relation.Channel.CrossingId.Value, new[] { direction }, VideoStructType.行人, startDate, endDate.AddDays(1),0,0, true);
                        Assert.AreEqual(days * 24 * 60 * laneCount, pedestrains.Datas.Count);
                    }
                }
            }

            //按路段查询
            foreach (TrafficDevice device in devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    int laneCount = relation.Channel.Lanes.Count;
                    var vehicles = controller.QueryBySection(relation.Channel.SectionId.Value, VideoStructType.机动车, startDate, endDate.AddDays(1), 0, 0, true);
                    Assert.AreEqual(days * 24 * 60 * laneCount, vehicles.Datas.Count);
                    var bikes = controller.QueryBySection(relation.Channel.SectionId.Value, VideoStructType.非机动车, startDate, endDate.AddDays(1), 0, 0, true);
                    Assert.AreEqual(days * 24 * 60 * laneCount, bikes.Datas.Count);
                    var pedestrains = controller.QueryBySection(relation.Channel.SectionId.Value, VideoStructType.行人, startDate, endDate.AddDays(1), 0, 0, true);
                    Assert.AreEqual(days * 24 * 60 * laneCount, pedestrains.Datas.Count);
                }
            }
        }

        [TestMethod]
        public void QueryList_TwoMonth()
        {
            DateTime startDate1 = new DateTime(2019, 4, 30);
            DateTime endDate1 = new DateTime(2019, 4, 30);
            DateTime startDate2 = new DateTime(2019, 5, 1);
            DateTime endDate2 = new DateTime(2019, 5, 1);
            int days = Convert.ToInt32((endDate2 - startDate1).TotalDays + 1);
            List<TrafficDevice> devices = DeviceDbSimulator.CreateFlowDevice(TestInit.ServiceProvider, 1, 1, 2, true);
            TestInit.RefreshFlowCache(devices);
            VideoStructDbSimulator.CreateData(TestInit.ServiceProvider, devices, DataCreateMode.Fixed, startDate1, endDate1, true);
            VideoStructDbSimulator.CreateData(TestInit.ServiceProvider, devices, DataCreateMode.Fixed, startDate2, endDate2);

            VideoStructsController controller = new VideoStructsController(
                TestInit.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<VideoStructManager_Alone>());

            //按路口查询
            foreach (TrafficDevice device in devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    int laneCount = relation.Channel.Lanes.Count;
                    var vehicles = controller.QueryByCrossing(relation.Channel.CrossingId.Value, VideoStructType.机动车, startDate1, endDate2.AddDays(1), 0, 0, true);
                    Assert.AreEqual(days * 24 * 60 * laneCount, vehicles.Datas.Count);
                    var bikes = controller.QueryByCrossing(relation.Channel.CrossingId.Value, VideoStructType.非机动车, startDate1, endDate2.AddDays(1), 0, 0, true);
                    Assert.AreEqual(days * 24 * 60 * laneCount, bikes.Datas.Count);
                    var pedestrains = controller.QueryByCrossing(relation.Channel.CrossingId.Value, VideoStructType.行人, startDate1, endDate2.AddDays(1), 0, 0, true);
                    Assert.AreEqual(days * 24 * 60 * laneCount, pedestrains.Datas.Count);
                }
            }

            //按路口方向查询
            foreach (TrafficDevice device in devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    int[] directions = relation.Channel.Lanes.Select(l => l.Direction).Distinct().ToArray();
                    foreach (int direction in directions)
                    {
                        int laneCount = relation.Channel.Lanes.Count(l => l.Direction == direction);
                        var vehicles = controller.QueryByCrossing(relation.Channel.CrossingId.Value, new[] { direction }, VideoStructType.机动车, startDate1, endDate2.AddDays(1), 0, 0, true);
                        Assert.AreEqual(days * 24 * 60 * laneCount, vehicles.Datas.Count);
                        var bikes = controller.QueryByCrossing(relation.Channel.CrossingId.Value, new[] { direction }, VideoStructType.非机动车, startDate1, endDate2.AddDays(1), 0, 0, true);
                        Assert.AreEqual(days * 24 * 60 * laneCount, bikes.Datas.Count);
                        var pedestrains = controller.QueryByCrossing(relation.Channel.CrossingId.Value, new[] { direction }, VideoStructType.行人, startDate1, endDate2.AddDays(1), 0, 0, true);
                        Assert.AreEqual(days * 24 * 60 * laneCount, pedestrains.Datas.Count);
                    }
                }
            }

            //按路段查询
            foreach (TrafficDevice device in devices)
            {
                foreach (var relation in device.Device_Channels)
                {
                    int laneCount = relation.Channel.Lanes.Count;
                    var vehicles = controller.QueryBySection(relation.Channel.SectionId.Value, VideoStructType.机动车, startDate1, endDate2.AddDays(1), 0, 0, true);
                    Assert.AreEqual(days * 24 * 60 * laneCount, vehicles.Datas.Count);
                    var bikes = controller.QueryBySection(relation.Channel.SectionId.Value, VideoStructType.非机动车, startDate1, endDate2.AddDays(1), 0, 0, true);
                    Assert.AreEqual(days * 24 * 60 * laneCount, bikes.Datas.Count);
                    var pedestrains = controller.QueryBySection(relation.Channel.SectionId.Value, VideoStructType.行人, startDate1, endDate2.AddDays(1), 0, 0, true);
                    Assert.AreEqual(days * 24 * 60 * laneCount, pedestrains.Datas.Count);
                }
            }
        }

    }
}
