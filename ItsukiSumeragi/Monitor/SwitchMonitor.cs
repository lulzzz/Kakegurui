using System;
using ItsukiSumeragi.DataFlow;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Kakegurui.Log;
using Kakegurui.Monitor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ItsukiSumeragi.Monitor
{
    /// <summary>
    /// 数据分支切换
    /// </summary>
    public abstract class SwitchMonitor<T,U>:IFixedJob where T:TrafficData where U:TrafficDevice
    {
        /// <summary>
        /// 实例工厂
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 日志
        /// </summary>
        protected readonly ILogger<SwitchMonitor<T,U>> _logger;

        /// <summary>
        /// 分支数据块
        /// </summary>
        private readonly TrafficBranchBlock<T, U> _branchBlock;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">实例工厂</param>
        /// <param name="branchBlock">数据块</param>
        protected SwitchMonitor(IServiceProvider serviceProvider, TrafficBranchBlock<T,U> branchBlock)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<SwitchMonitor<T, U>>>();
            _branchBlock = branchBlock;
        }

        /// <summary>
        /// 切换数据库
        /// </summary>
        /// <param name="serviceProvider">实例工厂</param>
        /// <param name="tableName">数据库当前分支表后缀</param>
        protected abstract void ChangeDatabase(IServiceProvider serviceProvider,string tableName);

        #region 实现 IFixedJob
        public void Handle(DateTime lastTime,DateTime currentTime, DateTime nextTime)
        {
            _logger.LogInformation((int)LogEvent.分支切换, $"开始分支切换 {lastTime}->{currentTime}");
            _branchBlock.Close();
            _logger.LogInformation((int)LogEvent.分支切换, "保存当前数据");
            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                string tableName = BranchDbConvert.GetTableName(lastTime);
                ChangeDatabase(serviceScope.ServiceProvider,tableName);
            }
            _branchBlock.SwitchBranch(currentTime, nextTime);
            _logger.LogInformation((int)LogEvent.分支切换, $"切换完成分支 {currentTime}->{nextTime}");
        }
        #endregion
    }
}
