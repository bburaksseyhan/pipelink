using Pipelink.Models;

namespace Pipelink.Interfaces.Metric
{
    public interface IMetricsCollector
    {
        Task RecordMetricsAsync(MetricsData metrics);
        Task<MetricsData[]> GetMetricsAsync(DateTime? startTime = null, DateTime? endTime = null, string? requestType = null);
        Task ClearMetricsAsync();
    }
} 