using ItsukiSumeragi.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MomobamiKirari.Adapter;
using MomobamiKirari.Controllers;
using MomobamiKirari.Data;
using MomobamiKirari.DataFlow;
using MomobamiKirari.Managers;
using MomobamiRirika.Adapter;
using MomobamiRirika.Data;
using MomobamiRirika.DataFlow;
using NishinotouinYuriko.Data;

namespace IntegrationTest
{
    /// <summary>
    /// 流量系统
    /// </summary>
    public class MySqlStartup
    {
        /// <summary>
        /// 数据库连接字符串格式
        /// </summary>
        private const string DbFormat = "server={0};port={1};user={2};password={3};database={4};CharSet=utf8";

        private readonly IConfiguration _configuration;

        public MySqlStartup(IConfiguration configuration) 
        {
            _configuration = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            string dbIp = _configuration.GetValue<string>("DbIp");
            int dbPort = _configuration.GetValue<int>("DbPort");
            string dbUser = _configuration.GetValue<string>("DbUser");
            string dbPassword = _configuration.GetValue<string>("DbPassword");
            string deviceDb = _configuration.GetValue<string>("DeviceDb");
            string flowDb = _configuration.GetValue<string>("FlowDb");
            string videoDb = _configuration.GetValue<string>("VideoDb");
            string densityDb = _configuration.GetValue<string>("DensityDb");
            string eventDb = _configuration.GetValue<string>("EventDb");
            string violationDb = _configuration.GetValue<string>("ViolationDb");

            services.AddDbContextPool<DeviceContext>(options => options.UseMySQL(string.Format(DbFormat, dbIp, dbPort, dbUser, dbPassword, deviceDb)));
            services.AddDbContextPool<FlowContext>(options => options.UseMySQL(string.Format(DbFormat, dbIp, dbPort, dbUser, dbPassword, flowDb)));
            services.AddDbContextPool<FlowContext>(options => options.UseMySQL(string.Format(DbFormat, dbIp, dbPort, dbUser, dbPassword, videoDb)));
            services.AddDbContextPool<DensityContext>(options => options.UseMySQL(string.Format(DbFormat, dbIp, dbPort, dbUser, dbPassword, densityDb)));
            services.AddDbContextPool<DensityContext>(options => options.UseMySQL(string.Format(DbFormat, dbIp, dbPort, dbUser, dbPassword, eventDb)));
            services.AddDbContextPool<ViolationContext>(options => options.UseMySQL(string.Format(DbFormat, dbIp, dbPort, dbUser, dbPassword, violationDb)));

            services.AddScoped(typeof(LaneFlowManager_Alone), typeof(LaneFlowManager_Alone));
            services.AddScoped(typeof(VideoStructsController), typeof(VideoStructsController));

            services.AddSingleton(typeof(FlowAdapter), typeof(FlowAdapter));
            services.AddSingleton(typeof(FlowBranchBlock), typeof(FlowBranchBlock));
            services.AddSingleton(typeof(VideoBranchBlock), typeof(VideoBranchBlock));
            services.AddSingleton(typeof(DensityAdapter), typeof(DensityAdapter));
            services.AddSingleton(typeof(DensityBranchBlock), typeof(DensityBranchBlock));
            services.AddSingleton(typeof(EventBranchBlock), typeof(EventBranchBlock));

            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }
}
