using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Codes;
using ItsukiSumeragi.Managers;
using ItsukiSumeragi.Models;
using ItsukiSumeragi.Monitor;
using Kakegurui.Core;
using Kakegurui.Log;
using Kakegurui.Monitor;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MomobamiRirika.Adapter;
using MomobamiRirika.Cache;
using MomobamiRirika.Codes;
using MomobamiRirika.Controllers;
using MomobamiRirika.Data;
using MomobamiRirika.DataFlow;
using MomobamiRirika.Managers;
using MomobamiRirika.Models;
using MomobamiRirika.Monitor;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Serialization;
using YumekoJabami.Controllers;
using YumekoJabami.Data;
using YumekoJabami.Models;
using YumekoJabami.Monitor;
using Claim = System.Security.Claims.Claim;

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
            _dbSpan = TimeSpan.FromMinutes(_configuration.GetValue<int>("DbSpan"));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            string dbIp = _configuration.GetValue<string>("DbIp");
            int dbPort = _configuration.GetValue<int>("DbPort");
            string dbUser = _configuration.GetValue<string>("DbUser");
            string dbPassword = _configuration.GetValue<string>("DbPassword");
            string dbName = _configuration.GetValue<string>("DbName");

            _logger.LogInformation((int)LogEvent.配置项, $"DbIp {dbIp}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbPort {dbPort}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbUser {dbUser}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbPassword {dbPassword}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbName {dbName}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbSpan {_dbSpan}");

            services.AddHealthChecks().AddCheck<SystemStatusMonitor>("系统状态");
            services.AddHealthChecks().AddCheck<FixedJobTask>("定时任务");
            services.AddHealthChecks().AddCheck<DeviceStatusMonitor>("设备状态");
            services.AddHealthChecks().AddCheck<DensityAdapter>("密度数据接收状态");
            services.AddHealthChecks().AddCheck<DensityBranchBlock>("密度数据块状态");
            services.AddHealthChecks().AddCheck<EventBranchBlock>("事件数据块状态");

            services.AddHealthChecks().AddDbContextCheck<DensityContext>("密度系统数据库");

            services.AddDbContextPool<DensityContext>(options => options.UseMySQL(string.Format(BranchDbConvert.DbFormat, dbIp, dbPort, dbUser, dbPassword, dbName)));

            services.AddScoped(typeof(SystemContext), typeof(DensityContext));
            services.AddScoped(typeof(CodesManager), typeof(CodesManager));
            services.AddScoped(typeof(ChannelsManager), typeof(ChannelsManager));
            services.AddScoped(typeof(DensitiesManager), typeof(DensitiesManager));
            services.AddScoped(typeof(DevicesManager), typeof(DevicesManager));
            services.AddScoped(typeof(RegionsManager), typeof(RegionsManager));
            services.AddScoped(typeof(RoadCrossingsManager), typeof(RoadCrossingsManager));

            services.AddSingleton(typeof(DensityAdapter), typeof(DensityAdapter));
            services.AddSingleton(typeof(DensityBranchBlock), typeof(DensityBranchBlock));
            services.AddSingleton(typeof(EventBranchBlock), typeof(EventBranchBlock));
            services.AddSingleton(typeof(FixedJobTask), typeof(FixedJobTask));
            services.AddSingleton(typeof(SystemStatusMonitor), typeof(SystemStatusMonitor));
            services.AddSingleton(typeof(DeviceStatusMonitor), typeof(DensityDeviceStatusMonitor));
            services.AddSingleton(typeof(StorageMonitor<TrafficDensity,DensityDevice>), typeof(StorageMonitor<TrafficDensity, DensityDevice>));
            services.AddSingleton(typeof(DensitySwitchMonitor), typeof(DensitySwitchMonitor));

            services.ConfigureIdentity<DensityContext>();

            services.AddHttpClient()
                .AddMemoryCache()
                .ConfigureTrafficJWTToken()
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .ConfigureBadRequest()
                .AddJsonOptions(options => { options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver(); })
                .AddApplicationPart(Assembly.GetAssembly(typeof(DensitiesController)))
                .AddApplicationPart(Assembly.GetAssembly(typeof(UsersController)))
                .AddApplicationPart(Assembly.GetAssembly(typeof(LogsController)));
        }

        public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime)
        {
            _serviceProvider = app.ApplicationServices;
            appLifetime.ApplicationStarted.Register(Start);
            appLifetime.ApplicationStopping.Register(Stop);

            //更新系统状态
            app.Use(async (context, next) =>
            {
                await next.Invoke();
                if (context.Response.StatusCode == (int)HttpStatusCode.OK)
                {
                    if (context.Request.Method == "PUT")
                    {
                        string path = context.Request.Path.Value.ToLower();
                        if (path.Length > 0 && path.EndsWith("/"))
                        {
                            path = path.Substring(0, path.Length - 1);
                        }
                        if (path == "/api/channels"
                            || path == "/api/channels/import"
                            || path == "/api/devices"
                            || path == "/api/devices/import"
                            || path == "/api/locations"
                            || path == "/api/locations/import"
                            || path == "/api/roadcrossings"
                            || path == "/api/roadcrossings/import"
                            || path == "/api/roadsections"
                            || path == "/api/roadsections/import"
                            || path == "/api/codes"
                        )
                        {
                            Reset();
                        }
                    }
                    else if (context.Request.Method == "POST" || context.Request.Method == "DELETE")
                    {
                        string path = context.Request.Path.Value.ToLower();
                        if (path.Contains("/api/channels")
                            || path.Contains("/api/devices")
                            || path.Contains("/api/locations")
                            || path.Contains("/api/roadcrossings")
                            || path.Contains("/api/roadsections")
                            || path.Contains("/api/codes")
                        )
                        {
                            Reset();
                        }
                    }
                }
            });

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
            List<DensityDevice> devices = InitCache();
            InitAdapter(devices);
            InitMonitor();
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        private async void InitDb()
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化数据库");

            DateTime minTime = TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel);

            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                using (DensityContext context = serviceScope.ServiceProvider.GetRequiredService<DensityContext>())
                {
                    if (context.Database.EnsureCreated())
                    {
                        #region 用户
                        _logger.LogInformation((int)LogEvent.系统, "创建管理员用户");
                        UserManager<IdentityUser> userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                        await userManager.CreateAsync(new IdentityUser("admin"), "123456");
                        #endregion

                        #region 权限
                        _logger.LogInformation((int)LogEvent.系统, "创建权限");
                        IdentityUser adminUser = await userManager.FindByNameAsync("admin");
                        List<YumekoJabami.Models.Claim> claims = new List<YumekoJabami.Models.Claim>
                            {
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03000000", Descirption = "智慧高点视频检测系统"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03010000", Descirption = "设备管理"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03010100", Descirption = "设备信息维护"},
                                //new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03010200", Descirption = "设备位置维护"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03010300", Descirption = "国标网关设置"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03010400", Descirption = "设备运行状态"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03010500", Descirption = "视频信息维护"},
                                //new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03010600", Descirption = "视频位置维护"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03020000", Descirption = "数据分析"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03020100", Descirption = "交通密度查询"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03020200", Descirption = "交通密度分析"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03020300", Descirption = "拥堵事件统计"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03020400", Descirption = "拥堵事件排名"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03020500", Descirption = "拥堵高发时段"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03030000", Descirption = "系统设置"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03030100", Descirption = "路口维护"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03030300", Descirption = "用户管理"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03030400", Descirption = "角色管理"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03030600", Descirption = "字典管理"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03030700", Descirption = "参数管理"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03030800", Descirption = "日志查询"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03030900", Descirption = "系统监控"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03040000", Descirption = "状况监测"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "03040100", Descirption = "应用检测"},
                            };
                        context.TrafficClaims.AddRange(claims);
                        foreach (YumekoJabami.Models.Claim claim in claims)
                        {
                            await userManager.AddClaimAsync(adminUser, new Claim(claim.Type, claim.Value));
                        }
                        #endregion

                        #region 字典
                        _logger.LogInformation((int)LogEvent.系统, "创建字典");

                        List<Code> densityCodes = new List<Code>
                        {
                            new Code
                            {
                                Key = "DensityDateLevel",
                                Value = (int) DateTimeLevel.FiveMinutes,
                                Description = "五分钟密度"
                            },
                            new Code
                            {
                                Key = "DensityDateLevel",
                                Value = (int) DateTimeLevel.FifteenMinutes,
                                Description = "十五分钟密度"
                            },
                            new Code
                            {
                                Key = "DensityDateLevel",
                                Value = (int) DateTimeLevel.Hour,
                                Description = "一小时密度"
                            },
                            new Code
                            {
                                Key = "DensityDateLevel",
                                Value = (int) DateTimeLevel.Day,
                                Description = "一天密度"
                            },
                            new Code
                            {
                                Key = "DensityDateLevel",
                                Value = (int) DateTimeLevel.Month,
                                Description = "一月密度"
                            }
                        };

                        densityCodes.AddRange(Enum.GetValues(typeof(ChannelType))
                           .Cast<ChannelType>()
                           .Select(e => new Code { Key = typeof(ChannelType).Name, Value = (int)e, Description = e.ToString() }));

                        densityCodes.AddRange(Enum.GetValues(typeof(DeviceModel))
                            .Cast<DeviceModel>()
                            .Select(e => new Code { Key = typeof(DeviceModel).Name, Value = (int)e, Description = e.ToString() }));

                        densityCodes.AddRange(Enum.GetValues(typeof(DeviceStatus))
                            .Cast<DeviceStatus>()
                            .Select(e => new Code { Key = typeof(DeviceStatus).Name, Value = (int)e, Description = e.ToString() }));

                        densityCodes.AddRange(Enum.GetValues(typeof(RtspProtocol))
                            .Cast<RtspProtocol>()
                            .Select(e => new Code { Key = typeof(RtspProtocol).Name, Value = (int)e, Description = e.ToString() }));

                        densityCodes.AddRange(Enum.GetValues(typeof(LogEvent))
                            .Cast<LogEvent>()
                            .Select(e => new Code { Key = typeof(LogEvent).Name, Value = (int)e, Description = e.ToString() }));

                        densityCodes.Add(new Code
                        {
                            Key = "LogLevel",
                            Value = (int)LogLevel.Debug,
                            Description = "调试"
                        });

                        densityCodes.Add(new Code
                        {
                            Key = "LogLevel",
                            Value = (int)LogLevel.Information,
                            Description = "消息"
                        });

                        densityCodes.Add(new Code
                        {
                            Key = "LogLevel",
                            Value = (int)LogLevel.Warning,
                            Description = "警告"
                        });

                        densityCodes.Add(new Code
                        {
                            Key = "LogLevel",
                            Value = (int)LogLevel.Error,
                            Description = "错误"
                        });
                        context.Codes.AddRange(densityCodes);

                        #endregion
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
        private List<DensityDevice> InitCache()
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化缓存");

            DensityCache.DensitiesCache.Clear();
            EventCache.LastEventsCache.Clear();
            WebSocketMiddleware.ClearUrl();

            WebSocketMiddleware.AddUrl(EventWebSocketBlock.EventUrl);

            DateTime now = DateTime.Now;
            DateTime yesterday = now.Date.AddDays(-1);

            IMemoryCache memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();

            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                
                CodesManager codesManager = serviceScope.ServiceProvider.GetRequiredService<CodesManager>();
                memoryCache.InitSystemCache(codesManager.GetList());
                DevicesManager devicesManager = serviceScope.ServiceProvider.GetRequiredService<DevicesManager>();
                List<DensityDevice> devices = devicesManager.GetList(null,0,0,null,null,0,0).Datas;
                memoryCache.InitDeviceCache(devices);
                RoadCrossingsManager roadCrossingsManager = serviceScope.ServiceProvider.GetRequiredService<RoadCrossingsManager>();
                memoryCache.InitCrossingCache(roadCrossingsManager.GetList(null,0,0).Datas);

                DensitiesManager densitiesManager = serviceScope.ServiceProvider.GetRequiredService<DensitiesManager>();
                foreach (DensityDevice device in devices)
                {
                    foreach (var relation in device.DensityDevice_DensityChannels)
                    {
                        foreach (TrafficRegion region in relation.Channel.Regions)
                        {
                            DensityCache.DensitiesCache.TryAdd(region.DataId, new ConcurrentQueue<TrafficDensity>(densitiesManager.QueryList(region.DataId,DateTimeLevel.Minute, yesterday, now)));
                            WebSocketMiddleware.AddUrl($"{DensityWebSocketBlock.DensityUrl}{region.DataId}");
                        }
                    }
                }
                return devices;
            }
        }

        /// <summary>
        /// 初始化缓存
        /// </summary>
        /// <param name="devices">设备集合</param>
        private void InitAdapter(List<DensityDevice> devices)
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

            StorageMonitor<TrafficDensity,DensityDevice> densityStorageMonitor = _serviceProvider.GetRequiredService<StorageMonitor<TrafficDensity, DensityDevice>>();
            densityStorageMonitor.SetBranchBlock(_serviceProvider.GetRequiredService<DensityBranchBlock>());
            DensitySwitchMonitor densitySwitchMonitor = _serviceProvider.GetRequiredService<DensitySwitchMonitor>();
            SystemStatusMonitor systemStatusMonitor = _serviceProvider.GetRequiredService<SystemStatusMonitor>();
            SystemStatusMonitor.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            DeviceStatusMonitor deviceStatusMonitor = _serviceProvider.GetRequiredService<DeviceStatusMonitor>();

            FixedJobTask fixedJobTask = _serviceProvider.GetRequiredService<FixedJobTask>();
            fixedJobTask.AddFixedJob(systemStatusMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "系统状态检查");
            fixedJobTask.AddFixedJob(deviceStatusMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "设备状态检查");
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
        private void Reset()
        {
            List<DensityDevice> devices = InitCache();

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
