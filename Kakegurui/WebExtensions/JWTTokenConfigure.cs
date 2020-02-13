using System;
using System.Text;
using Kakegurui.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Kakegurui.WebExtensions
{
    /// <summary>
    /// jwt 配置
    /// </summary>
    public static class JWTTokenConfigure
    {
        /// <summary>
        /// jwt验证
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection ConfigureTrafficJWTToken(this IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = Token.Issuer,
                        ValidateAudience = true,
                        ValidAudience = Token.Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Token.Key))
                    };
                });
            return services;
        }
    }
}
