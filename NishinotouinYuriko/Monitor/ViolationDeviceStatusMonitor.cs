using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Models;
using ItsukiSumeragi.Monitor;
using Kakegurui.Log;
using Kakegurui.WebExtensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ItsukiSumeragi.Codes.Device;

namespace NishinotouinYuriko.Monitor
{
    /// <summary>
    /// 流量设备监控
    /// </summary>
    public class ViolationDeviceStatusMonitor: DeviceStatusMonitor
    {
        /// <summary>
        /// 本地ip
        /// </summary>
        private readonly string _ip;

        /// <summary>
        /// 本地监听端口
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志</param>
        /// <param name="configuration">配置项</param>
        /// <param name="httpClientFactory">http客户端工厂</param>
        /// <param name="memoryCache">缓存</param>
        public ViolationDeviceStatusMonitor(ILogger<ViolationDeviceStatusMonitor> logger,IConfiguration configuration,IHttpClientFactory httpClientFactory,IMemoryCache memoryCache)
            :base(logger, configuration,httpClientFactory, memoryCache)
        {
            _port=configuration.GetValue<int>("ListenPort");

            _ip = NetworkInterface
                .GetAllNetworkInterfaces()
                .Select(p => p.GetIPProperties())
                .SelectMany(p => p.UnicastAddresses)
                .FirstOrDefault(p => p.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(p.Address))?.Address.ToString();

            _logger.LogInformation((int)LogEvent.配置项,$"本机地址{_ip}");
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
                 ViolationChannellistClass statusModel = client.Get<ViolationChannellistClass>(
                    $"http://{device.Ip}:{device.Port}/api/channels");

                if (statusModel != null && statusModel.Code == 0)
                {
                    device.DeviceStatus = (int)DeviceStatus.正常;
                    DeviceDataClass deviceInfo = client.Get<DeviceDataClass>(
                        $"http://{device.Ip}:{device.Port}/api/sys/status");
                    if (deviceInfo != null && deviceInfo.Code == 0)
                    {
                        device.License = deviceInfo.Data.LicenceStatus?"正常":"异常";
                        device.Systime = deviceInfo.Data.DevTime;
                        device.Runtime = deviceInfo.Data.RunTime;
                        device.Cpu = $"{deviceInfo.Data.Cpu[0].Use:N2}%";
                        device.Memory =
                            $"{deviceInfo.Data.Mem.Free / 1024}MB/{deviceInfo.Data.Mem.Total / 1024}MB";
                        device.Space = $"{deviceInfo.Data.Disk.Available}/{deviceInfo.Data.Disk.Size}";
                    }
                    else
                    {
                        device.License = null;
                        device.Systime = null;
                        device.Runtime = null;
                        device.Cpu = null;
                        device.Memory = null;
                        device.Space = null;
                    }
         
                    foreach (var relation in device.Device_Channels)
                    {
                        var model = statusModel.Data
                            .FirstOrDefault(c => c.Osd_info.DeviceId == relation.Channel.ChannelDeviceId);
                        if (model == null)
                        {
                            relation.Channel.ChannelStatus = (int)DeviceStatus.异常;
                            _logger.LogDebug((int)LogEvent.设备检查, $"通道异常 {device.Ip}_{relation.Channel.ChannelDeviceId} 未找到该通道");
                        }
                        else
                        {
                            if (model.Status == 1 && model.Analysis_status==1)
                            {
                                relation.Channel.ChannelStatus = (int)DeviceStatus.正常;
                                _logger.LogDebug((int)LogEvent.设备检查, $"通道正常 {device.Ip}_{relation.Channel.ChannelName}");
                            }
                            else
                            {
                                relation.Channel.ChannelStatus = (int)DeviceStatus.异常;
                                _logger.LogDebug((int)LogEvent.设备检查, $"通道异常 {device.Ip}_{relation.Channel.ChannelName} 状态值:{model.Status} 分析状态值:{model.Analysis_status}");
                            }
                        }
                    }

                    HttpStatusCode? result=client.Post($"http://{device.Ip}:{device.Port}/api/data_report", new DataReportClass
                    {
                        Http2 = new DataReportDetailClass { Enable = true, Ip = _ip, Port = _port.ToString() }
                    });
                    _logger.LogDebug((int)LogEvent.设备检查, $"设备正常 {device.Ip} 数据上报 {result}");

                }
                else
                {
                    device.DeviceStatus = (int)DeviceStatus.异常;
                    device.License = null;
                    device.Systime = null;
                    device.Runtime = null;
                    device.Cpu = null;
                    device.Memory = null;
                    device.Space = null;
                    _logger.LogDebug((int)LogEvent.设备检查, $"设备异常 {device.Ip} 接口返回错误 {statusModel?.Code}");

                    foreach (var relation in device.Device_Channels)
                    {
                        relation.Channel.ChannelStatus = (int)DeviceStatus.异常;
                    }
                }

                client.Put($"http://{_systemUrl}/api/devices/status", device);

                for (int i = 0; i < oldChannelStatuses.Count; ++i)
                {
                    if (oldChannelStatuses[i] != device.Device_Channels[i].Channel.ChannelStatus)
                    {
                        client.Put($"http://{_systemUrl}/api/channels/status", device.Device_Channels[i].Channel);
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

    public class ViolationChannellistClass
    {
        public int Code { get; set; }
        public ViolationChannelClass[] Data { get; set; }
    }

    public class ViolationChannelClass
    {
        public int Id { get; set; }
        public string Video_path { get; set; }
        public int Jump_frame_step { get; set; }
        public int Status { get; set; }
        public int Analysis_status { get; set; }
        public OsdClass Osd_info { get; set; }
    }

    public class OsdClass
    {
        public string DeviceId { get; set; }
    }

    public class DeviceDataClass
    {
        public int Code { get; set; }
        public DeviceDetailClass Data { get; set; }
    }

    public class DeviceDetailClass
    {
        public string DevTime { get; set; }
        public string RunTime { get; set; }
        public string RunStatus { get; set; }
        public bool LicenceStatus { get; set; }
        public DiskClass Disk { get; set; }
        public CpuClass[] Cpu { get; set; }
        public MemoryClass Mem { get; set; }
    }

    public class CpuClass
    {
        public double Use { get; set; }
    }

    public class MemoryClass
    {
        public int Total { get; set; }
        public int Free { get; set; }
    }
    public class DiskClass
    {
        public string Available { get; set; }
        public string Size { get; set; }
    }

    public class DataReportClass
    {
        public DataReportDetailClass Http2 { get; set; }
    }

    public class DataReportDetailClass
    {
        public bool Enable { get; set; }
        public string Ip { get; set; }
        public string Port { get; set; }
    }

}
