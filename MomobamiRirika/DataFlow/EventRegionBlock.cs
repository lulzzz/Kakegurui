using System;
using System.Threading.Tasks.Dataflow;
using ItsukiSumeragi.DataFlow;
using Kakegurui.Log;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MomobamiRirika.Models;

namespace MomobamiRirika.DataFlow
{
    /// <summary>
    /// 交通数据统计数据块
    /// </summary>
    public class EventRegionBlock:TrafficActionBlock<TrafficEvent>
    {
        /// <summary>
        /// 当前数据
        /// </summary>
        private readonly TrafficEvent _trafficEvent;

        /// <summary>
        /// 初始化数据发送的数据块
        /// </summary>
        private ITargetBlock<TrafficEvent> _insertBlock;

        /// <summary>
        /// 计算结束后发送的数据块
        /// </summary>
        private ITargetBlock<TrafficEvent> _updateBlock;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<EventRegionBlock> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        public EventRegionBlock(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<EventRegionBlock>>();
            _trafficEvent = new TrafficEvent
            {
                DateTime = DateTime.MinValue
            };
        }

        /// <summary>
        /// 设置初始化数据推送的数据块
        /// </summary>
        /// <param name="insertBlock">新增数据块</param>
        /// <param name="updateBlock">更新数据块</param>
        public void LinkTo(ITargetBlock<TrafficEvent> insertBlock, ITargetBlock<TrafficEvent> updateBlock)
        {
            _insertBlock = insertBlock;
            _updateBlock = updateBlock;
        }

        protected override void Handle(TrafficEvent t)
        {
            //初始化第一个
            if (_trafficEvent.DateTime == DateTime.MinValue)
            {
                if (t.DateTime != DateTime.MinValue)
                {
                    _trafficEvent.DateTime = t.DateTime;
                    _trafficEvent.EndTime = t.DateTime;
                    _trafficEvent.DataId = t.DataId;
                    _insertBlock?.Post(new TrafficEvent
                    {
                        DateTime = new DateTime(_trafficEvent.DateTime.Year, _trafficEvent.DateTime.Month, _trafficEvent.DateTime.Day, _trafficEvent.DateTime.Hour, _trafficEvent.DateTime.Minute, _trafficEvent.DateTime.Second),
                        EndTime = null,
                        DataId = _trafficEvent.DataId
                    });
                    _logger.LogDebug((int)LogEvent.事件数据块, $"初始化事件 {t.DataId} {t.DateTime}");
                }
            }
            else if (_trafficEvent.EndTime.HasValue)
            {
                //30秒内只处理有效数据
                if ((t.DateTime - _trafficEvent.EndTime.Value).TotalSeconds <= 30)
                {
                    if (t.DateTime != DateTime.MinValue)
                    {
                        _logger.LogDebug((int)LogEvent.事件数据块, $"更新结束时间 {t.DataId} {_trafficEvent.DateTime} {_trafficEvent.EndTime}->{t.DateTime}");
                        _trafficEvent.EndTime = t.DateTime;
                    }
                }
                //30秒外入库
                //然后保存新数据
                else
                {
                    TrafficEvent trafficEvent = new TrafficEvent
                    {
                        DateTime = new DateTime(_trafficEvent.DateTime.Year, _trafficEvent.DateTime.Month, _trafficEvent.DateTime.Day, _trafficEvent.DateTime.Hour, _trafficEvent.DateTime.Minute, _trafficEvent.DateTime.Second),
                        EndTime = new DateTime(_trafficEvent.EndTime.Value.Year, _trafficEvent.EndTime.Value.Month, _trafficEvent.EndTime.Value.Day, _trafficEvent.EndTime.Value.Hour, _trafficEvent.EndTime.Value.Minute, _trafficEvent.EndTime.Value.Second),
                        DataId = _trafficEvent.DataId
                    };
                    _logger.LogDebug((int)LogEvent.事件数据块, $"确认事件 {trafficEvent.DataId} {trafficEvent.DateTime} {trafficEvent.EndTime}");
                    _updateBlock?.Post(trafficEvent);
                    _trafficEvent.DateTime = t.DateTime;
                    _trafficEvent.EndTime = t.DateTime;
                    _insertBlock?.Post(new TrafficEvent
                    {
                        DateTime = new DateTime(_trafficEvent.DateTime.Year, _trafficEvent.DateTime.Month, _trafficEvent.DateTime.Day, _trafficEvent.DateTime.Hour, _trafficEvent.DateTime.Minute, _trafficEvent.DateTime.Second),
                        EndTime = null,
                        DataId = _trafficEvent.DataId
                    });
                    _logger.LogDebug((int)LogEvent.事件数据块, $"初始化事件 {t.DataId} {t.DateTime}");
                }
            }
        }

        public override void WaitCompletion()
        {
            base.WaitCompletion();
            if (_trafficEvent.DateTime != DateTime.MinValue)
            {
                TrafficEvent trafficEvent = new TrafficEvent
                {
                    DateTime = _trafficEvent.DateTime,
                    EndTime = _trafficEvent.EndTime,
                    DataId = _trafficEvent.DataId
                };
                _updateBlock?.Post(trafficEvent);
            }
        }
    }
}
