using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Kakegurui.Monitor;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace YumekoJabami.Monitor
{
    /// <summary>
    /// 系统监控
    /// </summary>
    public class SystemStatusMonitor:IFixedJob,IHealthCheck
    {
        /// <summary>
        /// 系统启动时间
        /// </summary>
        private readonly DateTime _startTime;

        /// <summary>
        /// 上一次进程cpu的时间总和
        /// </summary>
        private TimeSpan _lastCpuTime;

        /// <summary>
        /// 上一次计算的时间
        /// </summary>
        private DateTime _lastTime;

        /// <summary>
        /// cpu占用率
        /// </summary>
        private string _cpu;

        /// <summary>
        /// 系统版本
        /// </summary>
        public static string Version { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public SystemStatusMonitor()
        {
            _startTime=DateTime.Now;
        }

        #region 实现 IFixedJob

        public void Handle(DateTime lastTime,DateTime currentTime, DateTime nextTime)
        {
            DateTime now = DateTime.Now;
            Process process = Process.GetCurrentProcess();
            _cpu = $"{(process.TotalProcessorTime - _lastCpuTime).TotalMilliseconds / (now - _lastTime).TotalMilliseconds / Environment.ProcessorCount * 100:N2}";
            _lastCpuTime = process.TotalProcessorTime;
            _lastTime = now;
        }
        #endregion

        #region 实现 IHealthCheck
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            Process process = Process.GetCurrentProcess();
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                {"应用系统版本", Version},
                {"启动时间", _startTime.ToString("yyyy-MM-dd HH:mm:ss")},
                {"系统时间", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},
                {"cpu占用率", _cpu},
                {"内存占用", $"{process.WorkingSet64 / 1024.0 / 1024.0:N2} MB"},
                {"GC分配内存", $"{GC.GetTotalMemory(false) / 1024.0 / 1024.0:N2} MB"},
                {"线程数", process.Threads.Count}
            };
       
            return Task.FromResult(HealthCheckResult.Healthy("系统资源占用情况", data));
        }
        #endregion
    }
}
