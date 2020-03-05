using System;
using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Microsoft.Extensions.Caching.Memory;
using MomobamiRirika.Cache;
using MomobamiRirika.Data;
using MomobamiRirika.Models;

namespace MomobamiRirika.Managers
{
    /// <summary>
    /// 密度数据库操作
    /// </summary>
    public class DensitiesManager
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly DensityContext _context;

        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库实例</param>
        /// <param name="memoryCache">缓存</param>
        public DensitiesManager(DensityContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// 筛选
        /// </summary>
        /// <param name="queryable">数据源</param>
        /// <param name="dataId">数据编号</param>
        /// <param name="level">时间级别</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns></returns>
        private IQueryable<TrafficDensity> Where(IQueryable<TrafficDensity> queryable, string dataId, DateTimeLevel level, DateTime startTime, DateTime endTime)
        {
            if (level >= DateTimeLevel.Day)
            {
                startTime = TimePointConvert.CurrentTimePoint(level, startTime);
                endTime = TimePointConvert.NextTimePoint(level, TimePointConvert.CurrentTimePoint(level, endTime)).AddMinutes(-1);
            }

            return queryable
                .Where(f => f.DataId == dataId
                            && f.DateTime >= startTime
                            && f.DateTime <= endTime);
        }

        /// <summary>
        /// 选择图表
        /// </summary>
        /// <param name="dataId">数据编号</param>
        /// <param name="level">时间级别</param>
        /// <param name="baseTime">基准时间</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>查询结果</returns>
        private List<TrafficChart<DateTime, int>> SelectCharts(string dataId, DateTimeLevel level, DateTime baseTime, DateTime startTime, DateTime endTime)
        {
            List<IQueryable<TrafficDensity>> queryables =
                BranchDbConvert.GetQuerables(startTime, endTime, _context.Queryable(level));
            List<TrafficChart<DateTime, int>> result = new List<TrafficChart<DateTime, int>>();
            foreach (IQueryable<TrafficDensity> queryable in queryables)
            {
                try
                {
                    string timeFormat = TimePointConvert.TimeFormat(level);
                    TimeSpan span = TimePointConvert.CurrentTimePoint(level, baseTime) - TimePointConvert.CurrentTimePoint(level, startTime);
                    var whereQueryable = Where(queryable, dataId, level, startTime, endTime);
                    if (level >= DateTimeLevel.Day)
                    {
                        result.AddRange(whereQueryable
                            .GroupBy(f => TimePointConvert.CurrentTimePoint(level, f.DateTime))
                            .Select(g => new TrafficChart<DateTime, int>
                            {
                                Axis = g.Key.Add(span),
                                Remark = g.Key.ToString(timeFormat),
                                Value = Convert.ToInt32(g.Average(d => d.Value))
                            })
                            .ToList());
                    }
                    else
                    {
                        result.AddRange(whereQueryable
                            .ToList()
                            .Select(d => new TrafficChart<DateTime, int>
                            {
                                Axis = d.DateTime.Add(span),
                                Remark = d.DateTime.ToString(timeFormat),
                                Value = d.Value
                            })
                            .ToList());
                    }
                }
                catch
                {
                }
            }
            return result;
        }

        /// <summary>
        /// 按区域查询密度集合
        /// </summary>
        /// <param name="dataId">数据编号</param>
        /// <param name="level">时间级别</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>高点密度数据集合</returns>
        public List<TrafficDensity> QueryList(string dataId, DateTimeLevel level, DateTime startTime, DateTime endTime)
        {
            dataId = Uri.UnescapeDataString(dataId);
            List<IQueryable<TrafficDensity>> queryables = BranchDbConvert.GetQuerables(startTime, endTime, _context.Queryable(level));
            List<TrafficDensity> result = new List<TrafficDensity>();
            foreach (IQueryable<TrafficDensity> queryable in queryables)
            {
                try
                {
                    IQueryable<TrafficDensity> whereQueryable = Where(queryable, dataId, level, startTime, endTime);
                    if (level >= DateTimeLevel.Day)
                    {
                        result.AddRange(whereQueryable
                            .GroupBy(f => TimePointConvert.CurrentTimePoint(level, f.DateTime))
                            .Select(g => new TrafficDensity
                            {
                                DataId = g.First().DataId,
                                DateTime = g.Key,
                                Value = Convert.ToInt32(g.Average(d => d.Value))
                            })
                            .ToList());
                    }
                    else
                    {
                        result.AddRange(whereQueryable.ToList());
                    }
                }
                catch
                {
                }
            }
            return result;
        }

        /// <summary>
        /// 查询指定时间之后的top10
        /// </summary>
        /// <param name="startTime">计算开始时间</param>
        /// <returns>密度数据集合</returns>
        public List<TrafficDensity> QueryTop10(DateTime startTime)
        {
            List<TrafficDensity> densities = DensityCache.DensitiesCache
                .Select(p => _memoryCache.FillDensity(
                    new TrafficDensity
                    {
                        DataId = p.Key,
                        Value = Convert.ToInt32(
                            p.Value.Any(d => d.DateTime >= startTime)
                                ? Math.Ceiling(p.Value.Where(d => d.DateTime >= startTime).Average(d => d.Value))
                                : 0)
                    }))
                .OrderByDescending(d => d.Value)
                .Take(10)
                .ToList();
            return densities;
        }

        /// <summary>
        /// 查询密度变化量top10
        /// </summary>
        /// <param name="startTime1">当前时间</param>
        /// <param name="startTime2">比较开始时间</param>
        /// <param name="endTime2">比较结束时间</param>
        /// <returns></returns>
        public List<TrafficDensity> QueryChangeTop10(DateTime startTime1, DateTime startTime2, DateTime endTime2)
        {
            List<TrafficDensity> densities = DensityCache.DensitiesCache
                .Select(p => new
                {
                    DataId = p.Key,
                    Value = p.Value.Any(d => d.DateTime >= startTime1)
                        ? p.Value.Where(d => d.DateTime >= startTime1).Average(d => d.Value)
                        : 0,
                    LastValue = p.Value.Any(d => d.DateTime >= startTime2 && d.DateTime < endTime2)
                        ? p.Value.Where(d => d.DateTime >= startTime2 && d.DateTime < endTime2).Average(d => d.Value)
                        : 0
                })
                .OrderByDescending(d => Math.Abs(d.Value - d.LastValue))
                .Take(10)
                .Select(d => _memoryCache.FillDensity(new TrafficDensity
                {
                    DataId = d.DataId,
                    Value = Convert.ToInt32(Math.Ceiling(d.Value)),
                    LastValue = Convert.ToInt32(Math.Ceiling(d.LastValue))
                })).ToList();
            return densities;
        }

        /// <summary>
        /// 按区域查询密度同比环比分析
        /// </summary>
        /// <param name="dataId">数据编号</param>
        /// <param name="level">密度时间粒度</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>高点密度数据集合</returns>
        public List<List<TrafficChart<DateTime, int>>> QueryComparison(string dataId, DateTimeLevel level, DateTime startTime, DateTime endTime)
        {
            dataId = Uri.UnescapeDataString(dataId);
            startTime = TimePointConvert.CurrentTimePoint(level, startTime);
            endTime = TimePointConvert.CurrentTimePoint(level, endTime);
            List<DateTime> startTimes = new List<DateTime>();
            List<DateTime> endTimes = new List<DateTime>();
            startTimes.Add(startTime);
            endTimes.Add(endTime);

            //同比
            if (level == DateTimeLevel.FiveMinutes
                || level == DateTimeLevel.FifteenMinutes
                || level == DateTimeLevel.Hour)
            {
                startTimes.Add(TimePointConvert.PreTimePoint(DateTimeLevel.Day, startTime));
                endTimes.Add(TimePointConvert.PreTimePoint(DateTimeLevel.Day, endTime));
            }

            else if (level == DateTimeLevel.Day)
            {
                startTimes.Add(TimePointConvert.PreTimePoint(DateTimeLevel.Month, startTime));
                endTimes.Add(TimePointConvert.PreTimePoint(DateTimeLevel.Month, endTime));
            }
            else if (level == DateTimeLevel.Month)
            {
                startTimes.Add(TimePointConvert.PreTimePoint(DateTimeLevel.Year, startTime));
                endTimes.Add(TimePointConvert.PreTimePoint(DateTimeLevel.Year, endTime));
            }
            else
            {
                return new List<List<TrafficChart<DateTime, int>>>();
            }

            //环比
            startTimes.Add(TimePointConvert.PreTimePoint(level, startTime));
            endTimes.Add(TimePointConvert.PreTimePoint(level, endTime));
            return startTimes.Select((t, i) => SelectCharts(dataId, level, startTimes[0], t, endTimes[i])).ToList();
        }

        /// <summary>
        /// 重点区域密度
        /// </summary>
        /// <returns>密度数据集合</returns>
        public List<TrafficDensity> QueryVipRegions()
        {
            DateTime today = DateTime.Today;
            List<TrafficDensity> densities = (from dataId in _memoryCache.GetRegions().Where(r => r.IsVip)
                    .Select(r => r.DataId)
                where DensityCache.DensitiesCache.ContainsKey(dataId)
                select _memoryCache.FillDensity(new TrafficDensity
                {
                    DataId = dataId,
                    Value = Convert.ToInt32(
                        DensityCache.DensitiesCache[dataId].Any(d => d.DateTime >= today)
                            ? DensityCache.DensitiesCache[dataId].Where(d => d.DateTime >= today).Average(d => d.Value)
                            : 0)
                })).ToList();
            return densities;
        }
    }
}
