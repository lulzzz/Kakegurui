using System;
using System.Linq;
using Kakegurui.Monitor;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NishinotouinYuriko.Controllers;
using NishinotouinYuriko.Data;
using NishinotouinYuriko.Models;

namespace NishinotouinYuriko.Monitor
{
    public class TodayViolationMonitor : IFixedJob
    {
        private readonly IServiceProvider _serviceProvider;

        public TodayViolationMonitor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static ViolationStatus Status { get; set; }

        public void Handle(DateTime lastTime, DateTime current, DateTime nextTime)
        {
            DateTime today=DateTime.Today;
            DateTime now=DateTime.Now;
            using (IServiceScope serviceScope = _serviceProvider.CreateScope())
            {
                using (ViolationContext violationContext =
                    serviceScope.ServiceProvider.GetRequiredService<ViolationContext>())
                {
                    ViolationStructsController controller=new ViolationStructsController(violationContext, serviceScope.ServiceProvider.GetRequiredService<IMemoryCache>());
                    ViolationStatus status = new ViolationStatus()
                    {
                        ViolationChart = controller.QueryChart(null, null, null, null, null, null, "violation", today, now)
                            .Where(c => c.Value != 0)
                            .OrderByDescending(c => c.Value)
                            .Take(5)
                            .ToList(),
                        LocationChart = controller.QueryChart(null, null, null, null, null, null, "location", today, now)
                            .Where(c => c.Value != 0)
                            .OrderByDescending(c => c.Value)
                            .Take(10)
                            .ToList(),
                        TargetTypeChart = controller.QueryChart(null, null, null, null, null, null, "targetType", today, now),
                        CarTypeChart = controller.QueryChart(null, null, null, null, null, null, "carType", today, now)
                    };
                    Status = status;
                }
            }

        }
    }
}
