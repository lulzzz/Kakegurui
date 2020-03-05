using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kakegurui.Core;
using Kakegurui.Log;
using Kakegurui.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using MomobamiKirari.Codes;
using MomobamiKirari.DataFlow;
using MomobamiKirari.Models;
using Newtonsoft.Json;

namespace MomobamiKirari.Adapter
{
    /// <summary>
    /// 流量和视频结构化适配器
    /// </summary>
    public class FlowAdapter : IHealthCheck
    {
        /// <summary>
        /// 流量ws连接信息
        /// </summary>
        private class Item
        {
            /// <summary>
            /// 流量客户端
            /// </summary>
            public WebSocketClientChannel FlowChannel { get; set; }

            /// <summary>
            /// 流量正确数据数量
            /// </summary>
            public int FlowSuccess { get; set; }

            /// <summary>
            /// 流量异常数据数量
            /// </summary>
            public int FlowFailed { get; set; }

            /// <summary>
            /// 视频结构化客户端
            /// </summary>
            public WebSocketClientChannel VideoChannel { get; set; }

            /// <summary>
            /// 视频正确数据数量
            /// </summary>
            public int VideoSuccess { get; set; }

            /// <summary>
            /// 视频异常数据数量
            /// </summary>
            public int VideoFailed { get; set; }
        }

        /// <summary>
        /// 流量ws地址后缀
        /// </summary>
        private const string FlowUrl= "sub/crossingflow";

        /// <summary>
        /// 视频结构化ws地址后缀
        /// </summary>
        private const string VideoUrl= "sub/structall";

        /// <summary>
        /// 流量数据块
        /// </summary>
        public FlowBranchBlock _flowBranchBlock;

        /// <summary>
        /// 视频结构化数据块
        /// </summary>
        public VideoBranchBlock _videoBranchBlock;

        /// <summary>
        /// ws客户端集合
        /// </summary>
        private readonly ConcurrentDictionary<string,Item> _clients = new ConcurrentDictionary<string, Item>();

