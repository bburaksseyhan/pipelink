using Pipelink.Models;

namespace Pipelink.Interfaces.Metric
{
    public interface IMetricsStorage
    {
        Task StoreMetricsAsync(MetricsData metrics);
        Task<IEnumerable<MetricsData>> RetrieveMetricsAsync(DateTime? startTime = null, DateTime? endTime = null, string requestType = null);
    }
} 