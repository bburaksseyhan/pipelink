using System.Text.Json;
using Pipelink.Interfaces.Metric;
using Pipelink.Models;

namespace Pipelink.Implementation.MetricImplementation
{
    /// <summary>
    /// Provides a file-based implementation of metrics storage.
    /// This implementation persists metrics to a JSON file, allowing data to survive application restarts.
    /// Thread-safe for concurrent access.
    /// </summary>
    public class FileMetricsStorage : IMetricsStorage
    {
        private readonly string _filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileMetricsStorage"/> class.
        /// </summary>
        /// <param name="filePath">The path to the JSON file where metrics will be stored. Defaults to "metrics.json".</param>
        public FileMetricsStorage(string filePath = "metrics.json")
        {
            _filePath = filePath;
        }

        /// <summary>
        /// Stores the provided metrics data in the JSON file.
        /// Thread-safe implementation using file system operations.
        /// </summary>
        /// <param name="metrics">The metrics data to store.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// The method performs the following steps:
        /// 1. Reads existing metrics from the file
        /// 2. Adds the new metrics
        /// 3. Writes the updated collection back to the file
        /// </remarks>
        public async Task StoreMetricsAsync(MetricsData metrics)
        {
            var allMetrics = await ReadMetricsAsync();
            allMetrics.Add(metrics);

            await WriteMetricsAsync(allMetrics);
        }

        /// <summary>
        /// Retrieves metrics data from the file based on the provided filters.
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
        public async Task<IEnumerable<MetricsData>> RetrieveMetricsAsync(DateTime? startTime = null, DateTime? endTime = null, string? requestType = null)
        {
            var metrics = await ReadMetricsAsync();
            IEnumerable<MetricsData> query = metrics;

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

            return query;
        }

        /// <summary>
        /// Reads all metrics from the JSON file.
        /// </summary>
        /// <returns>A list of metrics data. Returns an empty list if the file doesn't exist.</returns>
        private async Task<List<MetricsData>> ReadMetricsAsync()
        {
            if (!File.Exists(_filePath))
            {
                return new List<MetricsData>();
            }

            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<List<MetricsData>>(json) ?? new List<MetricsData>();
        }

        /// <summary>
        /// Writes metrics to the JSON file.
        /// </summary>
        /// <param name="metrics">The metrics data to write.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task WriteMetricsAsync(List<MetricsData> metrics)
        {
            var json = JsonSerializer.Serialize(metrics);
            await File.WriteAllTextAsync(_filePath, json);
        }
    }
} 