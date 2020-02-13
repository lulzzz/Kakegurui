using System.Threading.Tasks;
using Kakegurui.Log;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Kakegurui.WebExtensions
{
    /// <summary>
    /// 异常处理中间件
    /// </summary>
    public static class ExceptionMiddleware
    {
        public static IApplicationBuilder UseTrafficException(
            this IApplicationBuilder builder)
        {
            builder.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(InvokeAsync);
            });
            return builder;
        }

        /// <summary>
        /// 执行中间件
        /// </summary>
        /// <param name="context">http上下文</param>
        /// <returns></returns>
        public static async Task InvokeAsync(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var exceptionHandlerPathFeature =
                context.Features.Get<IExceptionHandlerPathFeature>();
            string s = JsonConvert.SerializeObject(
                new
                {
                    exceptionHandlerPathFeature.Path,
                    exceptionHandlerPathFeature.Error.Message,
                    exceptionHandlerPathFeature.Error.StackTrace,
                    exceptionHandlerPathFeature.Error.InnerException
                },
                new JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                });
            LogPool.Logger.LogError((int)LogEvent.系统,exceptionHandlerPathFeature.Error,exceptionHandlerPathFeature.Path);
            await context.Response.WriteAsync(s);
        }
    }
}
