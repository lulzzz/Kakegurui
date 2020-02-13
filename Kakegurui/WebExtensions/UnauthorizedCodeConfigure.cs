using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Kakegurui.WebExtensions
{
    /// <summary>
    /// 配置cookie验证的失败代码
    /// </summary>
    public static class UnauthorizedCodeConfigure
    {
        public static IServiceCollection ConfigureUnauthorizedCode(this IServiceCollection services)
        {
            services.ConfigureApplicationCookie(options =>
            {
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return Task.CompletedTask;
                };
            });
            return services;
        }
    }
}
