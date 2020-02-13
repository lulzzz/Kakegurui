using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Kakegurui.WebExtensions
{
    /// <summary>
    /// 默认400返回配置
    /// </summary>
    public static class BadRequestConfiguration
    {
        public static IMvcBuilder ConfigureBadRequest(this IMvcBuilder mvc)
        {
            mvc.ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = 
                    context => new BadRequestObjectResult(context.ModelState)
                {
                    ContentTypes = { "application/problem+json" }
                };
            });
            return mvc;
        }
    }
}
