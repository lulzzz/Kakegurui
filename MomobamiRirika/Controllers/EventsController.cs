using System;
using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MomobamiRirika.Cache;
using MomobamiRirika.Data;
using MomobamiRirika.Models;

namespace MomobamiRirika.Controllers
{
    /// <summary>
    /// 高点事件
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class EventsController : Controller
    {
        /// <summary>
        /// 数据库
        /// </summary>
        private readonly DensityContext _context;

        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// 次数
        /// </summary>
        private const int _count = 3;

        /// <summary>
        /// 间隔时间(分)
        /// </summary>
        private const int _interval = 5;

        /// <summary>
        /// 持续时长(分)
        /// </summary>
        private const int _duration = 10;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库示例</param>
        /// <param name="memoryCache">缓存</param>
        public EventsController(DensityContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// 按路口拥堵事件次数统计
        /// </summary>
        /// <param name="crossingId">路口编号</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>key为拥堵时间value为拥堵次数</returns>
        [HttpGet("statistics/crossings/{crossingId}")]
        public List<TrafficChart<string, int>> StatisticsByRoad(int crossingId, [FromQuery]DateTime startTime, [FromQuery]DateTime endTime)
        {
            HashSet<string> dataIds = new HashSet<string>(
                _memoryCache.GetRegions()
                    .Where(r => r.Channel.CrossingId == crossingId)
                    .Select(p => p.DataId));
            string remark = _memoryCache.GetRegions().Any(r => r.Channel.CrossingId == crossingId)
                ? _memoryCache.GetRegions().First(r => r.Channel.CrossingId == crossingId).Channel.RoadCrossing?.CrossingName
                : string.Empty;
            DateTimeLevel level = DateTimeLevel.Hour;
            string timeFormat = TimePointConvert.TimeFormat(level);
            return _context.Events
                .Where(e => dataIds.Contains(e.DataId) && e.DateTime >= startTime && e.DateTime <= endTime)
                .GroupBy(e => TimePointConvert.CurrentTimePoint(level, e.DateTime))
                .OrderBy(g=>g.Key)
                .Select(g => new TrafficChart<string, int>
                {
                    Axis = g.Key.ToString(timeFormat),
                    Value = g.Count(),
                    Remark = remark
                })
                .ToList();
        }

        /// <summary>
        /// 按区域拥堵事件次数统计
        /// </summary>
        /// <param name="dataId">数据编号</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>key为拥堵时间value为拥堵次数</returns>
        [HttpGet("statistics/regions/{dataId}")]
        public List<TrafficChart<string, int>> StatisticsByRegion([FromRoute]string dataId, [FromQuery]DateTime startTime, [FromQuery]DateTime endTime)
        {
            dataId = Uri.UnescapeDataString(dataId);

            TrafficRegion region = _memoryCache.GetRegion(dataId);
        
            string remark = region==null?string.Empty:region.RegionName;

            DateTimeLevel level = DateTimeLevel.Hour;
            string timeFormat = TimePointConvert.TimeFormat(level);
            return _context.Events
                .Where(e => e.DataId == dataId && e.DateTime >= startTime && e.DateTime <= endTime)
                .GroupBy(e => TimePointConvert.CurrentTimePoint(level, e.DateTime))
                .OrderBy(g => g.Key)
                .Select(g => new TrafficChart<string, int>
                {
                    Axis = g.Key.ToString(timeFormat),
                    Value = g.Count(),
                    Remark = remark
                }).ToList();
        }

        /// <summary>
        /// 路口拥堵次数排名
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>key为路口编号value为拥堵次数</returns>
        [HttpGet("countRank/crossings")]
        public List<TrafficChart<int, int>> CountRankByRoad([FromQuery]DateTime startTime, [FromQuery]DateTime endTime)
        {
            return _context.Events
                .Where(e => _memoryCache.GetRegion(e.DataId,new TrafficRegion{Channel = new TrafficChannel()}).Channel.CrossingId != null
                            && e.DateTime >= startTime && e.DateTime < endTime)
                .Select(e => new TrafficEvent
                {
                    DataId = e.DataId,
                    DateTime = e.DateTime,
                    EndTime = e.EndTime,
                    CrossingId = _memoryCache.GetRegion(e.DataId).Channel.CrossingId.Value
                })
                .GroupBy(e => e.CrossingId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new TrafficChart<int, int>
                {
                    Axis = g.Key,
                    Value = g.Count(),
                    Remark = _memoryCache.GetCrossing(g.Key,new TrafficRoadCrossing()).CrossingName
                }).ToList();
        }

        /// <summary>
        /// 区域的拥堵次数排名
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>key为区域编号value为拥堵次数</returns>
        [HttpGet("countRank/regions")]
        public List<TrafficChart<string, int>> CountRankByRegion([FromQuery]DateTime startTime, [FromQuery]DateTime endTime)
        {
            return _context.Events
                .Where(e => _memoryCache.GetRegion(e.DataId) != null
                            && e.DateTime >= startTime && e.DateTime < endTime)
                .GroupBy(e => e.DataId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new TrafficChart<string, int>
                {
                    Axis = g.Key,
                    Value = g.Count(),
                    Remark = _memoryCache.GetRegion(g.Key).RegionName
                }).ToList();
        }

        /// <summary>
        /// 拥堵时长排名
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>key为路口编号value为拥堵次数</returns>
        [HttpGet("spanRank/crossings")]
        public List<TrafficChart<int, int>> SpanRankByRoad([FromQuery]DateTime startTime, [FromQuery]DateTime endTime)
        {
            return _context.Events
                .Where(e => _memoryCache.GetRegion(e.DataId, new TrafficRegion { Channel = new TrafficChannel() }).Channel.CrossingId != null
                            && e.EndTime != null && e.DateTime >= startTime && e.DateTime < endTime)
                .Select(e => new TrafficEvent
                {
                    DataId = e.DataId,
                    DateTime = e.DateTime,
                    EndTime = e.EndTime,
                    CrossingId = _memoryCache.GetRegion(e.DataId).Channel.CrossingId.Value
                })
                .GroupBy(e => e.CrossingId)
                .OrderByDescending(g => g.Sum(e => (e.EndTime.Value - e.DateTime).TotalSeconds))
                .Take(10)
                .Select(g => new TrafficChart<int, int>
                {
                    Axis = g.Key,
                    Value = Convert.ToInt32(g.Sum(f => (f.EndTime.Value - f.DateTime).TotalSeconds / 60.0)),
                    Remark = _memoryCache.GetCrossing(g.Key, new TrafficRoadCrossing()).CrossingName
                })
                .ToList();
        }

        /// <summary>
        /// 路口内区域拥堵排名
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>key为路口编号value为拥堵次数</returns>
        [HttpGet("spanRank/regions")]
        public List<TrafficChart<string, int>> SpanRankByRegion([FromQuery]DateTime startTime, [FromQuery]DateTime endTime)
        {
            return _context.Events
                .Where(e => _memoryCache.GetRegion(e.DataId) != null
                            && e.EndTime != null && e.DateTime >= startTime && e.DateTime < endTime)
                .GroupBy(e => e.DataId)
                .OrderByDescending(g => g.Sum(f => (f.EndTime.Value - f.DateTime).TotalSeconds))
                .Take(10)
                .Select(g => new TrafficChart<string, int>
                {
                    Axis = g.Key,
                    Value = Convert.ToInt32(g.Sum(f => (f.EndTime.Value - f.DateTime).TotalSeconds) / 60.0),
                    Remark = _memoryCache.GetRegion(g.Key).RegionName
                })
                .ToList();
        }

        /// <summary>
        /// 按区域高发时间段
        /// </summary>
        /// <param name="dataIds">数据编号集合</param>
        /// <param name="startDate">查询时间</param>
        /// <param name="endDate">查询时间</param>
        /// <returns>key为路口编号value为拥堵次数</returns>
        [StringsUrl("dataIds")]
        [HttpGet("incidence/regions/{dataIds}")]
        public List<List<TrafficEvent>> AnalysisIncidence([FromRoute]string[] dataIds, [FromQuery]DateTime startDate, [FromQuery]DateTime endDate)
        {
            dataIds = dataIds.Select(Uri.UnescapeDataString).ToArray();

            int days = Convert.ToInt32((endDate.AddDays(1) - startDate).TotalDays);
            List<DateTime> startTimes = new List<DateTime>();
            List<DateTime> endTimes = new List<DateTime>();
            for (int d = 0; d < days; ++d)
            {
                startTimes.Add(startDate.AddDays(d));
                endTimes.Add(startDate.AddDays(d + 1));
            }

            List<List<TrafficEvent>> result = new List<List<TrafficEvent>>();
            foreach (string dataId in dataIds)
            {
                List<TrafficEvent> regionEvents = new List<TrafficEvent>();
                for (int i = 0; i < startTimes.Count; ++i)
                {
                    List < TrafficEvent > list = _context.Events.Where(f =>
                        f.DataId == dataId && f.EndTime != null && f.DateTime >= startTimes[i] && f.DateTime < endTimes[i])
                        .ToList();
                    List<TrafficEvent> list1 = GetByCount(list);
                    List<TrafficEvent> list2 = GetBySpan(list);
                    List<TrafficEvent> events = Union(dataId, list1, list2);
                    regionEvents = i == 0 ? events : Intersect(dataId, regionEvents, events);
                }
                result.Add(regionEvents
                    .Select(e=>_memoryCache.FillEvent(e))
                    .ToList());
            }

            return result;
        }

        /// <summary>
        /// 按时间高发时间段
        /// </summary>
        /// <param name="dataId">数据编号</param>
        /// <param name="startDate">开始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <returns>key为路口编号value为拥堵次数</returns>
        [HttpGet("incidence/date/{dataId}")]
        public List<List<TrafficEvent>> AnalysisIncidence([FromRoute]string dataId, [FromQuery]DateTime startDate, [FromQuery]DateTime endDate)
        {
            dataId = Uri.UnescapeDataString(dataId);
            int days = Convert.ToInt32((endDate.AddDays(1) - startDate).TotalDays);
            List<DateTime> startTimes = new List<DateTime>();
            List<DateTime> endTimes = new List<DateTime>();
            for (int d = 0; d < days; ++d)
            {
                startTimes.Add(startDate.AddDays(d));
                endTimes.Add(startDate.AddDays(d + 1));
            }
            List<List<TrafficEvent>> result = new List<List<TrafficEvent>>();
            for (int i = 0; i < startTimes.Count; ++i)
            {
                List<TrafficEvent> list = _context.Events
                    .Where(f => f.DataId == dataId && f.EndTime != null && f.DateTime >= startTimes[i] && f.DateTime < endTimes[i])
                    .ToList();

                List<TrafficEvent> list1 = GetByCount(list);
                List<TrafficEvent> list2 = GetBySpan(list);
                List<TrafficEvent> events = Union(dataId, list1, list2);
                result.Add(events.Select(e => _memoryCache.FillEvent(e)).ToList());
            }

            return result;
        }

        /// <summary>
        /// 获取最后10个拥堵事件记录
        /// </summary>
        /// <returns>拥堵事件记录集合</returns>
        [HttpGet("last10")]
        public List<TrafficEvent> QueryLast10()
        {
            List<TrafficEvent> trafficEvents = EventCache.LastEventsCache
                .Where(e => _memoryCache.GetRegion(e.DataId)!= null)
                .Select(e=>_memoryCache.FillEvent(e))
                .OrderByDescending(e => e.DateTime)
                .ToList();
            return trafficEvents;
        }

        /// <summary>
        /// 按次数分析拥堵时间
        /// </summary>
        /// <param name="events">拥堵事件集合</param>
        /// <returns>分析后的事件集合</returns>
        private List<TrafficEvent> GetByCount(List<TrafficEvent> events)
        {
            List<TrafficEvent> result = new List<TrafficEvent>();

            int startTime = 0;
            for (int i = 1; i < events.Count; ++i)
            {
                var lastEndTime = events[i - 1].EndTime;
                if (lastEndTime.HasValue&&(events[i].DateTime - lastEndTime.Value).TotalMinutes < _interval)
                {
                    if (i == events.Count - 1)
                    {
                        if (i + 1 - startTime >= _count)
                        {
                            result.Add(new TrafficEvent
                            {
                                DataId = events[startTime].DataId,
                                DateTime = events[startTime].DateTime,
                                EndTime = events[i].EndTime,
                                RegionName = events[startTime].RegionName
                            });
                        }
                    }
                }
                else
                {
                    if (i - startTime >= _count)
                    {
                        result.Add(new TrafficEvent
                        {
                            DataId = events[startTime].DataId,
                            DateTime = events[startTime].DateTime,
                            EndTime = events[i - 1].EndTime,
                            RegionName = events[startTime].RegionName
                        });
                    }
                    startTime = i;
                }
            }

            return result;
        }

        /// <summary>
        /// 按拥堵时长分析拥堵时间
        /// </summary>
        /// <param name="events">拥堵事件集合</param>
        /// <returns>分析后的事件集合</returns>
        private List<TrafficEvent> GetBySpan(List<TrafficEvent> events)
        {
            List<TrafficEvent> result = new List<TrafficEvent>();
            for (int i = 0; i < events.Count; ++i)
            {
                if (events[i].EndTime.HasValue&&(events[i].EndTime.Value - events[i].DateTime).TotalMinutes >= _duration)
                {
                    int startTime;
                    if (i == 0)
                    {
                        startTime = i;
                    }
                    else
                    {
                        var lastEndTime = events[i - 1].EndTime;
                        if (lastEndTime.HasValue&&(events[i].DateTime - lastEndTime.Value).TotalMinutes <= _interval)
                        {
                            startTime = i - 1;
                        }
                        else
                        {
                            startTime = i;
                        }
                    }

                    int endTime;
                    if (i == events.Count - 1)
                    {
                        endTime = i;
                    }
                    else
                    {
                        if ((events[i + 1].DateTime - events[i].EndTime.Value).TotalMinutes <= _interval)
                        {
                            endTime = i + 1;
                        }
                        else
                        {
                            endTime = i;
                        }
                    }
                    result.Add(new TrafficEvent
                    {
                        DataId = events[startTime].DataId,
                        DateTime = events[startTime].DateTime,
                        EndTime = events[endTime].EndTime,
                        RegionName = events[startTime].RegionName
                    });
                }
            }
            return result;
        }

        /// <summary>
        /// 时间段交集
        /// </summary>
        /// <param name="dataId">数据编号</param>
        /// <param name="list1">事件集合1</param>
        /// <param name="list2">事件集合2</param>
        /// <returns>交集结果</returns>
        private List<TrafficEvent> Intersect(string dataId, List<TrafficEvent> list1, List<TrafficEvent> list2)
        {
            if (list1.Count == 0 || list2.Count == 0)
            {
                return new List<TrafficEvent>();
            }

            int days = Convert.ToInt32((new DateTime(list2[0].DateTime.Year, list2[0].DateTime.Month, list2[0].DateTime.Day) -
                        new DateTime(list1[0].DateTime.Year, list1[0].DateTime.Month, list1[0].DateTime.Day)).TotalDays);
            List<DateTime> d1 = new List<DateTime>();
            foreach (TrafficEvent trafficEvent in list1)
            {
                for (DateTime d = trafficEvent.DateTime; d <= trafficEvent.EndTime; d = d.AddSeconds(1))
                {
                    d1.Add(d);
                }
            }

            List<DateTime> d2 = new List<DateTime>();
            foreach (TrafficEvent trafficEvent in list2)
            {
                for (DateTime d = trafficEvent.DateTime; d <= trafficEvent.EndTime; d = d.AddSeconds(1))
                {
                    d2.Add(d.AddDays(-days));
                }
            }

            List<DateTime> d3 = d1.Intersect(d2).OrderBy(d => d).ToList();

            List<TrafficEvent> list = new List<TrafficEvent>();
            TrafficEvent temp = new TrafficEvent();
            for (int i = 0; i < d3.Count; ++i)
            {
                if (temp.DateTime == DateTime.MinValue)
                {
                    temp.DateTime = d3[i];
                    temp.EndTime = d3[i];
                }
                else
                {
                    if (d3[i - 1].AddSeconds(1) == d3[i])
                    {
                        temp.EndTime = d3[i];
                    }
                    else
                    {
                        list.Add(new TrafficEvent
                        {
                            DataId = dataId,
                            DateTime = temp.DateTime,
                            EndTime = temp.EndTime
                        });
                        temp.DateTime = d3[i];
                        temp.EndTime = d3[i];
                    }

                    if (i == d3.Count - 1)
                    {
                        list.Add(new TrafficEvent
                        {
                            DataId = dataId,
                            DateTime = temp.DateTime,
                            EndTime = temp.EndTime
                        });
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 时间段并集
        /// </summary>
        /// <param name="dataId">数据编号</param>
        /// <param name="list1">事件集合1</param>
        /// <param name="list2">事件集合2</param>
        /// <returns>并集结果</returns>
        private List<TrafficEvent> Union(string dataId, List<TrafficEvent> list1, List<TrafficEvent> list2)
        {
            List<DateTime> d1 = new List<DateTime>();
            foreach (TrafficEvent trafficEvent in list1)
            {
                for (DateTime d = trafficEvent.DateTime; d <= trafficEvent.EndTime; d = d.AddSeconds(1))
                {
                    d1.Add(d);
                }
            }

            List<DateTime> d2 = new List<DateTime>();
            foreach (TrafficEvent trafficEvent in list2)
            {
                for (DateTime d = trafficEvent.DateTime; d <= trafficEvent.EndTime; d = d.AddSeconds(1))
                {
                    d2.Add(d);
                }
            }

            List<DateTime> d3 = d1.Union(d2).OrderBy(d => d).ToList();


            List<TrafficEvent> list = new List<TrafficEvent>();

            TrafficEvent temp = new TrafficEvent();
            for (int i = 0; i < d3.Count; ++i)
            {
                if (temp.DateTime == DateTime.MinValue)
                {
                    temp.DateTime = d3[i];
                    temp.EndTime = d3[i];
                }
                else
                {
                    if (d3[i - 1].AddSeconds(1) == d3[i])
                    {
                        temp.EndTime = d3[i];
                    }
                    else
                    {
                        list.Add(new TrafficEvent
                        {
                            DataId = dataId,
                            DateTime = temp.DateTime,
                            EndTime = temp.EndTime
                        });
                        temp.DateTime = d3[i];
                        temp.EndTime = d3[i];
                    }

                    if (i == d3.Count - 1)
                    {
                        list.Add(new TrafficEvent
                        {
                            DataId = dataId,
                            DateTime = temp.DateTime,
                            EndTime = temp.EndTime
                        });
                    }
                }
            }

            return list;
        }
    }
}