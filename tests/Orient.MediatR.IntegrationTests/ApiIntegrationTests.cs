using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Orient.MediatR.SampleApi;
using Orient.MediatR.SampleApi.Features.Auth;
using Orient.MediatR.SampleApi.Features.Users;

namespace Orient.MediatR.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<ApiApplicationEntry>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<ApiApplicationEntry> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUser_ShouldReturnUserFromScopedRepository_WhenIdIsPositive()
    {
        var user = await _client.GetFromJsonAsync<UserDto>("/user/42");

        Assert.Equal(new UserDto(42, "Grace Hopper", "user42@example.com"), user);
    }

    [Fact]
    public async Task GetUser_ShouldReturnNotFound_WhenRepositoryReturnsNull()
    {
        var response = await _client.GetAsync("/user/0");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_ShouldReturnMatchingUsers_WhenCalledConcurrently()
    {
        var ids = Enumerable.Range(1, 50).ToArray();

        var users = await Task.WhenAll(ids.Select(id => _client.GetFromJsonAsync<UserDto>($"/user/{id}")));

        Assert.Equal(ids, users.Select(u => u!.Id));
    }

    [Fact]
    public async Task StreamUsers_ShouldReturnRequestedCount_WhenCountProvided()
    {
        var users = await _client.GetFromJsonAsync<List<UserDto>>("/users/stream?count=3");

        Assert.Equal([1, 2, 3], users!.Select(u => u.Id));
    }

    [Fact]
    public async Task Login_ShouldReturnToken_WhenCredentialsProvided()
    {
        var response = await _client.PostAsJsonAsync("/login", new { email = "qa@example.com", password = "secret" });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResultDto>();
        Assert.True(result!.Success);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordEmpty()
    {
        var response = await _client.PostAsJsonAsync("/login", new { email = "qa@example.com", password = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenBodyMissing()
    {
        var response = await _client.PostAsync("/login", new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NotifyUserCreated_ShouldReturnAccepted_WhenNotificationPublished()
    {
        var response = await _client.PostAsync("/users/7/created?email=qa@example.com", null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_ShouldListEndpoints_WhenRunningInDevelopment()
    {
        var document = await _client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("/user/{id}", document);
        Assert.Contains("/users/stream", document);
    }
}
