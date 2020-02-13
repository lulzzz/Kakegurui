using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Kakegurui.Monitor;
using Kakegurui.WebExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ItsukiSumeragi.Monitor
{
    /// <summary>
    /// 系统状态同步监控
    /// </summary>
    public class SystemSyncMonitor : IFixedJob, IHealthCheck
    {
        /// <summary>
        /// http客户端工厂
        /// </summary>
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// 当前时间
        /// </summary>
        private DateTime _dateTime;

        /// <summary>
        /// 配置项
        /// </summary>
        private readonly string _systemUrl;

        /// <summary>
        /// 字典改变
        /// </summary>
        public event EventHandler SystemStatusChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration">配置项</param>
        /// <param name="httpClientFactory">http客户端工厂</param>
        public SystemSyncMonitor(IConfiguration configuration,IHttpClientFactory httpClientFactory)
        {
            _systemUrl = configuration.GetValue<string>("SystemUrl");
            _httpClientFactory = httpClientFactory;
            _dateTime = DateTime.Now;
        }

        public void Handle(DateTime lastTime, DateTime current, DateTime nextTime)
        {
            HttpClient client = _httpClientFactory.CreateClient();
            SystemStatus status = client.Get<SystemStatus>($"http://{_systemUrl}/api/status");
            if (TimeStampConvert.ToUtcTimeStamp(_dateTime) < status.TimeStamp)
            {
                SystemStatusChanged?.Invoke(null, EventArgs.Empty);
                _dateTime = TimeStampConvert.ToLocalDateTime(status.TimeStamp);
            }
        }

        #region 实现 IHealthCheck
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            HttpClient client = _httpClientFactory.CreateClient();
            SystemStatus status = client.Get<SystemStatus>($"http://{_systemUrl}/api/status");
            if (status == null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("同步状态"));
            }

            DateTime remoteTime = TimeStampConvert.ToLocalDateTime(status.TimeStamp);
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                {"管理系统版本", status.Version},
                {"远程同步时间戳", remoteTime.ToString("yyyy-MM-dd HH:mm:ss")},
                {"本地同步时间戳", _dateTime.ToString("yyyy-MM-dd HH:mm:ss")}
            };
            return Task.FromResult(
                _dateTime < remoteTime
                    ? HealthCheckResult.Degraded("当前应用系统正在与管理系统同步", null, data)
                    : HealthCheckResult.Healthy("当前应用系统与管理系统处于同步状态", data));
        }
        #endregion
    }
}
