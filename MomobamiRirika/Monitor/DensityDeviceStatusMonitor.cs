using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Models;
using ItsukiSumeragi.Monitor;
using Kakegurui.Log;
using Kakegurui.WebExtensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ItsukiSumeragi.Codes.Device;

namespace MomobamiRirika.Monitor
{
    /// <summary>
    /// 密度设备监控
    /// </summary>
    public class DensityDeviceStatusMonitor : DeviceStatusMonitor
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志</param>
        /// <param name="configuration">配置项</param>
        /// <param name="httpClientFactory">http客户端工厂</param>
        /// <param name="memoryCache">缓存</param>
        public DensityDeviceStatusMonitor(ILogger<DensityDeviceStatusMonitor> logger, IConfiguration configuration,IHttpClientFactory httpClientFactory,IMemoryCache memoryCache)
            : base(logger, configuration,httpClientFactory, memoryCache)
        {

        }
        public override void Handle(DateTime lastTime,DateTime currentTime, DateTime nextTime)
        {
            HttpClient client = _httpClientFactory.CreateClient();
            List<TrafficDevice> devices = _memoryCache.GetDevices();
            foreach (TrafficDevice device in devices)
            {
                int oldDeviceStatus = device.DeviceStatus;
                List<int> oldChannelStatuses =
                    device.Device_Channels
                        .Select(c => c.Channel.ChannelStatus)
                        .ToList();
                DensityChannelList statusModel = client.Get<DensityChannelList>($"http://{device.Ip}:{device.Port}/api/channel/list");
                if (statusModel != null && statusModel.Code == 0)
                {
                    device.DeviceStatus = (int)DeviceStatus.正常;
                    _logger.LogDebug((int)LogEvent.设备检查, $"设备正常 {device.Ip}");

                    foreach (var relation in device.Device_Channels)
                    {
                        var model = statusModel.Data.FirstOrDefault(c =>
                            c.ChannelId == relation.Channel.ChannelIndex);
                        if (model == null)
                        {
                            relation.Channel.ChannelStatus = (int)DeviceStatus.异常;
                            _logger.LogDebug((int)LogEvent.设备检查, $"通道异常 {device.Ip}_{relation.Channel.ChannelName} 未找到通道");
                        }
                        else
                        {
                            if (model.Status == 1)
                            {
                                relation.Channel.ChannelStatus = (int)DeviceStatus.正常;
                                _logger.LogDebug((int)LogEvent.设备检查, $"通道正常 {device.Ip}_{relation.Channel.ChannelName}");
                            }
                            else
                            {
                                relation.Channel.ChannelStatus = (int)DeviceStatus.异常;
                                _logger.LogDebug((int)LogEvent.设备检查, $"通道异常 {device.Ip}_{relation.Channel.ChannelName} 状态值:{model.Status}");
                            }
                        }
                    }
                }
                else
                {
                    device.DeviceStatus = (int)DeviceStatus.异常;
                    foreach (var relation in device.Device_Channels)
                    {
                        relation.Channel.ChannelStatus = (int)DeviceStatus.异常;
                    }
                    _logger.LogDebug((int)LogEvent.设备检查, $"设备异常 {device.Ip} 接口返回错误 {statusModel?.Code}");
                }

                if (oldDeviceStatus != device.DeviceStatus)
                {
                    client.Put($"http://{_systemUrl}/api/devices/status", device);
                }

                for (int i = 0; i < oldChannelStatuses.Count; ++i)
                {
                    if (oldChannelStatuses[i] != device.Device_Channels[i].Channel.ChannelStatus)
                    {
                        client.Put($"http://{_systemUrl}/api/channels/status", device.Device_Channels[i].Channel);
                    }
                }
            }
            _result.Clear();
            foreach (TrafficDevice device in devices)
            {
                _result.TryAdd($"设备-{device.DeviceName}_{device.Ip}", ((DeviceStatus)device.DeviceStatus).ToString());
                foreach (var relation in device.Device_Channels)
                {
                    _result.TryAdd($"通道-{relation.Channel.ChannelName}_{relation.Channel.ChannelIndex}", ((DeviceStatus)relation.Channel.ChannelStatus).ToString());
                }
            }
        }
    }

    public class DensityChannelList
    {
        public int Code { get; set; }
        public DensityChannel[] Data { get; set; }
    }

    public class DensityChannel
    {
        public int ChannelId { get; set; }
        public int Status { get; set; }
        public string RtmpUrl { get; set; }
    }

}
