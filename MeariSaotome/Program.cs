using System;
using System.Collections.Generic;
using System.Linq;
using ItsukiSumeragi;
using Kakegurui.Log;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MomobamiKirari;
using MomobamiRirika;
using NishinotouinYuriko;
using SayakaIgarashi;
using YumekoJabami.Codes;
using YumekoJabami.Controllers;

namespace MeariSaotome
{
    /// <summary>
    /// 主程序
    /// </summary>
    public class Program
    {
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
            int system = commandConfig.GetValue<int>("System");
            string systemUrl = commandConfig.GetValue<string>("SystemUrl");
            string parameterType = commandConfig.GetValue("ParameterType", ParametersController.DefaultType);
            int listenPort = commandConfig.GetValue<int>("ListenPort");
            
            //初始化日志
            string logName = null;
            if (system == (int)SystemType.系统管理中心)
            {
                logName = "System";
            }
            else if (system == (int)SystemType.智慧交通视频检测系统)
            {
                logName = "Flow";
            }
            else if (system == (int)SystemType.智慧高点视频检测系统)
            {
                logName = "Density";
            }
            else if (system == (int)SystemType.智慧交通违法检测系统)
            {
                logName = "Violation";
            }
            else if (system == 5)
            {
                logName = "Data";
            }
            List<string> tempArgs = args.ToList();
            tempArgs.Add($"LogName={logName}");
            args = tempArgs.ToArray();
            List<ILoggerProvider> loggerProviders = new List<ILoggerProvider>
            {
                new ConsoleLogger(LogLevel.Debug,LogLevel.Error),
                new FileLogger(LogLevel.Debug,LogLevel.Error,logName, LogPool.Directory, LogPool.HoldDays)
            };
            LogPool.SetLoggerProviders(loggerProviders);
            LogPool.Logger.LogInformation("123");
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
            if (system!=(int)SystemType.系统管理中心&&systemUrl != null)
            {
                builder.ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddHttpConfiguration(systemUrl, parameterType);
                });
            }

            //配置启动类
            if (system == (int)SystemType.系统管理中心)
            {
                builder.UseStartup<SystemStartup>();
            }
            else if (system == (int)SystemType.智慧交通视频检测系统)
            {
                builder.UseStartup<FlowStartup>();
            }
            else if (system == (int)SystemType.智慧高点视频检测系统)
            {
                builder.UseStartup<DensityStartup>();
            }
            else if (system == (int)SystemType.智慧交通违法检测系统)
            {
                builder.UseStartup<ViolationStartup>();
            }
            else if (system == 5)
            {
                builder.UseStartup<DataStartup>();
            }

            IWebHost webHost = builder.Build();
            LogPool.Logger.LogInformation((int)LogEvent.配置项, $"System {system}");
            LogPool.Logger.LogInformation((int)LogEvent.配置项, $"SystemUrl {systemUrl}");
            LogPool.Logger.LogInformation((int)LogEvent.配置项, $"ParameterType {parameterType}");
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


