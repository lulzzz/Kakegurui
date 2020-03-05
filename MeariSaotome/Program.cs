using System;
using System.Collections.Generic;
using System.Linq;
using Kakegurui.Log;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MomobamiKirari;
using MomobamiRirika;
using YumekoJabami;

namespace MeariSaotome
{
    /// <summary>
    /// 主程序
    /// </summary>
    public class Program
    {
        /// <summary>
        /// 系统编号
        /// </summary>
        internal enum SystemType
        {
            User=1,
            Flow=2,
            Density=3
        }

        /// <summary>
        /// 主函数
        /// </summary>
        /// <param name="args">配置参数</param>
        public static void Main(string[] args)
        {
            //未捕获异常处理
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionEventHandler;
            
            //启动参数配置项
            IConfigurationRoot commandConfig =new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();
             SystemType system = commandConfig.GetValue<SystemType>("System");
             int listenPort = commandConfig.GetValue<int>("ListenPort");

            //初始化日志
            List<string> tempArgs = args.ToList();
            tempArgs.Add($"LogName={system}");
            args = tempArgs.ToArray();
            List<ILoggerProvider> loggerProviders = new List<ILoggerProvider>
            {
                new ConsoleLogger(LogLevel.Debug,LogLevel.Error),
                new FileLogger(LogLevel.Debug,LogLevel.Error,system.ToString(), LogPool.Directory, LogPool.HoldDays)
            };
            LogPool.SetLoggerProviders(loggerProviders);
            //添加日志和监听端口
            IWebHostBuilder builder = WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();
                    foreach (ILoggerProvider loggerProvider in loggerProviders)
                    {
                        logging.AddProvider(loggerProvider);
                    }
                })
                .UseUrls($"http://+:{listenPort}/");

            //http配置项
            //if (system!=(int)SystemType.系统管理中心&&systemUrl != null)
            //{
            //    builder.ConfigureAppConfiguration((hostingContext, config) =>
            //    {
            //        config.AddHttpConfiguration(systemUrl, parameterType);
            //    });
            //}

            if (system == SystemType.Flow)
            {
                builder.UseStartup<FlowStartup>();
            }
            else if (system == SystemType.Density)
            {
                builder.UseStartup<DensityStartup>();
            }
            else
            {
                builder.UseStartup<SystemStartup>();
            }

            IWebHost webHost = builder.Build();
            LogPool.Logger.LogInformation((int)LogEvent.配置项, $"System {system}");
            LogPool.Logger.LogInformation((int)LogEvent.配置项, $"ListenPort {listenPort}");
            webHost.Run();
        }

        /// <summary>
        /// 未捕获的异常
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void UnhandledExceptionEventHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            LogPool.Logger.LogError((int)LogEvent.系统, ex, $"Runtime terminating: {e.IsTerminating}");
        }
    }
}


