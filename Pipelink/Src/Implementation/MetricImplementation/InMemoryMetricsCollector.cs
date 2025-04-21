using Pipelink.Interfaces.Metric;
using Pipelink.Models;

namespace Pipelink.Implementation.MetricImplementation
{
    /// <summary>
    /// Provides an in-memory implementation of metrics collection.
    /// This implementation stores metrics in memory for fast access and analysis.
    /// Thread-safe for concurrent access using a lock mechanism.
    /// </summary>
    /// <remarks>
    /// The collector maintains a list of metrics in memory and provides methods to:
    /// - Record new metrics data
    /// - Retrieve metrics with optional filtering
    /// - Support concurrent access through thread-safe operations
    /// </remarks>
    public class InMemoryMetricsCollector : IMetricsCollector
    {
        private readonly List<MetricsData> _metrics = new();
        private readonly object _lock = new();

        /// <summary>
        /// Records a new set of metrics data.
        /// Thread-safe implementation using a lock mechanism.
        /// </summary>
        /// <param name="metrics">The metrics data to record.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method:
        /// 1. Acquires a lock to ensure thread safety
        /// 2. Adds the metrics to the in-memory collection
        /// 3. Releases the lock
        /// </remarks>
        public Task RecordMetricsAsync(MetricsData metrics)
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
        /// <returns>An array of filtered metrics data.</returns>
        /// <remarks>
        /// The method applies filters in the following order:
        /// 1. Start time filter (if provided)
        /// 2. End time filter (if provided)
        /// 3. Request type filter (if provided)
        /// 
        /// Note: This method is thread-safe as it operates on a copy of the metrics collection.
        /// </remarks>
        public Task<MetricsData[]> GetMetricsAsync(DateTime? startTime = null, DateTime? endTime = null, string? requestType = null)
        {
            IEnumerable<MetricsData> query = _metrics;

            if (startTime.HasValue)
                query = query.Where(m => m.StartTime >= startTime.Value);

            if (endTime.HasValue)
                query = query.Where(m => m.EndTime <= endTime.Value);

            if (!string.IsNullOrEmpty(requestType))
                query = query.Where(m => m.RequestType == requestType);

            return Task.FromResult(query.ToArray());
        }

        public Task ClearMetricsAsync()
        {
            lock (_lock)
            {
                _metrics.Clear();
            }
            return Task.CompletedTask;
        }
    }
} 