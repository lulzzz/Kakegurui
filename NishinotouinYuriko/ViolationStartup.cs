using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Models;
using ItsukiSumeragi.Monitor;
using Kakegurui.Core;
using Kakegurui.Log;
using Kakegurui.Monitor;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using NishinotouinYuriko.Cache;
using NishinotouinYuriko.Controllers;
using NishinotouinYuriko.Data;
using NishinotouinYuriko.DataFlow;
using NishinotouinYuriko.Models;
using NishinotouinYuriko.Monitor;
using YumekoJabami.Cache;
using YumekoJabami.Controllers;
using YumekoJabami.Models;
using YumekoJabami.Monitor;

namespace NishinotouinYuriko
{
    /// <summary>
    /// 违法系统
    /// </summary>
    public class ViolationStartup
    {
        /// <summary>
        /// 文件请求前缀名
        /// </summary>
        public const string FileRequestPath = "files";

        /// <summary>
        /// 实例工厂
        /// </summary>
        private IServiceProvider _serviceProvider;

        /// <summary>
        /// 配置项
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// 文件保存路径
        /// </summary>
        private readonly string _filePath;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration">配置项</param>
        /// <param name="logger">日志</param>
        public ViolationStartup(IConfiguration configuration, ILogger<ViolationStartup> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _filePath= _configuration.GetValue("FilePath", Path.Combine(AppDomain.CurrentDomain.BaseDirectory));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            string dbIp = _configuration.GetValue<string>("DbIp");
            int dbPort = _configuration.GetValue<int>("DbPort");
            string dbUser = _configuration.GetValue<string>("DbUser");
            string dbPassword = _configuration.GetValue<string>("DbPassword");
            string violationDb = _configuration.GetValue<string>("ViolationDb");
            string streamUrl = _configuration.GetValue<string>("StreamUrl");

            _logger.LogInformation((int)LogEvent.配置项, $"DbIp {dbIp}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbPort {dbPort}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbUser {dbUser}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbPassword {dbPassword}");
            _logger.LogInformation((int)LogEvent.配置项, $"ViolationDb {violationDb}");
            _logger.LogInformation((int)LogEvent.配置项, $"FilePath {_filePath}");
            _logger.LogInformation((int)LogEvent.配置项, $"StreamUrl {streamUrl}");

            services.AddHealthChecks().AddCheck<SystemStatusMonitor>("系统状态");
            services.AddHealthChecks().AddCheck<FixedJobTask>("定时任务");
            services.AddHealthChecks().AddCheck<SystemSyncMonitor>("设备同步");
            services.AddHealthChecks().AddCheck<ViolationBranchBlock>("违法数据块状态");

            services.AddHealthChecks().AddDbContextCheck<ViolationContext>("违法系统数据库");

            services.AddDbContextPool<ViolationContext>(options => options.UseMySQL(string.Format(BranchDbConvert.DbFormat, dbIp, dbPort, dbUser, dbPassword, violationDb)));

            services.AddSingleton(typeof(FixedJobTask), typeof(FixedJobTask));
            services.AddSingleton(typeof(SystemStatusMonitor), typeof(SystemStatusMonitor));
            services.AddSingleton(typeof(ViolationDeviceStatusMonitor), typeof(ViolationDeviceStatusMonitor));
            services.AddSingleton(typeof(ViolationBranchBlock), typeof(ViolationBranchBlock));
            services.AddSingleton(typeof(SystemSyncMonitor), typeof(SystemSyncMonitor));
            services.AddSingleton(typeof(StorageMonitor<ViolationStruct>), typeof(StorageMonitor<ViolationStruct>));
            services.AddSingleton(typeof(TodayViolationMonitor), typeof(TodayViolationMonitor));
            services.AddSingleton(typeof(StreamMonitor), typeof(StreamMonitor));

            services.AddHttpClient()
                .AddMemoryCache()
                .ConfigureTrafficJWTToken()
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                })
                .AddApplicationPart(Assembly.GetAssembly(typeof(ViolationStructsController)))
                .AddApplicationPart(Assembly.GetAssembly(typeof(LogsController)));
        }

        public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime)
        {
            _serviceProvider = app.ApplicationServices;
            appLifetime.ApplicationStarted.Register(Start);
            appLifetime.ApplicationStopping.Register(Stop);
            Directory.CreateDirectory(_filePath);
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    _filePath),
                RequestPath = $"/{FileRequestPath}"
            });

            app.UseWebSockets()
                .UseTrafficException()
                .UseTrafficCors()
                .UseTrafficHealthChecks()
                .UseAuthentication()
                .UseMvc();
        }

        /// <summary>
        /// 开始服务
        /// </summary>
        private void Start()
        {
            InitDb();

            InitCache();

            InitAdapter();

            InitMonitor();
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        private void InitDb()
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化数据库");

            using (IServiceScope serviceScope=_serviceProvider.CreateScope())
            {
                using (ViolationContext context = serviceScope.ServiceProvider.GetRequiredService<ViolationContext>())
                {
                    if (context.Database.EnsureCreated())
                    {
                        context.Version.Add(new TrafficVersion
                        {
                            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString()
                        });
                        context.SaveChanges();
                    }
                }
            }
        }

        /// <summary>
        /// 初始化缓存
        /// </summary>
        /// <returns>设备集合</returns>
        private void InitCache()
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化缓存");

            IMemoryCache memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();
            IHttpClientFactory httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
            HttpClient client = httpClientFactory.CreateClient();
            string systemUrl = _configuration.GetValue<string>("SystemUrl");

            List<TrafficCode> codes =
                client.Get<List<TrafficCode>>($"http://{systemUrl}/api/codes/");
            memoryCache.InitSystemCache(codes);

            PageModel<TrafficDevice> devices =
                client.Get<PageModel<TrafficDevice>>($"http://{systemUrl}/api/devices?deviceType=3");
            memoryCache.InitDeviceCache(devices.Datas);

            memoryCache.InitViolationChannelCache(devices.Datas);

            PageModel<TrafficViolation> violations =
                client.Get<PageModel<TrafficViolation>>($"http://{systemUrl}/api/violations/");
            memoryCache.InitViolationCache(violations.Datas);

            PageModel<TrafficLocation> locations =
                client.Get<PageModel<TrafficLocation>>($"http://{systemUrl}/api/locations/");
            memoryCache.InitLocationCache(locations.Datas);

        }

        /// <summary>
        /// 初始化缓存
        /// </summary>
        private void InitAdapter()
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化数据适配");
            ViolationBranchBlock violationBranchBlock = _serviceProvider.GetRequiredService<ViolationBranchBlock>();
            violationBranchBlock.Open();
        }

        /// <summary>
        /// 初始化监控
        /// </summary>
        private void InitMonitor()
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化监控任务");

            SystemStatusMonitor systemStatusMonitor = _serviceProvider.GetRequiredService<SystemStatusMonitor>();
            SystemStatusMonitor.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            SystemSyncMonitor systemSyncMonitor = _serviceProvider.GetRequiredService<SystemSyncMonitor>();
            systemSyncMonitor.SystemStatusChanged += Reset;
            StorageMonitor<ViolationStruct> violationStorageMonitor = _serviceProvider.GetRequiredService<StorageMonitor<ViolationStruct>>();
            violationStorageMonitor.SetBranchBlock(_serviceProvider.GetRequiredService<ViolationBranchBlock>());
            TodayViolationMonitor todayViolationMonitor = _serviceProvider.GetRequiredService<TodayViolationMonitor>();
            ViolationDeviceStatusMonitor violationDeviceStatusMonitor= _serviceProvider.GetRequiredService<ViolationDeviceStatusMonitor>();
            StreamMonitor streamMonitor= _serviceProvider.GetRequiredService<StreamMonitor>();
            FixedJobTask fixedJobTask = _serviceProvider.GetRequiredService<FixedJobTask>();
            fixedJobTask.AddFixedJob(systemStatusMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "系统状态检查");
            fixedJobTask.AddFixedJob(systemSyncMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "系统同步");
            fixedJobTask.AddFixedJob(violationStorageMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "违法数据定时保存");
            fixedJobTask.AddFixedJob(todayViolationMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "今日违法统计分析");
            fixedJobTask.AddFixedJob(violationDeviceStatusMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "违法设备状态检查");
            fixedJobTask.AddFixedJob(streamMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "流媒体连接检查");
            fixedJobTask.Start();
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        private void Stop()
        {
            FixedJobTask fixedJobTask = _serviceProvider.GetRequiredService<FixedJobTask>();
            fixedJobTask.Stop();
        }

        /// <summary>
        /// 重置服务
        /// </summary>
        private void Reset(object sender,EventArgs e)
        {
            InitCache();
        }
    }
}
