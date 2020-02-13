using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

namespace MomobamiKirari.Monitor
{
    /// <summary>
    /// 流量设备监控
    /// </summary>
    public class FlowDeviceStatusMonitor: DeviceStatusMonitor
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志</param>
        /// <param name="configuration">配置项</param>
        /// <param name="httpClientFactory">http客户端工厂</param>
        /// <param name="memoryCache">缓存</param>
        public FlowDeviceStatusMonitor(ILogger<FlowDeviceStatusMonitor> logger,IConfiguration configuration,IHttpClientFactory httpClientFactory,IMemoryCache memoryCache)
            :base(logger, configuration,httpClientFactory, memoryCache)
        {
  
        }

        public override void Handle(DateTime lastTime,DateTime currentTime, DateTime nextTime)
        {
            HttpClient client = _httpClientFactory.CreateClient();
            List<TrafficDevice> devices = _memoryCache.GetDevices();
            foreach (var device in devices)
            {
                 List<int> oldChannelStatuses =
                    device.Device_Channels
                        .Select(r => r.Channel.ChannelStatus)
                        .ToList();
                FlowChannellistClass statusModel = client.Get<FlowChannellistClass>(
                    $"http://{device.Ip}:{device.Port}/app/aiboxManagerAPI/config_handler/channelparams");

                if (statusModel != null && statusModel.Code == 0)
                {
                    device.DeviceStatus = (int)DeviceStatus.正常;
                    DeviceClass deviceInfo = client.Get<DeviceClass>(
                        $"http://{device.Ip}:{device.Port}/app/aiboxManagerAPI/config_handler/get_systemparam");
                    if (deviceInfo != null && deviceInfo.Code == 0)
                    {
                        device.License = deviceInfo.Data.Licstatus;
                        device.Space = deviceInfo.Data.Space;
                        device.Systime = deviceInfo.Data.Systime;
                        device.Runtime = deviceInfo.Data.Runtime;
                    }
                    else
                    {
                        device.License = null;
                        device.Space = null;
                        device.Systime = null;
                        device.Runtime = null;
                    }
                    _logger.LogDebug((int)LogEvent.设备检查, $"设备正常 {device.Ip}");

                    foreach (var relation in device.Device_Channels)
                    {
                        var model = statusModel.Data.Channelinfolist
                            .FirstOrDefault(c => c.ChannelId == relation.Channel.ChannelId);
                        if (model == null)
                        {
                            relation.Channel.ChannelStatus = (int)DeviceStatus.异常;
                            _logger.LogDebug((int)LogEvent.设备检查, $"通道异常 {device.Ip}_{relation.Channel.ChannelName} 未找到该通道");
                        }
                        else
                        {
                            if (model.ChannelStatus == 1)
                            {
                                relation.Channel.ChannelStatus = (int)DeviceStatus.正常;
                                _logger.LogDebug((int)LogEvent.设备检查, $"通道正常 {device.Ip}_{relation.Channel.ChannelName}");
                            }
                            else
                            {
                                relation.Channel.ChannelStatus = (int)DeviceStatus.异常;
                                _logger.LogDebug((int)LogEvent.设备检查, $"通道异常 {device.Ip}_{relation.Channel.ChannelName} 状态值:{model.ChannelStatus}");
                            }
                        }
                    }
                }
                else
                {
                    device.DeviceStatus = (int)DeviceStatus.异常;
                    device.License = null;
                    device.Space = null;
                    device.Systime = null;
                    device.Runtime = null;
                    _logger.LogDebug((int)LogEvent.设备检查, $"设备异常 {device.Ip} 接口返回错误 {statusModel?.Code}");

                    foreach (var relation in device.Device_Channels)
                    {
                        relation.Channel.ChannelStatus = (int)DeviceStatus.异常;
                    }
                }

                HttpStatusCode? deviceResult=client.Put($"http://{_systemUrl}/api/devices/status", device);
                _logger.LogDebug((int)LogEvent.设备检查, $"设备 {device.Ip} 更新结果:{deviceResult}");

                for (int i = 0; i < oldChannelStatuses.Count; ++i)
                {
                    if (oldChannelStatuses[i] != device.Device_Channels[i].Channel.ChannelStatus)
                    {
                        HttpStatusCode? code=client.Put($"http://{_systemUrl}/api/channels/status", device.Device_Channels[i].Channel);
                        _logger.LogDebug((int)LogEvent.设备检查, $"通道 {device.Ip}_{device.Device_Channels[i].Channel.ChannelName} 旧状态:{oldChannelStatuses[i]} 新状态:{device.Device_Channels[i].Channel.ChannelStatus} 结果:{code}");
                    }
                }
            }
            _result.Clear();
            foreach (var device in devices)
            {
                _result.TryAdd($"设备-{device.DeviceName}_{device.Ip}", ((DeviceStatus)device.DeviceStatus).ToString());
                foreach (var relation in device.Device_Channels)
                {
                    _result.TryAdd($"通道-{relation.Channel.ChannelName}_{relation.Channel.ChannelIndex}", ((DeviceStatus)relation.Channel.ChannelStatus).ToString());
                }
            }
        }
    }

    public class FlowChannellistClass
    {
        public int Code { get; set; }
        public ChannelDataClass Data { get; set; }
    }

    public class ChannelDataClass
    {
        public int Totalnumber { get; set; }

        public ChannelinfolistClass[] Channelinfolist { get; set; }
    }

    public class ChannelinfolistClass
    {
        public string ChannelId { get; set; }
        public int ChannelStatus { get; set; }
    }

    public class DeviceClass
    {
        public int Code { get; set; }
        public DeviceDataClass Data { get; set; }
    }

    public class DeviceDataClass
    {
        public string Licstatus { get; set; }
        public string Space { get; set; }
        public string Systime { get; set; }
        public string Runtime { get; set; }
    }
}
