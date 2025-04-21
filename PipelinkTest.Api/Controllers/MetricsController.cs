using Microsoft.AspNetCore.Mvc;
using Pipelink.Models;
using Pipelink.Interfaces.Metric;

namespace PipelinkTest.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MetricsController : ControllerBase
    {
        private readonly IMetricsCollector _metricsCollector;

        public MetricsController(IMetricsCollector metricsCollector)
        {
            _metricsCollector = metricsCollector;
        }

        [HttpGet]
        public async Task<ActionResult<MetricsData[]>> GetMetrics(
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null,
            [FromQuery] string? requestType = null)
        {
            var metrics = await _metricsCollector.GetMetricsAsync(startTime, endTime, requestType);
            return Ok(metrics);
        }
    }
} 