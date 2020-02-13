using System;
using System.Threading.Tasks.Dataflow;
using ItsukiSumeragi.DataFlow;
using Kakegurui.Core;
using Kakegurui.Log;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MomobamiKirari.Models;

namespace MomobamiKirari.DataFlow
{
    /// <summary>
    /// 一个指定时间间隔的流量统计数据块
    /// </summary>
    public class LaneFlowTimeSpanBlock:TrafficTimeSpanBlock<LaneFlow,LaneFlow>
    {
        /// <summary>
        /// 当前流量
        /// </summary>
        private readonly LaneFlow _laneFlow;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<LaneFlowTimeSpanBlock> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="level">时间级别</param>
        /// <param name="serviceProvider">实例工厂</param>
        public LaneFlowTimeSpanBlock(DateTimeLevel level, IServiceProvider serviceProvider)
            :base(level)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<LaneFlowTimeSpanBlock>>();
            _laneFlow = new LaneFlow
            {
                DateTime = DateTime.MinValue
            };
        }

        #region 重写TrafficTimeSpanBlock

        protected override void InitData()
        {
            _laneFlow.DateTime = DateTime.MinValue;

            _laneFlow.Cars = 0;
            _laneFlow.Tricycles = 0;
            _laneFlow.Buss = 0;
            _laneFlow.Vans = 0;
            _laneFlow.Trucks = 0;
            _laneFlow.Motorcycles = 0;
            _laneFlow.Bikes = 0;
            _laneFlow.Persons = 0;

            _laneFlow.Distance =0;
            _laneFlow.TravelTime =0.0;
            _laneFlow.HeadDistance =0.0;
            _laneFlow.Occupancy =0;
            _laneFlow.TimeOccupancy =0;

            _laneFlow.Count = 0;
        }

        protected override void SetData(LaneFlow t,DateTime dateTime)
        {
            _laneFlow.DataId = t.DataId;
            _laneFlow.DateTime = dateTime;

            _laneFlow.Cars += t.Cars;
            _laneFlow.Tricycles += t.Tricycles;
            _laneFlow.Buss += t.Buss;
            _laneFlow.Vans += t.Vans;
            _laneFlow.Trucks += t.Trucks;
            _laneFlow.Motorcycles += t.Motorcycles;
            _laneFlow.Bikes += t.Bikes;
            _laneFlow.Persons += t.Persons;

            _laneFlow.Distance += t.Distance;
            _laneFlow.TravelTime += t.TravelTime;
            _laneFlow.HeadDistance += t.HeadDistance;
            _laneFlow.Occupancy += t.Occupancy;
            _laneFlow.TimeOccupancy += t.TimeOccupancy;

            _laneFlow.TrafficStatus = t.TrafficStatus;

            _laneFlow.Count += 1;

            _laneFlow.FreeSpeed = t.FreeSpeed;
            _laneFlow.SectionLength = t.SectionLength;
           
        }

        protected override void PostData()
        {
            if (_laneFlow.DateTime != DateTime.MinValue)
            {
                LaneFlow flow = new LaneFlow
                {
                    DataId = _laneFlow.DataId,
                    DateTime = new DateTime(_laneFlow.DateTime.Year, _laneFlow.DateTime.Month, _laneFlow.DateTime.Day, _laneFlow.DateTime.Hour, _laneFlow.DateTime.Minute, _laneFlow.DateTime.Second),
                    Cars = _laneFlow.Cars,
                    Tricycles = _laneFlow.Tricycles,
                    Trucks = _laneFlow.Trucks,
                    Buss = _laneFlow.Buss,
                    Vans = _laneFlow.Vans,
                    Motorcycles = _laneFlow.Motorcycles,
                    Bikes = _laneFlow.Bikes,
                    Persons = _laneFlow.Persons,
                    TravelTime = _laneFlow.TravelTime,
                    Distance = _laneFlow.Distance,
                    HeadDistance = _laneFlow.HeadDistance,
                    Occupancy = _laneFlow.Occupancy,
                    TimeOccupancy = _laneFlow.TimeOccupancy,
                    TrafficStatus = _laneFlow.TrafficStatus,
                    Count = _laneFlow.Count,
                    DateLevel = _level
                };

                _logger.LogDebug((int)LogEvent.流量数据块, $"车道流量 level:{flow.DateLevel} id:{flow.DataId} time:{flow.DateTime} car:{flow.Cars} tricycle:{flow.Tricycles} truck:{flow.Trucks} bus:{flow.Buss} van:{flow.Vans} bike:{flow.Bikes} motorcycle:{flow.Motorcycles} person:{flow.Persons} speed:{ flow.AverageSpeed } distance:{ flow.HeadDistance } space:{ flow.HeadSpace } occ:{ flow.Occupancy } tocc:{ flow.TimeOccupancy } status:{ flow.TrafficStatus }");
                _targetBlock?.Post(flow);
            }
        }

        #endregion
    }
}
