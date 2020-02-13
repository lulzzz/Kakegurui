using Microsoft.AspNetCore.Builder;

namespace Kakegurui.WebExtensions
{
    /// <summary>
    /// 跨域配置
    /// </summary>
    public static class CorsMiddleware
    {
        public static IApplicationBuilder UseTrafficCors(
            this IApplicationBuilder builder)
        {
            //跨域
            builder.UseCors(b =>
                b.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            return builder;
        }
    }
}
