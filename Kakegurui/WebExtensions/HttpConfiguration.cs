using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Kakegurui.Log;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kakegurui.WebExtensions
{
    /// <summary>
    /// http配置读取实现
    /// </summary>
    public class HttpConfigurationProvider : ConfigurationProvider
    {
        /// <summary>
        /// 系统管理中心地址
        /// </summary>
        private readonly string _systemUrl;

        /// <summary>
        /// 参数类型
        /// </summary>
        private readonly string _parameterType;

        /// <summary>
        /// 参数
        /// </summary>
        internal class Parameter
        {
            /// <summary>
            /// 键
            /// </summary>
            public string Key { get; set; }

            /// <summary>
            /// 值
            /// </summary>
            public string Value { get; set; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="systemUrl">系统管理中心地址</param>
        /// <param name="parameterType">参数类型</param>
        public HttpConfigurationProvider(string systemUrl, string parameterType)
        {
            _systemUrl = systemUrl;
            _parameterType = parameterType;
        }

        public override void Load()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    List<Parameter> parameters =
                        client.Get<List<Parameter>>($"http://{_systemUrl}/api/parameters/types/{_parameterType}");
                    if (parameters != null)
                    {
                        Data = parameters.ToDictionary(p => p.Key, p => p.Value);
                    }
                }
                catch (Exception ex)
                {
                    LogPool.Logger.LogError((int)LogEvent.配置项,ex,$"读取配置项失败 {_systemUrl} {_parameterType}");
                }
            }
        }
    }

    /// <summary>
    /// http配置源
    /// </summary>
    public class HttpConfigurationSource : IConfigurationSource
    {
        private readonly string _systemUrl;
        private readonly string _parameterType;
        public HttpConfigurationSource(string systemUrl, string parameterType)
        {
            _systemUrl = systemUrl;
            _parameterType = parameterType;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new HttpConfigurationProvider(_systemUrl, _parameterType);
        }
    }

    /// <summary>
    /// http接口读取扩展
    /// </summary>
    public static class HttpConfigurationExtensions
    {
        public static IConfigurationBuilder AddHttpConfiguration(
            this IConfigurationBuilder builder,
            string systemUrl, string parameterType)
        {
            return builder.Add(new HttpConfigurationSource(systemUrl, parameterType));
        }
    }
}
