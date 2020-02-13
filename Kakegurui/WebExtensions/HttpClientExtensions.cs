using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Kakegurui.Log;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Kakegurui.WebExtensions
{
    /// <summary>
    /// http扩展
    /// </summary>
    public static class HttpClientExtensions
    {
        /// <summary>
        /// 超时时间
        /// </summary>
        private const int Timeout = 3000;

        /// <summary>
        /// get
        /// </summary>
        /// <param name="client">http客户端</param>
        /// <param name="url">访问地址</param>
        /// <returns>查询结果</returns>
        public static object Get(this HttpClient client, string url)
        {
            return Get<object>(client,url);
        }


        /// <summary>
        /// get
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="client">http客户端</param>
        /// <param name="url">访问地址</param>
        /// <returns>查询结果</returns>
        public static T Get<T>(this HttpClient client, string url)
        {
            try
            {
                Task<HttpResponseMessage> response = client.GetAsync(url);
                response.Wait(Timeout);
                if (response.IsCompleted)
                {
                    Task<string> json = response.Result.Content.ReadAsStringAsync();
                    json.Wait(Timeout);
                    return JsonConvert.DeserializeObject<T>(json.Result);
                }
                else
                {
                    LogPool.Logger.LogWarning((int)LogEvent.系统, $"http请求超时 {url}");
                    return default;
                }
            }
            catch (Exception ex)
            {
                LogPool.Logger.LogError((int)LogEvent.系统, ex, "http get");
                return default;
            }
        }

        /// <summary>
        /// post
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="client">http客户端</param>
        /// <param name="url">访问地址</param>
        /// <param name="t">参数</param>
        /// <returns>更新结果</returns>
        public static HttpStatusCode? Post<T>(this HttpClient client, string url, T t)
        {
            try
            {
                HttpContent content = new StringContent(JsonConvert.SerializeObject(t), Encoding.UTF8, "application/json");
                Task<HttpResponseMessage> response = client.PostAsync(url, content);
                response.Wait(Timeout);
                if (response.IsCompleted)
                {
                    return response.Result.StatusCode;
                }
                else
                {
                    LogPool.Logger.LogWarning((int)LogEvent.系统, $"http请求超时 {url}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogPool.Logger.LogError((int)LogEvent.系统, ex, "http post");
                return null;
            }
        }

        /// <summary>
        /// put
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="client">http客户端</param>
        /// <param name="url">访问地址</param>
        /// <param name="t">参数</param>
        /// <returns>更新结果</returns>
        public static HttpStatusCode? Put<T>(this HttpClient client, string url, T t)
        {
            try
            {
                HttpContent content = new StringContent(JsonConvert.SerializeObject(t), Encoding.UTF8, "application/json");
                Task<HttpResponseMessage> response = client.PutAsync(url, content);
                response.Wait(Timeout);
                if (response.IsCompleted)
                {
                    return response.Result.StatusCode;
                }
                else
                {
                    LogPool.Logger.LogWarning((int)LogEvent.系统, $"http请求超时 {url}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogPool.Logger.LogError((int)LogEvent.系统, ex, "http post");
                return null;
            }
        }
    }
}
