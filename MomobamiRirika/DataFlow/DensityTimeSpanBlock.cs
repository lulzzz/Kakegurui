using System;
using System.Threading.Tasks.Dataflow;
using ItsukiSumeragi.DataFlow;
using Kakegurui.Core;
using Kakegurui.Log;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MomobamiRirika.Models;

namespace MomobamiRirika.DataFlow
{
    /// <summary>
    /// 一个指定时间间隔的密度统计数据块
    /// </summary>
    public class DensityTimeSpanBlock:TrafficTimeSpanBlock<TrafficDensity, TrafficDensity>
    {
        /// <summary>
        /// 当前密度数据
        /// </summary>
        private readonly TrafficDensity _density;

        /// <summary>
        /// 当前密度数据的数量
        /// </summary>
        private int _count;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<DensityTimeSpanBlock> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="level">时间级别</param>
        /// <param name="serviceProvider">实例工厂</param>
        public DensityTimeSpanBlock(DateTimeLevel level, IServiceProvider serviceProvider)
            : base(level)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<DensityTimeSpanBlock>>();
            _density = new TrafficDensity
            {
                DateTime = DateTime.MinValue
            };
            _count = 0;
        }

        #region 重写TrafficTimeSpanBlock

        protected override void InitData()
        {
            _density.Value = 0;
            _count = 0;
        }

        protected override void SetData(TrafficDensity t, DateTime dateTime)
        {
            _density.DataId = t.DataId;
            _density.DateTime = dateTime;
            _density.Value += t.Value;
            _count += 1;
        }

        protected override void PostData()
        {
            if (_density.DateTime != DateTime.MinValue)
            {
                TrafficDensity temp = new TrafficDensity
                {
                    DataId = _density.DataId,
                    DateTime = new DateTime(_density.DateTime.Year, _density.DateTime.Month, _density.DateTime.Day, _density.DateTime.Hour, _density.DateTime.Minute, _density.DateTime.Second),
                    Value = Convert.ToInt32(_density.Value / Convert.ToDouble(_count)),
                    DateLevel = _level
                };
                _logger.LogDebug((int)LogEvent.高点数据块, $"密度数据 level:{temp.DateLevel} id:{temp.DataId} time:{temp.DateTime} value:{temp.Value} total:{_density.Value} count:{_count} ");
                _targetBlock?.Post(temp);
            }
        }

        #endregion

    }
}
