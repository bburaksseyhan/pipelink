using System;

namespace Pipelink.Models
{
    /// <summary>
    /// Represents performance metrics data collected during request processing.
    /// This class captures various performance indicators including timing, resource usage, and error information.
    /// </summary>
    public class MetricsData
    {
        /// <summary>
        /// Gets or sets the type of request being processed (e.g., HTTP endpoint path).
        /// </summary>
        public string RequestType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC timestamp when request processing started.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when request processing completed.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the total duration of request processing in milliseconds.
        /// </summary>
        public double DurationMs { get; set; }

        /// <summary>
        /// Gets or sets the total CPU time consumed during request processing in milliseconds.
        /// </summary>
        public double CpuUsage { get; set; }

        /// <summary>
        /// Gets or sets the change in memory usage during request processing in bytes.
        /// </summary>
        public long MemoryUsage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an error occurred during request processing.
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// Gets or sets the error message if an error occurred during request processing.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
        
        // Detailed Performance Metrics

        /// <summary>
        /// Gets or sets the CPU usage as a percentage of total processing time.
        /// Calculated as (CpuTime / Duration) * 100.
        /// </summary>
        public double CpuPercentage { get; set; }

        /// <summary>
        /// Gets or sets the memory usage as a percentage of total available memory.
        /// Calculated as (MemoryUsed / TotalMemory) * 100.
        /// </summary>
        public double MemoryPercentage { get; set; }

        /// <summary>
        /// Gets or sets the peak memory usage observed during request processing in bytes.
        /// </summary>
        public long PeakMemoryUsage { get; set; }

        /// <summary>
        /// Gets or sets the average CPU usage per millisecond during request processing.
        /// Calculated as CpuTime / Duration.
        /// </summary>
        public double AverageCpuUsage { get; set; }
    }
} 