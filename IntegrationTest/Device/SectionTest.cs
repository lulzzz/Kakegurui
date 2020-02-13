using ItsukiSumeragi.Controller;
using ItsukiSumeragi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ItsukiSumeragi.Codes.Device;
using ItsukiSumeragi.Codes.Flow;

namespace IntegrationTest.Device
{
    [TestClass]
    public class SectionTest
    {
        [TestMethod]
        public void InsertSection_200()
        {
            TestInit.ResetDeviceDb();
            TrafficRoadSection model = new TrafficRoadSection
            {
                SectionName = "roadName1",
                Direction = (int)LaneDirection.由东向西,
                Length = 1,
                SectionType = (int)SectionType.主干路,
                SpeedLimit = 2
            };
            RoadSectionsController controller = TestInit.GetSectionsController();
            var result = controller.PostSection(model);
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));

            var sections = controller.GetSections(null, 0, 0, 0);
            Assert.AreEqual(1, sections.Datas.Count);
            Assert.AreEqual("roadName1", sections.Datas[0].SectionName);
            Assert.AreEqual((int)LaneDirection.由东向西, sections.Datas[0].Direction);
            Assert.AreEqual(1, sections.Datas[0].Length);
            Assert.AreEqual((int)SectionType.主干路, sections.Datas[0].SectionType);
            Assert.AreEqual(2, sections.Datas[0].SpeedLimit);
        }

        [TestMethod]
        public void UpdateSection_200()
        {
            TestInit.ResetDeviceDb();
            TrafficRoadSection insertModel = new TrafficRoadSection
            {
                SectionId = 1,
                SectionName = "roadName1",
                Direction = (int)LaneDirection.由北向南,
                Length = 2,
                SectionType = (int)SectionType.快速路,
                SpeedLimit = 3
            };
            RoadSectionsController controller = TestInit.GetSectionsController();

            controller.PostSection(insertModel);

            TrafficRoadSection updateModel = new TrafficRoadSection
            {
                SectionId = 1,
                SectionName = "roadName2",
                Direction = (int)LaneDirection.由东向西,
                Length = 1,
                SectionType = (int)SectionType.主干路,
                SpeedLimit = 2
            };
            RoadSectionsController updateController = TestInit.GetSectionsController();
            var result = updateController.PutSection(updateModel);
            Assert.IsInstanceOfType(result, typeof(OkResult));

            var roads = updateController.GetSections(null, 0, 0, 0);
            Assert.AreEqual("roadName2", roads.Datas[0].SectionName);
            Assert.AreEqual((int)LaneDirection.由东向西, roads.Datas[0].Direction);
            Assert.AreEqual(1, roads.Datas[0].Length);
            Assert.AreEqual((int)SectionType.主干路, roads.Datas[0].SectionType);
            Assert.AreEqual(2, roads.Datas[0].SpeedLimit);
        }

        [TestMethod]
        public void UpdateSection_404()
        {
            TestInit.ResetDeviceDb();
            TrafficRoadSection updateModel = new TrafficRoadSection
            {
                SectionId = 1,
                SectionName = "roadName2",
                Direction = (int)LaneDirection.由东向西,
                Length = 1,
                SectionType = (int)SectionType.主干路,
                SpeedLimit = 2
            };
            RoadSectionsController updateController = TestInit.GetSectionsController();

            var result = updateController.PutSection(updateModel);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public void DeleteSection_200()
        {
            TestInit.ResetDeviceDb();
            TrafficRoadSection insertModel = new TrafficRoadSection
            {
                SectionId = 1,
                SectionName = "roadName1",
                Direction = (int)LaneDirection.由东向西,
                Length = 1,
                SectionType = (int)SectionType.主干路,
                SpeedLimit = 2
            };
            RoadSectionsController controller = TestInit.GetSectionsController();

            controller.PostSection(insertModel);

            var result = controller.DeleteSection(1);
            Assert.IsInstanceOfType(result, typeof(OkResult));

            var roads = controller.GetSections(null, 0, 0, 0);
            Assert.AreEqual(roads.Total, 0);
        }

        [TestMethod]
        public void DeleteSection_400()
        {
            TestInit.ResetDeviceDb();
            TrafficRoadSection insertModel = new TrafficRoadSection
            {
                SectionId = 1,
                SectionName = "roadName1",
                Direction = (int)LaneDirection.由东向西,
                Length = 1,
                SectionType = (int)SectionType.主干路,
                SpeedLimit = 2
            };
            RoadSectionsController controller = TestInit.GetSectionsController();

            controller.PostSection(insertModel);

            TrafficChannel channel = new TrafficChannel
            {
                ChannelId = "127.0.0.1_1",
                ChannelName = "testChannel1",
                ChannelType = (int)ChannelType.RTSP,
                RtspUser = "user",
                RtspPassword = "pwd",
                RtspProtocol = (int)RtspProtocol.Tcp,
                SectionId = 1
            };
            ChannelsController channelsController = TestInit.GetChannelsController();

            var result = channelsController.PostChannel(channel);
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));

            var deleteResult = controller.DeleteSection(1);
            Assert.IsInstanceOfType(deleteResult, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)deleteResult).Value;
            Assert.IsTrue(error.ContainsKey("Channel"));
        }


        [TestMethod]
        public void DeleteSection_404()
        {
            TestInit.ResetDeviceDb();
            RoadSectionsController controller = TestInit.GetSectionsController();

            var result = controller.DeleteSection(1);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }
    }
}
