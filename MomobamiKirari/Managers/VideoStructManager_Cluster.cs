using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using ItsukiSumeragi.Codes.Flow;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Kakegurui.WebExtensions;
using Microsoft.Extensions.Caching.Memory;
using MomobamiKirari.Models;

namespace MomobamiKirari.Managers
{
    /// <summary>
    /// 视频结构化集群查询
    /// </summary>
    public class VideoStructManager_Cluster : VideoStructManager
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        public VideoStructManager_Cluster(IMemoryCache memoryCache)
            : base(memoryCache)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lanes">车道集合</param>
        /// <param name="structType">视频结构化数据类型</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="pageSize">分页页码</param>
        /// <param name="pageNum">分页数量</param>
        /// <param name="hasTotal">是否查询总数</param>
        /// <returns></returns>
        private PageModel<VideoStruct> SelectList(List<TrafficLane> lanes, VideoStructType structType, DateTime startTime, DateTime endTime, int pageNum, int pageSize, bool hasTotal)
        {
            string dataIds = StringConvert.ToSplitString(lanes
                .Select(l => l.DataId)
                .ToArray());

            List<string> urls = lanes
                .Where(l => !string.IsNullOrEmpty(l.Channel.Device_Channel.Device.NodeUrl))
                .Select(l => $"http://{l.Channel.Device_Channel.Device.NodeUrl}/api/videoStructs/{dataIds}?structType={structType}&startTime={startTime}&endTime={endTime}&pageNum={pageNum}&pageSize={pageSize}&hasTotal={hasTotal}")
                .Distinct()
                .ToList();

            PageModel<VideoStruct> totalList = new PageModel<VideoStruct>
            {
                Datas = new List<VideoStruct>()
            };

            using (HttpClient client = new HttpClient())
            {
                foreach (string url in urls)
                {
                    PageModel<VideoStruct> itemList = client.Get<PageModel<VideoStruct>>(url);
                    if (itemList != null)
                    {
                        totalList.Datas.AddRange(itemList.Datas);
                        totalList.Total += itemList.Total;
                    }
                }
            }

            return totalList;
        }

        public override PageModel<VideoStruct> QueryList(List<TrafficLane> lanes, VideoStructType structType, DateTime startTime, DateTime endTime, int pageNum, int pageSize, bool hasTotal)
        {
            return SelectList(lanes, structType, startTime, endTime, pageNum, pageSize, hasTotal);
        }
    }
}
