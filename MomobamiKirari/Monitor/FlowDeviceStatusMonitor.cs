using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using ItsukiSumeragi.Codes;
using ItsukiSumeragi.Monitor;
using Kakegurui.Log;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MomobamiKirari.Cache;
using MomobamiKirari.Managers;
using MomobamiKirari.Models;

namespace MomobamiKirari.Monitor
{
    /// <summary>
    /// 流量设备监控
    /// </summary>
    public class FlowDeviceStatusMonitor: DeviceStatusMonitor
    {
        /// <summary>
        /// 实例工厂
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志</param>
        /// <param name="httpClientFactory">http客户端工厂</param>
        /// <param name="memoryCache">缓存</param>
        /// <param name="serviceProvider">实例工厂</param>
        public FlowDeviceStatusMonitor(ILogger<FlowDeviceStatusMonitor> logger,IHttpClientFactory httpClientFactory,IMemoryCache memoryCache,IServiceProvider serviceProvider)
            :base(logger,httpClientFactory, memoryCache)
        {
            _serviceProvider = serviceProvider;
        }

        public override void Handle(DateTime lastTime,DateTime currentTime, DateTime nextTime)
        {
            HttpClient client = _httpClientFactory.CreateClient();
            List<FlowDevice> devices = _memoryCache.GetDevices();
            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                DevicesManager devicesManager = serviceScope.ServiceProvider.GetRequiredService<DevicesManager>();
                ChannelsManager channelsManager = serviceScope.ServiceProvider.GetRequiredService<ChannelsManager>();
                foreach (var device in devices)
                {
                    List<int> oldChannelStatuses =
                       device.FlowDevice_FlowChannels
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

                        foreach (var relation in device.FlowDevice_FlowChannels)
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

                        foreach (var relation in device.FlowDevice_FlowChannels)
                        {
                            relation.Channel.ChannelStatus = (int)DeviceStatus.异常;
                        }
                    }

                    IStatusCodeActionResult deviceResult = devicesManager.UpdateStatus(new FlowDeviceUpdateStatus
                    {
                        DeviceId = device.DeviceId,
                        DeviceStatus = device.DeviceStatus,
                        License = device.License,
                        Space = device.Space,
                        Systime = device.Systime,
                        Runtime = device.Runtime
                    });
                    _logger.LogDebug((int)LogEvent.设备检查, $"设备:{device.Ip} 状态:{device.DeviceStatus} 更新结果:{deviceResult.StatusCode}");

                    for (int i = 0; i < oldChannelStatuses.Count; ++i)
                    {
                        if (oldChannelStatuses[i] != device.FlowDevice_FlowChannels[i].Channel.ChannelStatus)
                        {
                            IStatusCodeActionResult channelResult = channelsManager.UpdateStatus(new FlowChannelUpdateStatus
                            {
                                ChannelId = device.FlowDevice_FlowChannels[i].Channel.ChannelId,
                                ChannelStatus = device.FlowDevice_FlowChannels[i].Channel.ChannelStatus
                            });
                            _logger.LogDebug((int)LogEvent.设备检查, $"通道 {device.Ip}_{device.FlowDevice_FlowChannels[i].Channel.ChannelName} 状态:{device.FlowDevice_FlowChannels[i].Channel.ChannelStatus} 结果:{channelResult.StatusCode}");
                        }
                    }
                }
            }
    
            _result.Clear();
            foreach (var device in devices)
            {
                _result.TryAdd($"设备-{device.DeviceName}_{device.Ip}", ((DeviceStatus)device.DeviceStatus).ToString());
                foreach (var relation in device.FlowDevice_FlowChannels)
                {
                    _result.TryAdd($"通道-{relation.Channel.ChannelName}_{relation.Channel.ChannelIndex}", ((DeviceStatus)relation.Channel.ChannelStatus).ToString());
                }
            }
        }

        internal class FlowChannellistClass
        {
            public int Code { get; set; }
            public ChannelDataClass Data { get; set; }
        }

        internal class ChannelDataClass
        {
            public int Totalnumber { get; set; }

            public ChannelinfolistClass[] Channelinfolist { get; set; }
        }

        internal class ChannelinfolistClass
        {
            public string ChannelId { get; set; }
            public int ChannelStatus { get; set; }
        }

        internal class DeviceClass
        {
            public int Code { get; set; }
            public DeviceDataClass Data { get; set; }
        }

        internal class DeviceDataClass
        {
            public string Licstatus { get; set; }
            public string Space { get; set; }
            public string Systime { get; set; }
            public string Runtime { get; set; }
        }
    }

}
