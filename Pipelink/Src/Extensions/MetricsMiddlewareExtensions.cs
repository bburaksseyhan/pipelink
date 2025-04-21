using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Pipelink.Interfaces.Metric;
using Pipelink.Middleware;

namespace Pipelink.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring metrics collection in the ASP.NET Core pipeline.
    /// </summary>
    public static class MetricsMiddlewareExtensions
    {
        /// <summary>
        /// Adds metrics collection middleware to the application's request pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        /// <remarks>
        /// This extension method:
        /// 1. Retrieves the metrics collector from the service container
        /// 2. Adds the metrics middleware to the pipeline
        /// 3. Enables automatic collection of performance metrics for all requests
        /// </remarks>
        public static IApplicationBuilder UseMetrics(this IApplicationBuilder app)
        {
            var metricsCollector = app.ApplicationServices.GetRequiredService<IMetricsCollector>();
            return app.UseMiddleware<MetricsMiddleware>(metricsCollector);
        }
    }
} 