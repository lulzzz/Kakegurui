using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Kakegurui.Core
{
    /// <summary>
    /// jwt
    /// </summary>
    public class Token
    {
        /// <summary>
        /// 接收人
        /// </summary>
        public const string Audience = "seemmo";
        /// <summary>
        /// 发布者
        /// </summary>
        public const string Issuer = "seemmo";
        /// <summary>
        /// 密钥
        /// </summary>
        public const string Key = "dd%88*377f6d&f£$$£$FdddFF33fssDG^!3";

        /// <summary>
        /// 创建jwt字符串
        /// </summary>
        /// <param name="claims">权限集合</param>
        /// <returns>jwt字符串</returns>
        public static string WriteToken(List<Claim> claims)
        {
            DateTime utcNow = DateTime.UtcNow;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(Issuer,Audience, claims, utcNow, utcNow.AddDays(7),creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// 获取token中的权限
        /// </summary>
        /// <param name="token">jwt字符串</param>
        /// <returns>权限集合</returns>
        public static JwtSecurityToken ReadToken(string token)
        {
            JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return jwt;
        }
    }
}
