using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Pipelink.Interfaces.Metric;
using Pipelink.Models;

namespace Pipelink.Middleware
{
    public class MetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMetricsCollector _metricsCollector;

        public MetricsMiddleware(RequestDelegate next, IMetricsCollector metricsCollector)
        {
            _next = next;
            _metricsCollector = metricsCollector;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startTime = DateTime.UtcNow;
            var process = Process.GetCurrentProcess();
            var startCpu = process.TotalProcessorTime;
            var startMemory = process.WorkingSet64;
            var startPeakMemory = process.PeakWorkingSet64;

            try
            {
                await _next(context);
            }
            finally
            {
                var endTime = DateTime.UtcNow;
                var endCpu = process.TotalProcessorTime;
                var endMemory = process.WorkingSet64;
                var endPeakMemory = process.PeakWorkingSet64;

                var duration = (endTime - startTime).TotalMilliseconds;
                var cpuTime = (endCpu - startCpu).TotalMilliseconds;
                var memoryDiff = endMemory - startMemory;
                var peakMemory = Math.Max(endPeakMemory, startPeakMemory);

                // Calculate CPU percentage (approximate)
                var cpuPercentage = (cpuTime / duration) * 100.0;
                if (double.IsInfinity(cpuPercentage) || double.IsNaN(cpuPercentage))
                {
                    cpuPercentage = 0;
                }

                // Calculate memory percentage (approximate)
                var totalMemory = GC.GetTotalMemory(false);
                var memoryPercentage = (memoryDiff / (double)totalMemory) * 100.0;
                if (double.IsInfinity(memoryPercentage) || double.IsNaN(memoryPercentage))
                {
                    memoryPercentage = 0;
                }

                var metrics = new MetricsData
                {
                    RequestType = context.Request.Path,
                    StartTime = startTime,
                    EndTime = endTime,
                    DurationMs = duration,
                    CpuUsage = cpuTime,
                    MemoryUsage = memoryDiff,
                    HasError = context.Response.StatusCode >= 400,
                    CpuPercentage = cpuPercentage,
                    MemoryPercentage = memoryPercentage,
                    PeakMemoryUsage = peakMemory,
                    AverageCpuUsage = cpuTime / duration
                };

                await _metricsCollector.RecordMetricsAsync(metrics);
            }
        }
    }
} 