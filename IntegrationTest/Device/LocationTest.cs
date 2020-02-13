using ItsukiSumeragi.Controller;
using ItsukiSumeragi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ItsukiSumeragi.Codes.Device;

namespace IntegrationTest.Device
{
    [TestClass]
    public class LocationTest
    {
        [TestMethod]
        public void InsertLocation_200()
        {
            TestInit.ResetDeviceDb();
            TrafficLocation model = new TrafficLocation
            {
                LocationCode="001",
                LocationName = "地点1"
            };
            LocationsController controller = TestInit.GetLocationsController();

            var result = controller.PostLocation(model);
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));

            var locations = controller.GetLocations(null, null,0, 0);
            Assert.AreEqual(1, locations.Datas.Count);
            Assert.AreEqual("001", locations.Datas[0].LocationCode);
            Assert.AreEqual("地点1", locations.Datas[0].LocationName);
        }

        [TestMethod]
        public void InsertLocation_409()
        {
            TestInit.ResetDeviceDb();
            TrafficLocation model = new TrafficLocation
            {
                LocationCode = "001",
                LocationName = "地点1"
            };
            LocationsController controller = TestInit.GetLocationsController();

            var result = controller.PostLocation(model);
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));

            TrafficLocation model2 = new TrafficLocation
            {
                LocationCode = "001",
                LocationName = "地点2"
            };
            var result1 = controller.PostLocation(model2);
            Assert.IsInstanceOfType(result1, typeof(ConflictResult));
        }

        [TestMethod]
        public void UpdateLocation_200()
        {
            TestInit.ResetDeviceDb();
            TrafficLocation insertModel = new TrafficLocation
            {
                LocationId = 1,
                LocationCode = "001",
                LocationName = "地点1"
            };
            LocationsController controller = TestInit.GetLocationsController();
            controller.PostLocation(insertModel);

            TrafficLocation updateModel = new TrafficLocation
            {
                LocationId = 1,
                LocationCode = "001",
                LocationName = "地点2"
            };
            LocationsController updateController = TestInit.GetLocationsController();
            var result = updateController.PutLocation(updateModel);
            Assert.IsInstanceOfType(result, typeof(OkResult));

            var locations = updateController.GetLocations(null, null, 0, 0);
            Assert.AreEqual("地点2", locations.Datas[0].LocationName);
        }

        [TestMethod]
        public void UpdateLocation_404()
        {
            TestInit.ResetDeviceDb();
            TrafficLocation updateModel = new TrafficLocation
            {
                LocationId = 1,
                LocationCode = "001",
                LocationName = "地点2"
            };
            LocationsController controller = TestInit.GetLocationsController();

            var result = controller.PutLocation(updateModel);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public void UpdateLocation_409()
        {
            TestInit.ResetDeviceDb();
            TrafficLocation insertModel = new TrafficLocation
            {
                LocationId = 1,
                LocationCode = "001",
                LocationName = "地点1"
            };
            TrafficLocation insertModel1 = new TrafficLocation
            {
                LocationId = 2,
                LocationCode = "002",
                LocationName = "地点2"
            };
            LocationsController controller = TestInit.GetLocationsController();

            controller.PostLocation(insertModel);
            controller.PostLocation(insertModel1);

            TrafficLocation updateModel = new TrafficLocation
            {
                LocationId = 2,
                LocationCode = "001",
                LocationName = "地点1"
            };
            LocationsController updateController = TestInit.GetLocationsController();
            var result = updateController.PutLocation(updateModel);
            Assert.IsInstanceOfType(result, typeof(ConflictResult));
        }

        [TestMethod]
        public void DeleteLocation_200()
        {
            TestInit.ResetDeviceDb();
            TrafficLocation model = new TrafficLocation
            {
                LocationId = 1,
                LocationCode = "001",
                LocationName = "地点1"
            };
            LocationsController controller = TestInit.GetLocationsController();

            var insertResult = controller.PostLocation(model);
            Assert.IsInstanceOfType(insertResult, typeof(OkObjectResult));

            var deleteResult = controller.DeleteLocation(1);
            Assert.IsInstanceOfType(deleteResult, typeof(OkObjectResult));

            var locations = controller.GetLocations(null, null, 0, 0);
            Assert.AreEqual(locations.Total, 0);
        }

        [TestMethod]
        public void DeleteLocation_400()
        {
            TestInit.ResetDeviceDb();
            TrafficLocation model = new TrafficLocation
            {
                LocationId = 1,
                LocationCode = "001",
                LocationName = "地点1"
            };
            LocationsController controller = TestInit.GetLocationsController();
            controller.PostLocation(model);

            TrafficChannel channel = new TrafficChannel
            {
                ChannelId = "127.0.0.1_1",
                ChannelName = "testChannel1",
                ChannelType = (int)ChannelType.RTSP,
                RtspUser = "user",
                RtspPassword = "pwd",
                RtspProtocol = (int)RtspProtocol.Tcp,
                LocationId = 1
            };
            ChannelsController channelsController = TestInit.GetChannelsController();
            channelsController.PostChannel(channel);

            var deleteResult = controller.DeleteLocation(1);
            Assert.IsInstanceOfType(deleteResult, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)deleteResult).Value;
            Assert.IsTrue(error.ContainsKey("Channel"));
        }


        [TestMethod]
        public void DeleteLocation_404()
        {
            TestInit.ResetDeviceDb();
            LocationsController controller = TestInit.GetLocationsController();
            var result = controller.DeleteLocation(1);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }
    }
}
