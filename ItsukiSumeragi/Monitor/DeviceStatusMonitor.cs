using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ItsukiSumeragi.Codes;
using Kakegurui.Monitor;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ItsukiSumeragi.Monitor
{
    /// <summary>
    /// 设备监控
    /// </summary>
    public abstract class DeviceStatusMonitor : IFixedJob, IHealthCheck
    {
        /// <summary>
        /// 日志
        /// </summary>
        protected readonly ILogger _logger;

        /// <summary>
        /// http客户端工厂
        /// </summary>
        protected readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// 缓存
        /// </summary>
        protected readonly IMemoryCache _memoryCache;

        /// <summary>
        /// 检查结果
        /// </summary>
        protected readonly ConcurrentDictionary<string, object> _result = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志</param>
        /// <param name="httpClientFactory">http客户端工厂</param>
        /// <param name="memoryCache">缓存</param>
        protected DeviceStatusMonitor(ILogger logger,IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;
        }

        #region 实现IFixedJob
        public abstract void Handle(DateTime lastTime, DateTime currentTime, DateTime nextTime);
        #endregion

        #region 实现 IHealthCheck
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _result.Any(p => p.Value.ToString() == DeviceStatus.异常.ToString())
                    ? HealthCheckResult.Degraded("有连接异常的设备", null, _result)
                    : HealthCheckResult.Healthy("全部设备连接正常", _result));
        }
        #endregion
    }
}
