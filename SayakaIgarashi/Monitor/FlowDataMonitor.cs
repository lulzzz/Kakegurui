using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ItsukiSumeragi.Data;
using ItsukiSumeragi.Models;
using Kakegurui.Log;
using Kakegurui.Monitor;
using Kakegurui.WebExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using MomobamiKirari.Adapter;
using MomobamiKirari.Data;
using MomobamiKirari.Models;
using ItsukiSumeragi.Codes.Device;

namespace SayakaIgarashi.Monitor
{
    public class FlowDataMonitor:IFixedJob, IHealthCheck
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<FlowDataMonitor> _logger;

        private readonly IHttpClientFactory _httpClientFactory;

        private readonly Dictionary<string, HashSet<DateTime>> _flowTimes = new Dictionary<string, HashSet<DateTime>>();

        private readonly Dictionary<string, HashSet<DateTime>> _videoTimes = new Dictionary<string, HashSet<DateTime>>();

        private Dictionary<string, object> _result = new Dictionary<string, object>();


        public FlowDataMonitor(IServiceProvider serviceProvider, ILogger<FlowDataMonitor> logger,IHttpClientFactory httpClientFactory)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public void Handle(DateTime lastTime, DateTime current, DateTime nextTime)
        {
            DateTime now=DateTime.Now;
            DateTime hour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
            Dictionary<string,object> result = new Dictionary<string,object>();
            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                using (DeviceContext deviceContext = serviceScope.ServiceProvider.GetRequiredService<DeviceContext>())
                {
                    using (FlowContext flowContext = serviceScope.ServiceProvider.GetRequiredService<FlowContext>())
                    {
                        List<TrafficDevice> flowDevices = deviceContext.Devices
                            .Where(d => d.DeviceType == DeviceType.流量检测器)
                            .Include(d => d.Device_Channels)
                            .ThenInclude(r => r.Channel)
                            .ThenInclude(c => c.Lanes)
                            .ToList();

                        foreach (TrafficDevice device in flowDevices)
                        {
                            foreach (var relation in device.Device_Channels.Where(rc => rc.Channel.ChannelStatus == (int)DeviceStatus.正常))
                            {
                                foreach (TrafficLane lane in relation.Channel.Lanes)
                                {
                                    if (!_videoTimes.ContainsKey(lane.DataId))
                                    {
                                        _videoTimes.Add(lane.DataId, new HashSet<DateTime>());
                                        _flowTimes.Add(lane.DataId, new HashSet<DateTime>());
                                    }
                                    FillFlowDatas(result, flowContext, hour, device, relation.Channel, lane);

                                    FillVideoDatas(result, flowContext, hour, device, relation.Channel, lane);

                                }
                            }
                        }
                    }
                }
            }
            _result = new Dictionary<string, object>(result);
        }

        private void FillFlowDatas(Dictionary<string, object> result,FlowContext flowContext, DateTime hour, TrafficDevice device, TrafficChannel channel, TrafficLane lane)
        {
            DateTime startTime = hour.AddHours(-2);
            DateTime endTime = hour.AddHours(-1);

            HttpClient client = _httpClientFactory.CreateClient();

            while (true)
            {
                if (_flowTimes[lane.DataId].Contains(startTime))
                {
                    _logger.LogInformation((int)LogEvent.数据监控, $"流量已存在 {device.Ip} {channel.ChannelId} {lane.LaneId} {startTime} {endTime}");
                    result.Add($"v_{device.Ip}_{channel.ChannelId}_{lane.LaneId}_{startTime}_{endTime}", "exist");
                }
                else
                {
                    try
                    {
                        FlowAdapterData historyFlows = client.Get<FlowAdapterData>(
                               $"http://{device.Ip}:{device.Port}/app/aiboxManagerAPI/historydata_handler/crossing_data?channelId={channel.ChannelId}&laneIds={lane.LaneId}&startTime={startTime:yyyy-MM-dd HH:mm:ss}&endTime={endTime:yyyy-MM-dd HH:mm:ss}&pageSize=0");
                        if (historyFlows == null || historyFlows.Code == 1)
                        {
                            _flowTimes[lane.DataId].RemoveWhere(t => t < endTime);
                            result.Add($"v_{device.Ip}_{channel.ChannelId}_{lane.LaneId}_{startTime}_{endTime}", "over");
                            _logger.LogInformation((int)LogEvent.数据监控, $"流量读取完成 {device.Ip} {channel.ChannelId} {lane.LaneId} {startTime} {endTime}");
                            break;
                        }
                        else
                        {
                            int oneMinuteCount = flowContext.LaneFlows_One
                                    .Count(f => f.DataId == lane.DataId && f.DateTime >= startTime && f.DateTime < endTime);
                            if (oneMinuteCount != 60)
                            {
                                List<LaneFlow_One> dbFlows = flowContext.LaneFlows_One
                                    .Where(f => f.DataId == lane.DataId && f.DateTime >= startTime && f.DateTime < endTime)
                                    .ToList();
                                for (int m = 0; m < 60; ++m)
                                {
                                    if (dbFlows.All(f => f.DateTime != startTime.AddMinutes(m)))
                                    {
                                        LaneAdapterData historyFlow =
                                            historyFlows.Datas.SingleOrDefault(f => f.DateTime == startTime.AddMinutes(m));
                                        if (historyFlow != null)
                                        {
                                            flowContext.LaneFlows_One.Add(new LaneFlow_One
                                            {
                                                DataId = lane.DataId,
                                                DateTime = historyFlow.DateTime,
                                                Cars = historyFlow.Cars,
                                                Buss = historyFlow.Buss,
                                                Vans = historyFlow.Vans,
                                                Tricycles = historyFlow.Tricycles,
                                                Trucks = historyFlow.Trucks,
                                                Motorcycles = historyFlow.Motorcycles,
                                                Bikes = historyFlow.Bikes,
                                                Persons = historyFlow.Persons,
                                                Distance = historyFlow.Vehicle * lane.Length,
                                                TravelTime = historyFlow.AverageSpeed == 0
                                                    ? 0 :
                                                    historyFlow.Vehicle * lane.Length / Convert.ToDouble(historyFlow.AverageSpeed * 1000 / 3600),
                                                HeadDistance = historyFlow.HeadDistance,
                                                Occupancy = historyFlow.Occupancy,
                                                TimeOccupancy = historyFlow.TimeOccupancy,
                                                Count = 1
                                            });
                                        }
                                    }
                                }
                            }

                            int fiveMinuteCount = flowContext.LaneFlows_Five
                                .Count(f => f.DataId == lane.DataId && f.DateTime >= startTime && f.DateTime < endTime);
                            if (fiveMinuteCount != 12)
                            {
                                List<LaneFlow_Five> dbFlows = flowContext.LaneFlows_Five
                                    .Where(f => f.DataId == lane.DataId && f.DateTime >= startTime && f.DateTime < endTime)
                                    .ToList();
                                for (int i = 0; i < 12; ++i)
                                {
                                    if (dbFlows.All(f => f.DateTime != startTime.AddMinutes(5 * i)))
                                    {
                                        var list = historyFlows.Datas
                                            .Where(f => f.DateTime >= startTime.AddMinutes(5 * i) &&
                                                        f.DateTime < startTime.AddMinutes(5 * i + 5))
                                            .ToList();
                                        flowContext.LaneFlows_Five.Add(new LaneFlow_Five
                                        {
                                            DataId = lane.DataId,
                                            DateTime = startTime.AddMinutes(5 * i),
                                            Cars = list.Sum(f => f.Cars),
                                            Buss = list.Sum(f => f.Buss),
                                            Vans = list.Sum(f => f.Vans),
                                            Tricycles = list.Sum(f => f.Tricycles),
                                            Trucks = list.Sum(f => f.Trucks),
                                            Motorcycles = list.Sum(f => f.Motorcycles),
                                            Bikes = list.Sum(f => f.Bikes),
                                            Persons = list.Sum(f => f.Persons),
                                            Distance = list.Sum(f => f.Vehicle * lane.Length),
                                            TravelTime = list.Sum(f => f.AverageSpeed == 0
                                                ? 0 :
                                                f.Vehicle * lane.Length / Convert.ToDouble(f.AverageSpeed * 1000 / 3600)),
                                            HeadDistance = list.Sum(f => f.HeadDistance),
                                            Occupancy = list.Sum(f => f.Occupancy),
                                            TimeOccupancy = list.Sum(f => f.TimeOccupancy),
                                            Count = 5
                                        });
                                    }
                                }
                            }

                            int fifteenMinuteCount = flowContext.LaneFlows_Fifteen
                                .Count(f => f.DataId == lane.DataId && f.DateTime >= startTime && f.DateTime < endTime);
                            if (fifteenMinuteCount != 4)
                            {
                                List<LaneFlow_Fifteen> dbFlows = flowContext.LaneFlows_Fifteen
                                    .Where(f => f.DataId == lane.DataId && f.DateTime >= startTime && f.DateTime < endTime)
                                    .ToList();
                                for (int i = 0; i < 4; ++i)
                                {
                                    if (dbFlows.All(f => f.DateTime != startTime.AddMinutes(15 * i)))
                                    {
                                        var list = historyFlows.Datas
                                            .Where(f => f.DateTime >= startTime.AddMinutes(15 * i) &&
                                                        f.DateTime < startTime.AddMinutes(15 * i + 15))
                                            .ToList();
                                        flowContext.LaneFlows_Fifteen.Add(new LaneFlow_Fifteen
                                        {
                                            DataId = lane.DataId,
                                            DateTime = startTime.AddMinutes(15 * i),
                                            Cars = list.Sum(f => f.Cars),
                                            Buss = list.Sum(f => f.Buss),
                                            Vans = list.Sum(f => f.Vans),
                                            Tricycles = list.Sum(f => f.Tricycles),
                                            Trucks = list.Sum(f => f.Trucks),
                                            Motorcycles = list.Sum(f => f.Motorcycles),
                                            Bikes = list.Sum(f => f.Bikes),
                                            Persons = list.Sum(f => f.Persons),
                                            Distance = list.Sum(f => f.Vehicle * lane.Length),
                                            TravelTime = list.Sum(f => f.AverageSpeed == 0
                                                ? 0 :
                                                f.Vehicle * lane.Length / Convert.ToDouble(f.AverageSpeed * 1000 / 3600)),
                                            HeadDistance = list.Sum(f => f.HeadDistance),
                                            Occupancy = list.Sum(f => f.Occupancy),
                                            TimeOccupancy = list.Sum(f => f.TimeOccupancy),
                                            Count = 15
                                        });
                                    }
                                }
                            }

                            int hourCount = flowContext.LaneFlows_Hour
                               .Count(f => f.DataId == lane.DataId && f.DateTime >= startTime && f.DateTime < endTime);
                            if (hourCount != 1)
                            {
                                List<LaneFlow_Hour> dbFlows = flowContext.LaneFlows_Hour
                                    .Where(f => f.DataId == lane.DataId && f.DateTime >= startTime && f.DateTime < endTime)
                                    .ToList();
                                for (int i = 0; i < 1; ++i)
                                {
                                    if (dbFlows.All(f => f.DateTime != startTime.AddMinutes(60 * i)))
                                    {
                                        var list = historyFlows.Datas
                                            .Where(f => f.DateTime >= startTime.AddMinutes(60 * i) &&
                                                        f.DateTime < startTime.AddMinutes(60 * i + 60))
                                            .ToList();
                                        flowContext.LaneFlows_Hour.Add(new LaneFlow_Hour
                                        {
                                            DataId = lane.DataId,
                                            DateTime = startTime.AddMinutes(60 * i),
                                            Cars = list.Sum(f => f.Cars),
                                            Buss = list.Sum(f => f.Buss),
                                            Vans = list.Sum(f => f.Vans),
                                            Tricycles = list.Sum(f => f.Tricycles),
                                            Trucks = list.Sum(f => f.Trucks),
                                            Motorcycles = list.Sum(f => f.Motorcycles),
                                            Bikes = list.Sum(f => f.Bikes),
                                            Persons = list.Sum(f => f.Persons),
                                            Distance = list.Sum(f => f.Vehicle * lane.Length),
                                            TravelTime = list.Sum(f => f.AverageSpeed == 0
                                                ? 0 :
                                                f.Vehicle * lane.Length / Convert.ToDouble(f.AverageSpeed * 1000 / 3600)),
                                            HeadDistance = list.Sum(f => f.HeadDistance),
                                            Occupancy = list.Sum(f => f.Occupancy),
                                            TimeOccupancy = list.Sum(f => f.TimeOccupancy),
                                            Count = 60
                                        });
                                    }
                                }
                            }
                            flowContext.BulkSaveChanges();
                            _flowTimes[lane.DataId].Add(new DateTime(startTime.Year, startTime.Month, startTime.Day,
                                startTime.Hour, 0, 0));
                            _logger.LogInformation((int)LogEvent.数据监控, $"流量入库 {device.Ip} {channel.ChannelId} {lane.LaneId} {startTime} {endTime} history:{historyFlows.Datas.Length} one:{oneMinuteCount} five:{fiveMinuteCount} fifteen:{fifteenMinuteCount} sixty:{hourCount}");
                            result.Add($"v_{device.Ip}_{channel.ChannelId}_{lane.LaneId}_{startTime}_{endTime}", "db");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError((int)LogEvent.数据监控, ex, $"流量异常 {device.Ip} {channel.ChannelId} {lane.LaneId}");
                        result.Add($"v_{device.Ip}_{channel.ChannelId}_{lane.LaneId}_{startTime}_{endTime}", "exception");
                        break;
                    }
                }

                startTime = startTime.AddHours(-1);
                endTime = endTime.AddHours(-1);
            }

        }

        private void FillVideoDatas(Dictionary<string, object> result,FlowContext flowContext, DateTime hour, TrafficDevice device,
            TrafficChannel channel, TrafficLane lane)
        {
            DateTime startTime = hour.AddHours(-1).AddMinutes(-5);
            DateTime endTime = hour.AddHours(-1);

            HttpClient client = _httpClientFactory.CreateClient();

            while (true)
            {
                if (_videoTimes[lane.DataId].Contains(startTime))
                {
                    _logger.LogInformation((int)LogEvent.数据监控, $"视频存在 {device.Ip} {channel.ChannelId} {lane.LaneId} {startTime}");
                    result.Add($"f_{device.Ip}_{channel.ChannelId}_{lane.LaneId}_{startTime}_{endTime}", "exist");
                }
                else
                {
                    try
                    {
                        VideoQueryData historyVehicles = client.Get<VideoQueryData>(
                            $"http://{device.Ip}:{device.Port}/app/aiboxManagerAPI/historydata_handler/struct_data?videoStructType=1&channelId={channel.ChannelId}&laneIds={lane.LaneId}&startTime={startTime:yyyy-MM-dd HH:mm:ss}&endTime={endTime:yyyy-MM-dd HH:mm:ss}&pageSize=0");
                        int dbVehicleCount;
                        if (historyVehicles.Code == 1)
                        {
                            _videoTimes[lane.DataId].RemoveWhere(t => t < endTime);
                            _logger.LogInformation((int)LogEvent.数据监控, $"视频完成 {device.Ip} {channel.ChannelId} {lane.LaneId} {startTime} {endTime}");
                            result.Add($"f_{device.Ip}_{channel.ChannelId}_{lane.LaneId}_{startTime}_{endTime}", "over");
                            break;
                        }
                        else
                        {
                            var vehicles = flowContext.Vehicles
                                .Where(v => v.DataId == lane.DataId && v.DateTime >= startTime && v.DateTime < endTime)
                                .ToList();
                            dbVehicleCount = vehicles.Count;
                            if (historyVehicles.Datas.Length > vehicles.Count)
                            {
                                foreach (var data in historyVehicles.Datas)
                                {
                                    if (vehicles.All(v => v.CountIndex != data.CountIndex))
                                    {
                                        flowContext.Vehicles.Add(new VideoVehicle
                                        {
                                            DataId = lane.DataId,
                                            DateTime = data.DateTime,
                                            Image = data.Image,
                                            Feature = data.Feature,
                                            CountIndex = data.CountIndex,
                                            CarBrand = data.CarBrand,
                                            CarType = data.CarType,
                                            CarColor = data.CarColor,
                                            PlateType = data.PlateType,
                                            PlateNumber = data.PlateNumber
                                        });
                                    }
                                }
                            }
                        }

                        VideoQueryData historyBikes = client.Get<VideoQueryData>(
                            $"http://{device.Ip}:{device.Port}/app/aiboxManagerAPI/historydata_handler/struct_data?videoStructType=2&channelId={channel.ChannelId}&laneIds={lane.LaneId}&startTime={startTime:yyyy-MM-dd HH:mm:ss}&endTime={endTime:yyyy-MM-dd HH:mm:ss}&pageSize=0");
                        int dbBikeCount = 0;
                        if (historyBikes.Code == 0)
                        {
                            var bikes = flowContext.Bikes
                                .Where(v => v.DataId == lane.DataId && v.DateTime >= startTime && v.DateTime < endTime)
                                .ToList();
                            dbBikeCount = bikes.Count;
                            if (historyBikes.Datas.Length > bikes.Count)
                            {
                                foreach (var data in historyBikes.Datas)
                                {
                                    if (bikes.All(v => v.CountIndex != data.CountIndex))
                                    {
                                        flowContext.Bikes.Add(new VideoBike
                                        {
                                            DataId = lane.DataId,
                                            DateTime = data.DateTime,
                                            Image = data.Image,
                                            Feature = data.Feature,
                                            CountIndex = data.CountIndex,
                                            BikeType = data.BikeType
                                        });
                                    }
                                }
                            }
                        }

                        VideoQueryData historyPedestrains = client.Get<VideoQueryData>($"http://{device.Ip}:{device.Port}/app/aiboxManagerAPI/historydata_handler/struct_data?videoStructType=3&channelId={channel.ChannelId}&laneIds={lane.LaneId}&startTime={startTime:yyyy-MM-dd HH:mm:ss}&endTime={endTime:yyyy-MM-dd HH:mm:ss}&pageSize=0");
                        int dbPedestrainCount = 0;
                        if (historyPedestrains.Code == 0)
                        {
                            var pedestrains = flowContext.Pedestrains
                                .Where(v => v.DataId == lane.DataId && v.DateTime >= startTime && v.DateTime < endTime)
                                .ToList();
                            dbPedestrainCount = pedestrains.Count;
                            if (historyPedestrains.Datas.Length > pedestrains.Count)
                            {
                                foreach (var data in historyPedestrains.Datas)
                                {
                                    if (pedestrains.All(v => v.CountIndex != data.CountIndex))
                                    {
                                        flowContext.Pedestrains.Add(new VideoPedestrain
                                        {
                                            DataId = lane.DataId,
                                            DateTime = data.DateTime,
                                            Image = data.Image,
                                            Feature = data.Feature,
                                            CountIndex = data.CountIndex,
                                            Age = data.Age,
                                            Sex = data.Sex,
                                            UpperColor = data.UpperColor
                                        });
                                    }
                                }
                            }
                        }

                        flowContext.BulkSaveChanges();
                        _videoTimes[lane.DataId].Add(new DateTime(startTime.Year, startTime.Month, startTime.Day,
                            startTime.Hour, startTime.Minute, 0));
                        _logger.LogInformation((int)LogEvent.数据监控, $"视频入库 {device.Ip} {channel.ChannelId} {lane.LaneId} {startTime} {endTime} vehicleHistory:{historyVehicles.Datas.Length} vehicleDb:{dbVehicleCount} bikeHistory:{historyBikes.Datas.Length} bikeDb:{dbBikeCount} pedestrainHistory:{historyPedestrains.Datas.Length} pedestrainDb:{dbPedestrainCount}");
                        result.Add($"f_{device.Ip}_{channel.ChannelId}_{lane.LaneId}_{startTime}_{endTime}", "db");

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError((int)LogEvent.数据监控, ex, $"视频异常 {device.Ip} {channel.ChannelId} {lane.LaneId}");
                        result.Add($"f_{device.Ip}_{channel.ChannelId}_{lane.LaneId}_{startTime}_{endTime}", "exception");
                        break;
                    }
                }

                startTime = startTime.AddMinutes(-5);
                endTime = endTime.AddMinutes(-5);
            }

        }


        #region 实现 IHealthCheck
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(HealthCheckResult.Healthy("数据检查", _result));
        }
        #endregion
    }

    public class VideoQueryData
    {
        public int Code { get; set; }
        public VideoStructAdapterData[] Datas { get; set; }
    }
}
