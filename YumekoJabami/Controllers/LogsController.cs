using System;
using System.Linq;
using Kakegurui.Log;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace YumekoJabami.Controllers
{
    /// <summary>
    /// 日志
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class LogsController : Controller
    {
        /// <summary>
        /// 日志文件名
        /// </summary>
        private readonly string _logName;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration">配置项</param>
        public LogsController(IConfiguration configuration)
        {
            _logName = configuration.GetValue<string>("LogName");
        }

        /// <summary>
        /// 日志查询
        /// </summary>
        /// <param name="logLevel">日志级别，为0时查询所有</param>
        /// <param name="logEvent">日志事件，为0时查询所有</param>
        /// <param name="logDate">日志日期</param>
        /// <param name="pageNum">分页页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <param name="hasTotal">是否查询全部</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        public IActionResult GetLogs([FromQuery]DateTime logDate, [FromQuery]int logLevel, [FromQuery]int logEvent, [FromQuery]int pageNum, [FromQuery]int pageSize, [FromQuery]bool hasTotal)
        {
            var (item1, item2) = LogReader.ReadLogs(_logName, logDate, logLevel, logEvent, pageNum, pageSize, hasTotal);

            PageModel<object> model = new PageModel<object>
            {
                Datas = item1
                    .Select(l =>
                    {
                        string[] datas = l.Split(']', StringSplitOptions.RemoveEmptyEntries);
                        //4个]1个空格
                        int headLength = datas[0].Length + datas[1].Length + datas[2].Length + datas[3].Length + 5;
                        return new
                        {
                            Time = datas[0].Substring(1, datas[0].Length - 1),
                            Level = ((LogLevel)Convert.ToInt32(datas[1].Substring(1, datas[1].Length - 1))).ToString(),
                            Event = ((LogEvent)Convert.ToInt32(datas[2].Substring(1, datas[2].Length - 1))).ToString(),
                            User = datas[3].Substring(1, datas[3].Length - 1),
                            Content = l.Substring(headLength, l.Length - headLength)
                        };
                    })
                    .Cast<object>()
                    .ToList(),
                Total = hasTotal ? item2 : 0
            };
            return Ok(model);
        }
    }
}
