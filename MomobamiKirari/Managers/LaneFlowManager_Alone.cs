using System;
using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using MomobamiKirari.Data;
using MomobamiKirari.Models;
using ItsukiSumeragi.Codes.Flow;
using Microsoft.Extensions.Caching.Memory;

namespace MomobamiKirari.Managers
{
    /// <summary>
    /// 流量数据单点查询
    /// </summary>
    public class LaneFlowManager_Alone: LaneFlowManager
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly FlowContext _flowContext;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="flowContext">数据库实例</param>
        public LaneFlowManager_Alone(IMemoryCache memoryCache,FlowContext flowContext)
            :base(memoryCache)
        {
            _flowContext = flowContext;
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
            List<LaneFlow> result=new List<LaneFlow>();
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

        public override List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryCharts(List<TrafficLane> lanes, DateTimeLevel level, DateTime[] startTimes, DateTime[] endTimes, DateTime baseTime, FlowType[] flowTypes = null)
        {
            List<List<TrafficChart<DateTime, int, LaneFlow>>> result=new List<List<TrafficChart<DateTime, int, LaneFlow>>>();
            HashSet<string> dataIds = lanes.Select(l => l.DataId).ToHashSet();
            for (int i = 0; i < startTimes.Length; ++i)
            {
                List<TrafficChart<DateTime, int, LaneFlow>> item=new List<TrafficChart<DateTime, int, LaneFlow>>();
                foreach (IQueryable<LaneFlow> queryable in BranchDbConvert.GetQuerables(startTimes[i], endTimes[i], _flowContext.Queryable(level)))
                {
                    item.AddRange(SelectChart(Where(queryable, dataIds, level, startTimes[i], endTimes[i]),level,startTimes[0],startTimes[i],flowTypes));
                }
                result.Add(item);
            }
            return result;
        }

        public override List<List<LaneFlow>> QueryList(List<TrafficLane> lanes, DateTimeLevel level, DateTime[] startTimes, DateTime[] endTimes)
        {
            return startTimes.Select((t, i) => SelectList(BranchDbConvert.GetQuerables(t, endTimes[i], _flowContext.Queryable(level)), lanes.Select(l=>l.DataId).ToHashSet(), level, t, endTimes[i])).ToList();
        }
    }
}
