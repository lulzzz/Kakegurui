using System;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Mvc;
using MomobamiKirari.Codes;
using MomobamiKirari.Models;
using MomobamiKirari.Managers;

namespace MomobamiKirari.Controllers
{
    /// <summary>
    /// 机动车视频结构化
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class VideoStructsController : Controller
    {
        /// <summary>
        /// 路段流量数据库操作实例
        /// </summary>
        private readonly VideoStructManager _manager;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="manager">路段流量数据库操作实例</param>
        public VideoStructsController(VideoStructManager manager)
        {
            _manager = manager;
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
        [HttpGet("crossings/{crossingId}")]
        public PageModel<VideoStruct> QueryByCrossing([FromRoute]int crossingId, [FromQuery]VideoStructType structType,[FromQuery]DateTime startTime, [FromQuery]DateTime endTime, [FromQuery]int pageNum, [FromQuery]int pageSize, [FromQuery]bool hasTotal)
        {
            return _manager.QueryByCrossing(crossingId, structType, startTime, endTime,pageNum,pageSize,hasTotal);
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
        [IntegersUrl("directions")]
        [HttpGet("crossings/{crossingId}/directions/{directions}")]
        public PageModel<VideoStruct> QueryByCrossing([FromRoute]int crossingId, [FromRoute]int[] directions, [FromQuery]VideoStructType structType, [FromQuery]DateTime startTime, [FromQuery]DateTime endTime, [FromQuery]int pageNum, [FromQuery]int pageSize,[FromQuery]bool hasTotal)
        {
            return _manager.QueryByCrossing(crossingId,directions, structType, startTime, endTime, pageNum, pageSize, hasTotal);
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
        [HttpGet("sections/{sectionId}")]
        public PageModel<VideoStruct> QueryBySection([FromRoute]int sectionId, [FromQuery]VideoStructType structType, [FromQuery]DateTime startTime, [FromQuery]DateTime endTime, [FromQuery]int pageNum, [FromQuery]int pageSize, [FromQuery]bool hasTotal)
        {
            return _manager.QueryBySection(sectionId, structType, startTime, endTime, pageNum, pageSize, hasTotal);
        }

        /// <summary>
        /// 按车道查询视频数据化结构数据集合
        /// </summary>
        /// <param name="dataIds">车道数据编号集合</param>
        /// <param name="structType">视频结构化数据类型</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="pageSize">分页页码</param>
        /// <param name="pageNum">分页数量</param>
        /// <param name="hasTotal">是否查询总数</param>
        /// <returns>视频数据化结构数据集合</returns>
        [StringsUrl("dataIds")]
        [HttpGet("{dataIds}")]
        public PageModel<VideoStruct> QueryList([FromRoute]string[] dataIds, [FromQuery]VideoStructType structType, [FromQuery]DateTime startTime, [FromQuery]DateTime endTime, [FromQuery]int pageNum, [FromQuery]int pageSize, [FromQuery]bool hasTotal)
        {
            return _manager.QueryList(dataIds,structType,startTime,endTime,pageNum,pageSize,hasTotal);
        }
    }
}