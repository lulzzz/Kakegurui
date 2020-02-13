using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kakegurui.Log;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Kakegurui.WebExtensions
{
    /// <summary>
    /// websocket中间件
    /// </summary>
    public class WebSocketMiddleware
    {
        /// <summary>
        /// url集合
        /// </summary>
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<WebSocket, object>> _clients = new ConcurrentDictionary<string, ConcurrentDictionary<WebSocket, object>>();

        /// <summary>
        /// 下一个中间件
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<WebSocketMiddleware> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="next">下一个中间件</param>
        /// <param name="logger">日志</param>
        public WebSocketMiddleware(RequestDelegate next,ILogger<WebSocketMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// 清空url
        /// </summary>
        public static void ClearUrl()
        {
            _clients.Clear();
        }

        /// <summary>
        /// 添加url
        /// </summary>
        /// <param name="url">接受的ws地址</param>
        public static void AddUrl(string url)
        {
            _clients.TryAdd(url, new ConcurrentDictionary<WebSocket, object>());
        }

        /// <summary>
        /// 移除url
        /// </summary>
        /// <param name="url">移除的ws地址</param>
        public static async void RemoveUrl(string url)
        {
            if (_clients.TryRemove(url, out var clients))
            {
                foreach (var pair in clients)
                {
                    await pair.Key.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);
                }
            }
        }

        /// <summary>
        /// 向指定的url客户端广播数据
        /// </summary>
        /// <param name="url">ws地址</param>
        /// <param name="data">数据</param>
        public static async void Broadcast(string url, object data)
        {
            if (_clients.ContainsKey(url) && _clients[url].Count > 0)
            {
                string json=JsonConvert.SerializeObject(data, 
                    new JsonSerializerSettings
                    {
                        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                    });
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                foreach (var pair in _clients[url])
                {
                    try
                    {
                        await pair.Key.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length),
                            WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (WebSocketException)
                    {

                    }
                    catch (OperationCanceledException)
                    {

                    }
                }
            }
        }

        /// <summary>
        /// 执行中间件
        /// </summary>
        /// <param name="context">http上下文</param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                string url = Uri.UnescapeDataString(context.Request.Path.Value);
                if (_clients.ContainsKey(url))
                {
                    _logger.LogInformation((int)LogEvent.套接字, $"ws_accept {url}");
                    WebSocket client = await context.WebSockets.AcceptWebSocketAsync();
                    _clients[url][client] = null;
                    var buffer = new byte[0];
                    try
                    {
                        WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        while (!result.CloseStatus.HasValue)
                        {
                            result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        }
                        _logger.LogInformation((int)LogEvent.套接字, $"ws_close {url}");
                        await client.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                    }
                    catch (WebSocketException)
                    {
                        _logger.LogInformation((int)LogEvent.套接字, $"ws_shutdown {url}");
                    }
                    if (_clients.TryGetValue(url, out var clients))
                    {
                        clients.TryRemove(client, out _);
                    }
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                }
            }
            else
            {
                await _next(context);
            }
     
        }
    }
}
