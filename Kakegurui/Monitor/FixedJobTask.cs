using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Kakegurui.Core;
using Kakegurui.Log;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Monitor
{
    /// <summary>
    /// 定时任务
    /// </summary>
    public interface IFixedJob
    {
        /// <summary>
        /// 执行定时任务
        /// </summary>
        /// <param name="lastTime">上一个时间点</param>
        /// <param name="current">当前时间点</param>
        /// <param name="nextTime">下一个时间点</param>
        void Handle(DateTime lastTime, DateTime current, DateTime nextTime);
    }

    /// <summary>
    /// 定时任务信息
    /// </summary>
    public class FixedJobItem
    {
        public string Name { get; set; }
        public DateTimeLevel Level { get; set; }
        public DateTime CurrentTime { get; set; }
        public DateTime ChangeTime { get; set; }
        public TimeSpan Span { get; set; }
    }

    /// <summary>
    /// 定时任务
    /// </summary>
    public class FixedJobTask : TaskObject,IHealthCheck
    {
        /// <summary>
        /// 定时任务集合
        /// </summary>
        private readonly ConcurrentDictionary<IFixedJob, FixedJobItem> _fixedJobs = new ConcurrentDictionary<IFixedJob, FixedJobItem>();

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<FixedJobTask> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志</param>
        public FixedJobTask(ILogger<FixedJobTask> logger)
            : base("fixed job task")
        {
            _logger = logger;
        }

        /// <summary>
        /// 添加定时任务
        /// </summary>
        /// <param name="fixedJob">定时任务</param>
        /// <param name="level">时间间隔级别</param>
        /// <param name="span">时间偏移</param>
        /// <param name="name">任务名称</param>
        public void AddFixedJob(IFixedJob fixedJob, DateTimeLevel level, TimeSpan span, string name)
        {
            DateTime currentTime = TimePointConvert.CurrentTimePoint(level, DateTime.Now);
            DateTime changeTime = TimePointConvert.NextTimePoint(level, currentTime).Add(span);
            _logger.LogInformation((int)LogEvent.定时任务, $"添加定时任务 {name} {level} { changeTime:yyyy-MM-dd HH:mm:ss}");
            _fixedJobs.TryAdd(fixedJob, new FixedJobItem
            {
                Name = name,
                Level = level,
                Span = span,
                CurrentTime = currentTime,
                ChangeTime = changeTime
            });
        }

        /// <summary>
        ///移除定时任务
        /// </summary>
        /// <param name="fixedJob">定时任务</param>
        public void RemoteFixedJob(IFixedJob fixedJob)
        {
            if (_fixedJobs.TryRemove(fixedJob, out FixedJobItem item))
            {
                _logger.LogInformation((int)LogEvent.定时任务, $"移除定时任务 {item.Name} {item.Level}");
            }
        }

        #region 实现 IHealthCheck
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            foreach (var pair in _fixedJobs)
            {
                data.Add(pair.Value.Name, pair.Value.ChangeTime.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            return Task.FromResult(HealthCheckResult.Healthy("表示系统定时触发的一组后台任务", data));
        }
        #endregion

        protected override void ActionCore()
        {
            while (!IsCancelled())
            {
                DateTime now = DateTime.Now;
        
                foreach (var pair in _fixedJobs)
                {
                    if (now > pair.Value.ChangeTime)
                    {
                        DateTime lastTime = pair.Value.CurrentTime;
                        DateTime currentTime = TimePointConvert.NextTimePoint(pair.Value.Level, lastTime);
                        DateTime nextTime = TimePointConvert.NextTimePoint(pair.Value.Level, currentTime);

                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();
                        try
                        {
                            pair.Key.Handle(lastTime,currentTime,nextTime);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError((int)LogEvent.定时任务, ex, $"定制任务异常 {pair.Value.Name}");
                        }
                        stopwatch.Stop();

                        _logger.LogDebug((int)LogEvent.定时任务, $"定时任务完成 任务名称:{pair.Value.Name} 执行时间:{lastTime} 当前时间:{currentTime} 下次触发:{nextTime} 耗时:{stopwatch.ElapsedMilliseconds}毫秒");
                        pair.Value.CurrentTime = currentTime;
                        pair.Value.ChangeTime = nextTime.Add(pair.Value.Span);
                    }
                }
                Thread.Sleep(1000);
            }
        }
    }
}
