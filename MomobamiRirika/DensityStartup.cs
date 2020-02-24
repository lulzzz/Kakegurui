using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
using Microsoft.Extensions.Logging;
using MomobamiRirika.Adapter;
using MomobamiRirika.Cache;
using MomobamiRirika.Controllers;
using MomobamiRirika.Data;
using MomobamiRirika.DataFlow;
using MomobamiRirika.Models;
using MomobamiRirika.Monitor;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Serialization;
using YumekoJabami.Controllers;
using YumekoJabami.Models;
using YumekoJabami.Monitor;

namespace MomobamiRirika
{
    /// <summary>
    /// 高点系统
    /// </summary>
    public class DensityStartup
    {
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
        protected readonly ILogger _logger;

        /// <summary>
        /// 系统管理中心地址
        /// </summary>
        private readonly string _systemUrl;

        /// <summary>
        /// 数据偏移时间(分)
        /// </summary>
        private readonly TimeSpan _dbSpan;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration">配置项</param>
        /// <param name="logger">日志</param>
        public DensityStartup(IConfiguration configuration, ILogger<DensityStartup> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _systemUrl = _configuration.GetValue<string>("SystemUrl");
            _dbSpan = TimeSpan.FromMinutes(_configuration.GetValue<int>("DbSpan"));

        }

        public void ConfigureServices(IServiceCollection services)
        {
            string dbIp = _configuration.GetValue<string>("DbIp");
            int dbPort = _configuration.GetValue<int>("DbPort");
            string dbUser = _configuration.GetValue<string>("DbUser");
            string dbPassword = _configuration.GetValue<string>("DbPassword");
            string densityDb = _configuration.GetValue<string>("DensityDb");

            _logger.LogInformation((int)LogEvent.配置项, $"DbIp {dbIp}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbPort {dbPort}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbUser {dbUser}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbPassword {dbPassword}");
            _logger.LogInformation((int)LogEvent.配置项, $"DensityDb {densityDb}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbSpan {_dbSpan}");

            services.AddHealthChecks().AddCheck<SystemStatusMonitor>("系统状态");
            services.AddHealthChecks().AddCheck<FixedJobTask>("定时任务");
            services.AddHealthChecks().AddCheck<DeviceStatusMonitor>("设备状态");
            services.AddHealthChecks().AddCheck<SystemSyncMonitor>("设备同步");
            services.AddHealthChecks().AddCheck<DensityAdapter>("密度数据接收状态");
            services.AddHealthChecks().AddCheck<DensityBranchBlock>("密度数据块状态");
            services.AddHealthChecks().AddCheck<EventBranchBlock>("事件数据块状态");

            services.AddHealthChecks().AddDbContextCheck<DensityContext>("密度系统数据库");

            services.AddDbContextPool<DensityContext>(options => options.UseMySQL(string.Format(BranchDbConvert.DbFormat, dbIp, dbPort, dbUser, dbPassword, densityDb)));

            services.AddSingleton(typeof(FixedJobTask), typeof(FixedJobTask));
            services.AddSingleton(typeof(SystemStatusMonitor), typeof(SystemStatusMonitor));
            services.AddSingleton(typeof(DensityAdapter), typeof(DensityAdapter));
            services.AddSingleton(typeof(DensityBranchBlock), typeof(DensityBranchBlock));
            services.AddSingleton(typeof(EventBranchBlock), typeof(EventBranchBlock));
            services.AddSingleton(typeof(DeviceStatusMonitor), typeof(DensityDeviceStatusMonitor));
            services.AddSingleton(typeof(SystemSyncMonitor), typeof(SystemSyncMonitor));
            services.AddSingleton(typeof(StorageMonitor<TrafficDensity>), typeof(StorageMonitor<TrafficDensity>));
            services.AddSingleton(typeof(DensitySwitchMonitor), typeof(DensitySwitchMonitor));

            services.AddHttpClient()
                .AddMemoryCache()
                .ConfigureTrafficJWTToken()
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(options => { options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver(); })
                .AddApplicationPart(Assembly.GetAssembly(typeof(DensitiesController)))
                .AddApplicationPart(Assembly.GetAssembly(typeof(LogsController)));
        }

        public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime)
        {
            _serviceProvider = app.ApplicationServices;
            appLifetime.ApplicationStarted.Register(Start);
            appLifetime.ApplicationStopping.Register(Stop);

            app.UseWebSockets()
                .UseMiddleware<WebSocketMiddleware>()
                .UseTrafficException()
                .UseTrafficCors()
                .UseTrafficHealthChecks()
                .UseAuthentication()
                .UseMvc();
        }