        /// <summary>
        /// 当前启动时间
        /// </summary>
        private DateTime _startTime;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志</param>
        public FlowAdapter(ILogger<FlowAdapter> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 接收到流量数据事件函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FlowReceivedHandler(object sender, WebSocketReceivedEventArges e)
        {
            string json = Encoding.UTF8.GetString(e.Packet.ToArray());
            if (_clients.TryGetValue(e.Uri.Authority, out Item item))
            {
                try
                {
                    var v = JsonConvert.DeserializeObject<FlowAdapterData>(json);
                    foreach (LaneAdapterData laneData in v.Data)
                    {
                        LaneFlow laneFlow = new LaneFlow
                        {
                            DataId = $"{laneData.ChannelId}_{laneData.LaneId}",
                            DateTime = laneData.DateTime,
                            Bikes = laneData.Bikes,
                            Tricycles = laneData.Tricycles,
                            Persons = laneData.Persons,
                            Cars = laneData.Cars,
                            Motorcycles = laneData.Motorcycles,
                            Buss = laneData.Buss,
                            Trucks = laneData.Trucks,
                            Vans = laneData.Vans,
                            AverageSpeedData = laneData.AverageSpeed,
                            HeadDistance = laneData.HeadDistance,
                            TimeOccupancy = laneData.TimeOccupancy,
                            Occupancy = laneData.Occupancy,
                            TrafficStatus = laneData.TrafficStatus,
                            Count = 1
                        };
                        item.FlowSuccess += 1;
                        _flowBranchBlock.Post(laneFlow);
                    }

                }
                catch (Exception ex)
                {
                    item.FlowFailed += 1;
                    _logger.LogError((int)LogEvent.数据适配, ex, "流量数据解析异常");
                }
            }
            else
            {
                _logger.LogWarning((int)LogEvent.数据适配, $"未知的数据项 {e.Uri.Authority}");
            }
        }

        /// <summary>
        /// 接收到视频结构化数据事件函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VideoReceivedHandler(object sender, WebSocketReceivedEventArges e)
        {
            string json = Encoding.UTF8.GetString(e.Packet.ToArray());
            if (_clients.TryGetValue(e.Uri.Authority, out Item item))
            {
                try
                {
                    var data = JsonConvert.DeserializeObject<VideoStructAdapterData>(json);
                    if (data.VideoStructType == VideoStructType.机动车)
                    {
                        VideoVehicle videoVehicle = new VideoVehicle
                        {
                            DataId = $"{data.ChannelId}_{ data.LaneId}",
                            DateTime = data.DateTime,
                            Image = string.IsNullOrEmpty(data.Image) ? string.Empty : data.Image,
                            Feature = string.IsNullOrEmpty(data.Feature) ? string.Empty : data.Feature,
                            CountIndex = data.CountIndex,
                            CarBrand = string.IsNullOrEmpty(data.CarBrand) ? string.Empty : data.CarBrand,
                            //为避免出现0
                            CarType = data.CarType + 1,
                            CarColor = data.CarColor,
                            PlateType = data.PlateType,
                            PlateNumber = string.IsNullOrEmpty(data.PlateNumber) ? string.Empty : data.PlateNumber
                        };
                        item.VideoSuccess += 1;
                        _videoBranchBlock.Post(videoVehicle);
                    }
                    else if (data.VideoStructType == VideoStructType.非机动车)
                    {
                        VideoBike videoBike = new VideoBike
                        {
                            DataId = $"{data.ChannelId}_{ data.LaneId}",
                            DateTime = data.DateTime,
                            Image = data.Image,
                            Feature = data.Feature,
                            CountIndex = data.CountIndex,
                            BikeType = data.BikeType
                        };
                        item.VideoSuccess += 1;
                        _videoBranchBlock.Post(videoBike);
                    }
                    else if (data.VideoStructType == VideoStructType.行人)
                    {
                        VideoPedestrain videoPedestrain = new VideoPedestrain
                        {
                            DataId = $"{data.ChannelId}_{ data.LaneId}",
                            DateTime = data.DateTime,
                            Image = data.Image,
                            Feature = data.Feature,
                            CountIndex = data.CountIndex,
                            Age = data.Age,
                            Sex = data.Sex,
                            UpperColor = data.UpperColor
                        };
                        item.VideoSuccess += 1;
                        _videoBranchBlock.Post(videoPedestrain);
                    }
                }
                catch (Exception ex)
                {
                    item.VideoFailed += 1;
                    _logger.LogError((int)LogEvent.数据适配, ex, "视频结构化数据解析异常");
                }
            }
            else
            {
                _logger.LogWarning((int)LogEvent.数据适配,$"未知的数据项 {e.Uri.Authority}");
            }
        }

        /// <summary>
        /// 获取设备在客户端集合中的key
        /// </summary>
        /// <param name="device">设备</param>
        /// <returns>key</returns>
        private string GetDeviceKey(FlowDevice device)
        {
            return GetDeviceUrl(device, string.Empty).Authority;
        }

        /// <summary>
        /// 获取设备在客户端集合中的url
        /// </summary>
        /// <param name="device">设备</param>
        /// <param name="url">相对路径</param>
        /// <returns>url</returns>
        private Uri GetDeviceUrl(FlowDevice device,string url)
        {
            return new Uri($"ws://{device.Ip}:{device.Port}/{url}");
        }

        /// <summary>
        /// 开始适配器
        /// </summary>
        /// <param name="devices">设备集合</param>
        /// <param name="flowBranchBlock">流量数据分支</param>
        /// <param name="videoBranchBlock">视频结构化数据分支</param>
        public void Start(List<FlowDevice> devices, FlowBranchBlock flowBranchBlock,VideoBranchBlock videoBranchBlock)
        {
            _startTime = DateTime.Now;

            _flowBranchBlock = flowBranchBlock;
            _videoBranchBlock = videoBranchBlock;
            foreach (FlowDevice device in devices.Where(d => d.FlowDevice_FlowChannels.Count > 0 && d.FlowDevice_FlowChannels.Any(c => c.Channel.Lanes.Count > 0)))
            {
                WebSocketClientChannel flowWebsocket = new WebSocketClientChannel(GetDeviceUrl(device,FlowUrl));
                flowWebsocket.WebSocketReceived += FlowReceivedHandler;
                WebSocketClientChannel videoWebsocket = new WebSocketClientChannel(GetDeviceUrl(device, VideoUrl));
                videoWebsocket.WebSocketReceived += VideoReceivedHandler;
                _clients.TryAdd(GetDeviceKey(device), new Item{ FlowChannel = flowWebsocket, VideoChannel = videoWebsocket });
                flowWebsocket.Start();
                videoWebsocket.Start();
            }
        }

        /// <summary>
        /// 重置设备集合
        /// </summary>
        /// <param name="devices">设备集合</param>
        public void Reset(List<FlowDevice> devices)
        {
            _startTime = DateTime.Now;

            foreach (var client in _clients)
            {
                bool exist = false;
                foreach (FlowDevice device in devices)
                {
                    if (client.Key== GetDeviceKey(device))
                    {
                        exist = true;
                        break;
                    }
                }
                if (!exist)
                {
                    client.Value.FlowChannel.Stop();
                    client.Value.VideoChannel.Stop();
                    _clients.TryRemove(client.Key,out _);
                }
            }

            foreach (FlowDevice device in devices.Where(d => d.FlowDevice_FlowChannels.Count > 0 && d.FlowDevice_FlowChannels.Any(c => c.Channel.Lanes.Count > 0)))
            {
                bool exist = false;
                foreach (var client in _clients)
                {
                    if (client.Key == GetDeviceKey(device))
                    {
                        exist = true;
                        break;
                    }
                }

                if (!exist)
                {
                    WebSocketClientChannel flowWebsocket = new WebSocketClientChannel(GetDeviceUrl(device, FlowUrl));
                    flowWebsocket.WebSocketReceived += FlowReceivedHandler;
                    WebSocketClientChannel videoWebsocket = new WebSocketClientChannel(GetDeviceUrl(device, VideoUrl));
                    videoWebsocket.WebSocketReceived += VideoReceivedHandler;
                    _clients.TryAdd(GetDeviceKey(device), new Item { FlowChannel = flowWebsocket, VideoChannel = videoWebsocket });
                    flowWebsocket.Start();
                    videoWebsocket.Start();
                }
            }
        }

        /// <summary>
        /// 停止适配器
        /// </summary>
        public void Stop()
        {
            foreach (var client in _clients)
            {
                client.Value.FlowChannel.Stop();
                client.Value.VideoChannel.Stop();
            }
            _clients.Clear();
        }

        #region 实现 IHealthCheck
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                {"启动时间", _startTime.ToString("yyyy-MM-dd HH:mm:ss")}
            };
            HealthStatus status=HealthStatus.Healthy;
            foreach (var client in _clients)
            {
                data.Add(client.Value.FlowChannel.Uri.ToString(), client.Value.FlowChannel.Connected?$"流量数据接收:{client.Value.FlowSuccess} 数据接收异常:{client.Value.FlowFailed}" :"断开连接");
                if (!client.Value.FlowChannel.Connected)
                {
                    status = HealthStatus.Degraded;
                }
            }
            foreach (var client in _clients)
            {
                data.Add(client.Value.VideoChannel.Uri.ToString(), client.Value.VideoChannel.Connected ? $"视频数据接收:{client.Value.VideoSuccess} 数据接收异常:{client.Value.VideoFailed}" : "断开连接");
                if (!client.Value.VideoChannel.Connected)
                {
                    status = HealthStatus.Degraded;
                }
            }

