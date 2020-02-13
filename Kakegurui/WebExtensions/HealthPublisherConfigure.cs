using System;
using Kakegurui.Monitor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Kakegurui.WebExtensions
{
    /// <summary>
    /// 健康检查配置
    /// </summary>
    public static class HealthPublisherConfigure
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection ConfigureTrafficHelthPublisher(this IServiceCollection services)
        {
            services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.FromSeconds(10);
                options.Period = TimeSpan.FromMinutes(1);
            });
            services.AddSingleton<IHealthCheckPublisher, HealthLogPublish>();
            return services;
        }
    }
}
