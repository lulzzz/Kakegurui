using System.Collections.Generic;
using ItsukiSumeragi.Controller;
using ItsukiSumeragi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ItsukiSumeragi.Codes.Device;
using ItsukiSumeragi.Codes.Flow;

namespace IntegrationTest.Device
{
    [TestClass]
    public class ChannelTest
    {
        [TestMethod]
        public void InsertChannel_200_OnlyRequired()
        {
            TestInit.ResetDeviceDb();

            TrafficChannel model = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP
            };
            ChannelsController controller = TestInit.GetChannelsController();
            var result = controller.PostChannel(model);
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var channels = controller.GetChannels(0,null, 0, 0, false, 0, 0);
            Assert.AreEqual(1, channels.Datas.Count);
            Assert.AreEqual("channelId", channels.Datas[0].ChannelId);
            Assert.AreEqual("channelName", channels.Datas[0].ChannelName);
            Assert.AreEqual((int)ChannelType.RTSP, channels.Datas[0].ChannelType);

        }

        [TestMethod]
        public void InsertChannel_200_All()
        {
            TestInit.ResetDeviceDb();
            TestInit.AddChannelDependency();

            TrafficChannel model = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
                RtspUser = "rtspUser",
                RtspPassword = "rtspPwd",
                RtspProtocol = (int)RtspProtocol.Tcp,
                SectionId = 1,
                CrossingId = 1,
                LocationId = 1,
                Lanes = new List<TrafficLane>
                {
                    new TrafficLane
                    {
                        LaneId = "1",
                        LaneName="车道1",
                        LaneIndex = 1,
                        Direction = (int)LaneDirection.由南向北,
                        FlowDirection = (int)FlowDirection.左右,
                        Region = "[]"
                    }
                },
                Regions = new List<TrafficRegion>
                {
                    new TrafficRegion
                    {
                        RegionIndex = 1,
                        RegionName= "区域1",
                        Region = "[]",
                        IsVip = true,
                        CarCount=1,
                        Density=2,
                        DensityRange=3,
                        Frequency=4,
                        Saturation=5,
                        Warning=6,
                        WarningDuration=7
                    }
                },
                Shapes = new List<TrafficShape>
                {
                    new TrafficShape
                    {
                        TagName = "LL",
                        ShapeIndex = 1,
                        Region = "[]"
                    }
                },
                Channel_Violations = new List<TrafficChannel_TrafficViolation>
                {
                    new TrafficChannel_TrafficViolation
                    {
                        ViolationId = 1
                    }
                }
            };
            ChannelsController controller = TestInit.GetChannelsController();

            var result = controller.PostChannel(model);
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var channels = controller.GetChannels(0, null, 0, 0, false, 0, 0);
            Assert.AreEqual("channelId", channels.Datas[0].ChannelId);
            Assert.AreEqual("channelName", channels.Datas[0].ChannelName);
            Assert.AreEqual((int)ChannelType.RTSP, channels.Datas[0].ChannelType);
            Assert.AreEqual("rtspUser", channels.Datas[0].RtspUser);
            Assert.AreEqual("rtspPwd", channels.Datas[0].RtspPassword);
            Assert.AreEqual((int)RtspProtocol.Tcp, channels.Datas[0].RtspProtocol);
            Assert.AreEqual(1, channels.Datas[0].SectionId);
            Assert.AreEqual(1, channels.Datas[0].CrossingId);
            Assert.AreEqual(1, channels.Datas[0].LocationId);

            Assert.AreEqual(1, channels.Datas[0].Lanes.Count);
            Assert.AreEqual("1", channels.Datas[0].Lanes[0].LaneId);
            Assert.AreEqual("车道1", channels.Datas[0].Lanes[0].LaneName);
            Assert.AreEqual(1, channels.Datas[0].Lanes[0].LaneIndex);
            Assert.AreEqual((int)LaneDirection.由南向北, channels.Datas[0].Lanes[0].Direction);
            Assert.AreEqual((int)FlowDirection.左右, channels.Datas[0].Lanes[0].FlowDirection);
            Assert.AreEqual("[]", channels.Datas[0].Lanes[0].Region);

