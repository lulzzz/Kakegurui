﻿using System;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Log
{
    /// <summary>
    /// 控制台日志类
    /// </summary>
    public class ConsoleLogger : Logger
    {
        /// <summary>
        /// 构造函数，指定级别的日志
        /// </summary>
        /// <param name="minLevel">日志筛选最低级别</param>
        /// <param name="maxLevel">日志筛选最高级别</param>
        public ConsoleLogger(LogLevel minLevel, LogLevel maxLevel)
            :base(minLevel,maxLevel)
        {

        }

        protected override void LogCore(int eventId,string log)
        {
            Console.WriteLine(log);
        }

    };
}
