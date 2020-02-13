using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using ItsukiSumeragi.Models;
using Kakegurui.Log;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ItsukiSumeragi.DataFlow
{
    /// <summary>
    /// 交通数据源数据块
    /// </summary>
    public abstract class TrafficBranchBlock<T> : IHealthCheck where T : TrafficData
    {
        /// <summary>
        /// 最大批处理数量
        /// </summary>
        public const int BatchSize = 1000;

        /// <summary>
        /// 入库线程数
        /// </summary>
        public const int ThreadCount = 8;

        /// <summary>
        /// 当前接受数据的最小时间点
        /// </summary>
        private DateTime _minTime;

        /// <summary>
        /// 当前接受数据的最大时间点
        /// </summary>
        private DateTime _maxTime;

        /// <summary>
        /// 当前时间分支的数据块
        /// </summary>
        private BufferBlock<T> _currentBlock;

        /// <summary>
        /// 下一个时间分支的数据块
        /// </summary>
        private BufferBlock<T> _nextBlock;

        /// <summary>
        /// 数据处理数据块
        /// </summary>
        private ActionBlock<T> _actionBlock;

        /// <summary>
        /// 当前时间分支数据块的释放接口
        /// </summary>
        private IDisposable _currentDisposable;

        /// <summary>
        /// 日志事件
        /// </summary>
        private readonly LogEvent _logEvent;

        /// <summary>
        /// 实例工厂
        /// </summary>
        protected readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 日志
        /// </summary>
        protected readonly ILogger _logger;

        /// <summary>
        /// 设备列表
        /// </summary>
        protected readonly List<TrafficDevice> _devices = new List<TrafficDevice>();

        /// <summary>
        /// 下个时间段总数
        /// </summary>
        protected int _next;

        /// <summary>
        /// 时间范围外总数
        /// </summary>
        protected int _over;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">实例工厂</param>
        /// <param name="logEvent">日志事件</param>
        protected TrafficBranchBlock(IServiceProvider serviceProvider,LogEvent logEvent)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<TrafficBranchBlock<T>>>();
            _logEvent = logEvent;
        }

        /// <summary>
        /// 供子类实现的打开数据块
        /// </summary>
        /// <returns></returns>
        protected abstract void OpenCore();

        /// <summary>
        /// 供子类实现的重置数据块
        /// </summary>
        /// <param name="devices">设备集合</param>
        protected virtual void ResetCore(List<TrafficDevice> devices)
        {

        }

        /// <summary>
        /// 处理交通数据
        /// </summary>
        /// <param name="t">交通数据</param>
        protected abstract void Handle(T t);

        /// <summary>
        /// 供子类实现的关闭数据块
        /// </summary>
        protected abstract void CloseCore();

        /// <summary>
        /// 打开数据块，不会根据设备划分数据块，此时重置无效。将接收任何时间范围内的数据，此时切换分支无效
        /// </summary>
        public void Open()
        {
            Open(new List<TrafficDevice>(), DateTime.MinValue, DateTime.MaxValue);
        }

        /// <summary>
        /// 打开数据块，将接收任何时间范围内的数据，此时切换分支无效
        /// </summary>
        /// <param name="devices">设备列表</param>
        public void Open(List<TrafficDevice> devices)
        {
            Open(devices,DateTime.MinValue, DateTime.MaxValue);
        }

        /// <summary>
        /// 打开数据块，不会根据设备划分数据块，此时重置无效
        /// </summary>
        /// <param name="minTime">接收数据的最小时间</param>
        /// <param name="maxTime">接收数据的最大时间</param>
        public void Open(DateTime minTime, DateTime maxTime)
        {
            Open(new List<TrafficDevice>(),minTime,maxTime);
        }

        /// <summary>
        /// 打开数据块
        /// </summary>
        /// <param name="devices">设备列表</param>
        /// <param name="minTime">接收数据的最小时间</param>
        /// <param name="maxTime">接收数据的最大时间</param>
        public void Open(List<TrafficDevice> devices, DateTime minTime, DateTime maxTime)
        {
            _devices.Clear();
            _devices.AddRange(devices);
            _minTime = minTime;
            _maxTime = maxTime;
            _currentBlock = new BufferBlock<T>();
            _nextBlock = new BufferBlock<T>();
            _next = 0;
            _over = 0;
            OpenCore();
            _actionBlock = new ActionBlock<T>(Handle);
            _currentDisposable = _currentBlock.LinkTo(_actionBlock, new DataflowLinkOptions { PropagateCompletion = true });
        }

        /// <summary>
        /// 重置数据块
        /// </summary>
        /// <param name="devices">设备集合</param>
        public void Reset(List<TrafficDevice> devices)
        {
            ResetCore(devices);
            _devices.Clear();
            _devices.AddRange(devices);
        }

        /// <summary>
        /// 向数据块发送交通数据
        /// </summary>
        /// <param name="t">交通数据</param>
        public void Post(T t)
        {
            if (t.DateTime >= _minTime && t.DateTime < _maxTime)
            {
                _currentBlock.Post(t);
            }
            else if (t.DateTime >= _maxTime)
            {
                _nextBlock.Post(t);
                _next += 1;
            }
            else
            {
                _over += 1;
                _logger.LogWarning((int)_logEvent, $"过期数据 最小时间:{_minTime:yyyy-MM-dd HH:mm:ss} 最大时间:{_maxTime:yyyy-MM-dd HH:mm:ss} 当前时间:{t.DateTime:yyyy-MM-dd HH:mm:ss}");
            }
        }

        /// <summary>
        /// 触发保存
        /// </summary>
        public virtual void TriggerSave()
        {

        }
        /// <summary>
        /// 切换分支
        /// </summary>
        public void SwitchBranch(DateTime minTime, DateTime maxTime)
        {
            if (_minTime == DateTime.MinValue && _maxTime == DateTime.MaxValue)
            {
                return;
            }
            _currentDisposable.Dispose();
            OpenCore();
            _actionBlock = new ActionBlock<T>(Handle);
            _currentDisposable = _nextBlock.LinkTo(_actionBlock, new DataflowLinkOptions { PropagateCompletion = true });
            _currentBlock = _nextBlock;
            _minTime = minTime;
            _maxTime = maxTime;
            _nextBlock = new BufferBlock<T>();
        }

        /// <summary>
        /// 保存当前数据块的数据
        /// </summary>
        public void Close()
        {
            _currentBlock.Complete();
            _actionBlock.Completion.Wait();
            CloseCore();
        }

        public abstract Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = default);
    }
}