            Assert.AreEqual(1, channels.Datas[0].Regions.Count);
            Assert.AreEqual("区域1", channels.Datas[0].Regions[0].RegionName);
            Assert.AreEqual("[]", channels.Datas[0].Regions[0].Region);
            Assert.AreEqual(true, channels.Datas[0].Regions[0].IsVip);
            Assert.AreEqual(1, channels.Datas[0].Regions[0].CarCount);
            Assert.AreEqual(2, channels.Datas[0].Regions[0].Density);
            Assert.AreEqual(3, channels.Datas[0].Regions[0].DensityRange);
            Assert.AreEqual(4, channels.Datas[0].Regions[0].Frequency);
            Assert.AreEqual(5, channels.Datas[0].Regions[0].Saturation);
            Assert.AreEqual(6, channels.Datas[0].Regions[0].Warning);
            Assert.AreEqual(7, channels.Datas[0].Regions[0].WarningDuration);

            Assert.AreEqual(1, channels.Datas[0].Shapes.Count);
            Assert.AreEqual("LL", channels.Datas[0].Shapes[0].TagName);
            Assert.AreEqual(1, channels.Datas[0].Shapes[0].ShapeIndex);
            Assert.AreEqual("[]", channels.Datas[0].Shapes[0].Region);

            Assert.AreEqual(1, channels.Datas[0].Channel_Violations.Count);
            Assert.AreEqual(1, channels.Datas[0].Channel_Violations[0].ViolationId);
        }

        [TestMethod]
        public void InsertChannel_400_CrossingError()
        {
            TestInit.ResetDeviceDb();

            TrafficChannel model = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
                CrossingId = 1
            };
            ChannelsController controller = TestInit.GetChannelsController();

            var result = controller.PostChannel(model);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)result).Value;
            Assert.IsTrue(error.ContainsKey("Crossing"));
        }

        [TestMethod]
        public void InsertChannel_400_SectionError()
        {
            TestInit.ResetDeviceDb();

            TrafficChannel model = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
                SectionId = 1
            };
            ChannelsController controller = TestInit.GetChannelsController();

            var result = controller.PostChannel(model);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)result).Value;
            Assert.IsTrue(error.ContainsKey("Section"));
        }

        [TestMethod]
        public void InsertChannel_400_LocationError()
        {
            TestInit.ResetDeviceDb();

            TrafficChannel model = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
                LocationId = 1
            };
            ChannelsController controller = TestInit.GetChannelsController();

            var result = controller.PostChannel(model);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)result).Value;
            Assert.IsTrue(error.ContainsKey("Location"));
        }

        [TestMethod]
        public void InsertChannel_400_ViolationError()
        {
            TestInit.ResetDeviceDb();

            TrafficChannel model = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
                Channel_Violations = new List<TrafficChannel_TrafficViolation>
                {
                    new TrafficChannel_TrafficViolation
                    {
                        ViolationId = 1,
                    }
                }
            };
            ChannelsController controller = TestInit.GetChannelsController();

            var result = controller.PostChannel(model);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)result).Value;
            Assert.IsTrue(error.ContainsKey("Violation"));
        }

        [TestMethod]
        public void InsertChannel_409_IdConflict()
        {
            TestInit.ResetDeviceDb();

            TrafficChannel model = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
            };
            ChannelsController insertController = TestInit.GetChannelsController();

            var result = insertController.PostChannel(model);
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));

            TrafficChannel model2 = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
            };
            ChannelsController insertController1 = TestInit.GetChannelsController();
            result = insertController1.PostChannel(model2);
            Assert.IsInstanceOfType(result, typeof(ConflictResult));
        }

        [TestMethod]
        public void UpdateChannel_200()
        {
            TestInit.ResetDeviceDb();
            TestInit.AddChannelDependency();
            TrafficChannel model = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName1",
                ChannelType = (int)ChannelType.GB28181,
                RtspUser = "rtspUser1",
                RtspPassword = "rtspPwd1",
                RtspProtocol = (int)RtspProtocol.Udp,
                SectionId = 2,
                CrossingId = 2,
                LocationId = 2,
                Lanes = new List<TrafficLane>
                {
                    new TrafficLane
                    {
                        LaneId = "11",
                        LaneName="车道11",
                        LaneIndex = 2,
                        Direction = (int)LaneDirection.由东向西,
                        FlowDirection = (int)FlowDirection.右转,
                        Region = "[]1"
                    }
                },
                Regions = new List<TrafficRegion>
                {
                    new TrafficRegion
                    {
                        RegionIndex = 11,
                        RegionName= "区域11",
                        Region = "[]1",
                        IsVip = false,
                        CarCount=11,
                        Density=21,
                        DensityRange=31,
                        Frequency=41,
                        Saturation=51,
                        Warning=61,
                        WarningDuration=71
                    }
                },
                Shapes = new List<TrafficShape>
                {
                    new TrafficShape
                    {
                        TagName = "LR",
                        ShapeIndex = 2,
                        Region = "[]1"
                    }
                },
                Channel_Violations = new List<TrafficChannel_TrafficViolation>
                {
                    new TrafficChannel_TrafficViolation
                    {
                        ViolationId = 2
                    }
                }
            };

            ChannelsController insertController = TestInit.GetChannelsController();
            insertController.PostChannel(model);

            TrafficChannel updateModel = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
                RtspUser = "rtspUser",
                RtspPassword = "rtspPwd",
                RtspProtocol = (int)RtspProtocol.Tcp,
                SectionId = 1,
                CrossingId = 1,
                LocationId = 1,
                Lanes = new List<TrafficLane>
                {
                    new TrafficLane
                    {
                        LaneId = "1",
                        LaneName="车道1",
                        LaneIndex = 1,
                        Direction = (int)LaneDirection.由南向北,
                        FlowDirection = (int)FlowDirection.左右,
                        Region = "[]"
                    }
                },
                Regions = new List<TrafficRegion>
                {
                    new TrafficRegion
                    {
                        RegionIndex = 1,
                        RegionName= "区域1",
                        Region = "[]",
                        IsVip = true,
                        CarCount=1,
                        Density=2,
                        DensityRange=3,
                        Frequency=4,
                        Saturation=5,
                        Warning=6,
                        WarningDuration=7
                    }
                },
                Shapes = new List<TrafficShape>
                {
                    new TrafficShape
                    {
                        TagName = "LL",
                        ShapeIndex = 1,
                        Region = "[]"
                    }
                },
                Channel_Violations = new List<TrafficChannel_TrafficViolation>
                {
                    new TrafficChannel_TrafficViolation
                    {
                        ViolationId = 1
                    }
                }
            };

            ChannelsController updateController = TestInit.GetChannelsController();
            var result = updateController.PutChannel(updateModel);

            Assert.IsInstanceOfType(result, typeof(OkResult));
            var channels = updateController.GetChannels(0, null, 0, 0, false, 0, 0);
            Assert.AreEqual("channelId", channels.Datas[0].ChannelId);
            Assert.AreEqual("channelName", channels.Datas[0].ChannelName);
            Assert.AreEqual((int)ChannelType.RTSP, channels.Datas[0].ChannelType);
            Assert.AreEqual("rtspUser", channels.Datas[0].RtspUser);
            Assert.AreEqual("rtspPwd", channels.Datas[0].RtspPassword);
            Assert.AreEqual((int)RtspProtocol.Tcp, channels.Datas[0].RtspProtocol);
            Assert.AreEqual(1, channels.Datas[0].SectionId);
            Assert.AreEqual(1, channels.Datas[0].CrossingId);
            Assert.AreEqual(1, channels.Datas[0].LocationId);

            Assert.AreEqual(1, channels.Datas[0].Lanes.Count);
            Assert.AreEqual("1", channels.Datas[0].Lanes[0].LaneId);
            Assert.AreEqual("车道1", channels.Datas[0].Lanes[0].LaneName);
            Assert.AreEqual(1, channels.Datas[0].Lanes[0].LaneIndex);
            Assert.AreEqual((int)LaneDirection.由南向北, channels.Datas[0].Lanes[0].Direction);
            Assert.AreEqual((int)FlowDirection.左右, channels.Datas[0].Lanes[0].FlowDirection);
            Assert.AreEqual("[]", channels.Datas[0].Lanes[0].Region);

            Assert.AreEqual(1, channels.Datas[0].Regions.Count);
            Assert.AreEqual("区域1", channels.Datas[0].Regions[0].RegionName);
            Assert.AreEqual("[]", channels.Datas[0].Regions[0].Region);
            Assert.AreEqual(true, channels.Datas[0].Regions[0].IsVip);
            Assert.AreEqual(1, channels.Datas[0].Regions[0].CarCount);
            Assert.AreEqual(2, channels.Datas[0].Regions[0].Density);
            Assert.AreEqual(3, channels.Datas[0].Regions[0].DensityRange);
            Assert.AreEqual(4, channels.Datas[0].Regions[0].Frequency);
            Assert.AreEqual(5, channels.Datas[0].Regions[0].Saturation);
            Assert.AreEqual(6, channels.Datas[0].Regions[0].Warning);
            Assert.AreEqual(7, channels.Datas[0].Regions[0].WarningDuration);

            Assert.AreEqual(1, channels.Datas[0].Shapes.Count);
            Assert.AreEqual("LL", channels.Datas[0].Shapes[0].TagName);
            Assert.AreEqual(1, channels.Datas[0].Shapes[0].ShapeIndex);
            Assert.AreEqual("[]", channels.Datas[0].Shapes[0].Region);

            Assert.AreEqual(1, channels.Datas[0].Channel_Violations.Count);
            Assert.AreEqual(1, channels.Datas[0].Channel_Violations[0].ViolationId);

        }

        [TestMethod]
        public void UpdateChannel_400_CrossingError()
        {
            TestInit.ResetDeviceDb();
            TestInit.AddChannelDependency();

            TrafficChannel model = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
                CrossingId = 1
            };
            ChannelsController controller = TestInit.GetChannelsController();

            controller.PostChannel(model);

            TrafficChannel updateModel = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
                CrossingId = 3
            };
            var result = controller.PutChannel(updateModel);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)result).Value;
            Assert.IsTrue(error.ContainsKey("Crossing"));
        }

        [TestMethod]
        public void UpdateChannel_400_SectionError()
        {
            TestInit.ResetDeviceDb();
            TestInit.AddChannelDependency();
            TrafficChannel model = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
                SectionId = 1
            };
            ChannelsController controller = TestInit.GetChannelsController();

            controller.PostChannel(model);
            TrafficChannel updateModel = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
                SectionId = 3
            };
            var result = controller.PutChannel(updateModel);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)result).Value;
            Assert.IsTrue(error.ContainsKey("Section"));
        }

        [TestMethod]
        public void UpdateChannel_400_LocationError()
        {
            TestInit.ResetDeviceDb();
            TestInit.AddChannelDependency();
            TrafficChannel model = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
                LocationId = 1
            };
            ChannelsController controller = TestInit.GetChannelsController();

            controller.PostChannel(model);
            TrafficChannel updateModel = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
                LocationId = 3
            };
            var result = controller.PutChannel(updateModel);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)result).Value;
            Assert.IsTrue(error.ContainsKey("Location"));
        }

        [TestMethod]
        public void UpdateChannel_400_ViolationError()
        {
            TestInit.ResetDeviceDb();
            TestInit.AddChannelDependency();
            TrafficChannel model = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
                Channel_Violations = new List<TrafficChannel_TrafficViolation>
                {
                    new TrafficChannel_TrafficViolation
                    {
                        ViolationId = 1
                    }
                }
            };
            ChannelsController controller = TestInit.GetChannelsController();

            controller.PostChannel(model);
            TrafficChannel updateModel = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
                Channel_Violations = new List<TrafficChannel_TrafficViolation>
                {
                    new TrafficChannel_TrafficViolation
                    {
                        ViolationId = 3
                    }
                }
            };
            var result = controller.PutChannel(updateModel);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)result).Value;
            Assert.IsTrue(error.ContainsKey("Violation"));
        }

        [TestMethod]
        public void UpdateChannel_404()
        {
            TestInit.ResetDeviceDb();

            TrafficChannel updateModel = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
            };
            ChannelsController controller = TestInit.GetChannelsController();

            var result = controller.PutChannel(updateModel);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public void DeleteChannel_200()
        {
            TestInit.ResetDeviceDb();

            TrafficChannel model = new TrafficChannel
            {
                ChannelId = "channelId",
                ChannelName = "channelName",
                ChannelType = (int)ChannelType.RTSP,
            };
            ChannelsController controller = TestInit.GetChannelsController();
            controller.PostChannel(model);
            var channels = controller.GetChannels(0, null, 0, 0, false, 0, 0);
            Assert.AreEqual(1,channels.Datas.Count);
            controller.DeleteChannel(model.ChannelId);
            channels = controller.GetChannels(0, null, 0, 0, false, 0, 0);
            Assert.AreEqual(0, channels.Datas.Count);
        }

        [TestMethod]
        public void DeleteChannel_404()
        {
            TestInit.ResetDeviceDb();
            ChannelsController controller = TestInit.GetChannelsController();

            var result = controller.DeleteChannel("127.0.0.1_1");
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }
    }
}
