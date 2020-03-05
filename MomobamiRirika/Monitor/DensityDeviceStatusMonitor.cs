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
using MomobamiRirika.Cache;
using MomobamiRirika.Managers;
using MomobamiRirika.Models;

namespace MomobamiRirika.Monitor
{
    /// <summary>
    /// 密度设备监控
    /// </summary>
    public class DensityDeviceStatusMonitor : DeviceStatusMonitor
    {
        internal class DensityChannelList
        {
            public int Code { get; set; }
            public DensityChannel[] Data { get; set; }
        }

        internal class DensityChannel
        {
            public int ChannelId { get; set; }
            public int Status { get; set; }
        }

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
        public DensityDeviceStatusMonitor(ILogger<DensityDeviceStatusMonitor> logger,IHttpClientFactory httpClientFactory,IMemoryCache memoryCache,IServiceProvider serviceProvider)
            : base(logger,httpClientFactory, memoryCache)
        {
            _serviceProvider = serviceProvider;
        }

        public override void Handle(DateTime lastTime,DateTime currentTime, DateTime nextTime)
        {
            HttpClient client = _httpClientFactory.CreateClient();
            List<DensityDevice> devices = _memoryCache.GetDevices();

            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                DevicesManager devicesManager = serviceScope.ServiceProvider.GetRequiredService<DevicesManager>();
                ChannelsManager channelsManager = serviceScope.ServiceProvider.GetRequiredService<ChannelsManager>();
                foreach (DensityDevice device in devices)
                {
                    int oldDeviceStatus = device.DeviceStatus;
                    List<int> oldChannelStatuses =
                        device.DensityDevice_DensityChannels
                            .Select(c => c.Channel.ChannelStatus)
                            .ToList();
                    DensityChannelList statusModel = client.Get<DensityChannelList>($"http://{device.Ip}:{device.Port}/api/channel/list");
                    if (statusModel != null && statusModel.Code == 0)
                    {
                        device.DeviceStatus = (int)DeviceStatus.正常;
                        _logger.LogDebug((int)LogEvent.设备检查, $"设备正常 {device.Ip}");

                        foreach (var relation in device.DensityDevice_DensityChannels)
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
                        foreach (var relation in device.DensityDevice_DensityChannels)
                        {
                            relation.Channel.ChannelStatus = (int)DeviceStatus.异常;
                        }
                        _logger.LogDebug((int)LogEvent.设备检查, $"设备异常 {device.Ip} 接口返回错误 {statusModel?.Code}");
                    }

                    if (oldDeviceStatus != device.DeviceStatus)
                    {
                        IStatusCodeActionResult deviceResult = devicesManager.UpdateStatus(new DensityDeviceUpdateStatus
                        {
                            DeviceId = device.DeviceId,
                            DeviceStatus = device.DeviceStatus
                        });
                        _logger.LogDebug((int)LogEvent.设备检查, $"设备 {device.Ip} 更新结果:{deviceResult.StatusCode}");
                    }

                    for (int i = 0; i < oldChannelStatuses.Count; ++i)
                    {
                        if (oldChannelStatuses[i] != device.DensityDevice_DensityChannels[i].Channel.ChannelStatus)
                        {
                            IStatusCodeActionResult channelResult = channelsManager.UpdateStatus(new DensityChannelUpdateStatus
                            {
                                ChannelId = device.DensityDevice_DensityChannels[i].Channel.ChannelId,
                                ChannelStatus = device.DensityDevice_DensityChannels[i].Channel.ChannelStatus
                            });
                            _logger.LogDebug((int)LogEvent.设备检查, $"通道 {device.Ip}_{device.DensityDevice_DensityChannels[i].Channel.ChannelName} 状态:{device.DensityDevice_DensityChannels[i].Channel.ChannelStatus} 结果:{channelResult.StatusCode}");
                        }
                    }
                }
                _result.Clear();
                foreach (DensityDevice device in devices)
                {
                    _result.TryAdd($"设备-{device.DeviceName}_{device.Ip}", ((DeviceStatus)device.DeviceStatus).ToString());
                    foreach (var relation in device.DensityDevice_DensityChannels)
                    {
                        _result.TryAdd($"通道-{relation.Channel.ChannelName}_{relation.Channel.ChannelIndex}", ((DeviceStatus)relation.Channel.ChannelStatus).ToString());
                    }
                }
            }
        }
    }

}
