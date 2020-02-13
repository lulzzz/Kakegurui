using ItsukiSumeragi.Controller;
using ItsukiSumeragi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ItsukiSumeragi.Codes.Device;

namespace IntegrationTest.Device
{
    [TestClass]
    public class CrossingTest
    {
        [TestMethod]
        public void InsertCrossing_200()
        {
            TestInit.ResetDeviceDb();
            TrafficRoadCrossing model = new TrafficRoadCrossing
            {
                CrossingName = "roadName1"
            };
            RoadCrossingsController controller = TestInit.GetCrossingsController();
            var result = controller.PostCrossing(model);
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        }

        [TestMethod]
        public void UpdateCrossing_200()
        {
            TestInit.ResetDeviceDb();
            RoadCrossingsController controller = TestInit.GetCrossingsController();

            TrafficRoadCrossing insertModel = new TrafficRoadCrossing
            {
                CrossingId = 1,
                CrossingName = "roadName1"
            };
            controller.PostCrossing(insertModel);

            TrafficRoadCrossing updateModel = new TrafficRoadCrossing
            {
                CrossingId = 1,
                CrossingName = "roadName2"
            };
            RoadCrossingsController updateController = TestInit.GetCrossingsController();
            var result = updateController.PutCrossing(updateModel);
            Assert.IsInstanceOfType(result, typeof(OkResult));

            var roads = updateController.GetCrossings(null, 0, 0);
            Assert.AreEqual("roadName2", roads.Datas[0].CrossingName);
        }

        [TestMethod]
        public void UpdateCrossing_404()
        {
            TestInit.ResetDeviceDb();
            TrafficRoadCrossing updateModel = new TrafficRoadCrossing
            {
                CrossingId = 1,
                CrossingName = "roadName2"
            };
            RoadCrossingsController controller = TestInit.GetCrossingsController();

            var result = controller.PutCrossing(updateModel);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public void DeleteCrossing_200()
        {
            TestInit.ResetDeviceDb();
            TrafficRoadCrossing insertModel = new TrafficRoadCrossing
            {
                CrossingId = 1,
                CrossingName = "roadName1"
            };
            RoadCrossingsController controller = TestInit.GetCrossingsController();

            controller.PostCrossing(insertModel);

            var result = controller.DeleteCrossing(1);
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));

            var roads = controller.GetCrossings(null, 0, 0);
            Assert.AreEqual(roads.Datas.Count, 0);
        }

        [TestMethod]
        public void DeleteCrossing_400()
        {
            TestInit.ResetDeviceDb();
            TrafficRoadCrossing crossing = new TrafficRoadCrossing
            {
                CrossingId = 1,
                CrossingName = "roadName1"
            };
            RoadCrossingsController controller = TestInit.GetCrossingsController();

            controller.PostCrossing(crossing);

            TrafficChannel channel = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
                CrossingId = 1
            };
            ChannelsController channelsController = TestInit.GetChannelsController();

            var result = channelsController.PostChannel(channel);
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));

            var deleteResult = controller.DeleteCrossing(1);
            Assert.IsInstanceOfType(deleteResult, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)deleteResult).Value;
            Assert.IsTrue(error.ContainsKey("Channel"));
        }

        [TestMethod]
        public void DeleteCrossing_404()
        {
            TestInit.ResetDeviceDb();
            RoadCrossingsController controller = TestInit.GetCrossingsController();

            var result = controller.DeleteCrossing(1);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }
    }
}
