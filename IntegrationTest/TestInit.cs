using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Controller;
using ItsukiSumeragi.Data;
using ItsukiSumeragi.Models;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MomobamiKirari.Data;
using MomobamiRirika.Cache;
using MomobamiRirika.Controllers;
using MomobamiRirika.Data;
using MomobamiRirika.DataFlow;
using MomobamiRirika.Models;
using ItsukiSumeragi.Codes.Device;
using ItsukiSumeragi.Codes.Flow;

namespace IntegrationTest
{
    [TestClass]
    public class TestInit
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        [AssemblyInitialize]
        public static void MysqlDb(TestContext testContext)
        {
            IWebHost webHost = WebHost.CreateDefaultBuilder(new[]
                {
                    "DbIp=192.168.201.19",
                    "DbPort=3306",
                    "DbUser=traffic",
                    "DbPassword=Traffic1234.",
                    "DeviceDb=Traffic_Device_Temp",
                    "FlowDb=Traffic_Flow_Temp",
                    "VideoDb=Traffic_Video_Temp",
                    "DensityDb=Traffic_Density_Temp",
                    "EventDb=Traffic_Event_Temp"
                })
                .UseStartup<MySqlStartup>()
                .Build();
            ServiceProvider = webHost.Services;
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            using (DeviceContext context = ServiceProvider.GetRequiredService<DeviceContext>())
            {
                context.Database.EnsureDeleted();
            }

            using (FlowContext context = ServiceProvider.GetRequiredService<FlowContext>())
            {
                context.Database.EnsureDeleted();
            }

            using (FlowContext context = ServiceProvider.GetRequiredService<FlowContext>())
            {
                context.Database.EnsureDeleted();
            }

            using (DensityContext context = ServiceProvider.GetRequiredService<DensityContext>())
            {
                context.Database.EnsureDeleted();
            }

            using (DensityContext context = ServiceProvider.GetRequiredService<DensityContext>())
            {
                context.Database.EnsureDeleted();
            }
        }

        public static void ResetDeviceDb()
        {
            using (DeviceContext context =
                ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<DeviceContext>())
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }
        }

        public static void AddChannelDependency()
        {
            using (DeviceContext deviceContext =
                ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<DeviceContext>())
            {
                deviceContext.RoadCrossings.Add(new TrafficRoadCrossing { CrossingId = 1, CrossingName = "路口1" });
                deviceContext.RoadCrossings.Add(new TrafficRoadCrossing { CrossingId = 2, CrossingName = "路口2" });

                deviceContext.RoadSections.Add(
                    new TrafficRoadSection
                    {
                        SectionId = 1,
                        SectionName = "路口1",
                        SectionType = (int)SectionType.主干路,
                        Length = 1,
                        SpeedLimit = 1,
                        Direction = (int)LaneDirection.由东向西
                    });
                deviceContext.RoadSections.Add(new TrafficRoadSection
                {
                    SectionId = 2,
                    SectionName = "路口2",
                    SectionType = (int)SectionType.主干路,
                    Length = 1,
                    SpeedLimit = 1,
                    Direction = (int)LaneDirection.由东向西
                });
                deviceContext.Locations.Add(new TrafficLocation
                { LocationId = 1, LocationCode = "001", LocationName = "地点1" });
                deviceContext.Locations.Add(new TrafficLocation
                { LocationId = 2, LocationCode = "002", LocationName = "地点2" });

                deviceContext.Tags.Add(new TrafficTag { TagName = "LL", ChineseName = "标签1", Color = "", EnglishName = "", TagType = 1 });
                deviceContext.Tags.Add(new TrafficTag { TagName = "LR", ChineseName = "标签2", Color = "", EnglishName = "", TagType = 1 });

                deviceContext.Violations.Add(new TrafficViolation { ViolationId = 1, ViolationName = "违法1", GbCode = "", GbName = "", Violation_Tags = new List<TrafficViolation_TrafficTag> { new TrafficViolation_TrafficTag { TagName = "LL" } } });
                deviceContext.Violations.Add(new TrafficViolation { ViolationId = 2, ViolationName = "违法2", GbCode = "", GbName = "", Violation_Tags = new List<TrafficViolation_TrafficTag> { new TrafficViolation_TrafficTag { TagName = "LR" } } });
            }
           
        }

        public static List<TrafficDevice> RefreshFlowCache(List<TrafficDevice> devices)
        {
            using (IServiceScope serviceScope = ServiceProvider.CreateScope())
            {

                IMemoryCache memoryCache = serviceScope.ServiceProvider.GetRequiredService<IMemoryCache>();
                
                memoryCache.InitDeviceCache(devices);

                return devices;
            }
        }

        public static List<TrafficDevice> RefreshDensityCache(List<TrafficDevice> devices)
        {
            DensityCache.DensitiesCache.Clear();
            EventCache.LastEventsCache.Clear();
            WebSocketMiddleware.ClearUrl();

            WebSocketMiddleware.AddUrl(EventWebSocketBlock.EventUrl);

            DateTime now = DateTime.Now;
            DateTime yesterday = now.Date.AddDays(-1);

            using (IServiceScope serviceScope = ServiceProvider.CreateScope())
            {
                using (DensityContext context = serviceScope.ServiceProvider.GetRequiredService<DensityContext>())
                {
                    IMemoryCache memoryCache = serviceScope.ServiceProvider.GetRequiredService<IMemoryCache>();
                    
                    memoryCache.InitDeviceCache(devices);
                    DensitiesController densitiesController = new DensitiesController(context, memoryCache);
                    foreach (TrafficDevice device in devices)
                    {
                        foreach (var relation in device.Device_Channels)
                        {
                            foreach (TrafficRegion region in relation.Channel.Regions)
                            {
                                DensityCache.DensitiesCache.TryAdd(region.DataId, new ConcurrentQueue<TrafficDensity>(densitiesController.QueryList(region.DataId, yesterday, now)));
                                WebSocketMiddleware.AddUrl($"{DensityWebSocketBlock.DensityUrl}{region.DataId}");
                            }
                        }
                    }
                    return devices;
                }
            }
        }

        public static ChannelsController GetChannelsController()
        {
            return new ChannelsController(
                ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<DeviceContext>()
                , ServiceProvider.GetRequiredService<ILogger<ChannelsController>>()
                , ServiceProvider.GetRequiredService<IMemoryCache>());
        }

        public static DevicesController GetDevicesController()
        {
            return new DevicesController(
                ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<DeviceContext>()
                , ServiceProvider.GetRequiredService<ILogger<DevicesController>>()
                , ServiceProvider.GetRequiredService<IMemoryCache>());
        }

        public static RoadCrossingsController GetCrossingsController()
        {
            return new RoadCrossingsController(
                ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<DeviceContext>(),
                ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<ILogger<RoadCrossingsController>>());
        }

        public static RoadSectionsController GetSectionsController()
        {
            return new RoadSectionsController(
                ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<DeviceContext>()
                , ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<ILogger<RoadSectionsController>>()
                , ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<IMemoryCache>()
            );
        }

        public static LocationsController GetLocationsController()
        {
            return new LocationsController(
                ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<DeviceContext>()
                , ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<ILogger<LocationsController>>()
            );
        }

        public static TagsController GetTagsController()
        {
            return new TagsController(
                ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<DeviceContext>()
                , ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<IMemoryCache>()
            );
        }

        public static ViolationsController GetViolationsController()
        {
            return new ViolationsController(
                ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<DeviceContext>()
            );
        }
    }
}
