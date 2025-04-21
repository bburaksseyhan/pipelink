using Pipelink.Interfaces.Metric;
using Pipelink.Models;

namespace Pipelink.Implementation.MetricImplementation
{
    /// <summary>
    /// Provides an in-memory implementation of metrics storage.
    /// This implementation stores metrics in memory for fast access but does not persist across application restarts.
    /// Thread-safe for concurrent access.
    /// </summary>
    public class InMemoryMetricsStorage : IMetricsStorage
    {
        private readonly List<MetricsData> _metrics = new();
        private readonly object _lock = new();

        /// <summary>
        /// Stores the provided metrics data in memory.
        /// Thread-safe implementation using a lock mechanism.
        /// </summary>
        /// <param name="metrics">The metrics data to store.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task StoreMetricsAsync(MetricsData metrics)
        {
            lock (_lock)
            {
                _metrics.Add(metrics);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Retrieves metrics data based on the provided filters.
        /// </summary>
        /// <param name="startTime">Optional. Filter metrics recorded after this time.</param>
        /// <param name="endTime">Optional. Filter metrics recorded before this time.</param>
        /// <param name="requestType">Optional. Filter metrics for a specific request type.</param>
        /// <returns>A filtered collection of metrics data.</returns>
        /// <remarks>
        /// The method applies filters in the following order:
        /// 1. Start time filter (if provided)
        /// 2. End time filter (if provided)
        /// 3. Request type filter (if provided)
        /// </remarks>
        public Task<IEnumerable<MetricsData>> RetrieveMetricsAsync(DateTime? startTime = null, DateTime? endTime = null, string? requestType = null)
        {
            IEnumerable<MetricsData> query = _metrics;

            if (startTime.HasValue)
            {
                query = query.Where(m => m.StartTime >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(m => m.EndTime <= endTime.Value);
            }

            if (!string.IsNullOrEmpty(requestType))
            {
                query = query.Where(m => m.RequestType == requestType);
            }

            return Task.FromResult(query);
        }
    }
} 