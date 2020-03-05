using System;
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
using MomobamiKirari.Adapter;
using MomobamiKirari.Cache;
using MomobamiKirari.Codes;
using MomobamiKirari.Controllers;
using MomobamiKirari.Data;
using MomobamiKirari.DataFlow;
using MomobamiKirari.Managers;
using MomobamiKirari.Models;
using MomobamiKirari.Monitor;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Serialization;
using YumekoJabami.Controllers;
using YumekoJabami.Data;
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
            _dbSpan = TimeSpan.FromMinutes(_configuration.GetValue<int>("DbSpan"));
            _logger = logger;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            string dbIp = _configuration.GetValue<string>("DbIp");
            int dbPort = _configuration.GetValue<int>("DbPort");
            string dbUser = _configuration.GetValue<string>("DbUser");
            string dbPassword = _configuration.GetValue<string>("DbPassword");
            string dbName = _configuration.GetValue<string>("DbName");
            string cacheIp = _configuration.GetValue<string>("CacheIp");
            int cachePort = _configuration.GetValue<int>("CachePort");

            _logger.LogInformation((int)LogEvent.配置项, $"DbIp {dbIp}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbPort {dbPort}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbUser {dbUser}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbPassword {dbPassword}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbName {dbName}");
            _logger.LogInformation((int)LogEvent.配置项, $"CacheIp {cacheIp}");
            _logger.LogInformation((int)LogEvent.配置项, $"CachePort {cachePort}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbSpan {_dbSpan}");

            services.AddHealthChecks().AddCheck<SystemStatusMonitor>("系统状态");
            services.AddHealthChecks().AddCheck<FixedJobTask>("定时任务");
            services.AddHealthChecks().AddCheck<DeviceStatusMonitor>("设备状态");
            services.AddHealthChecks().AddCheck<FlowAdapter>("流量数据接收状态");
            services.AddHealthChecks().AddCheck<FlowBranchBlock>("流量数据块状态");
            services.AddHealthChecks().AddCheck<VideoBranchBlock>("视频数据块状态");

            services.AddHealthChecks().AddDbContextCheck<FlowContext>("流量系统数据库");

            services.AddDbContextPool<FlowContext>(options => options.UseMySQL(string.Format(BranchDbConvert.DbFormat, dbIp, dbPort, dbUser, dbPassword, dbName)));

            services.AddScoped(typeof(SystemContext), typeof(FlowContext));
            services.AddScoped(typeof(CodesManager), typeof(CodesManager));
            services.AddScoped(typeof(DevicesManager), typeof(DevicesManager));
            services.AddScoped(typeof(ChannelsManager), typeof(ChannelsManager));
            services.AddScoped(typeof(LanesManager), typeof(LanesManager));
            services.AddScoped(typeof(RoadCrossingsManager), typeof(RoadCrossingsManager));
            services.AddScoped(typeof(RoadSectionsManager), typeof(RoadSectionsManager));
            services.AddScoped(typeof(LaneFlowManager), typeof(LaneFlowManager));
            services.AddScoped(typeof(ChannelFlowsManager), typeof(ChannelFlowsManager));
            services.AddScoped(typeof(SectionFlowsManager), typeof(SectionFlowsManager));
            services.AddScoped(typeof(VideoStructManager), typeof(VideoStructManager));

            services.AddSingleton(typeof(FlowAdapter), typeof(FlowAdapter));
            services.AddSingleton(typeof(FlowBranchBlock), typeof(FlowBranchBlock));
            services.AddSingleton(typeof(VideoBranchBlock), typeof(VideoBranchBlock));
            services.AddSingleton(typeof(FixedJobTask), typeof(FixedJobTask));
            services.AddSingleton(typeof(SystemStatusMonitor), typeof(SystemStatusMonitor));
            services.AddSingleton(typeof(DeviceStatusMonitor), typeof(FlowDeviceStatusMonitor));
            services.AddSingleton(typeof(StorageMonitor<LaneFlow, FlowDevice>), typeof(StorageMonitor<LaneFlow, FlowDevice>));
            services.AddSingleton(typeof(StorageMonitor<VideoStruct, FlowDevice>), typeof(StorageMonitor<VideoStruct, FlowDevice>));
            services.AddSingleton(typeof(FlowSwitchMonitor), typeof(FlowSwitchMonitor));
            services.AddSingleton(typeof(VideoSwitchMonitor), typeof(VideoSwitchMonitor));
            services.AddSingleton(typeof(SectionFlowMonitor), typeof(SectionFlowMonitor));


            services.ConfigureIdentity<FlowContext>();

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
                .ConfigureBadRequest()
                .AddJsonOptions(options => { options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver(); })
                .AddApplicationPart(Assembly.GetAssembly(typeof(LaneFlowsController)))
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
        /// 开始服务
        /// </summary>
        private void Start()
        {
            InitDb();

            List<FlowDevice> devices = InitCache();

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
            using (IServiceScope serviceScope=_serviceProvider.CreateScope())
            {
                using (FlowContext context = serviceScope.ServiceProvider.GetRequiredService<FlowContext>())
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
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02000000", Descirption = "智慧交通视频检测系统"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02010000", Descirption = "设备管理"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02010100", Descirption = "设备信息维护"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02010200", Descirption = "设备位置维护"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02010300", Descirption = "国标网关设置"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02010400", Descirption = "校时配置"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02010500", Descirption = "设备操作"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02010600", Descirption = "设备运行状态"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02010700", Descirption = "视频信息维护"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02010800", Descirption = "视频位置维护"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02020000", Descirption = "数据分析"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02020100", Descirption = "通行信息查询"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02020200", Descirption = "流量数据分析"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02020300", Descirption = "IO数据监测"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02020400", Descirption = "流量分布查询"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02020500", Descirption = "拥堵趋势分析"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02020600", Descirption = "状态时间统计"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02020700", Descirption = "交通状态分析"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02030000", Descirption = "系统设置"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02030100", Descirption = "路口维护"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02030200", Descirption = "路段维护"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02030300", Descirption = "用户管理"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02030400", Descirption = "角色管理"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02030600", Descirption = "字典管理"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02030700", Descirption = "参数管理"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02030800", Descirption = "日志查询"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02030900", Descirption = "系统监控"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02040000", Descirption = "状况监测"},
                                new YumekoJabami.Models.Claim{ Type = ClaimTypes.Webpage, Value = "02040100", Descirption = "应用检测"},
                            };
                        context.TrafficClaims.AddRange(claims);
                        foreach (YumekoJabami.Models.Claim claim in claims)
                        {
                            await userManager.AddClaimAsync(adminUser, new System.Security.Claims.Claim(claim.Type, claim.Value));
                        }
                        #endregion

                        #region 字典
                        _logger.LogInformation((int)LogEvent.系统, "创建字典");
                        List<Code> flowCodes = new List<Code>();

                        flowCodes.AddRange(Enum.GetValues(typeof(LogEvent))
                            .Cast<LogEvent>()
                            .Select(e => new Code { Key = typeof(LogEvent).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.Add(new Code
                        {
                            Key = "LogLevel",
                            Value = (int)LogLevel.Debug,
                            Description = "调试"
                        });

                        flowCodes.Add(new Code
                        {
                            Key = "LogLevel",
                            Value = (int)LogLevel.Information,
                            Description = "消息"
                        });

                        flowCodes.Add(new Code
                        {
                            Key = "LogLevel",
                            Value = (int)LogLevel.Warning,
                            Description = "警告"
                        });

                        flowCodes.Add(new Code
                        {
                            Key = "LogLevel",
                            Value = (int)LogLevel.Error,
                            Description = "错误"
                        });

                       
                        flowCodes.AddRange(Enum.GetValues(typeof(ChannelType))
                            .Cast<ChannelType>()
                            .Select(e => new Code { Key = typeof(ChannelType).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(DeviceModel))
                            .Cast<DeviceModel>()
                            .Select(e => new Code { Key = typeof(DeviceModel).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(DeviceStatus))
                            .Cast<DeviceStatus>()
                            .Select(e => new Code { Key = typeof(DeviceStatus).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(RtspProtocol))
                            .Cast<RtspProtocol>()
                            .Select(e => new Code { Key = typeof(RtspProtocol).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(SectionDirection))
                            .Cast<SectionDirection>()
                            .Select(e => new Code { Key = typeof(SectionDirection).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(SectionType))
                            .Cast<SectionType>()
                            .Select(e => new Code { Key = typeof(SectionType).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(Age))
                            .Cast<Age>()
                            .Select(e => new Code { Key = typeof(Age).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(NonVehicle))
                            .Cast<NonVehicle>()
                            .Select(e => new Code { Key = typeof(NonVehicle).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(CarColor))
                            .Cast<CarColor>()
                            .Select(e => new Code { Key = typeof(CarColor).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(CarType))
                            .Cast<CarType>()
                            .Select(e => new Code { Key = typeof(CarType).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(FlowDirection))
                            .Cast<FlowDirection>()
                            .Select(e => new Code { Key = typeof(FlowDirection).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.Add(new Code
                        {
                            Key = "VehicleType",
                            Value = (int)FlowType.三轮车,
                            Description = FlowType.三轮车.ToString()
                        });
                        flowCodes.Add(new Code
                        {
                            Key = "VehicleType",
                            Value = (int)FlowType.卡车,
                            Description = FlowType.卡车.ToString()
                        });
                        flowCodes.Add(new Code
                        {
                            Key = "VehicleType",
                            Value = (int)FlowType.客车,
                            Description = FlowType.客车.ToString()
                        });

                        flowCodes.Add(new Code
                        {                            Key = "VehicleType",
                            Value = (int)FlowType.轿车,
                            Description = FlowType.轿车.ToString()
                        });

                        flowCodes.Add(new Code
                        {
                            Key = "VehicleType",
                            Value = (int)FlowType.面包车,
                            Description = FlowType.面包车.ToString()
                        });
                        flowCodes.Add(new Code
                        {
                            Key = "BikeType",
                            Value = (int)FlowType.自行车,
                            Description = FlowType.自行车.ToString()
                        });
                        flowCodes.Add(new Code
                        {
                            Key = "BikeType",
                            Value = (int)FlowType.摩托车,
                            Description = FlowType.摩托车.ToString()
                        });
                        flowCodes.Add(new Code
                        {
                            Key = "PedestrainType",
                            Value = (int)FlowType.行人,
                            Description = FlowType.行人.ToString()
                        });
                        flowCodes.Add(new Code
                        {
                            Key = "FlowType",
                            Value = (int)FlowType.平均速度,
                            Description = FlowType.平均速度.ToString()
                        });
                        flowCodes.Add(new Code
                        {
                            Key = "FlowType",
                            Value = (int)FlowType.车头时距,
                            Description = FlowType.车头时距.ToString()
                        });
                        flowCodes.Add(new Code
                        {
                            Key = "FlowType",
                            Value = (int)FlowType.车头间距,
                            Description = FlowType.车头间距.ToString()
                        });
                        flowCodes.Add(new Code
                        {
                            Key = "FlowType",
                            Value = (int)FlowType.时间占有率,
                            Description = FlowType.时间占有率.ToString()
                        });
                        flowCodes.Add(new Code
                        {
                            Key = "FlowType",
                            Value = (int)FlowType.空间占有率,
                            Description = FlowType.空间占有率.ToString()
                        });

                        flowCodes.AddRange(Enum.GetValues(typeof(LaneDirection))
                            .Cast<LaneDirection>()
                            .Select(e => new Code { Key = typeof(LaneDirection).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(LaneType))
                            .Cast<LaneType>()
                            .Select(e => new Code { Key = typeof(LaneType).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(PlateType))
                            .Cast<PlateType>()
                            .Select(e => new Code { Key = typeof(PlateType).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(Sex))
                            .Cast<Sex>()
                            .Select(e => new Code { Key = typeof(Sex).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(TrafficStatus))
                            .Cast<TrafficStatus>()
                            .Select(e => new Code { Key = typeof(TrafficStatus).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(UpperColor))
                            .Cast<UpperColor>()
                            .Select(e => new Code { Key = typeof(UpperColor).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(VideoStructType))
                            .Cast<VideoStructType>()
                            .Select(e => new Code { Key = typeof(VideoStructType).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.Add(new Code
                        {
                            
                            Key = "FlowDateLevel",
                            Value = (int)DateTimeLevel.Minute,
                            Description = "一分钟"
                        });
                        flowCodes.Add(new Code
                        {
                            
                            Key = "FlowDateLevel",
                            Value = (int)DateTimeLevel.FiveMinutes,
                            Description = "五分钟"
                        });
                        flowCodes.Add(new Code
                        {
                            
                            Key = "FlowDateLevel",
                            Value = (int)DateTimeLevel.FifteenMinutes,
                            Description = "十五分钟"
                        });
                        flowCodes.Add(new Code
                        {
                            
                            Key = "FlowDateLevel",
                            Value = (int)DateTimeLevel.Hour,
                            Description = "小时"
                        });
                        flowCodes.Add(new Code
                        {
                            
                            Key = "FlowDateLevel",
                            Value = (int)DateTimeLevel.Day,
                            Description = "天"
                        });
                        flowCodes.Add(new Code
                        {
                            
                            Key = "FlowDateLevel",
                            Value = (int)DateTimeLevel.Month,
                            Description = "月"
                        });

                        flowCodes.Add(new Code
                        {
                            
                            Key = "CongestionDateLevel",
                            Value = (int)DateTimeLevel.Hour,
                            Description = "小时"
                        });
                        flowCodes.Add(new Code
                        {
                            
                            Key = "CongestionDateLevel",
                            Value = (int)DateTimeLevel.Day,
                            Description = "天"
                        });
                        flowCodes.Add(new Code
                        {
                            
                            Key = "CongestionDateLevel",
                            Value = (int)DateTimeLevel.Month,
                            Description = "月"
                        });

                        flowCodes.Add(new Code
                        {
                            
                            Key = "StatusTimeDateLevel",
                            Value = (int)DateTimeLevel.Hour,
                            Description = "小时"
                        });
                        flowCodes.Add(new Code
                        {
                            
                            Key = "StatusTimeDateLevel",
                            Value = (int)DateTimeLevel.Day,
                            Description = "天"
                        });
                        flowCodes.Add(new Code
                        {
                            
                            Key = "StatusTimeDateLevel",
                            Value = (int)DateTimeLevel.Month,
                            Description = "月"
                        });
                        context.Codes.AddRange(flowCodes);

                        #endregion

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
        private List<FlowDevice> InitCache()
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化缓存");

            IMemoryCache memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();
            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                CodesManager codesManager = serviceScope.ServiceProvider.GetRequiredService<CodesManager>();
                memoryCache.InitSystemCache(codesManager.GetList());
                DevicesManager devicesManager = serviceScope.ServiceProvider.GetRequiredService<DevicesManager>();
                List<FlowDevice> devices = devicesManager.GetList(null, 0, 0, null, null, null, 0, 0).Datas;
                memoryCache.InitDeviceCache(devices);
                RoadCrossingsManager roadCrossingsManager = serviceScope.ServiceProvider.GetRequiredService<RoadCrossingsManager>();
                memoryCache.InitCrossingCache(roadCrossingsManager.GetList(null, 0, 0).Datas);
                RoadSectionsManager roadSectionsManager = serviceScope.ServiceProvider.GetRequiredService<RoadSectionsManager>();
                memoryCache.InitSectionCache(roadSectionsManager.GetList(null, 0, 0, 0).Datas);
                return devices;
            }
        }

        /// <summary>
        /// 初始化缓存
        /// </summary>
        /// <param name="devices">设备集合</param>
        private void InitAdapter(List<FlowDevice> devices)
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化数据适配");

            DateTime minTime = TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel, DateTime.Now);
            DateTime maxTime = TimePointConvert.NextTimePoint(BranchDbConvert.DateLevel, minTime);
            FlowAdapter flowAdapter = _serviceProvider.GetRequiredService<FlowAdapter>();
            FlowBranchBlock flowBranchBlock = _serviceProvider.GetRequiredService<FlowBranchBlock>();
            VideoBranchBlock videoBranchBlock = _serviceProvider.GetRequiredService<VideoBranchBlock>();
            flowBranchBlock.Open(devices, minTime, maxTime);
            videoBranchBlock.Open(minTime, maxTime);
            flowAdapter.Start(devices, flowBranchBlock, videoBranchBlock);
        }

        /// <summary>
        /// 重置服务
        /// </summary>
        private void Reset()
        {
            List<FlowDevice> devices = InitCache();
            _logger.LogInformation((int)LogEvent.系统, "重置数据适配");
            FlowAdapter flowAdapter = _serviceProvider.GetRequiredService<FlowAdapter>();
            flowAdapter.Reset(devices);

            FlowBranchBlock flowBranchBlock = _serviceProvider.GetRequiredService<FlowBranchBlock>();
            flowBranchBlock.Reset(devices);
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        private void Stop()
        {
            FlowAdapter flowAdapter = _serviceProvider.GetRequiredService<FlowAdapter>();
            flowAdapter.Stop();
            FixedJobTask fixedJobTask = _serviceProvider.GetRequiredService<FixedJobTask>();
            fixedJobTask.Stop();
        }

        /// <summary>
        /// 初始化监控
        /// </summary>
        private void InitMonitor()
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化监控任务");
            FixedJobTask fixedJobTask = _serviceProvider.GetRequiredService<FixedJobTask>();
            StorageMonitor<LaneFlow, FlowDevice> flowStorageMonitor = _serviceProvider.GetRequiredService<StorageMonitor<LaneFlow, FlowDevice>>();
            flowStorageMonitor.SetBranchBlock(_serviceProvider.GetRequiredService<FlowBranchBlock>());
            StorageMonitor<VideoStruct, FlowDevice> videoStorageMonitor = _serviceProvider.GetRequiredService<StorageMonitor<VideoStruct, FlowDevice>>();
            videoStorageMonitor.SetBranchBlock(_serviceProvider.GetRequiredService<VideoBranchBlock>());
            FlowSwitchMonitor flowSwitchMonitor = _serviceProvider.GetRequiredService<FlowSwitchMonitor>();
            VideoSwitchMonitor videoSwitchMonitor = _serviceProvider.GetRequiredService<VideoSwitchMonitor>();
            SectionFlowMonitor sectionFlowMonitor = _serviceProvider.GetRequiredService<SectionFlowMonitor>();
            SystemStatusMonitor systemStatusMonitor = _serviceProvider.GetRequiredService<SystemStatusMonitor>();
            SystemStatusMonitor.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            DeviceStatusMonitor deviceStatusMonitor = _serviceProvider.GetRequiredService<DeviceStatusMonitor>();

            fixedJobTask.AddFixedJob(systemStatusMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "系统状态检查");
            fixedJobTask.AddFixedJob(sectionFlowMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "路段流量计算");
            fixedJobTask.AddFixedJob(flowSwitchMonitor, BranchDbConvert.DateLevel, _dbSpan, "流量数据分表切换");
            fixedJobTask.AddFixedJob(videoSwitchMonitor, BranchDbConvert.DateLevel, _dbSpan, "视频数据分表切换");
            fixedJobTask.AddFixedJob(deviceStatusMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "设备状态检查");
            fixedJobTask.AddFixedJob(flowStorageMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "流量数据定时保存");
            fixedJobTask.AddFixedJob(videoStorageMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "视频数据定时保存");
            fixedJobTask.Start();
        }

    }
}
