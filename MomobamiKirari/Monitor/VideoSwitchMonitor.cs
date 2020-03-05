using System;
using ItsukiSumeragi.Monitor;
using Kakegurui.Log;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MomobamiKirari.Data;
using MomobamiKirari.DataFlow;
using MomobamiKirari.Models;
using MySql.Data.MySqlClient;

namespace MomobamiKirari.Monitor
{
    /// <summary>
    /// 视频结构化数据分支切换
    /// </summary>
    public class VideoSwitchMonitor: SwitchMonitor<VideoStruct, FlowDevice>
    {

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">实例工厂</param>
        /// <param name="videoBranchBranchBlock">视频数据块</param>
        public VideoSwitchMonitor(IServiceProvider serviceProvider,VideoBranchBlock videoBranchBranchBlock)
            :base(serviceProvider, videoBranchBranchBlock)
        {

        }

        protected override void ChangeDatabase(IServiceProvider serviceProvider, string tableName)
        {
            using (FlowContext context = serviceProvider.GetRequiredService<FlowContext>())
            {
                try
                {
                    context.ChangeVehicleTable(tableName);
                    context.ChangeBikeTable(tableName);
                    context.ChangePedestrainTable(tableName);
                    _logger.LogInformation((int)LogEvent.分支切换, $"切换视频结构化数据表成功 {tableName}");
                }
                catch (MySqlException ex)
                {
                    _logger.LogError((int)LogEvent.分支切换, ex, "切换视频结构化数据表失败");
                }
            }
        }
    }
}
