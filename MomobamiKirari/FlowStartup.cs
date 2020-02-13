using System;
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
using MomobamiKirari.Adapter;
using MomobamiKirari.Controllers;
using MomobamiKirari.Data;
using MomobamiKirari.DataFlow;
using MomobamiKirari.Managers;
using MomobamiKirari.Models;
using MomobamiKirari.Monitor;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Serialization;
using YumekoJabami.Cache;
using YumekoJabami.Codes;
using YumekoJabami.Models;
using YumekoJabami.Monitor;

namespace MomobamiKirari
{
    /// <summary>
    /// 流量系统
    /// </summary>
    public class FlowStartup
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
        private readonly ILogger _logger;

        /// <summary>
        /// 系统管理中心地址
        /// </summary>
        private readonly string _systemUrl;
        
        /// <summary>
        /// 节点模式
        /// </summary>
        private readonly int _nodeMode;

        /// <summary>
        /// 节点地址
        /// </summary>
        private readonly string _nodeUrl;

        /// <summary>
        /// 数据偏移时间(分)
        /// </summary>
        private readonly TimeSpan _dbSpan;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration">配置项</param>
        /// <param name="logger">日志</param>
        public FlowStartup(IConfiguration configuration, ILogger<FlowStartup> logger)
        {
            _configuration = configuration;
            _systemUrl = _configuration.GetValue<string>("SystemUrl");
            _nodeMode = _configuration.GetValue<int>("NodeMode");
            _nodeUrl = _configuration.GetValue<string>("NodeUrl");
            _dbSpan = TimeSpan.FromMinutes(_configuration.GetValue<int>("DbSpan"));
            _logger = logger;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            string dbIp = _configuration.GetValue<string>("DbIp");
            int dbPort = _configuration.GetValue<int>("DbPort");
            string dbUser = _configuration.GetValue<string>("DbUser");
            string dbPassword = _configuration.GetValue<string>("DbPassword");
            string flowDb = _configuration.GetValue<string>("FlowDb");
            string cacheIp = _configuration.GetValue<string>("CacheIp");
            int cachePort = _configuration.GetValue<int>("CachePort");

            _logger.LogInformation((int)LogEvent.配置项, $"DbIp {dbIp}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbPort {dbPort}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbUser {dbUser}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbPassword {dbPassword}");
            _logger.LogInformation((int)LogEvent.配置项, $"FlowDb {flowDb}");
            _logger.LogInformation((int)LogEvent.配置项, $"CacheIp {cacheIp}");
            _logger.LogInformation((int)LogEvent.配置项, $"CachePort {cachePort}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbSpan {_dbSpan}");
            _logger.LogInformation((int)LogEvent.配置项, $"NodeMode {_nodeMode}");
            _logger.LogInformation((int)LogEvent.配置项, $"NodeUrl {_nodeUrl}");

            services.AddHealthChecks().AddCheck<SystemStatusMonitor>("系统状态");
            services.AddHealthChecks().AddCheck<FixedJobTask>("定时任务");
            services.AddHealthChecks().AddCheck<DeviceStatusMonitor>("设备状态");
            services.AddHealthChecks().AddCheck<SystemSyncMonitor>("设备同步");
            services.AddHealthChecks().AddCheck<FlowAdapter>("流量数据接收状态");
            services.AddHealthChecks().AddCheck<FlowBranchBlock>("流量数据块状态");
            services.AddHealthChecks().AddCheck<VideoBranchBlock>("视频数据块状态");

            services.AddHealthChecks().AddDbContextCheck<FlowContext>("流量系统数据库");

            services.AddDbContextPool<FlowContext>(options => options.UseMySQL(string.Format(BranchDbConvert.DbFormat, dbIp, dbPort, dbUser, dbPassword, flowDb)));

            services.AddSingleton(typeof(FixedJobTask), typeof(FixedJobTask));
            services.AddSingleton(typeof(SystemStatusMonitor), typeof(SystemStatusMonitor));
            services.AddSingleton(typeof(FlowAdapter), typeof(FlowAdapter));
            services.AddSingleton(typeof(FlowBranchBlock), typeof(FlowBranchBlock));
            services.AddSingleton(typeof(VideoBranchBlock), typeof(VideoBranchBlock));
            services.AddSingleton(typeof(DeviceStatusMonitor), typeof(FlowDeviceStatusMonitor));
            services.AddSingleton(typeof(SystemSyncMonitor), typeof(SystemSyncMonitor));
            services.AddSingleton(typeof(StorageMonitor<LaneFlow>), typeof(StorageMonitor<LaneFlow>));
            services.AddSingleton(typeof(StorageMonitor<VideoStruct>), typeof(StorageMonitor<VideoStruct>));
            services.AddSingleton(typeof(FlowSwitchMonitor), typeof(FlowSwitchMonitor));
            services.AddSingleton(typeof(VideoSwitchMonitor), typeof(VideoSwitchMonitor));
            services.AddSingleton(typeof(SectionFlowMonitor), typeof(SectionFlowMonitor));

            if (_nodeMode == (int)NodeMode.Cluster_Manager)
            {
                services.AddScoped(typeof(LaneFlowManager), typeof(LaneFlowManager_Cluster));
                services.AddScoped(typeof(VideoStructManager), typeof(LaneFlowManager_Cluster));
            }
            else
            {
                services.AddScoped(typeof(LaneFlowManager), typeof(LaneFlowManager_Alone));
                services.AddScoped(typeof(VideoStructManager), typeof(VideoStructManager_Alone));
            }

            services.AddHttpClient()
                .AddMemoryCache()
                .AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = $"{cacheIp}:{cachePort}";
                    options.InstanceName = string.Empty;
                })
                .ConfigureTrafficJWTToken()
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(options => { options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver(); })
                .AddApplicationPart(Assembly.GetAssembly(typeof(LaneFlowsController)))
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
        /// 开始服务
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
            using (IServiceScope serviceScope=_serviceProvider.CreateScope())
            {
                using (FlowContext context = serviceScope.ServiceProvider.GetRequiredService<FlowContext>())
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
                        try
                        {
                            LaneFlow laneFlow = context.LaneFlows_One.OrderByDescending(f => f.Id).FirstOrDefault();
                            if (laneFlow != null &&
                                TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel, laneFlow.DateTime) != minTime)
                            {
                                context.ChangeDatabase(BranchDbConvert.GetTableName(laneFlow.DateTime));
                            }

                            VideoVehicle vehicle = context.Vehicles.OrderByDescending(v => v.Id).FirstOrDefault();
                            if (vehicle != null &&
                                TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel, vehicle.DateTime) != minTime)
                            {
                                context.ChangeVehicleTable(BranchDbConvert.GetTableName(vehicle.DateTime));
                            }

                            VideoBike bike = context.Bikes.OrderByDescending(v => v.Id).FirstOrDefault();
                            if (bike != null &&
                                TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel, bike.DateTime) != minTime)
                            {
                                context.ChangeBikeTable(BranchDbConvert.GetTableName(bike.DateTime));
                            }

                            VideoPedestrain pedestrain = context.Pedestrains.OrderByDescending(v => v.Id).FirstOrDefault();
                            if (pedestrain != null &&
                                TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel, pedestrain.DateTime) != minTime)
                            {
                                context.ChangePedestrainTable(BranchDbConvert.GetTableName(pedestrain.DateTime));
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

            IMemoryCache memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();
            IHttpClientFactory httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
            HttpClient client = httpClientFactory.CreateClient();

            List<TrafficCode> codes =
                client.Get<List<TrafficCode>>($"http://{_systemUrl}/api/codes/");
            memoryCache.InitSystemCache(codes);

            PageModel<TrafficDevice> devices =
                client.Get<PageModel<TrafficDevice>>($"http://{_systemUrl}/api/devices?deviceType=1&nodeUrl={_nodeUrl}");
            memoryCache.InitDeviceCache(devices.Datas);

            PageModel<TrafficRoadCrossing> crossings =
                client.Get<PageModel<TrafficRoadCrossing>>($"http://{_systemUrl}/api/roadCrossings/");
            memoryCache.InitCrossingCache(crossings.Datas);

            PageModel<TrafficRoadSection> sections =
                client.Get<PageModel<TrafficRoadSection>>($"http://{_systemUrl}/api/roadSections/");
            memoryCache.InitSectionCache(sections.Datas);
            return devices.Datas;
        }

        /// <summary>
        /// 初始化缓存
        /// </summary>
        /// <param name="devices">设备集合</param>
        private void InitAdapter(List<TrafficDevice> devices)
        { 
            //集群管理不接收数据
            if (_nodeMode != (int) NodeMode.Cluster_Manager)
            {
                _logger.LogInformation((int) LogEvent.系统, "初始化数据适配");

                DateTime minTime = TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel, DateTime.Now);
                DateTime maxTime = TimePointConvert.NextTimePoint(BranchDbConvert.DateLevel, minTime);
                FlowAdapter flowAdapter = _serviceProvider.GetRequiredService<FlowAdapter>();
                FlowBranchBlock flowBranchBlock = _serviceProvider.GetRequiredService<FlowBranchBlock>();
                VideoBranchBlock videoBranchBlock = _serviceProvider.GetRequiredService<VideoBranchBlock>();
                flowBranchBlock.Open(devices, minTime, maxTime);
                videoBranchBlock.Open(minTime, maxTime);
                flowAdapter.Start(devices, flowBranchBlock, videoBranchBlock);
            }
        }

        /// <summary>
        /// 重置服务
        /// </summary>
        private void Reset(object sender, EventArgs e)
        {
            List<TrafficDevice> devices = InitCache();
            //集群管理不接收数据
            if (_nodeMode != (int)NodeMode.Cluster_Manager)
            {
                _logger.LogInformation((int)LogEvent.系统, "重启数据适配");
                FlowAdapter flowAdapter = _serviceProvider.GetRequiredService<FlowAdapter>();
                flowAdapter.Reset(devices);

                FlowBranchBlock flowBranchBlock = _serviceProvider.GetRequiredService<FlowBranchBlock>();
                flowBranchBlock.Reset(devices);
            }
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        private void Stop()
        {
            //集群管理不接收数据
            if (_nodeMode != (int)NodeMode.Cluster_Manager)
            {
                FlowAdapter flowAdapter = _serviceProvider.GetRequiredService<FlowAdapter>();
                flowAdapter.Stop();
                FixedJobTask fixedJobTask = _serviceProvider.GetRequiredService<FixedJobTask>();
                fixedJobTask.Stop();
            }
        }

        /// <summary>
        /// 初始化监控
        /// </summary>
        private void InitMonitor()
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化监控任务");
  
            StorageMonitor<LaneFlow> flowStorageMonitor = _serviceProvider.GetRequiredService<StorageMonitor<LaneFlow>>();
            flowStorageMonitor.SetBranchBlock(_serviceProvider.GetRequiredService<FlowBranchBlock>());
            StorageMonitor<VideoStruct> videoStorageMonitor = _serviceProvider.GetRequiredService<StorageMonitor<VideoStruct>>();
            videoStorageMonitor.SetBranchBlock(_serviceProvider.GetRequiredService<VideoBranchBlock>());
            FlowSwitchMonitor flowSwitchMonitor = _serviceProvider.GetRequiredService<FlowSwitchMonitor>();
            VideoSwitchMonitor videoSwitchMonitor = _serviceProvider.GetRequiredService<VideoSwitchMonitor>();
            SectionFlowMonitor sectionFlowMonitor = _serviceProvider.GetRequiredService<SectionFlowMonitor>();
            SystemStatusMonitor systemStatusMonitor = _serviceProvider.GetRequiredService<SystemStatusMonitor>();
            SystemStatusMonitor.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            DeviceStatusMonitor deviceStatusMonitor = _serviceProvider.GetRequiredService<DeviceStatusMonitor>();
            SystemSyncMonitor systemSyncMonitor = _serviceProvider.GetRequiredService<SystemSyncMonitor>();
            systemSyncMonitor.SystemStatusChanged += Reset;

            FixedJobTask fixedJobTask = _serviceProvider.GetRequiredService<FixedJobTask>();
       
            fixedJobTask.AddFixedJob(systemStatusMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "系统状态检查");
            fixedJobTask.AddFixedJob(systemSyncMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "系统同步");

            //集群单点不计算路段
            if (_nodeMode != (int)NodeMode.Cluster_Data)
            {
                fixedJobTask.AddFixedJob(sectionFlowMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "路段流量计算");
            }

            //集群管理不检查设备，不接收数据
            if (_nodeMode != (int) NodeMode.Cluster_Manager)
            {
                fixedJobTask.AddFixedJob(flowSwitchMonitor, BranchDbConvert.DateLevel, _dbSpan, "流量数据分表切换");
                fixedJobTask.AddFixedJob(videoSwitchMonitor, BranchDbConvert.DateLevel, _dbSpan, "视频数据分表切换");
                fixedJobTask.AddFixedJob(deviceStatusMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "设备状态检查");
                fixedJobTask.AddFixedJob(flowStorageMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "流量数据定时保存");
                fixedJobTask.AddFixedJob(videoStorageMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "视频数据定时保存");
            }

            fixedJobTask.Start();
        }

    }
}
