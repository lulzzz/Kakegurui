using System;
using ItsukiSumeragi.Data;
using Kakegurui.Core;
using Kakegurui.Log;
using Kakegurui.Monitor;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MomobamiKirari.Data;
using SayakaIgarashi.Monitor;

namespace SayakaIgarashi
{
    /// <summary>
    /// 数据检查系统
    /// </summary>
    public class DataStartup
    {
        /// <summary>
        /// 实例工厂
        /// </summary>
        private IServiceProvider _serviceProvider;

        /// <summary>
        /// 配置参数
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<DataStartup> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration">配置项</param>
        /// <param name="logger">日志</param>
        public DataStartup(IConfiguration configuration,ILogger<DataStartup> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            string dbIp =_configuration.GetValue<string>("DbIp");
            int dbPort = _configuration.GetValue<int>("DbPort");
            string dbUser = _configuration.GetValue<string>("DbUser");
            string dbPassword = _configuration.GetValue<string>("DbPassword");
            string deviceDb = _configuration.GetValue<string>("DeviceDb");
            string flowDb = _configuration.GetValue<string>("FlowDb");

            _logger.LogInformation((int)LogEvent.配置项, $"DbIp {dbIp}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbPort {dbPort}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbUser {dbUser}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbPassword {dbPassword}");
            _logger.LogInformation((int)LogEvent.配置项, $"DeviceDb {deviceDb}");
            _logger.LogInformation((int)LogEvent.配置项, $"FlowDb {flowDb}");

            services.AddDbContext<DeviceContext>(options =>
                options.UseMySQL(string.Format(BranchDbConvert.DbFormat,dbIp,dbPort,dbUser,dbPassword,deviceDb)));
            services.AddDbContext<FlowContext>(options =>
                options.UseMySQL(string.Format(BranchDbConvert.DbFormat, dbIp, dbPort, dbUser, dbPassword, flowDb)));
            services.AddSingleton(typeof(FixedJobTask), typeof(FixedJobTask));
            services.AddSingleton(typeof(FlowDataMonitor), typeof(FlowDataMonitor));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

        }

        public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime)
        {
            _serviceProvider = app.ApplicationServices;
            appLifetime.ApplicationStarted.Register(Start);
            appLifetime.ApplicationStopping.Register(Stop);
            app.UseTrafficHealthChecks()
               .UseMvc();
        }

        private void Start()
        {
            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                FixedJobTask fixedJobTask = serviceScope.ServiceProvider.GetRequiredService<FixedJobTask>();
                FlowDataMonitor flowDataMonitor = serviceScope.ServiceProvider.GetRequiredService<FlowDataMonitor>();
                fixedJobTask.AddFixedJob(flowDataMonitor,DateTimeLevel.FifteenMinutes,TimeSpan.Zero,"flow data");
                fixedJobTask.Start();
            }
        }

        private void Stop()
        {
            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                FixedJobTask fixedJobTask = serviceScope.ServiceProvider.GetRequiredService<FixedJobTask>();
                fixedJobTask.Stop();
            }
        }
    }
}
