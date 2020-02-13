using System;
using ItsukiSumeragi.DataFlow;
using ItsukiSumeragi.Models;
using Kakegurui.Monitor;

namespace ItsukiSumeragi.Monitor
{
    /// <summary>
    /// 数据定时触发入库
    /// </summary>
    public class StorageMonitor<T> : IFixedJob where T:TrafficData
    {
        /// <summary>
        /// 数据块
        /// </summary>
        private TrafficBranchBlock<T> _block;

        /// <summary>
        /// 设置数据块
        /// </summary>
        /// <param name="block"></param>
        public void SetBranchBlock(TrafficBranchBlock<T> block)
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
