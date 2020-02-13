using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Log
{
    /// <summary>
    /// 日志
    /// </summary>
    public static class LogPool
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        static LogPool()
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"))
                .Build();
            Directory = _config.GetValue("Logging:Directory", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../log/"));
            HoldDays = _config.GetValue("Logging:HoldDays", 0);
            SetLoggerProviders(GetLoggerProviders(Directory, HoldDays));
        }

        /// <summary>
        /// 设置日志提供者
        /// </summary>
        /// <param name="loggerProviders">日志提供者集合</param>
        public static void SetLoggerProviders(List<ILoggerProvider> loggerProviders)
        {
            Logger = new LoggerFactory(loggerProviders, new LoggerFilterOptions { MinLevel = GetLogLevel("Logging:LogLevel:Default") }).CreateLogger(typeof(LogPool));
        }

        /// <summary>
        /// 配置项
        /// </summary>
        private static readonly IConfiguration _config;

        /// <summary>
        /// 日志接口
        /// </summary>
        public static ILogger Logger { get; private set; }

        /// <summary>
        /// 文件日志保存目录
        /// </summary>
        public static string Directory { get; }

        /// <summary>
        /// 文件日志保存天数
        /// </summary>
        public static int HoldDays { get;  }

        /// <summary>
        /// 读取日志级别
        /// </summary>
        /// <param name="key">日志配置文件中的配置顺序</param>
        /// <returns>读取成功返回日志级别，否则返回None(6)</returns>
        private static LogLevel GetLogLevel(string key)
        {
            string level = _config.GetValue<string>(key);
            return Enum.TryParse(level, out LogLevel l) ? l : LogLevel.None;
        }

        /// <summary>
        /// 从配置文件获取日志提供者
        /// </summary>
        /// <param name="direction">文件日志目录</param>
        /// <param name="holdDays">文件日志保留天数</param>
        /// <returns>日志提供者集合</returns>
        private static List<ILoggerProvider> GetLoggerProviders(string direction,int holdDays)
        {
            List<ILoggerProvider> providers = new List<ILoggerProvider>();
            for (int i = 0; ; ++i)
            {
                string value = _config.GetValue<string>($"Logging:Logger:{i}:Type");
                if (value == null)
                {
                    break;
                }
                else
                {
                    LogLevel minLevel = GetLogLevel($"Logging:Logger:{i}:MinLevel");
                    if (minLevel == LogLevel.None)
                    {
                        minLevel = LogLevel.Trace;
                    }

                    LogLevel maxLevel = GetLogLevel($"Logging:Logger:{i}:MaxLevel");
                    if (maxLevel == LogLevel.None)
                    {
                        maxLevel = LogLevel.Critical;
                    }
                    switch (value)
                    {
                        case "Console":
                            {
                                providers.Add(new ConsoleLogger(minLevel, maxLevel));
                                break;
                            }
                        case "File":
                            {
                                string name = _config.GetValue<string>($"Logging:Logger:{i}:Name");
                                providers.Add(new FileLogger(minLevel, maxLevel, name,direction,holdDays));
                                break;
                            }
                    }
                }
            }
            return providers;
        }
    }
}
