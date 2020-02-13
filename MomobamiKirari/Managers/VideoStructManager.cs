using System;
using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Codes.Flow;
using ItsukiSumeragi.Models;
using Kakegurui.WebExtensions;
using Microsoft.Extensions.Caching.Memory;
using MomobamiKirari.Models;

namespace MomobamiKirari.Managers
{
    /// <summary>
    /// 视频结构化查询基类
    /// </summary>
    public abstract class VideoStructManager
    {
        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        protected VideoStructManager(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// 按路口查询机动车视频结构化数据
        /// </summary>
        /// <param name="crossingId">路口编号</param>
        /// <param name="structType">视频结构化数据类型</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="pageSize">分页页码</param>
        /// <param name="pageNum">分页数量</param>
        /// <param name="hasTotal">是否查询总数</param>
        /// <returns>视频结构化数据集合</returns>
        public PageModel<VideoStruct> QueryByCrossing(int crossingId, VideoStructType structType, DateTime startTime, DateTime endTime, int pageNum, int pageSize, bool hasTotal)
        {
            List<TrafficLane> lanes = _memoryCache.GetLanes()
                .Where(l => l.Channel.CrossingId == crossingId)
                .ToList();
            return QueryList(lanes, structType, startTime, endTime, pageNum, pageSize, hasTotal);
        }

        /// <summary>
        /// 按路口方向查询机动车视频结构化数据
        /// </summary>
        /// <param name="crossingId">路口编号</param>
        /// <param name="directions">路口方向</param>
        /// <param name="structType">视频结构化数据类型</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="pageSize">分页页码</param>
        /// <param name="pageNum">分页数量</param>
        /// <param name="hasTotal">是否查询总数</param>
        /// <returns>视频结构化数据集合</returns>
        public PageModel<VideoStruct> QueryByCrossing(int crossingId, int[] directions, VideoStructType structType, DateTime startTime, DateTime endTime, int pageNum, int pageSize, bool hasTotal)
        {
            List<TrafficLane> lanes = _memoryCache.GetLanes()
                .Where(l => l.Channel.CrossingId == crossingId && directions.Contains(l.Direction))
                .ToList();
            return QueryList(lanes, structType, startTime, endTime, pageNum, pageSize, hasTotal);
        }

        /// <summary>
        /// 按路段查询机动车视频结构化数据
        /// </summary>
        /// <param name="sectionId">路段编号</param>
        /// <param name="structType">视频结构化数据类型</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="pageSize">分页页码</param>
        /// <param name="pageNum">分页数量</param>
        /// <param name="hasTotal">是否查询总数</param>
        /// <returns>视频结构化数据集合</returns>
        public PageModel<VideoStruct> QueryBySection(int sectionId, VideoStructType structType, DateTime startTime, DateTime endTime, int pageNum, int pageSize, bool hasTotal)
        {
            List<TrafficLane> lanes= _memoryCache.GetLanes()
                .Where(l => l.Channel.SectionId == sectionId)
                .ToList();
            return QueryList(lanes, structType, startTime, endTime, pageNum, pageSize, hasTotal);
        }

        /// <summary>
        /// 按车道查询视频数据化结构数据集合
        /// </summary>
        /// <param name="dataIds">数据编号集合</param>
        /// <param name="structType">视频结构化数据类型</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="pageSize">分页页码</param>
        /// <param name="pageNum">分页数量</param>
        /// <param name="hasTotal">是否查询总数</param>
        /// <returns>视频数据化结构数据集合</returns>
        public PageModel<VideoStruct> QueryList(string[] dataIds, VideoStructType structType, DateTime startTime,
            DateTime endTime, int pageNum, int pageSize, bool hasTotal)
        {
            List<TrafficLane> lanes = _memoryCache.GetLanes()
                .Where(l => dataIds.Contains(l.DataId))
                .ToList();
            return QueryList(lanes, structType, startTime, endTime, pageNum, pageSize, hasTotal);
        }

        /// <summary>
        /// 按路口查询视频数据化结构数据集合
        /// </summary>
        /// <param name="lanes">车道集合</param>
        /// <param name="structType">视频结构化数据类型</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="pageSize">分页页码</param>
        /// <param name="pageNum">分页数量</param>
        /// <param name="hasTotal">是否查询总数</param>
        /// <returns>视频数据化结构数据集合</returns>
        public abstract PageModel<VideoStruct> QueryList(List<TrafficLane> lanes, VideoStructType structType, DateTime startTime,
            DateTime endTime, int pageNum, int pageSize, bool hasTotal);
    }
}
