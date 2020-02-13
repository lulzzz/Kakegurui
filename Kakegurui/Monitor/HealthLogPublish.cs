using System.Threading;
using System.Threading.Tasks;
using Kakegurui.Log;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Monitor
{
    /// <summary>
    /// 系统状态日志发布
    /// </summary>
    public class HealthLogPublish : IHealthCheckPublisher
    {
        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志</param>
        public HealthLogPublish(ILogger<HealthLogPublish> logger)
        {
            _logger = logger;
        }

        #region 实现 IHealthCheckPublisher
        public Task PublishAsync(HealthReport report,
            CancellationToken cancellationToken)
        {
 
            _logger.LogInformation((int)LogEvent.系统, $"系统状态 {report.Status}");

            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }
        #endregion
    }
}
