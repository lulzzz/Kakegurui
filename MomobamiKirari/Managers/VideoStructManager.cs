using System;
using System.Collections.Generic;
using System.Linq;
using Kakegurui.Core;
using Kakegurui.WebExtensions;
using Microsoft.Extensions.Caching.Memory;
using MomobamiKirari.Cache;
using MomobamiKirari.Codes;
using MomobamiKirari.Data;
using MomobamiKirari.Models;

namespace MomobamiKirari.Managers
{
    /// <summary>
    /// 视频结构化查询基类
    /// </summary>
    public class VideoStructManager
    {
        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// 视频结构化数据库
        /// </summary>
        private readonly FlowContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="context">数据库实例</param>
        public VideoStructManager(IMemoryCache memoryCache, FlowContext context)
        {
            _memoryCache = memoryCache;
            _context = context;
        }

        /// <summary>
        /// 查询列表
        /// </summary>
        /// <param name="queryables">数据源集合</param>
        /// <param name="dataIds">车道数据编号集合</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="pageNum">分页页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <param name="hasTotal">是否查询总数</param>
        /// <returns>查询结果</returns>
        private PageModel<VideoStruct> SelectList(List<IQueryable<VideoStruct>> queryables, HashSet<string> dataIds, DateTime startTime, DateTime endTime, int pageNum, int pageSize, bool hasTotal)
        {
            PageModel<VideoStruct> result = new PageModel<VideoStruct>
            {
                Datas = new List<VideoStruct>(),
                Total = 0
            };

            int skipNum = 0;
            bool hasDatas = true;
            int queryPageNum = pageNum;
            int queryPageSize = pageSize;

            //如果有多个数据源，因为是倒序搜索，所以先倒置集合
            //多数据源查询，必须查询总数
            if (queryables.Count > 1)
            {
                queryables.Reverse();
                hasTotal = true;
            }

            foreach (IQueryable<VideoStruct> queryable in queryables)
            {
                //第一个判断表示已经经历过一个数据源的查询
                //第二和第三个判断表示是否查询全部数据
                if (result.Total > 0 && pageNum > 0 && pageSize > 0)
                {
                    if (result.Datas.Count >= pageSize)
                    {
                        hasDatas = false;
                    }
                    //补足
                    else
                    {
                        if (result.Datas.Count == 0)
                        {
                            skipNum = (pageNum - 1) * pageSize - result.Total;
                        }
                        queryPageNum = 1;
                        queryPageSize = pageSize - result.Datas.Count;
                    }
                }

                try
                {
                    PageModel<VideoStruct> model = queryable
                        .Where(f => dataIds.Contains(f.DataId) && f.DateTime >= startTime && f.DateTime <= endTime)
                        .OrderByDescending(f => f.DateTime)
                        .Page(queryPageNum, queryPageSize, skipNum, hasDatas, hasTotal);
                    result.Datas.AddRange(model.Datas.Select(v => _memoryCache.FillVideo(v)));
                    result.Total += model.Total;
                }
                catch
                {

                }
            }
            return result;
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
            List<Lane> lanes = _memoryCache.GetLanes()
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
            List<Lane> lanes = _memoryCache.GetLanes()
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
            List<Lane> lanes= _memoryCache.GetLanes()
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
            List<Lane> lanes = _memoryCache.GetLanes()
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
        public virtual PageModel<VideoStruct> QueryList(List<Lane> lanes, VideoStructType structType, DateTime startTime, DateTime endTime, int pageNum, int pageSize, bool hasTotal)
        {
            return SelectList(BranchDbConvert.GetQuerables(startTime, endTime, _context.Queryable(structType)), lanes.Select(l => l.DataId).ToHashSet(), startTime, endTime, pageNum, pageSize, hasTotal);
        }
    }
}
