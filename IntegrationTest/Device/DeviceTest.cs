using System.Collections.Generic;
using ItsukiSumeragi.Controller;
using ItsukiSumeragi.Models;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ItsukiSumeragi.Codes.Device;

namespace IntegrationTest.Device
{
    [TestClass]
    public class DeviceTest
    {
        [TestMethod]
        public void InsertDevice_200()
        {
            TestInit.ResetDeviceDb();
            TrafficDeviceInsert model = new TrafficDeviceInsert
            {
                DeviceName = "testDevice1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000,
                Channels = new List<TrafficChannel>
                {
                    new TrafficChannel
                    {
                        ChannelId="channelId",
                        ChannelName="channelName",
                        ChannelType=(int)ChannelType.RTSP,
                        ChannelIndex = 1,
                    }
                }
            };
            DevicesController controller = TestInit.GetDevicesController();
            var result = controller.PostDevice(model);
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            PageModel<TrafficDevice> devices = controller.GetDevices(1, null, 0, 0, null, null,null, 0, 0);
            Assert.AreEqual(1, devices.Datas.Count);
            Assert.AreEqual("testDevice1", devices.Datas[0].DeviceName);
            Assert.AreEqual((int)DeviceModel.MO_AF_A11_04_4X, devices.Datas[0].DeviceModel);
            Assert.AreEqual(DeviceType.流量检测器, devices.Datas[0].DeviceType);
            Assert.AreEqual("127.0.0.1", devices.Datas[0].Ip);
            Assert.AreEqual(17000, devices.Datas[0].Port);
            Assert.AreEqual(17000, devices.Datas[0].DataPort);
            Assert.AreEqual(1, devices.Datas[0].Device_Channels.Count);
            Assert.AreEqual("channelId", devices.Datas[0].Device_Channels[0].Channel.ChannelId);
            Assert.AreEqual("channelName", devices.Datas[0].Device_Channels[0].Channel.ChannelName);
            Assert.AreEqual(1, devices.Datas[0].Device_Channels[0].Channel.ChannelIndex);
        }

        [TestMethod]
        public void InsertDevice_409_IpConfilict()
        {
            TestInit.ResetDeviceDb();
            TrafficDeviceInsert model = new TrafficDeviceInsert
            {
                DeviceName = "test1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000
            };
            DevicesController insertController = TestInit.GetDevicesController();

            insertController.PostDevice(model);
            TrafficDeviceInsert model1 = new TrafficDeviceInsert
            {
                DeviceName = "test1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000
            };
            DevicesController insertController1 = TestInit.GetDevicesController();
            var result = insertController1.PostDevice(model1);
            Assert.IsInstanceOfType(result, typeof(ConflictResult));
        }

