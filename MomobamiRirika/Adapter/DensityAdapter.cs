using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kakegurui.Log;
using Kakegurui.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using MomobamiRirika.DataFlow;
using MomobamiRirika.Models;
using Newtonsoft.Json;

namespace MomobamiRirika.Adapter
{
    /// <summary>
    /// 高点密度数据适配器
    /// </summary>
    public class DensityAdapter:IHealthCheck
    {
        /// <summary>
        /// 密度ws连接信息
        /// </summary>
        internal class Item
        {
            /// <summary>
            /// 密度客户端
            /// </summary>
            public WebSocketClientChannel Channel { get; set; }

            /// <summary>
            /// 密度数据正确数量
            /// </summary>
            public int DensitySuccess { get; set; }

            /// <summary>
            /// 事件数据正确数量
            /// </summary>
            public int EventSuccess { get; set; }

            /// <summary>
            /// 数据异常数量
            /// </summary>
            public int Failed { get; set; }
        }

        internal class DensityAdapterData
        {
            public DensityData data { get; set; }
            public string type { get; set; }
        }

        internal class DensityData
        {
            public string record_time { get; set; }
            public int channel_id { get; set; }
            public int region_id { get; set; }
            public int count { get; set; }
        }

        /// <summary>
        /// url
        /// </summary>
        private const string Url = "high_point";

        /// <summary>
        /// ws客户端集合
        /// </summary>
        private readonly ConcurrentDictionary<string, Item> _clients=new ConcurrentDictionary<string, Item>();

        /// <summary>
        /// 密度数据块
        /// </summary>
        private DensityBranchBlock _densityBranchBlock;

        /// <summary>
        /// 拥堵事件数据块
        /// </summary>
        private EventBranchBlock _eventBranchBlock;

        /// <summary>
        /// 当前启动时间
        /// </summary>
        private DateTime _startTime;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<DensityAdapter> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志</param>
        public DensityAdapter(ILogger<DensityAdapter> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 接收数据处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReceiveHandler(object sender, WebSocketReceivedEventArges e)
        {
            string json = Encoding.UTF8.GetString(e.Packet.ToArray());
            if (_clients.TryGetValue(e.Uri.Authority, out Item item))
            {
                try
                {
                    var v = JsonConvert.DeserializeObject<DensityAdapterData>(json);
                    if (v.type == "car_count")
                    {
                        _densityBranchBlock.Post(new TrafficDensity
                        {
                            MatchId = $"{e.Uri.Host}_{v.data.channel_id}_{v.data.region_id}",
                            DateTime =
                                DateTime.ParseExact(v.data.record_time, "yyyyMMddHHmmss", CultureInfo.CurrentCulture),
                            Value = v.data.count
                        });
                        item.DensitySuccess += 1;
                    }
                    else if (v.type == "crowd_data")
                    {
                        _eventBranchBlock.Post(new TrafficEvent
                        {
                            MatchId = $"{e.Uri.Host}_{v.data.channel_id}_{v.data.region_id}",
                            DateTime = DateTime.ParseExact(v.data.record_time, "yyyyMMddHHmmss", CultureInfo.CurrentCulture)
                        });
                        item.EventSuccess += 1;
                    }
                }
                catch (Exception ex)
                {
                    item.Failed += 1;
                    _logger.LogError((int)LogEvent.数据适配, ex, "密度数据解析异常");
                }
            }
            else
            {
                _logger.LogWarning((int)LogEvent.数据适配, $"未知的数据项 {e.Uri.Authority}");
            }
        }

        /// <summary>
        /// 获取设备在客户端集合中的key
        /// </summary>
        /// <param name="device">设备</param>
        /// <returns>key</returns>
        private string GetDeviceKey(DensityDevice device)
        {
            return GetDeviceUrl(device).Authority;
        }

        /// <summary>
        /// 获取设备在客户端集合中的url
        /// </summary>
        /// <param name="device">设备</param>
        /// <returns>url</returns>
        private Uri GetDeviceUrl(DensityDevice device)
        {
            return new Uri($"ws://{device.Ip}:{device.DataPort}/{Url}");
        }

        /// <summary>
        /// 开始适配器
        /// </summary>
        /// <param name="devices">设备集合</param>
        /// <param name="densityBranchBlock">密度数据块分支</param>
        /// <param name="eventBranchBlock">事件数据块分支</param>
        public void Start(List<DensityDevice> devices, DensityBranchBlock densityBranchBlock,EventBranchBlock eventBranchBlock)
        {
            _startTime = DateTime.Now;

            _densityBranchBlock = densityBranchBlock;
            _eventBranchBlock = eventBranchBlock;

            foreach (DensityDevice device in devices.Where(d => d.DensityDevice_DensityChannels.Count > 0 && d.DensityDevice_DensityChannels.Any(r => r.Channel.Regions.Count > 0)))
            {
                WebSocketClientChannel websocket = new WebSocketClientChannel(GetDeviceUrl(device));
                websocket.WebSocketReceived += ReceiveHandler;
                _clients.TryAdd(GetDeviceKey(device), new Item
                {
                    Channel = websocket
                });
                websocket.Start();
            }
        }

        public void Reset(List<DensityDevice> devices)
        {
            _startTime = DateTime.Now;

            foreach (var client in _clients)
            {
                bool exist = false;
                foreach (DensityDevice device in devices)
                {
                    if (client.Key == GetDeviceKey(device))
                    {
                        exist = true;
                        break;
                    }
                }
                if (!exist)
                {
                    client.Value.Channel.Stop();
                    _clients.TryRemove(client.Key,out _);
                }
            }

            foreach (DensityDevice device in devices.Where(d => d.DensityDevice_DensityChannels.Count > 0 && d.DensityDevice_DensityChannels.Any(c => c.Channel.Regions.Count > 0)))
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
                    WebSocketClientChannel websocket = new WebSocketClientChannel(GetDeviceUrl(device));
                    websocket.WebSocketReceived += ReceiveHandler;
                    _clients.TryAdd(GetDeviceKey(device), new Item
                    {
                        Channel = websocket
                    });
                    websocket.Start();
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
                client.Value.Channel.Stop();
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
            HealthStatus status = HealthStatus.Healthy;
            foreach (var client in _clients)
            {
                data.Add(client.Value.Channel.Uri.ToString(), client.Value.Channel.Connected ? $"密度数据接收:{client.Value.DensitySuccess} 事件数据接收:{client.Value.EventSuccess} 数据解析异常:{client.Value.Failed}" : "断开连接");
                if (!client.Value.Channel.Connected)
                {
                    status = HealthStatus.Degraded;
                }
            }
            return Task.FromResult(status == HealthStatus.Healthy ? HealthCheckResult.Healthy("数据源全部连接正常", data) : HealthCheckResult.Degraded("有连接异常的数据源", null, data));
        }
        #endregion
    }
}
