using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kakegurui.WebExtensions
{
    /// <summary>
    /// 用户配置
    /// </summary>
    public static class IdentityConfigure
    {
        public static IdentityBuilder ConfigureIdentity<T>(this IServiceCollection services) where T : DbContext
        {
           return services.AddIdentityCore<IdentityUser>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequiredLength = 4;
                    options.Password.RequiredUniqueChars = 0;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<T>()
                .AddSignInManager<SignInManager<IdentityUser>>()
                .AddDefaultTokenProviders();
        }
    }
}