        /// <summary>
        /// 开启服务
        /// </summary>
        private void Start()
        {
            InitDb();
            List<TrafficDevice> devices = InitCache();
            InitAdapter(devices);
            InitMonitor();
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        private void InitDb()
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化数据库");

            DateTime minTime = TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel);

            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                using (DensityContext context = serviceScope.ServiceProvider.GetRequiredService<DensityContext>())
                {
                    if (context.Database.EnsureCreated())
                    {
                        context.Version.Add(new TrafficVersion
                        {
                            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString()
                        });
                        context.SaveChanges();
                    }
                    else
                    {
                        context.SaveChanges();
                        try
                        {
                            TrafficDensity_One density = context.Densities_One.OrderByDescending(d => d.Id).FirstOrDefault();
                            if (density != null &&
                                TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel, density.DateTime) != minTime)
                            {
                                context.ChangeDatabase(BranchDbConvert.GetTableName(density.DateTime));
                            }
                        }
                        catch (MySqlException)
                        {
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 初始化缓存
        /// </summary>
        /// <returns>设备集合</returns>
        private List<TrafficDevice> InitCache()
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化缓存");

            DensityCache.DensitiesCache.Clear();
            EventCache.LastEventsCache.Clear();
            WebSocketMiddleware.ClearUrl();

            WebSocketMiddleware.AddUrl(EventWebSocketBlock.EventUrl);

            DateTime now = DateTime.Now;
            DateTime yesterday = now.Date.AddDays(-1);

            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                using (DensityContext context = serviceScope.ServiceProvider.GetRequiredService<DensityContext>())
                {
                    IMemoryCache memoryCache = serviceScope.ServiceProvider.GetRequiredService<IMemoryCache>();
                    IHttpClientFactory httpClientFactory = serviceScope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                    HttpClient client = httpClientFactory.CreateClient();

                    PageModel<TrafficDevice> devices = 
                        client.Get<PageModel<TrafficDevice>>($"http://{_systemUrl}/api/devices?deviceType=2");
                    memoryCache.InitDeviceCache(devices.Datas);

                    PageModel<TrafficRoadCrossing> crossings =
                        client.Get<PageModel<TrafficRoadCrossing>>($"http://{_systemUrl}/api/roadCrossings/");
                    memoryCache.InitCrossingCache(crossings.Datas);

                    DensitiesController densitiesController = new DensitiesController(context, memoryCache);
                    foreach (TrafficDevice device in devices.Datas)
                    {
                        foreach (var relation in device.Device_Channels)
                        {
                            foreach (TrafficRegion region in relation.Channel.Regions)
                            {
                                DensityCache.DensitiesCache.TryAdd(region.DataId, new ConcurrentQueue<TrafficDensity>(densitiesController.QueryList(region.DataId, yesterday, now)));
                                WebSocketMiddleware.AddUrl($"{DensityWebSocketBlock.DensityUrl}{region.DataId}");
                            }
                        }
                    }
                    return devices.Datas;
                }
            }
        }

        /// <summary>
        /// 初始化缓存
        /// </summary>
        /// <param name="devices">设备集合</param>
        private void InitAdapter(List<TrafficDevice> devices)
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化数据适配");

            DateTime minTime = TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel, DateTime.Now);
            DateTime maxTime = TimePointConvert.NextTimePoint(BranchDbConvert.DateLevel, minTime);
            DensityAdapter densityAdapter = _serviceProvider.GetRequiredService<DensityAdapter>();
            DensityBranchBlock densityBranchBlock = _serviceProvider.GetRequiredService<DensityBranchBlock>();
            EventBranchBlock eventBranchBlock = _serviceProvider.GetRequiredService<EventBranchBlock>();
            densityBranchBlock.Open(devices, minTime, maxTime);
            eventBranchBlock.Open(devices);
            densityAdapter.Start(devices, densityBranchBlock, eventBranchBlock);
        }

        private void InitMonitor()
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化监控任务");

            StorageMonitor<TrafficDensity> densityStorageMonitor = _serviceProvider.GetRequiredService<StorageMonitor<TrafficDensity>>();
            densityStorageMonitor.SetBranchBlock(_serviceProvider.GetRequiredService<DensityBranchBlock>());
            DensitySwitchMonitor densitySwitchMonitor = _serviceProvider.GetRequiredService<DensitySwitchMonitor>();
            SystemStatusMonitor systemStatusMonitor = _serviceProvider.GetRequiredService<SystemStatusMonitor>();
            SystemStatusMonitor.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            DeviceStatusMonitor deviceStatusMonitor = _serviceProvider.GetRequiredService<DeviceStatusMonitor>();
            SystemSyncMonitor systemSyncMonitor = _serviceProvider.GetRequiredService<SystemSyncMonitor>();
            systemSyncMonitor.SystemStatusChanged += Reset;

            FixedJobTask fixedJobTask = _serviceProvider.GetRequiredService<FixedJobTask>();
            fixedJobTask.AddFixedJob(systemStatusMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "系统状态检查");
            fixedJobTask.AddFixedJob(deviceStatusMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "设备状态检查");
            fixedJobTask.AddFixedJob(systemSyncMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "系统同步");
            fixedJobTask.AddFixedJob(densitySwitchMonitor, BranchDbConvert.DateLevel, _dbSpan, "密度数据分表切换");
            fixedJobTask.AddFixedJob(densityStorageMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "密度数据定时保存");
            fixedJobTask.Start();
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        private void Stop()
        {
            DensityAdapter densityAdapter = _serviceProvider.GetRequiredService<DensityAdapter>();
            densityAdapter.Stop();
            FixedJobTask fixedJobTask = _serviceProvider.GetRequiredService<FixedJobTask>();
            fixedJobTask.Stop();
        }

        /// <summary>
        /// 重置服务
        /// </summary>
        private void Reset(object sender, EventArgs e)
        {
            List<TrafficDevice> devices = InitCache();

            _logger.LogInformation((int)LogEvent.系统, "重启数据适配");
            DensityAdapter densityAdapter = _serviceProvider.GetRequiredService<DensityAdapter>();
            densityAdapter.Reset(devices);
            DensityBranchBlock densityBranchBlock = _serviceProvider.GetRequiredService<DensityBranchBlock>();
            EventBranchBlock eventBranchBlock = _serviceProvider.GetRequiredService<EventBranchBlock>();
            densityBranchBlock.Reset(devices);
            eventBranchBlock.Reset(devices);

        }
    }
}
