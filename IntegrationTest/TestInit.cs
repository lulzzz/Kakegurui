using System;
using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MomobamiKirari.Cache;
using MomobamiKirari.Data;
using MomobamiKirari.Models;
using MomobamiRirika.Data;
using MomobamiRirika.Models;

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
            using (FlowContext context = ServiceProvider.GetRequiredService<FlowContext>())
            {
                context.Database.EnsureDeleted();
            }

            using (DensityContext context = ServiceProvider.GetRequiredService<DensityContext>())
            {
                context.Database.EnsureDeleted();
            }
        }

        public static List<FlowDevice> RefreshFlowCache(List<FlowDevice> devices)
        {
            //using (IServiceScope serviceScope = ServiceProvider.CreateScope())
            //{

            //    IMemoryCache memoryCache = serviceScope.ServiceProvider.GetRequiredService<IMemoryCache>();

            //    memoryCache.InitDeviceCache(devices);

            //    return devices;
            //}
            return null;
        }

        public static List<DensityDevice> RefreshDensityCache(List<DensityDevice> devices)
        {
            //DensityCache.DensitiesCache.Clear();
            //EventCache.LastEventsCache.Clear();
            //WebSocketMiddleware.ClearUrl();

            //WebSocketMiddleware.AddUrl(EventWebSocketBlock.EventUrl);

            //DateTime now = DateTime.Now;
            //DateTime yesterday = now.Date.AddDays(-1);

            //using (IServiceScope serviceScope = ServiceProvider.CreateScope())
            //{
            //    using (DensityContext context = serviceScope.ServiceProvider.GetRequiredService<DensityContext>())
            //    {
            //        IMemoryCache memoryCache = serviceScope.ServiceProvider.GetRequiredService<IMemoryCache>();

            //        memoryCache.InitDeviceCache(devices);
            //        DensitiesController densitiesController = new DensitiesController(context, memoryCache);
            //        foreach (FlowDevice device in devices)
            //        {
            //            foreach (var relation in device.FlowDevice_FlowChannels)
            //            {
            //                foreach (TrafficRegion region in relation.Channel.Regions)
            //                {
            //                    DensityCache.DensitiesCache.TryAdd(region.DataId, new ConcurrentQueue<TrafficDensity>(densitiesController.QueryList(region.DataId, yesterday, now)));
            //                    WebSocketMiddleware.AddUrl($"{DensityWebSocketBlock.DensityUrl}{region.DataId}");
            //                }
            //            }
            //        }
            //        return devices;
            //    }
            //}
            return null;

        }
    }
}
