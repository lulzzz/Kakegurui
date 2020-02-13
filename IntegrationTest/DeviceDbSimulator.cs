using System;
using System.Collections.Generic;
using ItsukiSumeragi.Data;
using ItsukiSumeragi.Models;
using Microsoft.Extensions.DependencyInjection;
using ItsukiSumeragi.Codes.Device;
using ItsukiSumeragi.Codes.Flow;

namespace IntegrationTest
{
    public class DeviceDbSimulator
    {
        public static void ResetDatabase(IServiceProvider serviceProvider,bool addDefaultRoads=false)
        {
            using (DeviceContext context = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DeviceContext>())
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }
        }


        public static List<TrafficDevice> CreateFlowDevice(IServiceProvider serviceProvider,int deviceCount, int channelCount, int laneCount,bool initDatabase = false, string ip1 = "127.0.0.", int ip2 = 1,int id=100)
        {
            List<TrafficDevice> devices = new List<TrafficDevice>();
            using (DeviceContext context = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DeviceContext>())
            {
                if (initDatabase)
                {
                    ResetDatabase(serviceProvider);
                }

                for (int i = 0; i < deviceCount; ++i)
                {
                    TrafficDevice device = new TrafficDevice
                    {
                        DeviceId = id,
                        DeviceName = $"流量测试设备_{id}",
                        DeviceModel = (int) DeviceModel.MO_AF_A11_04_4X,
                        Ip = $"{ip1}{ip2++}",
                        Port = 17000,
                        DataPort = 8000,
                        DeviceType = DeviceType.流量检测器,
                        Device_Channels = new List<TrafficDevice_TrafficChannel>()
                    };
                    for (int j = 0; j < channelCount; ++j)
                    {
                        TrafficRoadCrossing roadCrossing = new TrafficRoadCrossing
                        {
                            CrossingId = id,
                            CrossingName = $"流量测试路口_{id}"
                        };
                        TrafficRoadSection roadSection = new TrafficRoadSection
                        {
                            SectionId = id,
                            SectionName = $"流量测试通路段_{id}",
                            SectionType = (int)SectionType.主干路,
                            SpeedLimit = 10,
                            Length = 10,
                            Direction = (int)LaneDirection.由东向西
                        };
                        TrafficChannel channel = new TrafficChannel
                        {
                            ChannelId = $"channel_{id}",
                            ChannelName = $"流量测试通道_{id}",
                            ChannelIndex = j + 1,
                            CrossingId = id,
                            SectionId = id,
                            ChannelType = (int)ChannelType.GB28181,
                            Lanes = new List<TrafficLane>(),
                            RoadCrossing = roadCrossing,
                            RoadSection = roadSection
                        };

                        TrafficDevice_TrafficChannel relation = new TrafficDevice_TrafficChannel
                        {
                            DeviceId = id,
                            ChannelId = channel.ChannelId,
                            Channel = channel
                        };
                        id++;
                        device.Device_Channels.Add(relation);
                        for (int k = 0; k < laneCount; ++k)
                        {
                            LaneDirection direction;
                            if (k >= 0 && k < 3)
                            {
                                direction = LaneDirection.由南向北;
                            }
                            else if (k >= 3 && k < 6)
                            {
                                direction = LaneDirection.由北向南;
                            }
                            else if (k >= 6 && k < 9)
                            {
                                direction = LaneDirection.由东向西;
                            }
                            else
                            {
                                direction = LaneDirection.由西向东;
                            }

                            FlowDirection flowDirection;
                            if (k % 3 == 0)
                            {
                                flowDirection = FlowDirection.直行;
                            }
                            else if (k % 3 == 1)
                            {
                                flowDirection = FlowDirection.左转;
                            }
                            else
                            {
                                flowDirection = FlowDirection.右转;
                            }
                            channel.Lanes.Add(new TrafficLane
                            {
                                ChannelId = channel.ChannelId,
                                LaneId = $"{k + 1:D2}",
                                LaneName = $"流量测试车道_{k + 1:D2}",
                                Channel = channel,
                                Direction = (int)direction,
                                FlowDirection = (int)flowDirection,
                                LaneIndex = k + 1,
                                Region = "[]",
                                Length = 10
                            });
                        }
                    }
                    context.Devices.Add(device);
                    devices.Add(device);
                    context.SaveChanges();
                }
            }
            return devices;
        }

