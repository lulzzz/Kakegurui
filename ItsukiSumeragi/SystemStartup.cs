using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using ItsukiSumeragi.Controller;
using ItsukiSumeragi.Data;
using ItsukiSumeragi.Models;
using ItsukiSumeragi.Monitor;
using Kakegurui.Core;
using Kakegurui.Log;
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
using Newtonsoft.Json.Serialization;
using YumekoJabami.Cache;
using YumekoJabami.Codes;
using ItsukiSumeragi.Codes.Device;
using ItsukiSumeragi.Codes.Flow;
using ItsukiSumeragi.Codes.Violation;
using YumekoJabami.Controllers;
using YumekoJabami.Data;
using YumekoJabami.Models;

namespace ItsukiSumeragi
{
    /// <summary>
    /// 用户管理中心系统
    /// </summary>
    public class SystemStartup
    {
        /// <summary>
        /// 配置参数
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// 实例工厂
        /// </summary>
        private IServiceProvider _serviceProvider;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<SystemStartup> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration">配置项</param>
        /// <param name="logger">日志</param>
        public SystemStartup(IConfiguration configuration,ILogger<SystemStartup> logger)
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
            string userDb = _configuration.GetValue<string>("UserDb");
            string deviceDb = _configuration.GetValue<string>("DeviceDb");
            string cacheIp = _configuration.GetValue<string>("CacheIp");
            int cachePort = _configuration.GetValue<int>("CachePort");

            _logger.LogInformation((int)LogEvent.配置项, $"DbIp {dbIp}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbPort {dbPort}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbUser {dbUser}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbPassword {dbPassword}");
            _logger.LogInformation((int)LogEvent.配置项, $"UserDb {userDb}");
            _logger.LogInformation((int)LogEvent.配置项, $"DeviceDb {deviceDb}");
            _logger.LogInformation((int)LogEvent.配置项, $"CacheIp {cacheIp}");
            _logger.LogInformation((int)LogEvent.配置项, $"CachePort {cachePort}");

            services.AddDbContext<SystemContext>(options =>
                options.UseMySQL(string.Format(BranchDbConvert.DbFormat,dbIp,dbPort,dbUser,dbPassword,userDb)));
            services.AddDbContextPool<DeviceContext>(options => 
                options.UseMySQL(string.Format(BranchDbConvert.DbFormat, dbIp, dbPort, dbUser, dbPassword, deviceDb)));

            services.AddIdentityCore<IdentityUser>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequiredLength = 4;
                    options.Password.RequiredUniqueChars = 0;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<SystemContext>()
                .AddSignInManager<SignInManager<IdentityUser>>()
                .AddDefaultTokenProviders();

            services
                .AddHttpClient()
                .ConfigureTrafficJWTToken()
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .ConfigureBadRequest()
                .AddJsonOptions(options => { options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver(); })
                .AddApplicationPart(Assembly.GetAssembly(typeof(DevicesController)))
                .AddApplicationPart(Assembly.GetAssembly(typeof(UsersController)))
                .AddApplicationPart(Assembly.GetAssembly(typeof(LogsController)));

        }

