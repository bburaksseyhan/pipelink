using Pipelink.Extensions;
using PipelinkTest.Api.Commands;
using PipelinkTest.Api.Notifications;
using PipelinkTest.Api.Queries;
using Pipelink.Implementation.MetricImplementation;
using Microsoft.AspNetCore.Mvc;
using Pipelink.Interfaces.Metric;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register metrics collector
builder.Services.AddSingleton<IMetricsCollector, InMemoryMetricsCollector>();

// Register Pipelink with automatic handler and behavior registration
builder.Services.AddPipelink(cfg => 
{
    // Register handlers from the current assembly
    cfg.RegisterServicesFromAssemblyContaining<Program>();
});

var app = builder.Build();

var pipelink = app.Services.GetRequiredService<Pipelink.Implementation.Pipelink>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add metrics middleware
app.UseMetrics();

// Map gRPC service
// app.MapGrpcService<MetricsGrpcService>();

app.MapGet("/user/{id}", async (int id) =>
{
    var user = await pipelink.Send(new GetUserQuery(id));
    return Results.Ok(user);
}).WithName("GetUserById")
.WithOpenApi();

app.MapPost("/login", async (LoginUserCommand command) => 
{
    var loginUser = await pipelink.Send(command);
    return Results.Ok(loginUser);
}).WithName("LoginUser")
.WithOpenApi();

app.MapPost("/notify-user", async (UserCreatedNotification notification) =>
{
    await pipelink.Publish(notification);
    return Results.Ok("Notification sent");
}).WithName("NotifyUser")
.WithOpenApi();

app.MapGet("/stream-users", async (int count = 10) =>
{
    var users = pipelink.SendStream(new StreamUserQuery(count));
    return Results.Ok(users);
}).WithName("StreamUsers")
.WithOpenApi();

// Add metrics endpoint
app.MapGet("/metrics", async (IMetricsCollector metricsCollector, 
    [FromQuery] DateTime? startTime = null,
    [FromQuery] DateTime? endTime = null,
    [FromQuery] string? requestType = null) =>
{
    var metrics = await metricsCollector.GetMetricsAsync(startTime, endTime, requestType);
    return Results.Ok(metrics);
}).WithName("GetMetrics")
.WithOpenApi();

// Export metrics as CSV
app.MapGet("/metrics/export/csv", async (IMetricsCollector metricsCollector,
    [FromQuery] DateTime? startTime = null,
    [FromQuery] DateTime? endTime = null,
    [FromQuery] string? requestType = null) =>
{
    var metrics = await metricsCollector.GetMetricsAsync(startTime, endTime, requestType);
    var csv = new StringBuilder();
    
    // Add headers
    csv.AppendLine("RequestType,StartTime,EndTime,DurationMs,CpuUsage,MemoryUsage,CpuPercentage,MemoryPercentage,PeakMemoryUsage,HasError");
    
    // Add data rows
    foreach (var metric in metrics)
    {
        csv.AppendLine($"{metric.RequestType},{metric.StartTime:O},{metric.EndTime:O},{metric.DurationMs},{metric.CpuUsage},{metric.MemoryUsage},{metric.CpuPercentage},{metric.MemoryPercentage},{metric.PeakMemoryUsage},{metric.HasError}");
    }
    
    return Results.Text(csv.ToString(), "text/csv", Encoding.UTF8);
}).WithName("ExportMetricsCSV")
.WithOpenApi();

// Export metrics as HTML visualization
app.MapGet("/metrics/visualize", async (IMetricsCollector metricsCollector,
    [FromQuery] DateTime? startTime = null,
    [FromQuery] DateTime? endTime = null,
    [FromQuery] string? requestType = null) =>
{
    var metrics = await metricsCollector.GetMetricsAsync(startTime, endTime, requestType);
    var html = new StringBuilder();
    
    html.AppendLine(@"<!DOCTYPE html>
<html>
<head>
    <title>Metrics Visualization</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        table { border-collapse: collapse; width: 100%; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background-color: #f2f2f2; }
        tr:nth-child(even) { background-color: #f9f9f9; }
        .metric-header { margin: 20px 0; color: #333; }
    </style>
</head>
<body>
    <h1>Metrics Visualization</h1>");

    // Add summary section
    html.AppendLine($@"
    <div class='metric-header'>
        <h2>Summary</h2>
        <p>Total Requests: {metrics.Length}</p>
        <p>Average Duration: {metrics.Average(m => m.DurationMs):F2} ms</p>
        <p>Average CPU Usage: {metrics.Average(m => m.CpuUsage):F2} ms</p>
        <p>Total Memory Usage: {metrics.Sum(m => m.MemoryUsage):N0} bytes</p>
    </div>");

    // Add detailed table
    html.AppendLine(@"
    <h2>Detailed Metrics</h2>
    <table>
        <tr>
            <th>Request Type</th>
            <th>Start Time</th>
            <th>Duration (ms)</th>
            <th>CPU Usage (ms)</th>
            <th>Memory Usage (bytes)</th>
            <th>CPU %</th>
            <th>Memory %</th>
            <th>Status</th>
        </tr>");

    foreach (var metric in metrics)
    {
        html.AppendLine($@"
        <tr>
            <td>{metric.RequestType}</td>
            <td>{metric.StartTime:yyyy-MM-dd HH:mm:ss}</td>
            <td>{metric.DurationMs:F2}</td>
            <td>{metric.CpuUsage:F2}</td>
            <td>{metric.MemoryUsage:N0}</td>
            <td>{metric.CpuPercentage:F2}%</td>
            <td>{metric.MemoryPercentage:F2}%</td>
            <td>{(metric.HasError ? "Error" : "Success")}</td>
        </tr>");
    }

    html.AppendLine(@"
    </table>
</body>
</html>");

    return Results.Text(html.ToString(), "text/html", Encoding.UTF8);
}).WithName("VisualizeMetrics")
.WithOpenApi();

// Pretty print JSON metrics
app.MapGet("/metrics/pretty", async (IMetricsCollector metricsCollector,
    [FromQuery] DateTime? startTime = null,
    [FromQuery] DateTime? endTime = null,
    [FromQuery] string? requestType = null) =>
{
    var metrics = await metricsCollector.GetMetricsAsync(startTime, endTime, requestType);
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    var formattedMetrics = metrics.Select(m => new
    {
        RequestType = m.RequestType,
        StartTime = m.StartTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
        EndTime = m.EndTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
        DurationMs = Math.Round(m.DurationMs, 2),
        CpuUsageMs = Math.Round(m.CpuUsage, 2),
        MemoryUsageBytes = m.MemoryUsage,
        CpuPercentage = Math.Round(m.CpuPercentage, 2),
        MemoryPercentage = Math.Round(m.MemoryPercentage, 2),
        PeakMemoryUsageBytes = m.PeakMemoryUsage,
        HasError = m.HasError
    });
    
    return Results.Json(formattedMetrics, options);
}).WithName("PrettyPrintMetrics")
.WithOpenApi();

app.MapDelete("/metrics", async (IMetricsCollector metricsCollector) =>
{
    await metricsCollector.ClearMetricsAsync();
    return Results.Ok(new { message = "Metrics data cleared successfully" });
}).WithName("ClearMetrics")
.WithOpenApi()
.Produces(200, typeof(object), "application/json");

await app.RunAsync();
