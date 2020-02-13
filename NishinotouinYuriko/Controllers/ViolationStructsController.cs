using System;
using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Kakegurui.WebExtensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using NishinotouinYuriko.Cache;
using NishinotouinYuriko.Data;
using NishinotouinYuriko.Models;
using NishinotouinYuriko.Monitor;
using YumekoJabami.Cache;
using ItsukiSumeragi.Codes.Device;
using ItsukiSumeragi.Codes.Flow;
using YumekoJabami.Codes;
using ItsukiSumeragi.Codes.Violation;

namespace NishinotouinYuriko.Controllers
{
    /// <summary>
    /// 违法数据
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ViolationStructsController:Controller
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly ViolationContext _violationContext;

        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="violationContext">数据库实例</param>
        /// <param name="memoryCache">缓存</param>
        public ViolationStructsController(ViolationContext violationContext, IMemoryCache memoryCache)
        {
            _violationContext = violationContext;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// 查询列表
        /// </summary>
        /// <param name="channelNo">通道设备编号</param>
        /// <param name="violationId">违法行为编号</param>
        /// <param name="locationId">地点编号</param>
        /// <param name="carType">车辆类型</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="direction">行驶方向</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="pageNum">分页页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <param name="hasTotal">是否查询全部</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        public PageModel<ViolationStruct> QueryList([FromQuery]string channelNo, [FromQuery]int violationId, [FromQuery]int locationId, [FromQuery]int carType, [FromQuery]int targetType, [FromQuery]int direction, [FromQuery]DateTime startTime, [FromQuery]DateTime endTime, [FromQuery]int pageNum, [FromQuery]int pageSize, [FromQuery]bool hasTotal)
        {
            PageModel<ViolationStruct> model= 
                Where(_violationContext.Violations,DateTimeLevel.Minute,violationId,locationId,carType,targetType,direction,null,channelNo,startTime,endTime)
                    .Select(v=>_memoryCache.FillViolation(v))
                    .Page(pageNum,pageSize,hasTotal);

            return model;
        }

        /// <summary>
        /// 查询图表
        /// </summary>
        /// <param name="violationIds">违法行为编号集合</param>
        /// <param name="locationIds">地点编号集合</param>
        /// <param name="carTypes">车辆类型集合</param>
        /// <param name="targetTypes">目标类型集合</param>
        /// <param name="directions">方向集合</param>
        /// <param name="plateNumber">车牌</param>
        /// <param name="group">分组项</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>查询结果</returns>
        [IntegersUrl("violationIds")]
        [IntegersUrl("carTypes")]
        [IntegersUrl("locationIds")]
        [IntegersUrl("targetTypes")]
        [IntegersUrl("directions")]
        [HttpGet("chart")]
        public List<TrafficChart<string, int,int>> QueryChart([FromQuery]int[] violationIds, [FromQuery]int[] locationIds, [FromQuery]int[] carTypes, [FromQuery]int[] targetTypes, [FromQuery]int[] directions, [FromQuery]string plateNumber, [FromQuery]string group, [FromQuery]DateTime startTime, [FromQuery]DateTime endTime)
        {
            IQueryable<ViolationStruct> queryable =
                    Where(_violationContext.Violations,DateTimeLevel.Minute, violationIds, locationIds, carTypes, targetTypes, directions, plateNumber, null, startTime, endTime)
                    .Select(v => _memoryCache.FillViolation(v));
            return FillChartEmpty(SelectChart(queryable,group), 
                violationIds, locationIds, carTypes, targetTypes, directions, group);
        }

        /// <summary>
        /// 多时间段图表
        /// </summary>
        /// <param name="level">时间级别</param>
        /// <param name="violationIds">违法行为编号集合</param>
        /// <param name="locationIds">地点编号集合</param>
        /// <param name="carTypes">车辆类型集合</param>
        /// <param name="targetTypes">目标类型集合</param>
        /// <param name="directions">方向集合</param>
        /// <param name="plateNumber">车牌</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>查询结果</returns>
        [IntegersUrl("violationIds")]
        [IntegersUrl("carTypes")]
        [IntegersUrl("locationIds")]
        [IntegersUrl("targetTypes")]
        [IntegersUrl("directions")]
        [HttpGet("analysis")]
        public List<List<TrafficChart<string, int>>> QueryCharts([FromQuery]DateTimeLevel level, [FromQuery]int[] violationIds, [FromQuery]int[] locationIds, [FromQuery]int[] carTypes, [FromQuery]int[] targetTypes, [FromQuery]int[] directions, [FromQuery]string plateNumber, [FromQuery]DateTime startTime, [FromQuery]DateTime endTime)
        {
            List<DateTime> startTimes = new List<DateTime>();
            List<DateTime> endTimes = new List<DateTime>();
            if (level == DateTimeLevel.Day)
            {
                startTimes.Add(startTime);
                startTimes.Add(TimePointConvert.PreTimePoint(DateTimeLevel.Month, startTime));
                startTimes.Add(TimePointConvert.PreTimePoint(DateTimeLevel.Season, startTime));
                startTimes.Add(TimePointConvert.PreTimePoint(DateTimeLevel.Year, startTime));

                endTimes.Add(endTime);
                endTimes.Add(TimePointConvert.PreTimePoint(DateTimeLevel.Month, endTime));
                endTimes.Add(TimePointConvert.PreTimePoint(DateTimeLevel.Season, endTime));
                endTimes.Add(TimePointConvert.PreTimePoint(DateTimeLevel.Year, endTime));
            }
            else if (level == DateTimeLevel.Month||level==DateTimeLevel.Season)
            {
                startTimes.Add(startTime);
                startTimes.Add(TimePointConvert.PreTimePoint(DateTimeLevel.Year, startTime));

                endTimes.Add(endTime);
                endTimes.Add(TimePointConvert.PreTimePoint(DateTimeLevel.Year, endTime));
            }
            else if (level == DateTimeLevel.Year) 
            {
                startTimes.Add(startTime);
                endTimes.Add(endTime);
            }
            else
            {
                return new List<List<TrafficChart<string, int>>>();
            }

            return startTimes
                .Select((t, i) => SelectChart(
                    Where(_violationContext.Violations, level, violationIds, locationIds, carTypes, targetTypes, directions,plateNumber,null, startTimes[i], endTimes[i]), 
                    level,
                    startTimes[0], 
                    t))
                .ToList();
        }

        /// <summary>
        /// 分组图表
        /// </summary>
        /// <param name="violationIds">违法行为编号集合</param>
        /// <param name="locationIds">地点编号集合</param>
        /// <param name="carTypes">车辆类型集合</param>
        /// <param name="targetTypes">目标类型集合</param>
        /// <param name="directions">方向集合</param>
        /// <param name="groups">分组项集合</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>查询结果</returns>
        [IntegersUrl("violationIds")]
        [IntegersUrl("carTypes")]
        [IntegersUrl("locationIds")]
        [IntegersUrl("targetTypes")]
        [IntegersUrl("directions")]
        [StringsUrl("groups")]
        [HttpGet("groupChart")]
        public List<TrafficGroupChart<string, int, int>> QueryGroupChart([FromQuery]int[] violationIds, [FromQuery]int[] locationIds, [FromQuery]int[] carTypes, [FromQuery]int[] targetTypes, [FromQuery]int[] directions, [FromQuery]string[] groups, [FromQuery]DateTime startTime, [FromQuery]DateTime endTime)
        {
            IQueryable<ViolationStruct> queryable =
                Where(_violationContext.Violations, DateTimeLevel.Minute, violationIds, locationIds, carTypes, targetTypes, directions,null,null, startTime, endTime)
                    .Select(v => _memoryCache.FillViolation(v));
            return FillGroupChartEmpty(SelectGroupChart(queryable,groups),
                violationIds,locationIds,carTypes,targetTypes,directions,groups);
        }

        /// <summary>
        /// 分组列表
        /// </summary>
        /// <param name="violationIds">违法行为编号集合</param>
        /// <param name="locationIds">地点编号集合</param>
        /// <param name="carTypes">车辆类型集合</param>
        /// <param name="targetTypes">目标类型集合</param>
        /// <param name="directions">方向集合</param>
        /// <param name="groups">分组项集合</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="pageNum">分页页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <param name="hasTotal">是否查询全部</param>
        /// <returns>查询结果</returns>
        [IntegersUrl("violationIds")]
        [IntegersUrl("carTypes")]
        [IntegersUrl("locationIds")]
        [IntegersUrl("targetTypes")]
        [IntegersUrl("directions")]
        [StringsUrl("groups")]
        [HttpGet("groupList")]
        public PageModel<ViolationStruct> QueryGroupList([FromQuery]int[] violationIds, [FromQuery]int[] locationIds, [FromQuery]int[] carTypes, [FromQuery]int[] targetTypes, [FromQuery]int[] directions, [FromQuery]string[] groups, [FromQuery]DateTime startTime, [FromQuery]DateTime endTime, [FromQuery]int pageNum, [FromQuery]int pageSize, [FromQuery]bool hasTotal)
        {
            var model = Where(_violationContext.Violations, DateTimeLevel.Minute, violationIds, locationIds, carTypes,targetTypes,directions,null,null, startTime, endTime)
                .GroupBy(g =>
                    Tuple.Create(groups.Contains("violation") ? g.GetType().GetProperty("ViolationId").GetValue(g, null) : null
                        ,groups.Contains("location") ? g.GetType().GetProperty("LocationId").GetValue(g, null): null
                        , groups.Contains("carType") ? g.GetType().GetProperty("CarType").GetValue(g, null) : null
                        , groups.Contains("targetType") ? g.GetType().GetProperty("TargetType").GetValue(g, null) : null
                        , groups.Contains("direction") ? g.GetType().GetProperty("Direction").GetValue(g, null) : null))
                .Select(g => _memoryCache.FillViolation(new ViolationStruct
                {
                    LocationId = g.First().LocationId,
                    ViolationId = g.First().ViolationId,
                    CarType = g.First().CarType,
                    TargetType = g.First().TargetType,
                    Direction = g.First().Direction,
                    Count = g.Count()
                }))
                .Page(pageNum,pageSize,hasTotal);
            return model;
        }

        /// <summary>
        /// 查询今日违法状态
        /// </summary>
        /// <returns>查询结果</returns>
        [HttpGet("status")]
        public ViolationStatus Status()
        {
            return TodayViolationMonitor.Status;
        }

        /// <summary>
        /// 筛选
        /// </summary>
        /// <param name="queryable">数据源</param>
        /// <param name="level">时间等级</param>
        /// <param name="violationId">违法行为编号集合</param>
        /// <param name="locationId">地点编号集合</param>
        /// <param name="carType">车辆类型集合</param>
        /// <param name="targetType">目标类型集合</param>
        /// <param name="direction">方向集合</param>
        /// <param name="plateNumber">车牌</param>
        /// <param name="channelNo">通道设备编号</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>筛选后的数据源</returns>
        private IQueryable<ViolationStruct> Where(IQueryable<ViolationStruct> queryable, DateTimeLevel level, int violationId, int locationId, int carType, int targetType, int direction, string plateNumber, string channelNo, DateTime startTime, DateTime endTime)
        {
            if (level >= DateTimeLevel.Day)
            {
                startTime = TimePointConvert.CurrentTimePoint(level, startTime);
                endTime = TimePointConvert.NextTimePoint(level, TimePointConvert.CurrentTimePoint(level, endTime)).AddMilliseconds(-1);
            }

            if (violationId != 0)
            {
                queryable = queryable.Where(v => violationId == v.ViolationId);
            }

            if (locationId != 0)
            {
                queryable = queryable.Where(v => locationId==v.LocationId);
            }

            if (carType != 0)
            {
                queryable = queryable.Where(v => carType==v.CarType);
            }

            if (targetType != 0)
            {
                queryable = queryable.Where(v => targetType==v.TargetType);
            }

            if (direction != 0)
            {
                queryable = queryable.Where(v => direction==v.Direction);
            }

            if (!string.IsNullOrEmpty(plateNumber))
            {
                queryable = queryable.Where(v => v.PlateNumber == plateNumber);
            }

            if (!string.IsNullOrEmpty(channelNo))
            {
                queryable = queryable.Where(v => v.DataId == channelNo);
            }

            return queryable.Where(v => v.DateTime >= startTime && v.DateTime <= endTime);
        }


        /// <summary>
        /// 筛选
        /// </summary>
        /// <param name="queryable">数据源</param>
        /// <param name="level">时间等级</param>
        /// <param name="violationIds">违法行为编号集合</param>
        /// <param name="locationIds">地点编号集合</param>
        /// <param name="carTypes">车辆类型集合</param>
        /// <param name="targetTypes">目标类型集合</param>
        /// <param name="directions">方向集合</param>
        /// <param name="plateNumber">车牌</param>
        /// <param name="channelNo">通道编号</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>筛选后的数据源</returns>
        private IQueryable<ViolationStruct> Where(IQueryable<ViolationStruct> queryable, DateTimeLevel level,int[] violationIds, int[] locationIds, int[] carTypes, int[] targetTypes, int[] directions,string plateNumber, string channelNo,DateTime startTime,DateTime endTime)
        {
            if (level >= DateTimeLevel.Day)
            {
                startTime = TimePointConvert.CurrentTimePoint(level, startTime);
                endTime = TimePointConvert.NextTimePoint(level, TimePointConvert.CurrentTimePoint(level, endTime)).AddMilliseconds(-1);
            }

            if (violationIds != null && violationIds.Length > 0)
            {
                queryable = queryable.Where(v => violationIds.Contains(v.ViolationId));
            }

            if (locationIds != null && locationIds.Length > 0)
            {
                queryable = queryable.Where(v => locationIds.Contains(v.LocationId));
            }

            if (carTypes != null && carTypes.Length > 0)
            {
                queryable = queryable.Where(v => carTypes.Contains(v.CarType));
            }

            if (targetTypes != null && targetTypes.Length > 0)
            {
                queryable = queryable.Where(v => targetTypes.Contains(v.TargetType));
            }

            if (directions != null && directions.Length > 0)
            {
                queryable = queryable.Where(v => directions.Contains(v.Direction));
            }

            if (!string.IsNullOrEmpty(plateNumber))
            {
                queryable = queryable.Where(v => v.PlateNumber == plateNumber);
            }

            if (!string.IsNullOrEmpty(channelNo))
            {
                queryable = queryable.Where(v => v.DataId == channelNo);
            }
            return queryable.Where(v => v.DateTime >= startTime && v.DateTime <= endTime);
        }

        /// <summary>
        /// 查询图表
        /// </summary>
        /// <param name="queryable">数据源</param>
        /// <param name="group">分组项</param>
        /// <returns>查询结果</returns>
        private List<TrafficChart<string,int, int>> SelectChart(IQueryable<ViolationStruct> queryable, string group)
        {
            if (group == "violation")
            {
                return queryable
                    .GroupBy(v => v.ViolationId)
                    .Select(g => new TrafficChart<string, int,int>
                    {
                        Axis = g.First().ViolationName,
                        Value = g.Count(),
                        Data = g.Key
                    })
                    .ToList();
            }
            else if (group == "location")
            {
                return queryable
                    .GroupBy(v => v.LocationId)
                    .Select(g => new TrafficChart<string, int, int>
                    {
                        Axis = g.First().LocationName,
                        Value = g.Count(),
                        Data = g.Key
                    })
                    .ToList();
            }
            else if (group == "carType")
            {
                return queryable
                    .GroupBy(v => v.CarType)
                    .Select(g => new TrafficChart<string, int, int>
                    {
                        Axis = g.First().CarType_Desc,
                        Value = g.Count(),
                        Data = g.Key
                    })
                    .ToList();
            }
            else if (group == "targetType")
            {
                return queryable
                    .GroupBy(v => v.TargetType)
                    .Select(g => new TrafficChart<string, int, int>
                    {
                        Axis = g.First().TargetType_Desc,
                        Value = g.Count(),
                        Data = g.Key
                    })
                    .ToList();
            }
            else if (group == "direction")
            {
                return queryable
                    .GroupBy(v => v.Direction)
                    .Select(g => new TrafficChart<string, int, int>
                    {
                        Axis = g.First().Direction_Desc,
                        Value = g.Count(),
                        Data = g.Key
                    })
                    .ToList();
            }
            else
            {
                return new List<TrafficChart<string, int,int>>();
            }
        }

        /// <summary>
        /// 查询多时间段图表
        /// </summary>
        /// <param name="queryable">数据源</param>
        /// <param name="level">日期等级</param>
        /// <param name="baseTime">基准时间</param>
        /// <param name="startTime">开始时间</param>
        /// <returns>查询结果</returns>
        private List<TrafficChart<string, int>> SelectChart(IQueryable<ViolationStruct> queryable,DateTimeLevel level, DateTime baseTime, DateTime startTime)
        {
            TimeSpan span = TimePointConvert.CurrentTimePoint(level, baseTime) - TimePointConvert.CurrentTimePoint(level, startTime);
            string timeFormat = TimePointConvert.TimeFormat(level);
            return queryable
                .GroupBy(v => TimePointConvert.CurrentTimePoint(level, v.DateTime))
                .Select(g => new TrafficChart<string, int>
                {
                    Axis = g.Key.Add(span).ToString(timeFormat),
                    Remark = g.Key.ToString(timeFormat),
                    Value = g.Count()
                })
                .ToList();
        }

        /// <summary>
        /// 查询分组图表
        /// </summary>
        /// <param name="queryable">数据源</param>
        /// <param name="groups">分组项</param>
        /// <param name="groupIndex">当前递归到的分组序号</param>
        /// <returns>查询结果</returns>
        private List<TrafficGroupChart<string,int,int>> SelectGroupChart(IQueryable<ViolationStruct> queryable, string[] groups, int groupIndex = 0)
        {
            if (groups == null||groupIndex==groups.Length)
            {
                return null;
            }
            List<TrafficGroupChart<string, int, int>> list = new List<TrafficGroupChart<string, int, int>>();

            if (groups[groupIndex] == "violation")
            {
                foreach (var g in queryable.GroupBy(v => v.ViolationId))
                {
                    list.Add(new TrafficGroupChart<string, int, int>
                    {
                        Axis = g.First().ViolationName,
                        Value = g.Count(),
                        Data = g.Key,
                        Datas = groupIndex == groups.Length - 1 ? null : SelectGroupChart(g.AsQueryable(), groups, groupIndex + 1)
                    });
                }
            }
            else if (groups[groupIndex] == "location")
            {
                foreach (var g in queryable.GroupBy(v => v.LocationId))
                {
                    list.Add(new TrafficGroupChart<string, int, int>
                    {
                        Axis = g.First().LocationName,
                        Value = g.Count(),
                        Data = g.Key,
                        Datas = groupIndex == groups.Length - 1 ? null : SelectGroupChart(g.AsQueryable(), groups, groupIndex + 1)
                    });
                }
            }
            else if (groups[groupIndex] == "carType")
            {
                foreach (var g in queryable.GroupBy(v => v.CarType))
                {
                    list.Add(new TrafficGroupChart<string, int, int>
                    {
                        Axis = g.First().CarType_Desc,
                        Value = g.Count(),
                        Data = g.Key,
                        Datas = groupIndex == groups.Length - 1 ? null : SelectGroupChart(g.AsQueryable(), groups, groupIndex + 1)
                    });
                }
            }
            else if (groups[groupIndex] == "targetType")
            {
                foreach (var g in queryable.GroupBy(v => v.TargetType))
                {
                    list.Add(new TrafficGroupChart<string, int, int>
                    {
                        Axis = g.First().TargetType_Desc,
                        Value = g.Count(),
                        Data = g.Key,
                        Datas = groupIndex == groups.Length - 1 ? null : SelectGroupChart(g.AsQueryable(), groups, groupIndex + 1)
                    });
                }
            }
            else if (groups[groupIndex] == "direction")
            {
                foreach (var g in queryable.GroupBy(v => v.Direction))
                {
                    list.Add(new TrafficGroupChart<string, int, int>
                    {
                        Axis = g.First().Direction_Desc,
                        Value = g.Count(),
                        Data = g.Key,
                        Datas = groupIndex == groups.Length - 1 ? null : SelectGroupChart(g.AsQueryable(), groups, groupIndex + 1)
                    });
                }
            }

            return list;
        }

        /// <summary>
        /// 填充图表空白项
        /// </summary>
        /// <typeparam name="T">图表类型</typeparam>
        /// <param name="list">要填充的图表数据</param>
        /// <param name="violationIds">违法行为编号集合</param>
        /// <param name="locationIds">地点编号集合</param>
        /// <param name="carTypes">车辆类型集合</param>
        /// <param name="targetTypes">目标类型集合</param>
        /// <param name="directions">方向集合</param>
        /// <param name="group">分组项</param>
        /// <returns>填充后的图表数据</returns>
        private List<T> FillChartEmpty<T>(List<T> list, int[] violationIds, int[] locationIds, int[] carTypes, int[] targetTypes, int[] directions, string group)
            where T : TrafficChart<string, int, int>, new()
        {
            if (group == "violation")
            {
                if (violationIds == null || violationIds.Length == 0)
                {
                    violationIds = _memoryCache.GetViolations().Select(v => v.ViolationId).ToArray();
                }
                violationIds = violationIds.OrderBy(i => i).ToArray();
                for (int i = 0; i < violationIds.Length; ++i)
                {
                    if (list.All(v => v.Data != violationIds[i]))
                    {
                        list.Insert(i, new T
                        {
                            Axis = _memoryCache.GetViolation(violationIds[i],new TrafficViolation()).ViolationName,
                            Value = 0,
                            Data = violationIds[i]
                        });
                    }
                }
            }
            else if (group == "location")
            {
                if (locationIds == null || locationIds.Length == 0)
                {
                    locationIds = _memoryCache.GetLocations().Select(l => l.LocationId).ToArray();
                }
                locationIds = locationIds.OrderBy(i => i).ToArray();
                for (int i = 0; i < locationIds.Length; ++i)
                {
                    if (list.All(v => v.Data != locationIds[i]))
                    {
                        list.Insert(i, new T
                        {
                            Axis = _memoryCache.GetLocation(locationIds[i],new TrafficLocation()).LocationName,
                            Value = 0,
                            Data = locationIds[i]
                        });
                    }
                }
            }
            else if (group == "carType")
            {
                if (carTypes == null || carTypes.Length == 0)
                {
                    carTypes = _memoryCache.GetCodes(SystemType.智慧交通违法检测系统, typeof(CarType)).Select(c => c.Value).ToArray();
                }
                carTypes = carTypes.OrderBy(i => i).ToArray();

                for (int i = 0; i < carTypes.Length; ++i)
                {
                    if (list.All(v => v.Data != carTypes[i]))
                    {
                        list.Insert(i, new T
                        {
                            Axis = _memoryCache.GetCode(SystemType.智慧交通违法检测系统, typeof(CarType), carTypes[i]),
                            Value = 0,
                            Data = carTypes[i]
                        });
                    }
                }
            }
            else if (group == "targetType")
            {
                if (targetTypes == null || targetTypes.Length == 0)
                {
                    targetTypes = _memoryCache.GetCodes(SystemType.智慧交通违法检测系统, typeof(TargetType)).Select(c => c.Value).ToArray();
                }
                targetTypes = targetTypes.OrderBy(i => i).ToArray();

                for (int i = 0; i < targetTypes.Length; ++i)
                {
                    if (list.All(v => v.Data != targetTypes[i]))
                    {
                        list.Insert(i, new T
                        {
                            Axis = _memoryCache.GetCode(SystemType.智慧交通违法检测系统, typeof(TargetType), targetTypes[i]),
                            Value = 0,
                            Data = targetTypes[i]
                        });
                    }
                }
            }
            else if (group == "direction")
            {
                if (directions == null || directions.Length == 0)
                {
                    directions = _memoryCache.GetCodes(SystemType.系统管理中心, typeof(ChannelDirection)).Select(c => c.Value).ToArray();
                }
                directions = directions.OrderBy(i => i).ToArray();

                for (int i = 0; i < directions.Length; ++i)
                {
                    if (list.All(v => v.Data != directions[i]))
                    {
                        list.Insert(i, new T
                        {
                            Axis = _memoryCache.GetCode(SystemType.系统管理中心, typeof(ChannelDirection), directions[i]),
                            Value = 0,
                            Data = directions[i]
                        });
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 填充分组图表空白项
        /// </summary>
        /// <param name="list">要填充的图表数据</param>
        /// <param name="violationIds">违法行为编号集合</param>
        /// <param name="locationIds">地点编号集合</param>
        /// <param name="carTypes">车辆类型集合</param>
        /// <param name="targetTypes">目标类型集合</param>
        /// <param name="directions">方向集合</param>
        /// <param name="groups">分组项集合</param>
        /// <param name="groupIndex">当前递归到的序号</param>
        /// <returns>填充后的图表数据</returns>
        private List<TrafficGroupChart<string, int, int>> FillGroupChartEmpty(List<TrafficGroupChart<string, int, int>> list, int[] violationIds, int[] locationIds, int[] carTypes, int[] targetTypes, int[] directions, string[] groups, int groupIndex = 0)
        {
            if (groups == null || groupIndex == groups.Length)
            {
                return list;
            }

            FillChartEmpty(list, violationIds, locationIds, carTypes, targetTypes, directions, groups[groupIndex]);

            if (groupIndex == groups.Length-1)
            {
                return list;
            }
            
            foreach (TrafficGroupChart<string, int, int> chart in list)
            {
                if (chart.Datas == null&& groupIndex < groups.Length - 1)
                {
                    chart.Datas = new List<TrafficGroupChart<string, int, int>>();
                }
                FillGroupChartEmpty(chart.Datas, violationIds, locationIds, carTypes, targetTypes, directions, groups, groupIndex + 1);
            }
            return list;
        }

    }
}
