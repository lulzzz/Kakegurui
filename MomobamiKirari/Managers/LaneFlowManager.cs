using System;
using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Microsoft.Extensions.Caching.Memory;
using MomobamiKirari.Cache;
using MomobamiKirari.Codes;
using MomobamiKirari.Data;
using MomobamiKirari.Models;

namespace MomobamiKirari.Managers
{
    /// <summary>
    /// 流量数据查询基类
    /// </summary>
    public class LaneFlowManager
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly FlowContext _flowContext;

        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="flowContext">数据库实例</param>
        public LaneFlowManager(IMemoryCache memoryCache, FlowContext flowContext)
        {
            _flowContext = flowContext;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// 筛选
        /// </summary>
        /// <param name="queryable">数据源</param>
        /// <param name="dataIds">车道数据编号集合</param>
        /// <param name="level">时间级别</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns></returns>
        protected IQueryable<LaneFlow> Where(IQueryable<LaneFlow> queryable, HashSet<string> dataIds, DateTimeLevel level, DateTime startTime, DateTime endTime)
        {
            if (level >= DateTimeLevel.Day)
            {
                startTime = TimePointConvert.CurrentTimePoint(level, startTime);
                endTime = TimePointConvert.NextTimePoint(level, TimePointConvert.CurrentTimePoint(level, endTime)).AddMinutes(-1);
            }

            return queryable
                .Where(f => dataIds.Contains(f.DataId) && f.DateTime >= startTime && f.DateTime <= endTime);
        }

        /// <summary>
        /// 分组
        /// </summary>
        /// <param name="queryable">数据源</param>
        /// <param name="level">时间级别</param>
        /// <returns>分组后的数据源</returns>
        protected IQueryable<IGrouping<DateTime, LaneFlow>> Group(IQueryable<LaneFlow> queryable, DateTimeLevel level)
        {
            IQueryable<IGrouping<DateTime, LaneFlow>> groupQueryable;
            if (level >= DateTimeLevel.Day)
            {
                groupQueryable = queryable
                    .GroupBy(f => TimePointConvert.CurrentTimePoint(level, f.DateTime));
            }
            else
            {
                groupQueryable = queryable
                    .GroupBy(f => f.DateTime);
            }

            return groupQueryable;
        }

        /// <summary>
        /// 查询图表
        /// </summary>
        /// <param name="queryable">数据源</param>
        /// <param name="level">时间级别</param>
        /// <param name="baseTime">基准时间</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="flowTypes">数据类型</param>
        /// <returns>查询结果</returns>
        protected List<TrafficChart<DateTime, int, LaneFlow>> SelectChart(IQueryable<LaneFlow> queryable, DateTimeLevel level, DateTime baseTime, DateTime startTime, FlowType[] flowTypes)
        {
            try
            {
                IQueryable<IGrouping<DateTime, LaneFlow>> groupQueryable =
                    Group(queryable, level);
                string timeFormat = TimePointConvert.TimeFormat(level);
                TimeSpan span = TimePointConvert.CurrentTimePoint(level, baseTime) - TimePointConvert.CurrentTimePoint(level, startTime);
                if (flowTypes == null || flowTypes.Length == 0)
                {
                    return groupQueryable
                        .Select(g => new TrafficChart<DateTime, int, LaneFlow>
                        {
                            Axis = g.Key.Add(span),
                            Remark = g.Key.ToString(timeFormat),
                            Value = g.Sum(f => f.Bikes + f.Buss + f.Cars + f.Motorcycles
                                               + f.Persons + f.Tricycles + f.Trucks + f.Vans)
                        })
                        .ToList();
                }
                else if (flowTypes.Contains(FlowType.平均速度))
                {
                    return groupQueryable
                        .Select(g => new TrafficChart<DateTime, int, LaneFlow>
                        {
                            Axis = g.Key.Add(span),
                            Remark = g.Key.ToString(timeFormat),
                            Value = g.Sum(f => f.TravelTime) > 0
                                    ? Convert.ToInt32(g.Sum(f => f.Distance) / g.Sum(f => f.TravelTime) * 3600 / 1000)
                                    : 0
                        })
                        .ToList();
                }
                else if (flowTypes.Contains(FlowType.车头时距))
                {
                    return groupQueryable
                        .Select(g => new TrafficChart<DateTime, int, LaneFlow>
                        {
                            Axis = g.Key.Add(span),
                            Remark = g.Key.ToString(timeFormat),
                            Value = g.Sum(f => f.Count) > 0
                                ? Convert.ToInt32(g.Sum(f => f.HeadDistance) / g.Sum(f => f.Count))
                                : 0

                        })
                        .ToList();
                }
                else if (flowTypes.Contains(FlowType.车头间距))
                {
                    return groupQueryable
                        .Select(g => new TrafficChart<DateTime, int, LaneFlow>
                        {
                            Axis = g.Key.Add(span),
                            Remark = g.Key.ToString(timeFormat),
                            Value = g.Sum(f => f.Count) > 0 && g.Sum(f => f.TravelTime) > 0
                            ? Convert.ToInt32(g.Sum(f => f.HeadDistance) / g.Sum(f => f.Count) * (g.Sum(f => f.Distance) / g.Sum(f => f.TravelTime)))
                            : 0
                        })
                        .ToList();
                }
                else if (flowTypes.Contains(FlowType.空间占有率))
                {
                    return groupQueryable
                        .Select(g => new TrafficChart<DateTime, int, LaneFlow>
                        {
                            Axis = g.Key.Add(span),
                            Remark = g.Key.ToString(timeFormat),
                            Value = g.Sum(f => f.Count) > 0
                                ? Convert.ToInt32(g.Sum(f => f.Occupancy) / g.Sum(f => f.Count))
                                : 0
                        })
                        .ToList();
                }
                else if (flowTypes.Contains(FlowType.时间占有率))
                {
                    return groupQueryable
                        .Select(g => new TrafficChart<DateTime, int, LaneFlow>
                        {
                            Axis = g.Key.Add(span),
                            Remark = g.Key.ToString(timeFormat),
                            Value = g.Sum(f => f.Count) > 0
                                ? Convert.ToInt32(g.Sum(f => f.TimeOccupancy) / g.Sum(f => f.Count))
                                : 0
                        })
                        .ToList();
                }
                else
                {
                    return groupQueryable
                        .Select(g => new TrafficChart<DateTime, int, LaneFlow>
                        {
                            Axis = g.Key.Add(span),
                            Remark = g.Key.ToString(timeFormat),
                            Value = g.Sum(f => (flowTypes.Contains(FlowType.自行车) ? f.Bikes : 0)
                                               + (flowTypes.Contains(FlowType.客车) ? f.Buss : 0)
                                               + (flowTypes.Contains(FlowType.轿车) ? f.Cars : 0)
                                               + (flowTypes.Contains(FlowType.摩托车) ? f.Motorcycles : 0)
                                               + (flowTypes.Contains(FlowType.三轮车) ? f.Tricycles : 0)
                                               + (flowTypes.Contains(FlowType.卡车) ? f.Trucks : 0)
                                               + (flowTypes.Contains(FlowType.行人) ? f.Persons : 0)
                                               + (flowTypes.Contains(FlowType.面包车) ? f.Vans : 0)),
                            Data = new LaneFlow
                            {
                                DataId = g.First().DataId,
                                Cars = g.Sum(f => f.Cars),
                                Tricycles = g.Sum(f => f.Tricycles),
                                Trucks = g.Sum(f => f.Trucks),
                                Vans = g.Sum(f => f.Vans),
                                Buss = g.Sum(f => f.Buss),
                                Motorcycles = g.Sum(f => f.Motorcycles),
                                Bikes = g.Sum(f => f.Bikes),
                                Persons = g.Sum(f => f.Persons),
                                LaneName = _memoryCache.GetLane(g.First().DataId).LaneName,
                                FlowDirection_Desc = _memoryCache.GetLane(g.First().DataId).FlowDirection_Desc

                            }
                        })
                        .ToList();
                }
            }
            catch
            {
                return new List<TrafficChart<DateTime, int, LaneFlow>>();
            }
        }

        /// <summary>
        /// 查询列表
        /// </summary>
        /// <param name="queryables">数据源集合</param>
        /// <param name="dataIds">车道数据编号集合</param>
        /// <param name="level">时间级别</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>查询结果</returns>
        private List<LaneFlow> SelectList(List<IQueryable<LaneFlow>> queryables, HashSet<string> dataIds, DateTimeLevel level, DateTime startTime, DateTime endTime)
        {
            List<LaneFlow> result = new List<LaneFlow>();
            foreach (IQueryable<LaneFlow> queryable in queryables)
            {
                try
                {
                    result.AddRange(Group(Where(queryable, dataIds, level, startTime, endTime), level)
                        .Select(g => new LaneFlow
                        {
                            DataId = g.First().DataId,
                            DateTime = g.Key,
                            Cars = g.Sum(f => f.Cars),
                            Tricycles = g.Sum(f => f.Tricycles),
                            Trucks = g.Sum(f => f.Trucks),
                            Vans = g.Sum(f => f.Vans),
                            Buss = g.Sum(f => f.Buss),
                            Bikes = g.Sum(f => f.Bikes),
                            Motorcycles = g.Sum(f => f.Motorcycles),
                            Persons = g.Sum(f => f.Persons),
                            TravelTime = g.Sum(f => f.TravelTime),
                            Distance = g.Sum(f => f.Distance),
                            HeadDistance = g.Sum(f => f.HeadDistance),
                            Occupancy = g.Sum(f => f.Occupancy),
                            TimeOccupancy = g.Sum(f => f.TimeOccupancy),
                            Count = g.Sum(f => f.Count)
                        }).ToList());
                }
                catch
                {

                }
            }
            return result;
        }

        /// <summary>
        /// 按路口查询流量数据
        /// </summary>
        /// <param name="crossingId">路口编号</param>
        /// <param name="flowTypes">流量密度数据</param>
        /// <param name="level">时间粒度</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <returns>流量数据集合</returns>
        public List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryChartsByCrossing(int crossingId, DateTimeLevel level, FlowType[] flowTypes, DateTime[] startTimes, DateTime[] endTimes)
        {
            List<Lane> lanes = _memoryCache.GetLanes()
                .Where(l => l.Channel.CrossingId == crossingId)
                .ToList();
            return QueryCharts(lanes, level, startTimes, endTimes, startTimes[0], flowTypes);
        }

        /// <summary>
        /// 按路口方向查询流量数据
        /// </summary>
        /// <param name="crossingId">路口编号</param>
        /// <param name="directions">路口方向集合</param>
        /// <param name="level">时间粒度</param>
        /// <param name="flowTypes">流量密度数据</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <returns>流量数据集合</returns>
        public List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryChartsByCrossing(int crossingId, int[] directions, DateTimeLevel level, FlowType[] flowTypes, DateTime[] startTimes, DateTime[] endTimes)
        {
            List<Lane> lanes = _memoryCache.GetLanes()
                .Where(l => l.Channel.CrossingId == crossingId && directions.Contains(l.Direction))
                .ToList();
            return QueryCharts(lanes, level, startTimes, endTimes, startTimes[0], flowTypes);
        }

        /// <summary>
        /// 按路口流向查询流量数据
        /// </summary>
        /// <param name="crossingId">路口编号</param>
        /// <param name="direction">路口方向集</param>
        /// <param name="flowDirections">路口流向合</param>
        /// <param name="level">时间粒度</param>
        /// <param name="flowTypes">流量密度数据</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <returns>流量数据集合</returns>
        public List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryChartsByCrossing(int crossingId, int direction, int[] flowDirections, DateTimeLevel level, FlowType[] flowTypes, DateTime[] startTimes, DateTime[] endTimes)
        {
            List<Lane> lanes = _memoryCache.GetLanes()
                .Where(l => l.Channel.CrossingId == crossingId
                            && l.Direction == direction
                            && flowDirections.Contains(l.FlowDirection))
                .ToList();
            return QueryCharts(lanes, level, startTimes, endTimes, startTimes[0], flowTypes);
        }

        /// <summary>
        /// 按路段查询流量数据
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <param name="flowTypes">流量密度数据</param>
        /// <param name="level">时间粒度</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <returns>流量数据集合</returns>
        public List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryChartsBySection(int sectionId, DateTimeLevel level, FlowType[] flowTypes, DateTime[] startTimes, DateTime[] endTimes)
        {
            List<Lane> lanes = _memoryCache.GetLanes()
                .Where(l => l.Channel.SectionId == sectionId)
                .ToList();
            return QueryCharts(lanes, level, startTimes, endTimes, startTimes[0], flowTypes);
        }

        /// <summary>
        /// 按路段流向查询流量数据
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <param name="flowDirections">路口流向合</param>
        /// <param name="level">时间粒度</param>
        /// <param name="flowTypes">流量密度数据</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <returns>流量数据集合</returns>
        public List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryChartsBySection(int sectionId, int[] flowDirections, DateTimeLevel level, FlowType[] flowTypes, DateTime[] startTimes, DateTime[] endTimes)
        {
            List<Lane> lanes = _memoryCache.GetLanes()
                .Where(l => l.Channel.SectionId == sectionId
                            && flowDirections.Contains(l.FlowDirection))
                .ToList();
            return QueryCharts(lanes, level, startTimes, endTimes, startTimes[0], flowTypes);
        }

        /// <summary>
        /// 查询流量图表集合
        /// </summary>
        /// <param name="dataId">车道编号</param>
        /// <param name="level">时间级别</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <param name="baseTime">基准时间</param>
        /// <param name="flowTypes">流量数据类型,默认为null表示查询所有类型</param>
        /// <returns>按时间段划分的图表数据</returns>
        public List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryCharts(string dataId, DateTimeLevel level, DateTime[] startTimes, DateTime[] endTimes, DateTime baseTime,
            FlowType[] flowTypes = null)
        {
            List<Lane> lanes = _memoryCache.GetLanes()
                .Where(l => l.DataId == dataId)
                .ToList();
            return QueryCharts(lanes, level, startTimes, endTimes, baseTime, flowTypes);
        }

        /// <summary>
        /// 查询流量图表集合
        /// </summary>
        /// <param name="dataIds">车道编号集合</param>
        /// <param name="level">时间级别</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <param name="flowTypes">流量数据类型,默认为null表示查询所有类型</param>
        /// <returns>按时间段划分的图表数据</returns>
        public List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryCharts(HashSet<string> dataIds, DateTimeLevel level, DateTime[] startTimes, DateTime[] endTimes, FlowType[] flowTypes = null)
        {
            List<Lane> lanes = _memoryCache.GetLanes()
                .Where(l => dataIds.Contains(l.DataId))
                .ToList();
            return QueryCharts(lanes, level, startTimes, endTimes, startTimes[0], flowTypes);
        }

        /// <summary>
        /// 查询流量图表集合
        /// </summary>
        /// <param name="dataIds">车道编号集合</param>
        /// <param name="level">时间级别</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <param name="baseTime">基准时间</param>
        /// <param name="flowTypes">流量数据类型,默认为null表示查询所有类型</param>
        /// <returns>按时间段划分的图表数据</returns>
        public List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryCharts(HashSet<string> dataIds, DateTimeLevel level, DateTime[] startTimes, DateTime[] endTimes, DateTime baseTime,
            FlowType[] flowTypes = null)
        {
            List<Lane> lanes = _memoryCache.GetLanes()
                .Where(l => dataIds.Contains(l.DataId))
                .ToList();
            return QueryCharts(lanes, level, startTimes, endTimes, baseTime, flowTypes);
        }

        /// <summary>
        /// 按车道查询流量数据
        /// </summary>
        /// <param name="lanes">车道集合</param>
        /// <param name="level">时间粒度</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <param name="baseTime">基准时间</param>
        /// <param name="flowTypes">流量密度数据</param> 
        /// <returns>流量数据集合</returns>
        public virtual List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryCharts(List<Lane> lanes, DateTimeLevel level, DateTime[] startTimes, DateTime[] endTimes, DateTime baseTime, FlowType[] flowTypes = null)
        {
            List<List<TrafficChart<DateTime, int, LaneFlow>>> result = new List<List<TrafficChart<DateTime, int, LaneFlow>>>();
            HashSet<string> dataIds = lanes.Select(l => l.DataId).ToHashSet();
            for (int i = 0; i < startTimes.Length; ++i)
            {
                List<TrafficChart<DateTime, int, LaneFlow>> item = new List<TrafficChart<DateTime, int, LaneFlow>>();
                foreach (IQueryable<LaneFlow> queryable in BranchDbConvert.GetQuerables(startTimes[i], endTimes[i], _flowContext.Queryable(level)))
                {
                    item.AddRange(SelectChart(Where(queryable, dataIds, level, startTimes[i], endTimes[i]), level, startTimes[0], startTimes[i], flowTypes));
                }
                result.Add(item);
            }
            return result;
        }


        /// <summary>
        /// 按车道查询流量数据集合
        /// </summary>
        /// <param name="dataId">数据编号</param>
        /// <param name="level">时间粒度</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>流量数据集合</returns>
        public List<LaneFlow> QueryList(string dataId, DateTimeLevel level, DateTime startTime, DateTime endTime)
        {
            List<Lane> lanes = _memoryCache.GetLanes()
                .Where(l => l.DataId == dataId)
                .ToList();
            return QueryList(lanes, level, new[] { startTime }, new[] { endTime })[0];
        }

        /// <summary>
        /// 按车道查询流量数据集合
        /// </summary>
        /// <param name="dataIds">车道编号集合</param>
        /// <param name="level">时间粒度</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>流量数据集合</returns>
        public List<LaneFlow> QueryList(HashSet<string> dataIds, DateTimeLevel level, DateTime startTime, DateTime endTime)
        {
            List<Lane> lanes = _memoryCache.GetLanes()
                .Where(l => dataIds.Contains(l.DataId))
                .ToList();
            return QueryList(lanes, level, new[] { startTime }, new[] { endTime })[0];
        }

        /// <summary>
        /// 按车道查询多组流量数据
        /// </summary>
        /// <param name="dataIds">数据编号集合</param>
        /// <param name="level">时间粒度</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <returns>流量数据集合</returns>
        public List<List<LaneFlow>> QueryList(HashSet<string> dataIds, DateTimeLevel level, DateTime[] startTimes, DateTime[] endTimes)
        {
            List<Lane> lanes = _memoryCache.GetLanes()
                .Where(l => dataIds.Contains(l.DataId))
                .ToList();
            return QueryList(lanes, level, startTimes,endTimes);
        }

        /// <summary>
        /// 按车道查询多组流量数据
        /// </summary>
        /// <param name="lanes">车道集合</param>
        /// <param name="level">时间粒度</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <returns>流量数据集合</returns>
        public virtual List<List<LaneFlow>> QueryList(List<Lane> lanes, DateTimeLevel level, DateTime[] startTimes, DateTime[] endTimes)
        {
            return startTimes.Select((t, i) => SelectList(BranchDbConvert.GetQuerables(t, endTimes[i], _flowContext.Queryable(level)), lanes.Select(l => l.DataId).ToHashSet(), level, t, endTimes[i])).ToList();
        }
    }
}
