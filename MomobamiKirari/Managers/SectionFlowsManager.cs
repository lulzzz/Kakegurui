using System;
using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using MomobamiKirari.Data;
using MomobamiKirari.Models;

namespace MomobamiKirari.Managers
{
    /// <summary>
    /// 路段流量查询
    /// </summary>
    public class SectionFlowsManager
    {
        /// <summary>
        /// 流量数据库
        /// </summary>
        private readonly FlowContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">流量数据库实例</param>
        public SectionFlowsManager(FlowContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 筛选
        /// </summary>
        /// <param name="queryable">数据源</param>
        /// <param name="sectionId">路段编号</param>
        /// <param name="level">时间级别</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns></returns>
        private IQueryable<SectionStatus> Where(IQueryable<SectionStatus> queryable, int sectionId, DateTimeLevel level, DateTime startTime, DateTime endTime)
        {
            if (level >= DateTimeLevel.Day)
            {
                startTime = TimePointConvert.CurrentTimePoint(level, startTime);
                endTime = TimePointConvert.NextTimePoint(level, TimePointConvert.CurrentTimePoint(level, endTime)).AddMinutes(-1);
            }
            return queryable
                .Where(f => f.SectionId == sectionId && f.DateTime >= startTime && f.DateTime <= endTime);
        }

        /// <summary>
        /// 交通状态时间查询
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <param name="level">日期级别</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>查询结果</returns>
        public List<SectionStatus> QueryStatusList(int sectionId, DateTimeLevel level, DateTime startTime, DateTime endTime)
        {

            List<DateTime> startTimes = new List<DateTime>();
            List<DateTime> endTimes = new List<DateTime>();

            if (level == DateTimeLevel.Hour)
            {
                startTimes.Add(startTime);
                startTimes.Add(startTime.AddDays(-1));

                endTimes.Add(endTime);
                endTimes.Add(endTime.AddDays(-1));
            }
            else if (level == DateTimeLevel.Day)
            {
                startTimes.Add(startTime);
                startTimes.Add(startTime.AddMonths(-1));

                endTimes.Add(endTime);
                endTimes.Add(endTime.AddMonths(-1));
            }
            else if (level == DateTimeLevel.Month)
            {
                startTimes.Add(startTime);
                startTimes.Add(startTime.AddYears(-1));

                endTimes.Add(endTime);
                endTimes.Add(endTime.AddYears(-1));
            }

            return startTimes
                .Select((t, i) => Where(_context.SectionStatuses, sectionId, level, startTimes[i], endTimes[i]))
                .Select(list => new SectionStatus
                {
                    Good = list.Sum(l => l.Good),
                    Normal = list.Sum(l => l.Normal),
                    Warning = list.Sum(l => l.Warning),
                    Bad = list.Sum(l => l.Bad),
                    Dead = list.Sum(l => l.Dead)
                })
                .ToList();
        }

        /// <summary>
        /// 拥堵时长图表查询
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <param name="level">日期级别</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>查询结果</returns>
        public List<TrafficChart<DateTime, int>> QueryCongestionChart(int sectionId, DateTimeLevel level, DateTime startTime, DateTime endTime)
        {
            IQueryable<SectionStatus> whereQueryable = Where(_context.SectionStatuses, sectionId, level, startTime, endTime);
            string timeFormat = TimePointConvert.TimeFormat(level);

            if (level >= DateTimeLevel.Day)
            {
                return whereQueryable
                    .GroupBy(f => TimePointConvert.CurrentTimePoint(level, f.DateTime))
                    .Select(g => new TrafficChart<DateTime, int>
                    {
                        Axis = g.Key,
                        Remark = g.Key.ToString(timeFormat),
                        Value = g.Sum(f => f.Warning + f.Bad + f.Dead)
                    })
                    .ToList();
            }
            else
            {
                return whereQueryable
                    .Select(f => new TrafficChart<DateTime, int>
                    {
                        Axis = f.DateTime,
                        Remark = f.DateTime.ToString(timeFormat),
                        Value = f.Warning + f.Bad + f.Dead
                    })
                    .ToList();
            }
        }
    }
}
