using System;
using ItsukiSumeragi.Monitor;
using Kakegurui.Log;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MomobamiRirika.Data;
using MomobamiRirika.DataFlow;
using MomobamiRirika.Models;
using MySql.Data.MySqlClient;

namespace MomobamiRirika.Monitor
{
    /// <summary>
    /// 密度数据分支切换
    /// </summary>
    public class DensitySwitchMonitor : SwitchMonitor<TrafficDensity,DensityDevice>
    {

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">实例工厂</param>
        /// <param name="densityBranchBranchBlock">密度数据块</param>
        public DensitySwitchMonitor(IServiceProvider serviceProvider, DensityBranchBlock densityBranchBranchBlock)
            :base(serviceProvider,densityBranchBranchBlock)
        {

        }

        protected override void ChangeDatabase(IServiceProvider serviceProvider, string tableName)
        {
            using (DensityContext context = serviceProvider.GetRequiredService<DensityContext>())
            {
                try
                {
                    context.ChangeDatabase(tableName);
                    _logger.LogInformation((int)LogEvent.分支切换, $"切换密度数据表成功 {tableName}");
                }
                catch (MySqlException ex)
                {
                    _logger.LogError((int)LogEvent.分支切换, ex, "切换密度数据表失败");
                }
            }
        }
    }
}
