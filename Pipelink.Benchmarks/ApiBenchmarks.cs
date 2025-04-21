using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using PipelinkTest.Api.Dtos;
using Pipelink.Models;

namespace Pipelink.Benchmarks
{
    [MemoryDiagnoser]
    public class ApiBenchmarks
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl = "http://localhost:5000";

        public ApiBenchmarks()
        {
            _client = new HttpClient();
        }

        [Benchmark]
        public async Task GetUserById()
        {
            var response = await _client.GetAsync($"{_baseUrl}/user/1");
            response.EnsureSuccessStatusCode();
        }

        [Benchmark]
        public async Task LoginUser()
        {
            var loginDto = new LoginUserDto
            {
                Username = "testuser",
                Password = "testpassword"
            };
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/login", loginDto);
            response.EnsureSuccessStatusCode();
        }

        [Benchmark]
        public async Task GetMetrics()
        {
            var response = await _client.GetAsync($"{_baseUrl}/metrics");
            response.EnsureSuccessStatusCode();
        }

        [Benchmark]
        public async Task StreamUsers()
        {
            var response = await _client.GetAsync($"{_baseUrl}/users/stream");
            response.EnsureSuccessStatusCode();
        }
    }
} 