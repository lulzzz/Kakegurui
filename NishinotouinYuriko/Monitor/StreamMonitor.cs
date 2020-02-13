using System;
using System.Collections.Concurrent;
using System.Net.Http;
using Kakegurui.Log;
using Kakegurui.Monitor;
using Kakegurui.WebExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NishinotouinYuriko.Monitor
{
    /// <summary>
    /// 流媒体监控
    /// </summary>
    public class StreamMonitor : IFixedJob
    {
        /// <summary>
        /// 超时时间(分)
        /// </summary>
        private readonly TimeSpan Timeout = TimeSpan.FromMinutes(10);

        /// <summary>
        /// 流媒体集合
        /// </summary>
        private readonly ConcurrentDictionary<string, DataClass> _strams = new ConcurrentDictionary<string, DataClass>();

        /// <summary>
        /// http客户端
        /// </summary>
        private readonly HttpClient _client;

        /// <summary>
        /// 流媒体地址
        /// </summary>
        private readonly string _url;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<StreamMonitor> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="factory">http工厂</param>
        /// <param name="configuration">配置项</param>
        /// <param name="logger">日志</param>
        public StreamMonitor(IHttpClientFactory factory, IConfiguration configuration, ILogger<StreamMonitor> logger)
        {
            _client = factory.CreateClient();
            _url = configuration.GetValue<string>("StreamUrl");
            _logger = logger;
        }

        /// <summary>
        /// 添加流媒体
        /// </summary>
        /// <param name="url">视频地址</param>
        /// <param name="path">流媒体路径</param>
        /// <returns>添加结果</returns>
        public AddResultClass Add(string url, string path)
        {
            AddResultClass addResult = _client.Get<AddResultClass>($"http://{_url}/api/media/mss/stream/pushers/add?type=rtsp&url={url}&path={path}");
            if (addResult == null)
            {
                return new AddResultClass
                {
                    Code = -1
                };
            }
            _logger.LogInformation((int)LogEvent.流媒体检查, $"添加视频流 {path} {url} {addResult.Code} {addResult.Message}");
            if (addResult.Code == 0)
            {
                addResult.Data.Time = DateTime.Now;
                _strams[path] = addResult.Data;
                return addResult;
            }
            else
            {
                if (_strams.TryGetValue(path, out DataClass item))
                {
                    return new AddResultClass
                    {
                        Code = 0,
                        Data = item
                    };
                }
                else
                {
                    GetResultClass getResult = _client.Get<GetResultClass>($"http://{_url}/api/media/mss/stream/pushers/get?path={path}");
                    if (getResult == null)
                    {
                        return new AddResultClass
                        {
                            Code = -1
                        };
                    }
                    _logger.LogInformation((int)LogEvent.流媒体检查, $"获取视频流 {path} {url} {getResult.Code} {getResult.Message}");
                    if (getResult.Code == 0)
                    {
                        getResult.Data.Rows[0].Time = DateTime.Now;
                        _strams[path] = getResult.Data.Rows[0];
                        return new AddResultClass
                        {
                            Code = 0,
                            Data = getResult.Data.Rows[0]
                        };
                    }
                    else
                    {
                        return new AddResultClass
                        {
                            Code = -1
                        };
                    }
                }
            }
        }

        /// <summary>
        /// 更新流媒体时间
        /// </summary>
        /// <param name="path"></param>
        public void Update(string path)
        {
            if (_strams.TryGetValue(path, out DataClass item))
            {
                item.Time = DateTime.Now;
            }
        }

        public void Handle(DateTime lastTime, DateTime current, DateTime nextTime)
        {
            DateTime now = DateTime.Now;
            foreach (var pair in _strams)
            {
                _logger.LogDebug((int)LogEvent.流媒体检查, $"{pair.Key} {pair.Value.Time:yyyy-MM-dd HH:mm:ss}");

                if (now - pair.Value.Time > Timeout)
                {
                    _strams.TryRemove(pair.Key, out _);
                    AddResultClass addResult = _client.Get<AddResultClass>($"http://{_url}/api/media/mss/stream/pushers/del?path={pair.Key}");
                    _logger.LogInformation((int)LogEvent.流媒体检查, $"删除视频流 {pair.Key} {pair.Value.Time:yyyy-MM-dd HH:mm:ss} {addResult.Code} {addResult.Message}");
                }
            }
        }

        public class AddResultClass
        {
            public int Code { get; set; }
            public string Message { get; set; }
            public DataClass Data { get; set; }
        }

        public class DataClass
        {
            public string Path { get; set; }
            public string RtspUrl { get; set; }
            public string WsUrl { get; set; }
            public DateTime Time { get; set; }
        }

        public class GetResultClass
        {
            public int Code { get; set; }
            public string Message { get; set; }
            public GetDataClass Data { get; set; }
        }

        public class GetDataClass
        {
            public int Total { get; set; }
            public DataClass[] Rows { get; set; }
        }

    }
}
