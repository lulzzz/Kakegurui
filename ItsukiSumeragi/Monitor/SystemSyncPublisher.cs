using System;
using Kakegurui.Core;

namespace ItsukiSumeragi.Monitor
{
    /// <summary>
    /// 系统状态同步发布
    /// </summary>
    public class SystemSyncPublisher
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        static SystemSyncPublisher()
        {
            TimeStamp = TimeStampConvert.ToUtcTimeStamp();
        }

        /// <summary>
        /// 系统时间戳
        /// </summary>
        public static long TimeStamp { private set; get; }

        /// <summary>
        /// 字典改变
        /// </summary>
        public static event EventHandler SystemStatusChanged;

        /// <summary>
        /// 更新系统时间戳
        /// </summary>
        public static void Update()
        {
            TimeStamp = TimeStampConvert.ToUtcTimeStamp();
            SystemStatusChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