        public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime)
        {
            _serviceProvider = app.ApplicationServices;
            SystemSyncPublisher.SystemStatusChanged += InitCache;
            appLifetime.ApplicationStarted.Register(Start);

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
                            SystemSyncPublisher.Update();
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
                            SystemSyncPublisher.Update();
                        }
                    }
                }
            });
            app.UseTrafficException()
               .UseTrafficCors()
               .UseAuthentication()
               .UseMvc();
        }

        private async void Start()
        {
            await InitDb();
            InitCache(this,EventArgs.Empty);
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        private async Task InitDb()
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化数据库");

            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                using (SystemContext context =
                    serviceScope.ServiceProvider.GetRequiredService<SystemContext>())
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
                        List<TrafficClaim> claims = new List<TrafficClaim>
                            {
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02000000", Descirption = "智慧交通视频检测系统"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02010000", Descirption = "设备管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02010100", Descirption = "设备信息维护"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02010200", Descirption = "设备位置维护"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02010300", Descirption = "国标网关设置"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02010400", Descirption = "校时配置"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02010500", Descirption = "设备操作"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02010600", Descirption = "设备运行状态"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02010700", Descirption = "视频信息维护"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02010800", Descirption = "视频位置维护"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02020000", Descirption = "数据分析"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02020100", Descirption = "通行信息查询"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02020200", Descirption = "流量数据分析"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02020300", Descirption = "IO数据监测"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02020400", Descirption = "流量分布查询"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02020500", Descirption = "拥堵趋势分析"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02020600", Descirption = "状态时间统计"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02020700", Descirption = "交通状态分析"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02030000", Descirption = "系统设置"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02030100", Descirption = "路口维护"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02030200", Descirption = "路段维护"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02030300", Descirption = "用户管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02030400", Descirption = "角色管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02030600", Descirption = "字典管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02030700", Descirption = "参数管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02030800", Descirption = "日志查询"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02030900", Descirption = "系统监控"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02040000", Descirption = "状况监测"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "02040100", Descirption = "应用检测"},

                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03000000", Descirption = "智慧高点视频检测系统"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03010000", Descirption = "设备管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03010100", Descirption = "设备信息维护"},
                                //new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03010200", Descirption = "设备位置维护"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03010300", Descirption = "国标网关设置"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03010400", Descirption = "设备运行状态"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03010500", Descirption = "视频信息维护"},
                                //new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03010600", Descirption = "视频位置维护"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03020000", Descirption = "数据分析"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03020100", Descirption = "交通密度查询"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03020200", Descirption = "交通密度分析"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03020300", Descirption = "拥堵事件统计"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03020400", Descirption = "拥堵事件排名"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03020500", Descirption = "拥堵高发时段"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03030000", Descirption = "系统设置"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03030100", Descirption = "路口维护"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03030300", Descirption = "用户管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03030400", Descirption = "角色管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03030600", Descirption = "字典管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03030700", Descirption = "参数管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03030800", Descirption = "日志查询"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03030900", Descirption = "系统监控"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03040000", Descirption = "状况监测"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "03040100", Descirption = "应用检测"},

                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04000000", Descirption = "智慧交通违法检测系统"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04010000", Descirption = "设备管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04010100", Descirption = "设备信息维护"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04010200", Descirption = "设备位置维护"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04010300", Descirption = "视频信息维护"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04010400", Descirption = "视频位置维护"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04010500", Descirption = "违法参数设置"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04010600", Descirption = "授权管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04010700", Descirption = "校时配置"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04010800", Descirption = "设备操作"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04010900", Descirption = "设备运行状态"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04011000", Descirption = "标签设置"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04011100", Descirption = "违法行为维护"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04011200", Descirption = "数据上报"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04011300", Descirption = "系统升级"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04020000", Descirption = "数据分析"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04020100", Descirption = "违法行为分析"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04020200", Descirption = "车辆类型分析"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04020300", Descirption = "违法地点分析"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04020400", Descirption = "违法车辆分析"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04020500", Descirption = "违法综合分析"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04020600", Descirption = "违法三维统计表"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04020700", Descirption = "违法综合统计表"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04020800", Descirption = "违法记录查询"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04030000", Descirption = "系统设置"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04030100", Descirption = "地点维护"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04030200", Descirption = "用户管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04030300", Descirption = "角色管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04030400", Descirption = "字典管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04030500", Descirption = "参数管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04030600", Descirption = "日志查询"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04030700", Descirption = "系统监控"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04040000", Descirption = "态势监测"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "04040100", Descirption = "态势监测"},

                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05000000", Descirption = "智慧交通图片违法检测系统"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05010000", Descirption = "态势监测"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05010100", Descirption = "态势监测"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05020000", Descirption = "查询统计"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05020100", Descirption = "违法记录查询"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05020200", Descirption = "违法上传管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05020300", Descirption = "违法行为统计"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05020400", Descirption = "白名单违法记录"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05030000", Descirption = "系统设置"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05030100", Descirption = "违法监测设置"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05030200", Descirption = "接入点位管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05030300", Descirption = "用户管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05030400", Descirption = "角色管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05030500", Descirption = "字典管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05030600", Descirption = "参数管理"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05030700", Descirption = "日志查询"},
                                new TrafficClaim{ Type = ClaimTypes.Webpage, Value = "05030800", Descirption = "白名单管理"}
                            };
                        context.TrafficClaims.AddRange(claims);
                        foreach (TrafficClaim claim in claims)
                        {
                            await userManager.AddClaimAsync(adminUser, new Claim(claim.Type, claim.Value));
                        }
                        #endregion

                        #region 字典
                        _logger.LogInformation((int)LogEvent.系统, "创建字典");
                        List<TrafficCode> systemCodes = new List<TrafficCode>();

                        systemCodes.AddRange(Enum.GetValues(typeof(ChannelDirection))
                            .Cast<ChannelDirection>()
                            .Select(e => new TrafficCode { System = SystemType.系统管理中心, Key = typeof(ChannelDirection).Name, Value = (int)e, Description = e.ToString() }));

                        systemCodes.AddRange(Enum.GetValues(typeof(ChannelType))
                            .Cast<ChannelType>()
                            .Select(e => new TrafficCode { System = SystemType.系统管理中心, Key = typeof(ChannelType).Name, Value = (int)e, Description = e.ToString() }));

                        systemCodes.AddRange(Enum.GetValues(typeof(ChannelType))
                            .Cast<ChannelDeviceType>()
                            .Select(e => new TrafficCode { System = SystemType.系统管理中心, Key = typeof(ChannelDeviceType).Name, Value = (int)e, Description = e.ToString() }));

                        systemCodes.AddRange(Enum.GetValues(typeof(DeviceModel))
                            .Cast<DeviceModel>()
                            .Select(e => new TrafficCode { System = SystemType.系统管理中心, Key = typeof(DeviceModel).Name, Value = (int)e, Description = e.ToString() }));

                        systemCodes.AddRange(Enum.GetValues(typeof(DeviceStatus))
                            .Cast<DeviceStatus>()
                            .Select(e => new TrafficCode { System = SystemType.系统管理中心, Key = typeof(DeviceStatus).Name, Value = (int)e, Description = e.ToString() }));

                        systemCodes.AddRange(Enum.GetValues(typeof(RtspProtocol))
                            .Cast<RtspProtocol>()
                            .Select(e => new TrafficCode { System = SystemType.系统管理中心, Key = typeof(RtspProtocol).Name, Value = (int)e, Description = e.ToString() }));

                        systemCodes.AddRange(Enum.GetValues(typeof(SectionDirection))
                            .Cast<SectionDirection>()
                            .Select(e => new TrafficCode { System = SystemType.系统管理中心, Key = typeof(SectionDirection).Name, Value = (int)e, Description = e.ToString() }));

                        systemCodes.AddRange(Enum.GetValues(typeof(SectionType))
                            .Cast<SectionType>()
                            .Select(e => new TrafficCode { System = SystemType.系统管理中心, Key = typeof(SectionType).Name, Value = (int)e, Description = e.ToString() }));

                        systemCodes.AddRange(Enum.GetValues(typeof(LogEvent))
                            .Cast<LogEvent>()
                            .Select(e => new TrafficCode { System = SystemType.系统管理中心, Key = typeof(LogEvent).Name, Value = (int)e, Description = e.ToString() }));

                        systemCodes.Add(new TrafficCode
                        {
                            System = SystemType.系统管理中心,
                            Key = "LogLevel",
                            Value = (int)LogLevel.Debug,
                            Description = "调试"
                        });

                        systemCodes.Add(new TrafficCode
                        {
                            System = SystemType.系统管理中心,
                            Key = "LogLevel",
                            Value = (int)LogLevel.Information,
                            Description = "消息"
                        });

                        systemCodes.Add(new TrafficCode
                        {
                            System = SystemType.系统管理中心,
                            Key = "LogLevel",
                            Value = (int)LogLevel.Warning,
                            Description = "警告"
                        });

                        systemCodes.Add(new TrafficCode
                        {
                            System = SystemType.系统管理中心,
                            Key = "LogLevel",
                            Value = (int)LogLevel.Error,
                            Description = "错误"
                        });
                        context.Codes.AddRange(systemCodes);

                        List<TrafficCode> flowCodes = new List<TrafficCode>();

                        flowCodes.AddRange(Enum.GetValues(typeof(Age))
                            .Cast<Age>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通视频检测系统, Key = typeof(Age).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(NonVehicle))
                            .Cast<NonVehicle>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通视频检测系统, Key = typeof(NonVehicle).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(CarColor))
                            .Cast<CarColor>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通视频检测系统, Key = typeof(CarColor).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(CarType))
                            .Cast<CarType>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通视频检测系统, Key = typeof(CarType).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(FlowDirection))
                            .Cast<FlowDirection>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通视频检测系统, Key = typeof(FlowDirection).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "VehicleType",
                            Value = (int)FlowType.三轮车,
                            Description = FlowType.三轮车.ToString()
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "VehicleType",
                            Value = (int)FlowType.卡车,
                            Description = FlowType.卡车.ToString()
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "VehicleType",
                            Value = (int)FlowType.客车,
                            Description = FlowType.客车.ToString()
                        });

                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "VehicleType",
                            Value = (int)FlowType.轿车,
                            Description = FlowType.轿车.ToString()
                        });

                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "VehicleType",
                            Value = (int)FlowType.面包车,
                            Description = FlowType.面包车.ToString()
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "BikeType",
                            Value = (int)FlowType.自行车,
                            Description = FlowType.自行车.ToString()
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "BikeType",
                            Value = (int)FlowType.摩托车,
                            Description = FlowType.摩托车.ToString()
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "PedestrainType",
                            Value = (int)FlowType.行人,
                            Description = FlowType.行人.ToString()
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "FlowType",
                            Value = (int)FlowType.平均速度,
                            Description = FlowType.平均速度.ToString()
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "FlowType",
                            Value = (int)FlowType.车头时距,
                            Description = FlowType.车头时距.ToString()
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "FlowType",
                            Value = (int)FlowType.车头间距,
                            Description = FlowType.车头间距.ToString()
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "FlowType",
                            Value = (int)FlowType.时间占有率,
                            Description = FlowType.时间占有率.ToString()
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "FlowType",
                            Value = (int)FlowType.空间占有率,
                            Description = FlowType.空间占有率.ToString()
                        });

                        flowCodes.AddRange(Enum.GetValues(typeof(LaneDirection))
                            .Cast<LaneDirection>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通视频检测系统, Key = typeof(LaneDirection).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(LaneType))
                            .Cast<LaneType>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通视频检测系统, Key = typeof(LaneType).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(PlateType))
                            .Cast<PlateType>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通视频检测系统, Key = typeof(PlateType).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(Sex))
                            .Cast<Sex>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通视频检测系统, Key = typeof(Sex).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(TrafficStatus))
                            .Cast<TrafficStatus>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通视频检测系统, Key = typeof(TrafficStatus).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(UpperColor))
                            .Cast<UpperColor>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通视频检测系统, Key = typeof(UpperColor).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.AddRange(Enum.GetValues(typeof(VideoStructType))
                            .Cast<VideoStructType>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通视频检测系统, Key = typeof(VideoStructType).Name, Value = (int)e, Description = e.ToString() }));

                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "FlowDateLevel",
                            Value = (int)DateTimeLevel.Minute,
                            Description = "一分钟"
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "FlowDateLevel",
                            Value = (int)DateTimeLevel.FiveMinutes,
                            Description = "五分钟"
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "FlowDateLevel",
                            Value = (int)DateTimeLevel.FifteenMinutes,
                            Description = "十五分钟"
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "FlowDateLevel",
                            Value = (int)DateTimeLevel.Hour,
                            Description = "小时"
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "FlowDateLevel",
                            Value = (int)DateTimeLevel.Day,
                            Description = "天"
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "FlowDateLevel",
                            Value = (int)DateTimeLevel.Month,
                            Description = "月"
                        });

                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "CongestionDateLevel",
                            Value = (int)DateTimeLevel.Hour,
                            Description = "小时"
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "CongestionDateLevel",
                            Value = (int)DateTimeLevel.Day,
                            Description = "天"
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "CongestionDateLevel",
                            Value = (int)DateTimeLevel.Month,
                            Description = "月"
                        });

                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "StatusTimeDateLevel",
                            Value = (int)DateTimeLevel.Hour,
                            Description = "小时"
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "StatusTimeDateLevel",
                            Value = (int)DateTimeLevel.Day,
                            Description = "天"
                        });
                        flowCodes.Add(new TrafficCode
                        {
                            System = SystemType.智慧交通视频检测系统,
                            Key = "StatusTimeDateLevel",
                            Value = (int)DateTimeLevel.Month,
                            Description = "月"
                        });
                        context.Codes.AddRange(flowCodes);

                        List<TrafficCode> densityCodes = new List<TrafficCode>
                        {
                            new TrafficCode
                            {
                                System = SystemType.智慧高点视频检测系统,
                                Key = "DensityDateLevel",
                                Value = (int) DateTimeLevel.FiveMinutes,
                                Description = "五分钟密度"
                            },
                            new TrafficCode
                            {
                                System = SystemType.智慧高点视频检测系统,
                                Key = "DensityDateLevel",
                                Value = (int) DateTimeLevel.FifteenMinutes,
                                Description = "十五分钟密度"
                            },
                            new TrafficCode
                            {
                                System = SystemType.智慧高点视频检测系统,
                                Key = "DensityDateLevel",
                                Value = (int) DateTimeLevel.Hour,
                                Description = "一小时密度"
                            },
                            new TrafficCode
                            {
                                System = SystemType.智慧高点视频检测系统,
                                Key = "DensityDateLevel",
                                Value = (int) DateTimeLevel.Day,
                                Description = "一天密度"
                            },
                            new TrafficCode
                            {
                                System = SystemType.智慧高点视频检测系统,
                                Key = "DensityDateLevel",
                                Value = (int) DateTimeLevel.Month,
                                Description = "一月密度"
                            }
                        };
                        context.Codes.AddRange(densityCodes);

                        List<TrafficCode> violationCodes = new List<TrafficCode>()
                        {
                            new TrafficCode
                            {
                                System = SystemType.智慧交通违法检测系统,
                                Key = "ViolationDateLevel",
                                Value = (int) DateTimeLevel.Day,
                                Description = "天"
                            },
                            new TrafficCode
                            {
                                System = SystemType.智慧交通违法检测系统,
                                Key = "ViolationDateLevel",
                                Value = (int) DateTimeLevel.Month,
                                Description = "月"
                            },
                            new TrafficCode
                            {
                                System = SystemType.智慧交通违法检测系统,
                                Key = "ViolationDateLevel",
                                Value = (int) DateTimeLevel.Season,
                                Description = "季度"
                            },
                            new TrafficCode
                            {
                                System = SystemType.智慧交通违法检测系统,
                                Key = "ViolationDateLevel",
                                Value = (int) DateTimeLevel.Year,
                                Description = "年"
                            }
                        };
                        violationCodes.AddRange(Enum.GetValues(typeof(CarType))
                            .Cast<CarType>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通违法检测系统, Key = typeof(CarType).Name, Value = (int)e, Description = e.ToString() }));

                        violationCodes.AddRange(Enum.GetValues(typeof(TargetType))
                            .Cast<TargetType>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通违法检测系统, Key = typeof(TargetType).Name, Value = (int)e, Description = e.ToString() }));

                        violationCodes.AddRange(Enum.GetValues(typeof(TagType))
                            .Cast<TagType>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通违法检测系统, Key = typeof(TagType).Name, Value = (int)e, Description = e.ToString() }));

                        context.Codes.AddRange(violationCodes);

                        List<TrafficCode> violationCodes1 = new List<TrafficCode>();
                        violationCodes1.AddRange(Enum.GetValues(typeof(CarType))
                            .Cast<CarType>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通图片违法监测系统, Key = typeof(CarType).Name, Value = (int)e, Description = e.ToString() }));

                        violationCodes1.AddRange(Enum.GetValues(typeof(CarColor))
                            .Cast<CarColor>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通图片违法监测系统, Key = typeof(CarColor).Name, Value = (int)e, Description = e.ToString() }));

                        violationCodes1.AddRange(Enum.GetValues(typeof(PlateType))
                            .Cast<PlateType>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通图片违法监测系统, Key = typeof(PlateType).Name, Value = (int)e, Description = e.ToString() }));

                        violationCodes1.AddRange(Enum.GetValues(typeof(PlateColor))
                            .Cast<PlateColor>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通图片违法监测系统, Key = typeof(PlateColor).Name, Value = (int)e, Description = e.ToString() }));

                        violationCodes1.AddRange(Enum.GetValues(typeof(PlateMark))
                            .Cast<PlateMark>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通图片违法监测系统, Key = typeof(PlateMark).Name, Value = (int)e, Description = e.ToString() }));

                        violationCodes1.AddRange(Enum.GetValues(typeof(ChannelDirection))
                            .Cast<ChannelDirection>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通图片违法监测系统, Key = typeof(ChannelDirection).Name, Value = (int)e, Description = e.ToString() }));

                        violationCodes1.AddRange(Enum.GetValues(typeof(ProvinceAbb))
                            .Cast<ProvinceAbb>()
                            .Select(e => new TrafficCode { System = SystemType.智慧交通图片违法监测系统, Key = typeof(ProvinceAbb).Name, Value = (int)e, Description = e.ToString() }));
                        context.Codes.AddRange(violationCodes1);
                        #endregion

                        context.Version.Add(new TrafficVersion
                        {
                            Version = Assembly.GetAssembly(typeof(SystemContext)).GetName().Version.ToString()
                        });
                        context.SaveChanges();
                    }
                }

                using (DeviceContext context = serviceScope.ServiceProvider.GetRequiredService<DeviceContext>())
                {
                    if (context.Database.EnsureCreated())
                    {
                        #region 标签
                        _logger.LogInformation((int)LogEvent.系统, "创建违法标签");
                        context.Tags.AddRange(new List<TrafficTag>
                        {
                            new TrafficTag
                            {
                                TagName = "SL",
                                EnglishName = "straight lane",
                                ChineseName = "直行车道",
                                TagType = 1,
                                Color = "#3333CC",
                                Mark = "直行车道标注区域。在直行/直行加左转/直行加右转车道内绘制，区域边框颜色为蓝色。涉及多个直行车道时，标签名称加序号后缀，序号按照由内道到外道依次递增，如SL-1、SL-2等；涉及多功能车道时，标签之间使用“/”分割，如直行加左转SL/LL-1。\n标定规则：在直行车道内绘制多边形封闭区域，左右边线沿道路分界线绘制，与分界线保持平行，与车道等宽，前后长度从摄像机可视范围的最下沿到停止线。"
                            },
                            new TrafficTag
                            {
                                TagName = "LL",
                                EnglishName = "left-turn lane",
                                ChineseName = "左转车道",
                                TagType = 1,
                                Color = "#3333CC",
                                Mark = "左转车道标注区域。在左转车道内绘制，区域边框颜色为蓝色。涉及多个左转车道时，标签名称加序号后缀，序号按照由内道到外道依次递增，如LL-1、LL-2等；涉及多功能车道时，标签之间使用“/”分割，如左转加掉头LL/UL-1。\n标定规则：在左转车道内绘制多边形封闭区域，左右边线沿道路分界线绘制，与分界线保持平行，与车道等宽，前后长度从摄像机可视范围的最下沿到停止线。"
                            },
                            new TrafficTag
                            {
                                TagName = "UL",
                                EnglishName = "turn-around lane",
                                ChineseName = "掉头车道",
                                TagType = 1,
                                Color = "#3333CC",
                                Mark = "掉头车道标注区域。在掉头车道内绘制，区域边框颜色为蓝色。涉及多个掉头车道时，标签名称后缀加序号，序号按照由内道到外道依次递增，如UL-1、UL-2等；涉及多功能车道时，标签之间使用“/”分割，如掉头加左转UI/LL-1。\n标定规则： "
                            },
                            new TrafficTag
                            {
                                TagName = "RL",
                                EnglishName = "right-turn lane",
                                ChineseName = "右转车道",
                                TagType = 1,
                                Color = "#3333CC",
                                Mark = "右转车道标注区域。在右转车道内绘制，区域边框颜色为蓝色。涉及多个右转车道时，标签名称加序号后缀，序号按照由内道到外道依次递增，如RL-1、RL-2等；涉及多功能车道时，标签之间使用“/”分割，如右转加左转RL/LL-1。\n标定规则：在右转车道内绘制多边形封闭区域，左右边线沿道路分界线绘制，与分界线保持平行，与车道等宽，前后长度从摄像机可视范围的最下沿到停止线。"
                            },
                            new TrafficTag
                            {
                                TagName = "BSL",
                                EnglishName = "bus straight lane",
                                ChineseName = "公交车道",
                                TagType = 1,
                                Color = "#3333CC",
                                Mark = "公交车专用道标签。在公交车道内绘制，区域边框颜色为蓝色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "LWL",
                                EnglishName = "left waiting lane",
                                ChineseName = "左转待转车道",
                                TagType = 1,
                                Color = "#3333CC",
                                Mark = "左转待转车道标签。在左转待转车道内绘制，区域边框颜色为蓝色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "SWL",
                                EnglishName = "straight waiting lane",
                                ChineseName = "直行待行车道",
                                TagType = 1,
                                Color = "#3333CC",
                                Mark = "直行待行车道标签。在直行待行车道内绘制，区域边框颜色为蓝色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "TA",
                                EnglishName = "transition area",
                                ChineseName = "过渡区域",
                                TagType = 2,
                                Color = "#3CC48D",
                                Mark = "车辆进入路口后的公共区域，各个方向的车辆均会经过。区域边框颜色为绿色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "SA",
                                EnglishName = "straight area",
                                ChineseName = "直行区域",
                                TagType = 2,
                                Color = "#3CC48D",
                                Mark = "车辆直行通过路口后，在路口对面可能经过的区域。即直行方向路口对面的断面区域，通过该区域则表示直行行为。区域边框颜色为绿色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "LA",
                                EnglishName = "left area",
                                ChineseName = "左转区域",
                                TagType = 2,
                                Color = "#3CC48D",
                                Mark = "车辆左转通过路口后，在路口左侧可能经过的区域。即左转方向路口左侧的断面区域，通过该区域则表示左转行为。区域边框颜色为绿色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "UA",
                                EnglishName = "u-trun area",
                                ChineseName = "掉头区域",
                                TagType = 2,
                                Color = "#3CC48D",
                                Mark = "车辆在路口掉头可能经过的区域，通过该区域则表示掉头行为。区域边框颜色为绿色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "RA",
                                EnglishName = "right area",
                                ChineseName = "右转区域",
                                TagType = 2,
                                Color = "#3CC48D",
                                Mark = "车辆右转通过路口后，在路口右侧可能经过的区域。即右转方向路口右侧的断面区域，通过该区域则表示右转行为。区域边框颜色为绿色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "DA",
                                EnglishName = "direct area",
                                ChineseName = "抓拍区域",
                                TagType = 2,
                                Color = "#3CC48D",
                                Mark = "通用型标签。泛指车辆检测区域，只要车辆进入该区域即进行检测抓拍。区域边框颜色为绿色。\n标定规则：根据检测需求绘制多边形检测区域，区域大小和外形不固定。"
                            },
                            new TrafficTag
                            {
                                TagName = "AA",
                                EnglishName = "ahead area",
                                ChineseName = "前行区域",
                                TagType = 2,
                                Color = "#3CC48D",
                                Mark = "车辆先行通过的区域，与BA配对使用。针对逆行、倒车等违法行为定义。区域边框颜色为绿色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "BA",
                                EnglishName = "Back Area",
                                ChineseName = "后行区域",
                                TagType = 2,
                                Color = "#3CC48D",
                                Mark = "车辆后行通过的区域，与AA配对使用。针对逆行、倒车等违法行为定义。区域边框颜色为绿色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "ZCA",
                                EnglishName = "zebra crosssing area",
                                ChineseName = "斑马线交叉区域",
                                TagType = 2,
                                Color = "#3CC48D",
                                Mark = "与车道线（SL、RL、LL）检测区域相对应，标注在斑马线上，与车道线检测区域同宽，相当于车道的延伸。区域边框颜色为绿色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "PCA",
                                EnglishName = "pedestrian conflict area",
                                ChineseName = "行人冲突区",
                                TagType = 2,
                                Color = "#3CC48D",
                                Mark = "与车道线（SL、RL、LL）检测区域相对应, 标注在斑马线上，即机动车礼让行人的冲突检测区，区域宽度不定，视地方法规来调整。区域边框颜色为绿色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "UFA",
                                EnglishName = "u-turn forbidden area A",
                                ChineseName = "禁止掉头区域A",
                                TagType = 2,
                                Color = "#3CC48D",
                                Mark = "非独立标签,与UFB一起使用，标示车辆掉头前通过的区域。区域边框颜色为绿色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "UFB",
                                EnglishName = "u-turn forbidden area B",
                                ChineseName = "禁止掉头区域B",
                                TagType = 2,
                                Color = "#3CC48D",
                                Mark = "非独立标签，与UFA一起使用，标示车辆掉头后通过的区域。区域边框颜色为绿色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "YFPA",
                                EnglishName = "yellow forbidden parking area",
                                ChineseName = "禁止停车区域",
                                TagType = 2,
                                Color = "#3CC48D",
                                Mark = "禁止停车检测区域，根据禁停区域大小，标出禁止停车区域。区域边框颜色为绿色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "WSL",
                                EnglishName = "white solid line",
                                ChineseName = "白实线",
                                TagType = 3,
                                Color = "#F7F709",
                                Mark = "白实线检测区域标签。区域边框颜色为橙色。\n标定规则：沿白实线边缘勾勒出白实线检测区域。注意检测区域框要与白色实线外沿尽可能吻合。"
                            },
                            new TrafficTag
                            {
                                TagName = "YSL",
                                EnglishName = "yellow solid line",
                                ChineseName = "黄实线",
                                TagType = 3,
                                Color = "#F7F709",
                                Mark = "黄实线检测区域标签。区域边框颜色为橙色。\n标定规则：沿黄实线边缘勾勒出黄实线检测区域。注意检测区域框要与黄色实线外沿尽可能吻合。"
                            },
                            new TrafficTag
                            {
                                TagName = "SSL",
                                EnglishName = "straight stop line",
                                ChineseName = "停车线",
                                TagType = 3,
                                Color = "#F7F709",
                                Mark = "停车线检测区域标签。区域边框颜色为橙色。\n标定规则：沿停车线边缘勾勒出停车线检测区域。注意检测区域框要与停车线外沿尽可能吻合。"
                            },
                            new TrafficTag
                            {
                                TagName = "RDR",
                                EnglishName = "red detect",
                                ChineseName = "红灯检测大框",
                                TagType = 4,
                                Color = "#FF0000",
                                Mark = "一组红绿灯的检测框。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "LRL",
                                EnglishName = "left red light",
                                ChineseName = "左转红灯",
                                TagType = 4,
                                Color = "#FF0000",
                                Mark = "左转红灯检测区域标签，区域边框颜色为红色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "LYL",
                                EnglishName = "left yellow light",
                                ChineseName = "左转黄灯",
                                TagType = 4,
                                Color = "#FF0000",
                                Mark = "左转黄灯检测区域标签，区域边框颜色为红色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "LGL",
                                EnglishName = "left green light",
                                ChineseName = "左转绿灯",
                                TagType = 4,
                                Color = "#FF0000",
                                Mark = "左转绿灯检测区域标签，区域边框颜色为红色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "SRL",
                                EnglishName = "straight red light",
                                ChineseName = "直行红灯",
                                TagType = 4,
                                Color = "#FF0000",
                                Mark = "直行红灯检测区域标签，区域边框颜色为红色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "SYL",
                                EnglishName = "straight yellow light",
                                ChineseName = "直行黄灯",
                                TagType = 4,
                                Color = "#FF0000",
                                Mark = "直行黄灯检测区域标签，区域边框颜色为红色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "SGL",
                                EnglishName = "straight green light",
                                ChineseName = "直行绿灯",
                                TagType = 4,
                                Color = "#FF0000",
                                Mark = "直行绿灯检测区域标签，区域边框颜色为红色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "RRL",
                                EnglishName = "right red light",
                                ChineseName = "右转红灯",
                                TagType = 4,
                                Color = "#FF0000",
                                Mark = "右转红灯检测区域标签，区域边框颜色为红色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "RYL",
                                EnglishName = "right yellow light",
                                ChineseName = "右转黄灯",
                                TagType = 4,
                                Color = "#FF0000",
                                Mark = "右转黄灯检测区域标签，区域边框颜色为红色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "RGL",
                                EnglishName = "right green light",
                                ChineseName = "右转绿灯",
                                TagType = 4,
                                Color = "#FF0000",
                                Mark = "右转绿灯检测区域标签，区域边框颜色为红色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "URL",
                                EnglishName = "u-turn red light",
                                ChineseName = "掉头红灯",
                                TagType = 4,
                                Color = "#FF0000",
                                Mark = "掉头红灯检测区域标签，区域边框颜色为红色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "UYL",
                                EnglishName = "u-turn yellow light",
                                ChineseName = "掉头黄灯",
                                TagType = 4,
                                Color = "#FF0000",
                                Mark = "掉头黄灯检测区域标签，区域边框颜色为红色。\n标定规则："
                            },
                            new TrafficTag
                            {
                                TagName = "UGL",
                                EnglishName = "u-turn green light",
                                ChineseName = "掉头绿灯",
                                TagType = 4,
                                Color = "#FF0000",
                                Mark = "掉头绿灯检测区域标签，区域边框颜色为红色。\n标定规则："
                            }
                        });
                        #endregion

                        #region 违法参数
                        _logger.LogInformation((int)LogEvent.系统, "创建违法参数");
                        context.ViolationParameters.AddRange(new List<TrafficViolationParameter>
                        {
                            new TrafficViolationParameter
                            {
                                Key = "osd_location",
                                ParameterType = ViolationParameterType.Osd,
                                ValueType =ViolationValueType.None,
                                Description = "抓拍地点"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "osd_locationCode",
                                ParameterType = ViolationParameterType.Osd,
                                ValueType =ViolationValueType.None,
                                Description = "抓拍地点代码"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "osd_deviceId",
                                ParameterType = ViolationParameterType.Osd,
                                ValueType =ViolationValueType.None,
                                Description = "设备编号"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "osd_plateNum",
                                ParameterType = ViolationParameterType.Osd,
                                ValueType =ViolationValueType.None,
                                Description = "车牌号码"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "osd_plateType",
                                ParameterType = ViolationParameterType.Osd,
                                ValueType =ViolationValueType.None,
                                Description = "号牌种类"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "osd_time",
                                ParameterType = ViolationParameterType.Osd,
                                ValueType =ViolationValueType.None,
                                Description = "抓拍时间"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "osd_securityCode",
                                ParameterType = ViolationParameterType.Osd,
                                ValueType =ViolationValueType.None,
                                Description = "防伪码"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "osd_custom1",
                                ParameterType = ViolationParameterType.Osd,
                                ValueType =ViolationValueType.None,
                                Description = "自定义1"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "osd_custom2",
                                ParameterType = ViolationParameterType.Osd,
                                ValueType =ViolationValueType.None,
                                Description = "自定义2"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "osd_custom3",
                                ParameterType = ViolationParameterType.Osd,
                                ValueType =ViolationValueType.None,
                                Description = "自定义3"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "osd_custom4",
                                ParameterType = ViolationParameterType.Osd,
                                ValueType =ViolationValueType.None,
                                Description = "自定义4"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "osd_custom5",
                                ParameterType = ViolationParameterType.Osd,
                                ValueType =ViolationValueType.None,
                                Description = "自定义5"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "osd_custom6",
                                ParameterType = ViolationParameterType.Osd,
                                ValueType =ViolationValueType.None,
                                Description = "自定义6"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "file_location",
                                ParameterType = ViolationParameterType.File,
                                ValueType =ViolationValueType.None,
                                Description = "抓拍地点"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "file_locationCode",
                                ParameterType = ViolationParameterType.File,
                                ValueType =ViolationValueType.None,
                                Description = "抓拍地点代码"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "file_deviceId",
                                ParameterType = ViolationParameterType.File,
                                ValueType =ViolationValueType.None,
                                Description = "设备编号"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "file_securityCode",
                                ParameterType = ViolationParameterType.File,
                                ValueType =ViolationValueType.None,
                                Description = "防伪码"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "file_plateNum",
                                ParameterType = ViolationParameterType.File,
                                ValueType =ViolationValueType.None,
                                Description = "车牌号码"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "file_custom1",
                                ParameterType = ViolationParameterType.File,
                                ValueType =ViolationValueType.None,
                                Description = "自定义1"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "file_custom2",
                                ParameterType = ViolationParameterType.File,
                                ValueType =ViolationValueType.None,
                                Description = "自定义2"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "font_postion",
                                ParameterType = ViolationParameterType.Font,
                                ValueType =ViolationValueType.Enum,
                                Description = "叠加方式",
                                Keys = "0,1,2",
                                Values = "纵向叠加-左上,横向叠加-顶部,横向叠加-底部"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "font_size",
                                ParameterType = ViolationParameterType.Font,
                                ValueType =ViolationValueType.Integer,
                                Description = "字体大小"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "font_point",
                                ParameterType = ViolationParameterType.Font,
                                ValueType =ViolationValueType.Integer,
                                Description = "距水平边缘坐标"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "font_vertical",
                                ParameterType = ViolationParameterType.Font,
                                ValueType =ViolationValueType.Integer,
                                Description = "距垂直边缘坐标"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "font_height",
                                ParameterType = ViolationParameterType.Font,
                                ValueType =ViolationValueType.Integer,
                                Description = "黑边高度"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "font_color",
                                ParameterType = ViolationParameterType.Font,
                                ValueType =ViolationValueType.Color,
                                Description = "字体颜色"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "name_separator",
                                ParameterType = ViolationParameterType.FileName,
                                ValueType =ViolationValueType.Enum,
                                Description = "连接符",
                                Keys = "-,_",
                                Values = "-,_"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "recog_car_min_width",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 1080,
                                Description = "过滤车辆宽度",
                                Unit = "像素"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "output_debug",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Bool,
                                Description = "输出调试数据"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "max_alive_time",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 600,
                                Description = "目标生存最大时长",
                                Unit="秒"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "edge_thresh",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 100,
                                Description = "目标边界阈值"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "lamp_iou_thresh",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Float,
                                MinValue = 0,
                                MaxValue = 1,
                                Description = "小灯IOU阈值"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "ignore_light",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Bool,
                                Description = "忽略信号灯"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "thresh_in_lane",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Float,
                                MinValue = 0,
                                MaxValue = 1,
                                Description = "车道最小IOU值"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "thresh_in_area",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Float,
                                MinValue = 0,
                                MaxValue = 1,
                                Description = "区域最小IOU值"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "ignore_direction",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Bool,
                                Description = "忽略行驶方向"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "min_snapshot_interval",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 25,
                                Description = "抓拍间隔帧数",
                                Unit = "帧"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "acceptable_seg_ratio",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Float,
                                MinValue = 0,
                                MaxValue = 1,
                                Description = "车身压线比例"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "enter_area_thresh",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Float,
                                MinValue = 0,
                                MaxValue = 1,
                                Description = "驶入区域IOU阈值"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "exit_area_thresh",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Float,
                                MinValue = 0,
                                MaxValue = 1,
                                Description = "离开区域IOU阈值"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "in_bsl_count",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 100,
                                Description = "检测违章帧数"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "move_distance_ratio",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 10,
                                Description = "违章移动距离",
                                Unit = "像素"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "inner_area_thresh",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Float,
                                MinValue = 0,
                                MaxValue = 1,
                                Description = "停靠非法停车区"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "min_snapshot_hits",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 1,
                                MaxValue = 10000,
                                Description = "最少停靠帧数",
                                Unit = "帧"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "static_snapshot_hit",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 1,
                                MaxValue = 10000,
                                Description = "后续停靠帧数",
                                Unit = "帧"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "move_thresh",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 100,
                                Description = "移动最小距离"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "min_parking_interval",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 900,
                                Description = "停车最小间隔"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "parking_movement_thresh",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 100,
                                Description = "越线移动距离"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "apture_frame_interva",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 100,
                                Description = "缓存间隔帧数",
                                Unit = "帧"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "last_two_frame_interval",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 100,
                                Description = "间隔最大时间",
                                Unit = "秒"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "recog_score_thresh",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 100,
                                Description = "违章检测分数"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "filter_edge_thresh",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 100,
                                Description = "目标边界阈值"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "filter_plate_color",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 255,
                                Description = "过滤车牌颜色"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "pede_move_thresh",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 200,
                                Description = "行人移动距离"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "enter_zebra_thresh",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Float,
                                MinValue = 0,
                                MaxValue = 1,
                                Description = "驶入IOU车身面积"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "exit_zebra_thresh",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Float,
                                MinValue = 0,
                                MaxValue = 1,
                                Description = "离开IOU车身面积"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "pede_lost_num_thresh",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 5,
                                Description = "行人检丢次数"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "right_lane_enable",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Bool,
                                Description = "开启红绿灯检测"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "recent_disp_num",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 25,
                                Description = "最近行人位移数"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "need_recog_orient_thresh",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 100,
                                Description = "行驶方向识别阈值"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "min_total_iou",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Float,
                                MinValue = 0,
                                MaxValue = 1,
                                Description = "车道最小IOU值"
                            },
                            new TrafficViolationParameter
                            {
                                Key = "capture_frame_interva",
                                ParameterType = ViolationParameterType.Violation,
                                ValueType =ViolationValueType.Integer,
                                MinValue = 0,
                                MaxValue = 100,
                                Description = "缓存间隔帧数"
                            }
                        });

                        #endregion

                        #region 违法行为
                        _logger.LogInformation((int)LogEvent.系统, "创建违法行为");
                        context.Violations.AddRange(new List<TrafficViolation>()
                        {
                            new TrafficViolation
                            {
                                ViolationId = 1001,
                                ViolationName = "闯红灯",
                                GbCode = "16250",
                                GbName = "违法信号灯规定",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SWL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LWL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "UL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "RL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName= "SA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "RA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "UA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LRL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SRL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "RRL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "URL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SSL"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "edge_thresh",
                                        Value = "10"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "lamp_iou_thresh",
                                        Value = "0.4"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "ignore_light",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 1003,
                                ViolationName = "非机动车闯红灯",
                                GbCode = "2007",
                                GbName = "非机动车不按照交通信号规定通行的",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SWL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LWL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "UL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "RL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName= "SA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "RA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "UA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LRL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SRL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "RRL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "URL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SSL"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "edge_thresh",
                                        Value = "10"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "lamp_iou_thresh",
                                        Value = "0.4"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "ignore_light",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 2001,
                                ViolationName = "不按导向行驶",
                                GbCode = "12080",
                                GbName = "不按行车方向驶入导向车道",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "ZCA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "PCA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "UL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "RL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LWL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SWL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "TA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "UA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "RA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SSL"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "thresh_in_lane",
                                        Value = "0.5"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "thresh_in_area",
                                        Value = "0.5"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "ignore_direction",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 2004,
                                ViolationName = "压白线/违章变道",
                                GbCode = "13450",
                                GbName = "机动车违反禁止标线指示的",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "UL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "RL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "WSL"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "min_snapshot_interval",
                                        Value = "5"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "min_total_iou",
                                        Value = "0.8"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "acceptable_seg_ratio",
                                        Value = "0.3"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 2005,
                                ViolationName = "压黄线",
                                GbCode = "13450",
                                GbName = "机动车违反禁止标线指示的",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "UL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "RL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "YSL"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "min_snapshot_interval",
                                        Value = "5"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "min_total_iou",
                                        Value = "0.8"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "acceptable_seg_ratio",
                                        Value = "0.3"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 2006,
                                ViolationName = "占用公交车道",
                                GbCode = "10190",
                                GbName = "占用公交车道",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "BSL"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "enter_area_thresh",
                                        Value = "0.3"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "exit_area_thresh",
                                        Value = "0.1"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "in_bsl_count",
                                        Value = "10"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "move_distance_ratio",
                                        Value = "3"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 2008,
                                ViolationName = "非法停车",
                                GbCode = "13440",
                                GbName = "违反禁令标志表示",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "YFPA"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "enter_area_thresh",
                                        Value = "0.3"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "inner_area_thresh",
                                        Value = "0.2"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "min_snapshot_hits",
                                        Value = "100"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "static_snapshot_hit",
                                        Value = "250"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 2013,
                                ViolationName = "机动车逆行",
                                GbCode = "13010",
                                GbName = "逆向行驶",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "BA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "AA"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "enter_area_thresh",
                                        Value = "0.5"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "exit_area_thresh",
                                        Value = "0.5"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "move_thresh",
                                        Value = "50"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 2014,
                                ViolationName = "越线停车",
                                GbCode = "12110",
                                GbName = "通过路口遇停止信号时，停在停止线以内或路口内的",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SWL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LWL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "UL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "RL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LRL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SRL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "RRL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "URL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SSL"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "lamp_iou_thresh",
                                        Value = "0.4"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "ignore_light",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "min_parking_interval",
                                        Value = "100"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "parking_movement_thresh",
                                        Value = "10"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "capture_frame_interva",
                                        Value = "25"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 2015,
                                ViolationName = "非机动车逆行",
                                GbCode = "2004",
                                GbName = "非机动车逆向行驶的",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "BA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "AA"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "enter_area_thresh",
                                        Value = "0.5"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "exit_area_thresh",
                                        Value = "0.5"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "move_thresh",
                                        Value = "50"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "edge_thresh",
                                        Value = "10"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 2016,
                                ViolationName = "非法掉头",
                                GbCode = "1044",
                                GbName = "在禁止掉头或者禁止左转弯标志、标线的地点掉头的",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "UFA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "UFB"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "enter_area_thresh",
                                        Value = "0.3"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "exit_area_thresh",
                                        Value = "0.1"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "last_two_frame_interval",
                                        Value = "10"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 2017,
                                ViolationName = "非机动车占机动车道",
                                GbCode = "",
                                GbName = "",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "RL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "UL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SL"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "recog_score_thresh",
                                        Value = "75"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "filter_edge_thresh",
                                        Value = "10"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "recog_car_min_width",
                                        Value = "100"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "filter_plate_color",
                                        Value = "2"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 3001,
                                ViolationName = "接打电话",
                                GbCode = "12250",
                                GbName = "驾车时有其它安全行为",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "DA"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "recog_score_thresh",
                                        Value = "75"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "filter_edge_thresh",
                                        Value = "10"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "recog_car_min_width",
                                        Value = "100"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 3002,
                                ViolationName = "未系安全带",
                                GbCode = "60110",
                                GbName = "驾车时有其它安全行为",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "DA"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "recog_score_thresh",
                                        Value = "75"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "filter_edge_thresh",
                                        Value = "10"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "recog_car_min_width",
                                        Value = "100"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 3005,
                                ViolationName = "不礼让行人",
                                GbCode = "13570",
                                GbName = "遇行人正在通过人行横道时未停车让行",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "ZCA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "PCA"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "LL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "UL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "RL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SL"
                                    },
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "SSL"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "edge_thresh",
                                        Value = "5"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "pede_move_thresh",
                                        Value = "20"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "enter_zebra_thresh",
                                        Value = "0.4"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "exit_zebra_thresh",
                                        Value = "0.3"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "pede_lost_num_thresh",
                                        Value = "2"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "right_lane_enable",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "recent_disp_num",
                                        Value = "7"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "need_recog_orient_thresh",
                                        Value = "70"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 6001,
                                ViolationName = "大货车禁行",
                                GbCode = "13440",
                                GbName = "违反禁令标志指示",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "DA"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "recog_score_thresh",
                                        Value = "75"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "filter_edge_thresh",
                                        Value = "10"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "recog_car_min_width",
                                        Value = "100"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 6002,
                                ViolationName = "危化品车检验",
                                GbCode = "13440",
                                GbName = "违反禁令标志指示",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "DA"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "recog_score_thresh",
                                        Value = "75"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "filter_edge_thresh",
                                        Value = "10"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "recog_car_min_width",
                                        Value = "100"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            },
                            new TrafficViolation
                            {
                                ViolationId = 6003,
                                ViolationName = "渣土车",
                                GbCode = "13440",
                                GbName = "违反禁令标志指示",
                                Violation_Tags = new List<TrafficViolation_TrafficTag>
                                {
                                    new TrafficViolation_TrafficTag
                                    {
                                        TagName = "DA"
                                    }
                                },
                                Violation_Parameters = new List<TrafficViolation_TrafficViolationParameter>
                                {
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "recog_score_thresh",
                                        Value = "75"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "filter_edge_thresh",
                                        Value = "10"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "recog_car_min_width",
                                        Value = "100"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "output_debug",
                                        Value = "0"
                                    },
                                    new TrafficViolation_TrafficViolationParameter
                                    {
                                        Key = "max_alive_time",
                                        Value = "300"
                                    }
                                }
                            }
                        });
                        #endregion

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
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InitCache(object sender,EventArgs e)
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化缓存");

            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                using (SystemContext context = serviceScope.ServiceProvider.GetRequiredService<SystemContext>())
                {
                    IMemoryCache memoryCache = serviceScope.ServiceProvider.GetRequiredService<IMemoryCache>();
                    memoryCache.InitSystemCache(context.Codes.ToList());
                }
            }
        }
    }
}
