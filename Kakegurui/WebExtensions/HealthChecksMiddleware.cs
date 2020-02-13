using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kakegurui.WebExtensions
{
    /// <summary>
    /// 系统状态检查
    /// </summary>
    public static class HealthChecksMiddleware
    {
        public static IApplicationBuilder UseTrafficHealthChecks(
            this IApplicationBuilder builder)
        {
            builder.UseHealthChecks("/api/monitor", new HealthCheckOptions
            {
                AllowCachingResponses = false,
                ResponseWriter = WriteResponse
            });
            return builder;
        }

        /// <summary>
        /// 根据系统状态返回json结构
        /// </summary>
        /// <param name="httpContext">http上下文</param>
        /// <param name="result">系统状态</param>
        /// <returns></returns>
        private static Task WriteResponse(HttpContext httpContext, HealthReport result)
        {
            httpContext.Response.ContentType = "application/json";

            var json = new JObject(
                new JProperty("status", result.Status.ToString()),
                new JProperty("results", new JObject(result.Entries.Select(pair =>
                    new JProperty(pair.Key, new JObject(
                        new JProperty("status", pair.Value.Status.ToString()),
                        new JProperty("description", pair.Value.Description),
                        new JProperty("data", new JObject(pair.Value.Data.Select(
                            p => new JProperty(p.Key, p.Value))))))))));
            return httpContext.Response.WriteAsync(
                json.ToString(Formatting.Indented));
        }
    }
}