        public static List<TrafficDevice> CreateDensityDevice(IServiceProvider serviceProvider, int deviceCount, int channelCount, int regionCount, string ip = "127.0.0.1",  bool initDatabase = false)
        {
            List<TrafficDevice> devices = new List<TrafficDevice>();
            using (IServiceScope serviceScope = serviceProvider.CreateScope())
            {
                using (DeviceContext context = serviceScope.ServiceProvider.GetRequiredService<DeviceContext>())
                {
                    if (initDatabase)
                    {
                        context.Database.EnsureDeleted();
                        context.Database.EnsureCreated();
                    }

                    int deviceId = 20000;
                    int crossingId = 20000;
                    int regionId = 20000;
                    int port = 17000;
                    for (int i = 0; i < deviceCount; ++i)
                    {
                        TrafficDevice device = new TrafficDevice
                        {
                            DeviceId = deviceId,
                            DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                            Ip = ip,
                            DataPort = port,
                            Port = port,
                            DeviceType = DeviceType.高点检测器
                        };
                        device.DeviceName = "高点测试设备" + device.DataPort;
                        device.Device_Channels = new List<TrafficDevice_TrafficChannel>();
                        for (int j = 0; j < channelCount; ++j)
                        {
                            TrafficRoadCrossing roadCrossing = new TrafficRoadCrossing
                            {
                                CrossingId = crossingId,
                                CrossingName = "高点测试路口" + crossingId
                            };

                            TrafficChannel channel = new TrafficChannel()
                            {
                                ChannelId = $"channel_{device.DeviceId}_{j + 1}",
                                ChannelName = $"高点测试通道 { device.DeviceId} {j + 1}",
                                ChannelType=(int)ChannelType.GB28181,
                                ChannelIndex = j + 1,
                                CrossingId = crossingId,
                                Regions = new List<TrafficRegion>(),
                                RoadCrossing = roadCrossing
                            };
                            
                            TrafficDevice_TrafficChannel relation = new TrafficDevice_TrafficChannel
                            {
                                ChannelId = channel.ChannelId,
                                DeviceId = device.DeviceId,
                                Channel = channel
                            };
                            port++;
                            deviceId++;
                            crossingId++;
                            device.Device_Channels.Add(relation);

                            for (int k = 0; k < regionCount; ++k)
                            {
                                channel.Regions.Add(new TrafficRegion
                                {
                                    ChannelId = channel.ChannelId,
                                    Channel = channel,
                                    RegionIndex = k + 1,
                                    RegionName = "高点测试区域" + regionId++,
                                    Region = "[]",
                                    IsVip = true,
                                    CarCount = 1,
                                    DensityRange=1,
                                    Density = 1,
                                    Frequency=1,
                                    Warning=1,
                                    Saturation=1,
                                    WarningDuration=1     
                                });
                            }
                        }
                        context.Devices.Add(device);
                        devices.Add(device);
                        context.SaveChanges();
                    }
            
                }

            }

            return devices;
        }

        public static List<TrafficDevice> CreateViolationDevice(IServiceProvider serviceProvider, int deviceCount, int channelCount, string ip = "127.0.0.1", bool initDatabase = false)
        {
            List<TrafficDevice> devices = new List<TrafficDevice>();
            using (IServiceScope serviceScope = serviceProvider.CreateScope())
            {
                using (DeviceContext context = serviceScope.ServiceProvider.GetRequiredService<DeviceContext>())
                {
                    if (initDatabase)
                    {
                        context.Database.EnsureDeleted();
                        context.Database.EnsureCreated();
                    }


                    int deviceId = 30000;
                    int locationId = 30000;
                    int port = 17000;
                    for (int i = 0; i < deviceCount; ++i)
                    {
                        TrafficDevice device = new TrafficDevice
                        {
                            DeviceId = deviceId,
                            DeviceModel = (int)DeviceModel.MO_AF_A11_04_4X,
                            Ip = ip,
                            DataPort = port,
                            Port = port,
                            DeviceType = DeviceType.违法检测器
                        };
                        device.DeviceName = "违法测试设备" + device.DataPort;
                        device.Device_Channels = new List<TrafficDevice_TrafficChannel>();
                        for (int j = 0; j < channelCount; ++j)
                        {
                            TrafficLocation location = new TrafficLocation
                            {
                                LocationId = locationId,
                                LocationName = "违法测试地点" + locationId,
                                LocationCode = locationId.ToString()
                            };

                            TrafficChannel channel = new TrafficChannel()
                            {
                                ChannelId = $"channel_{device.DeviceId}_{j + 1}",
                                ChannelName = $"违法测试通道 { device.DeviceId} {j + 1}",
                                ChannelType = (int)ChannelType.GB28181,
                                ChannelIndex = j + 1,
                                LocationId = locationId,
                                TrafficLocation = location
                            };

                            TrafficDevice_TrafficChannel relation = new TrafficDevice_TrafficChannel
                            {
                                ChannelId = channel.ChannelId,
                                DeviceId = device.DeviceId,
                                Channel = channel
                            };
                            port++;
                            deviceId++;
                            locationId++;
                            device.Device_Channels.Add(relation);
                        }
                        context.Devices.Add(device);
                        devices.Add(device);
                    }
                    context.SaveChanges();
                    return devices;
                }

            }
        }
    }
}
