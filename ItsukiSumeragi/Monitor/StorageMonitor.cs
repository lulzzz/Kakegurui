using System;
using ItsukiSumeragi.DataFlow;
using ItsukiSumeragi.Models;
using Kakegurui.Monitor;

namespace ItsukiSumeragi.Monitor
{
    /// <summary>
    /// 数据定时触发入库
    /// </summary>
    public class StorageMonitor<T,U> : IFixedJob where T:TrafficData where U:TrafficDevice
    {
        /// <summary>
        /// 数据块
        /// </summary>
        private TrafficBranchBlock<T,U> _block;

        /// <summary>
        /// 设置数据块
        /// </summary>
        /// <param name="block"></param>
        public void SetBranchBlock(TrafficBranchBlock<T,U> block)
        {
            _block = block;
        }

        #region 实现 IFixedJob
        public void Handle(DateTime lastTime,DateTime currentTime, DateTime nextTime)
        {
            _block?.TriggerSave();
        }
        #endregion
    }
}
