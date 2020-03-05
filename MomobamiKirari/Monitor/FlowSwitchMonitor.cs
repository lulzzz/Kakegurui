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
    /// 流量数据分支切换
    /// </summary>
    public class FlowSwitchMonitor: SwitchMonitor<LaneFlow,FlowDevice>
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">实例工厂</param>
        /// <param name="flowBranchBranchBlock">流量数据块</param>
        public FlowSwitchMonitor(IServiceProvider serviceProvider,FlowBranchBlock flowBranchBranchBlock)
            :base(serviceProvider,flowBranchBranchBlock)
        {

        }

        protected override void ChangeDatabase(IServiceProvider serviceProvider, string tableName)
        {
            using (FlowContext context = serviceProvider.GetRequiredService<FlowContext>())
            {
                try
                {
                    context.ChangeDatabase(tableName);
                    _logger.LogInformation((int)LogEvent.分支切换, $"切换流量数据表成功 {tableName}");
                }
                catch (MySqlException ex)
                {
                    _logger.LogError((int)LogEvent.分支切换, ex, "切换流量数据表失败");
                }
            }
        }
    }
}