            return Task.FromResult(status == HealthStatus.Healthy ? HealthCheckResult.Healthy("数据源全部连接正常",data) : HealthCheckResult.Degraded("有连接异常的数据源",null, data));
        }
        #endregion

        internal class VideoStructAdapterData
        {
            public int Id { get; set; }
            public string ChannelId { get; set; }
            public string LaneId { get; set; }
            public long Timestamp { get; set; }
            public DateTime DateTime => TimeStampConvert.ToLocalDateTime(Timestamp / 1000 * 1000);
            public string Feature { get; set; }
            public string Image { get; set; }
            public int CountIndex { get; set; }
            public VideoStructType VideoStructType { get; set; }
            public int CarType { get; set; }
            public string CarBrand { get; set; }
            public int CarColor { get; set; }
            public string PlateNumber { get; set; }
            public int PlateType { get; set; }
            public int BikeType { get; set; }
            public int Sex { get; set; }
            public int Age { get; set; }
            public int UpperColor { get; set; }
        }

        internal class FlowAdapterData
        {
            public int Code { get; set; }
            public LaneAdapterData[] Data { get; set; }
            public LaneAdapterData[] Datas { get; set; }
        }

        internal class LaneAdapterData
        {
            public string ChannelId { get; set; }

            public string LaneId { get; set; }

            public long Timestamp { get; set; }

            public DateTime DateTime => TimeStampConvert.ToLocalDateTime(Timestamp);

            public int Cars { get; set; }

            public int Buss { get; set; }

            public int Trucks { get; set; }

            public int Vans { get; set; }

            public int Tricycles { get; set; }

            public int Vehicle => Cars + Buss + Tricycles + Trucks + Vans;

            public int Motorcycles { get; set; }

            public int Bikes { get; set; }

            public int Persons { get; set; }

            public int AverageSpeed { get; set; }

            public double HeadDistance { get; set; }

            public double HeadSpace { get; set; }

            public int TimeOccupancy { get; set; }

            public int Occupancy { get; set; }

            public TrafficStatus TrafficStatus { get; set; }
        }
    }
}
