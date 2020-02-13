using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using ItsukiSumeragi.Codes.Flow;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Kakegurui.Log;
using Kakegurui.WebExtensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MomobamiKirari.Models;

namespace MomobamiKirari.Managers
{
    /// <summary>
    /// 流量数据集群查询
    /// </summary>
    public class LaneFlowManager_Cluster : LaneFlowManager
    {
        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<LaneFlowManager> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="logger">日志</param>
        public LaneFlowManager_Cluster(IMemoryCache memoryCache,ILogger<LaneFlowManager> logger)
            :base(memoryCache)
        {
            _logger = logger;
        }

        /// <summary>
        /// 按路段流向查询流量数据
        /// </summary>
        /// <param name="lanes">车道集合</param>
        /// <param name="level">时间粒度</param>
        /// <param name="startTimes">开始时间集合</param>
        /// <param name="endTimes">结束时间集合</param>
        /// <returns>流量数据集合</returns>
        private List<List<LaneFlow>> SelectList(List<TrafficLane> lanes, DateTimeLevel level, DateTime[] startTimes, DateTime[] endTimes)
        {
            string dataIds = StringConvert.ToSplitString(lanes
                .Select(l => l.DataId)
                .ToArray());

            List<string> urls = lanes
                .Where(l=>!string.IsNullOrEmpty(l.Channel.Device_Channel.Device.NodeUrl))
                .Select(l => $"http://{l.Channel.Device_Channel.Device.NodeUrl}/api/laneflows/{dataIds}?level={level}&startTimes={StringConvert.ToSplitString(startTimes)}&endTimes={StringConvert.ToSplitString(endTimes)}")
                .Distinct()
                .ToList();

            List<List<LaneFlow>> totalList = new List<List<LaneFlow>>();
            for (int i = 0; i < startTimes.Length; ++i)
            {
                totalList.Add(new List<LaneFlow>());
            }
            using (HttpClient client = new HttpClient())
            {
                foreach (string url in urls)
                {
                    List<List<LaneFlow>> itemList = client.Get<List<List<LaneFlow>>>(url);
                    if (itemList == null)
                    {
                        _logger.LogDebug((int)LogEvent.流量查询, $"{url} 查询失败");
                    }
                    else
                    {
                        _logger.LogDebug((int)LogEvent.流量查询, $"{url} {itemList.Count}");
                        for (int i = 0; i < startTimes.Length; ++i)
                        {
                            totalList[i].AddRange(itemList[i]);
                        }
                    }
                }
            }

            return totalList;
        }


        public override List<List<TrafficChart<DateTime, int, LaneFlow>>> QueryCharts(List<TrafficLane> lanes, DateTimeLevel level, DateTime[] startTimes, DateTime[] endTimes,DateTime baseTime, FlowType[] flowTypes = null)
        { 
            List<List<LaneFlow>> totalList = SelectList(lanes, level, startTimes, endTimes);
            return startTimes.Select((t, i) => SelectChart(totalList[i].AsQueryable(), level, baseTime, t, flowTypes)).ToList();
        }

        public override List<List<LaneFlow>> QueryList(List<TrafficLane> lanes, DateTimeLevel level, DateTime[] startTimes, DateTime[] endTimes)
        {
            return SelectList(lanes, level, startTimes, endTimes);
        }

    }
}