        [TestMethod]
        public void InsertDevice_400_ChannelIndexError()
        {
            TestInit.ResetDeviceDb();
            TrafficDeviceInsert model = new TrafficDeviceInsert
            {
                DeviceName = "testDevice1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000,
                Channels = new List<TrafficChannel>
                {
                    new TrafficChannel
                    {
                        ChannelId="channelId",
                        ChannelName="channelName",
                        ChannelType=(int)ChannelType.RTSP,
                        ChannelIndex = 0
                    }
                }
            };
            DevicesController controller = TestInit.GetDevicesController();

            var result = controller.PostDevice(model);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)result).Value;
            Assert.IsTrue(error.ContainsKey("ChannelIndexError"));
        }

        [TestMethod]
        public void InsertDevice_400_ChannelIndexDuplicate()
        {
            TestInit.ResetDeviceDb();
            TrafficDeviceInsert model = new TrafficDeviceInsert
            {
                DeviceName = "testDevice1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000,
                Channels = new List<TrafficChannel>
                {
                    new TrafficChannel
                    {
                        ChannelId="channelId",
                        ChannelName="channelName",
                        ChannelType=(int)ChannelType.RTSP,
                        ChannelIndex = 1
                    },
                    new TrafficChannel
                    {
                        ChannelId="127.0.0.1_2",
                        ChannelName="testChannel1",
                        ChannelIndex = 1,
                        ChannelType=(int)ChannelType.RTSP
                    }
                }
            };
            DevicesController controller = TestInit.GetDevicesController();

            var result = controller.PostDevice(model);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)result).Value;
            Assert.IsTrue(error.ContainsKey("ChannelIndexDuplicate"));
        }

        [TestMethod]
        public void InsertDevice_400_ChannelBind()
        {
            TestInit.ResetDeviceDb();
            TrafficDeviceInsert model1 = new TrafficDeviceInsert
            {
                DeviceName = "testDevice1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000,
                Channels = new List<TrafficChannel>
                {
                    new TrafficChannel
                    {
                        ChannelId="channelId",
                        ChannelName="channelName",
                        ChannelType=(int)ChannelType.RTSP,
                        ChannelIndex = 1
                    }
                }
            };
            DevicesController controller = TestInit.GetDevicesController();

            controller.PostDevice(model1);

            TrafficDeviceInsert model2 = new TrafficDeviceInsert
            {
                DeviceName = "testDevice2",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.2",
                Port = 17000,
                DataPort = 17000,
                Channels = new List<TrafficChannel>
                {
                    new TrafficChannel
                    {
                        ChannelId="channelId",
                        ChannelName="channelName",
                        ChannelType=(int)ChannelType.RTSP,
                        ChannelIndex = 1
                    }
                }
            };
            var result = controller.PostDevice(model2);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)result).Value;
            Assert.IsTrue(error.ContainsKey("Relation"));
        }

        [TestMethod]
        public void InsertDevice_400_CrossingNotExisted()
        {
            TestInit.ResetDeviceDb();
            TrafficDeviceInsert model = new TrafficDeviceInsert
            {
                DeviceName = "testDevice1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000,
                Channels = new List<TrafficChannel>
                {
                    new TrafficChannel
                    {
                        ChannelId="channelId",
                        ChannelName="channelName",
                        ChannelType=(int)ChannelType.RTSP,
                        ChannelIndex = 1,
                        CrossingId = 1
                    }
                }
            };
            DevicesController controller = TestInit.GetDevicesController();

            var result = controller.PostDevice(model);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)result).Value;
            Assert.IsTrue(error.ContainsKey("Crossing"));
        }

        [TestMethod]
        public void InsertDevice_400_SectionNotExisted()
        {
            TestInit.ResetDeviceDb();
            TrafficDeviceInsert model = new TrafficDeviceInsert
            {
                DeviceName = "testDevice1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000,
                Channels = new List<TrafficChannel>
                {
                    new TrafficChannel
                    {
                        ChannelId="channelId",
                        ChannelName="channelName",
                        ChannelType=(int)ChannelType.RTSP,
                        ChannelIndex = 1,
                        SectionId = 1
                    }
                }
            };
            DevicesController controller = TestInit.GetDevicesController();

            var result = controller.PostDevice(model);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)result).Value;
            Assert.IsTrue(error.ContainsKey("Section"));
        }

        [TestMethod]
        public void InsertDevice_400_LocationNotExisted()
        {
            TestInit.ResetDeviceDb();
            TrafficDeviceInsert model = new TrafficDeviceInsert
            {
                DeviceName = "testDevice1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000,
                Channels = new List<TrafficChannel>
                {
                    new TrafficChannel
                    {
                        ChannelId="channelId",
                        ChannelName="channelName",
                        ChannelType=(int)ChannelType.RTSP,
                        ChannelIndex = 1,
                        LocationId = 1
                    }
                }
            };
            DevicesController controller = TestInit.GetDevicesController();

            var result = controller.PostDevice(model);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)result).Value;
            Assert.IsTrue(error.ContainsKey("Location"));
        }

        [TestMethod]
        public void InsertDevice_400_ViolationNotExisted()
        {
            TestInit.ResetDeviceDb();
            TrafficDeviceInsert model = new TrafficDeviceInsert
            {
                DeviceName = "testDevice1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000,
                Channels = new List<TrafficChannel>
                {
                    new TrafficChannel
                    {
                        ChannelId="channelId",
                        ChannelName="channelName",
                        ChannelType=(int)ChannelType.RTSP,
                        ChannelIndex = 1,
                        Channel_Violations = new List<TrafficChannel_TrafficViolation>
                        {
                            new TrafficChannel_TrafficViolation
                            {
                                ViolationId = 1
                            }
                        }
                    }
                }
            };
            DevicesController controller = TestInit.GetDevicesController();

            var result = controller.PostDevice(model);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            SerializableError error = (SerializableError)((BadRequestObjectResult)result).Value;
            Assert.IsTrue(error.ContainsKey("Violation"));
        }

        [TestMethod]
        public void UpdateDevice_200_Device()
        {
            TestInit.ResetDeviceDb();
            TrafficDeviceInsert insertModel = new TrafficDeviceInsert
            {
                DeviceName = "testDevice1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000
            };
            DevicesController controller = TestInit.GetDevicesController();

            OkObjectResult result = (OkObjectResult)controller.PostDevice(insertModel);

            TrafficDeviceUpdate updateModel = new TrafficDeviceUpdate
            {
                DeviceId = ((TrafficDevice)result.Value).DeviceId,
                DeviceName = "testDevice2",
                DeviceModel = (int)DeviceModel.MO_AF_A11_08_8X,
                Ip = "127.0.0.2",
                Port = 17001,
                DataPort = 17001
            };
            var updateResult = controller.PutDevice(updateModel);
            Assert.IsInstanceOfType(updateResult, typeof(OkObjectResult));
            PageModel<TrafficDevice> devices = controller.GetDevices(1,null, 0, 0, null, null, null, 0, 0);
            Assert.AreEqual("testDevice2", devices.Datas[0].DeviceName);
            Assert.AreEqual((int)DeviceModel.MO_AF_A11_08_8X, devices.Datas[0].DeviceModel);
            Assert.AreEqual("127.0.0.2", devices.Datas[0].Ip);
            Assert.AreEqual(17001, devices.Datas[0].Port);
            Assert.AreEqual(17001, devices.Datas[0].DataPort);
        }

        [TestMethod]
        public void UpdateDevice_200_AddChannel()
        {
            TestInit.ResetDeviceDb();

            TrafficDeviceInsert insertModel = new TrafficDeviceInsert
            {
                DeviceName = "testDevice1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000
            };
            DevicesController controller = TestInit.GetDevicesController();

            OkObjectResult result = (OkObjectResult)controller.PostDevice(insertModel);
            PageModel<TrafficDevice> devices = controller.GetDevices(1, null, 0, 0, null, null, null, 0, 0);
            Assert.AreEqual(devices.Datas.Count, 1);
            Assert.AreEqual(devices.Datas[0].Device_Channels.Count, 0);

            TrafficDeviceUpdate updateModel = new TrafficDeviceUpdate
            {
                DeviceId = ((TrafficDevice)result.Value).DeviceId,
                DeviceName = "testDevice2",
                DeviceModel = (int)DeviceModel.MO_AF_A11_08_8X,
                Ip = "127.0.0.2",
                Port = 17001,
                DataPort = 17001,
                Channels = new List<TrafficChannel>
                {
                    new TrafficChannel
                    {
                        ChannelId="channelId",
                        ChannelName="channelName",
                        ChannelType=(int)ChannelType.RTSP,
                        ChannelIndex = 1
                    }
                }
            };
            var updateResult = controller.PutDevice(updateModel);
            Assert.IsInstanceOfType(updateResult, typeof(OkObjectResult));
            devices = controller.GetDevices(1, null, 0, 0, null, null, null, 0, 0);
            Assert.AreEqual(1, devices.Datas.Count);
            Assert.AreEqual(1, devices.Datas[0].Device_Channels.Count);
            Assert.AreEqual("channelId", devices.Datas[0].Device_Channels[0].Channel.ChannelId);
            Assert.AreEqual("channelName", devices.Datas[0].Device_Channels[0].Channel.ChannelName);
            Assert.AreEqual((int)ChannelType.RTSP, devices.Datas[0].Device_Channels[0].Channel.ChannelType);
            Assert.AreEqual(1, devices.Datas[0].Device_Channels[0].Channel.ChannelIndex);

        }

        [TestMethod]
        public void UpdateDevice_200_UpdateChannel()
        {
            TestInit.ResetDeviceDb();

            TrafficDeviceInsert insertModel = new TrafficDeviceInsert
            {
                DeviceName = "testDevice1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000,
                Channels = new List<TrafficChannel>
                {
                    new TrafficChannel
                    {
                        ChannelId="channelId",
                        ChannelName="channelName",
                        ChannelType=(int)ChannelType.RTSP,
                        ChannelIndex = 1
                    }
                }
            };
            DevicesController controller = TestInit.GetDevicesController();

            OkObjectResult result = (OkObjectResult)controller.PostDevice(insertModel);

            TrafficDeviceUpdate updateModel = new TrafficDeviceUpdate
            {
                DeviceId = ((TrafficDevice)result.Value).DeviceId,
                DeviceName = "testDevice1",
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000,
                Channels = new List<TrafficChannel>
                {
                    new TrafficChannel
                    {
                        ChannelId="channelId",
                        ChannelName="channelName1",
                        ChannelType=(int)ChannelType.GB28181,
                        ChannelIndex = 2
                    }
                }
            };
            var updateResult = controller.PutDevice(updateModel);
            Assert.IsInstanceOfType(updateResult, typeof(OkObjectResult));
            var devices = controller.GetDevices(1, null, 0, 0, null, null, null, 0, 0);
            Assert.AreEqual(1, devices.Datas.Count);
            Assert.AreEqual(1, devices.Datas[0].Device_Channels.Count);
            Assert.AreEqual("channelId", devices.Datas[0].Device_Channels[0].Channel.ChannelId);
            Assert.AreEqual("channelName1", devices.Datas[0].Device_Channels[0].Channel.ChannelName);
            Assert.AreEqual((int)ChannelType.GB28181, devices.Datas[0].Device_Channels[0].Channel.ChannelType);
            Assert.AreEqual(2, devices.Datas[0].Device_Channels[0].Channel.ChannelIndex);
        }

        [TestMethod]
        public void UpdateDevice_200_DeleteChannel()
        {
            TestInit.ResetDeviceDb();

            TrafficDeviceInsert insertModel = new TrafficDeviceInsert
            {
                DeviceName = "testDevice1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000,
                Channels = new List<TrafficChannel>
                {
                    new TrafficChannel
                    {
                        ChannelId="channelId",
                        ChannelName="channelName",
                        ChannelType=(int)ChannelType.RTSP,
                        ChannelIndex = 1
                    }
                }
            };
            DevicesController controller = TestInit.GetDevicesController();

            OkObjectResult result = (OkObjectResult)controller.PostDevice(insertModel);

            TrafficDeviceUpdate updateModel = new TrafficDeviceUpdate
            {
                DeviceId = ((TrafficDevice)result.Value).DeviceId,
                DeviceName = "testDevice1",
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000,
                Channels = new List<TrafficChannel>()
            };
            var updateResult = controller.PutDevice(updateModel);
            Assert.IsInstanceOfType(updateResult, typeof(OkObjectResult));
            var devices = controller.GetDevices(1, null, 0, 0, null, null, null, 0, 0);
            Assert.AreEqual(1, devices.Datas.Count);
            Assert.AreEqual(0, devices.Datas[0].Device_Channels.Count);
        }

        [TestMethod]
        public void UpdateDevice_409()
        {
            TestInit.ResetDeviceDb();

            TrafficDeviceInsert insertModel1 = new TrafficDeviceInsert
            {
                DeviceName = "testDevice1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000
            };
            TrafficDeviceInsert insertModel2 = new TrafficDeviceInsert
            {
                DeviceName = "testDevice1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.2",
                Port = 17000,
                DataPort = 17000
            };
            DevicesController controller = TestInit.GetDevicesController();

            controller.PostDevice(insertModel1);
            OkObjectResult result = (OkObjectResult)controller.PostDevice(insertModel2);

            TrafficDeviceUpdate updateModel = new TrafficDeviceUpdate
            {
                DeviceId = ((TrafficDevice)result.Value).DeviceId,
                DeviceName = "testDevice2",
                DeviceModel = (int)DeviceModel.MO_AF_A11_08_8X,
                Ip = "127.0.0.1",
                Port = 17001,
                DataPort = 17001
            };
            var updateResult = controller.PutDevice(updateModel);
            Assert.IsInstanceOfType(updateResult, typeof(ConflictResult));
        }

        [TestMethod]
        public void UpdateDevice_404()
        {
            TestInit.ResetDeviceDb();
            TrafficDeviceUpdate model = new TrafficDeviceUpdate
            {
                DeviceId = 1,
                DeviceName = "testDevice1",
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000
            };
            DevicesController controller = TestInit.GetDevicesController();

            var result = controller.PutDevice(model);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public void DeleteDevice_200()
        {
            TestInit.ResetDeviceDb();

            TrafficDeviceInsert model = new TrafficDeviceInsert
            {
                DeviceName = "testDevice1",
                DeviceType = DeviceType.流量检测器,
                DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                Ip = "127.0.0.1",
                Port = 17000,
                DataPort = 17000
            };
            DevicesController controller = TestInit.GetDevicesController();

            OkObjectResult result = (OkObjectResult)controller.PostDevice(model);
            var deleteResult = controller.DeleteDevice(((TrafficDevice)result.Value).DeviceId);
            Assert.IsInstanceOfType(deleteResult, typeof(OkObjectResult));
            var list = controller.GetDevices(1, null, 0, 0, null, null, null, 0, 0);
            Assert.AreEqual(list.Datas.Count, 0);
        }

        [TestMethod]
        public void DeleteDevice_404()
        {
            TestInit.ResetDeviceDb();
            DevicesController controller = TestInit.GetDevicesController();
            var result = controller.DeleteDevice(1);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }
    }
}
