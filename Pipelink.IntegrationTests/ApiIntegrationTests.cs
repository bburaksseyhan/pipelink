using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PipelinkTest.Api;
using PipelinkTest.Api.Dtos;

namespace Pipelink.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<ApiApplicationEntry>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<ApiApplicationEntry> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUser_ShouldReturnUserThroughPipeline_WhenIdProvided()
    {
        var user = await _client.GetFromJsonAsync<UserDto>("/user/42");

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
        Assert.Equal("John Doe", user.Name);
    }

    [Fact]
    public async Task Login_ShouldReturnLoginDto_WhenCommandPosted()
    {
        var response = await _client.PostAsJsonAsync("/login", new { email = "qa@example.com" });

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<LoginUserDto>();
        Assert.Equal("burak.seyhan@commencis.com", dto!.Email);
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenBodyMissing()
    {
        var response = await _client.PostAsync("/login", new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NotifyUser_ShouldReturnOk_WhenNotificationPublished()
    {
        var response = await _client.PostAsJsonAsync("/notify-user", new { userId = 7 });

        response.EnsureSuccessStatusCode();
        Assert.Equal("\"Notification sent\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task StreamUsers_ShouldReturnRequestedCount_WhenCountProvided()
    {
        var users = await _client.GetFromJsonAsync<List<UserDto>>("/stream-users?count=3");

        Assert.NotNull(users);
        Assert.Equal([1, 2, 3], users.Select(u => u.Id));
    }

    [Fact]
    public async Task GetUser_ShouldReturnMatchingUsers_WhenCalledConcurrently()
    {
        var ids = Enumerable.Range(1, 50).ToArray();

        var users = await Task.WhenAll(ids.Select(id => _client.GetFromJsonAsync<UserDto>($"/user/{id}")));

        Assert.Equal(ids, users.Select(u => u!.Id));
    }

    [Fact]
    public async Task Metrics_ShouldRecordRequestPath_WhenEndpointCalled()
    {
        await _client.GetAsync("/user/5");

        var metrics = await _client.GetFromJsonAsync<List<MetricsRow>>("/metrics?requestType=/user/5");

        Assert.NotNull(metrics);
        Assert.NotEmpty(metrics);
        Assert.All(metrics, m => Assert.False(m.HasError));
    }

    [Fact]
    public async Task MetricsCsv_ShouldReturnCsvWithHeader_WhenRequested()
    {
        var response = await _client.GetAsync("/metrics/export/csv");

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/csv", response.Content.Headers.ContentType!.MediaType);
        Assert.StartsWith("RequestType,StartTime", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MetricsVisualize_ShouldReturnHtml_WhenNoMetricsRecorded()
    {
        await using var factory = new WebApplicationFactory<ApiApplicationEntry>();
        var client = factory.CreateClient();
        await client.DeleteAsync("/metrics");

        var response = await client.GetAsync("/metrics/visualize?requestType=/none");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed record MetricsRow(string RequestType, bool HasError);
}
