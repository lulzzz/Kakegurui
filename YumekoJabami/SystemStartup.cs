using System;
using System.Reflection;
using Kakegurui.Core;
using Kakegurui.Log;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using YumekoJabami.Controllers;
using YumekoJabami.Data;

namespace YumekoJabami
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
            string dbName = _configuration.GetValue<string>("DbName");

            _logger.LogInformation((int)LogEvent.配置项, $"DbIp {dbIp}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbPort {dbPort}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbUser {dbUser}");
            _logger.LogInformation((int)LogEvent.配置项, $"DbPassword {dbPassword}");
            _logger.LogInformation((int)LogEvent.配置项, $"UserDb {dbName}");

            services.AddDbContext<SystemContext>(options =>
                options.UseMySQL(string.Format(BranchDbConvert.DbFormat,dbIp,dbPort,dbUser,dbPassword, dbName)));

            services.ConfigureIdentity<SystemContext>();

            services
                .AddHttpClient()
                .ConfigureTrafficJWTToken()
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .ConfigureBadRequest()
                .AddJsonOptions(options => { options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver(); })
                .AddApplicationPart(Assembly.GetAssembly(typeof(UsersController)));

        }

        public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime)
        {
            _serviceProvider = app.ApplicationServices;
            appLifetime.ApplicationStarted.Register(InitDb);

            app.UseTrafficException()
                .UseTrafficCors()
                .UseAuthentication()
                .UseMvc();
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        private void InitDb()
        {
            _logger.LogInformation((int)LogEvent.系统, "初始化数据库");

            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                using (SystemContext context =
                    serviceScope.ServiceProvider.GetRequiredService<SystemContext>())
                {
                    context.Database.EnsureCreated();
                }

            }
        }
    }
}
