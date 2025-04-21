```

BenchmarkDotNet v0.13.12, macOS 15.3.2 (24D81) [Darwin 24.3.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 8.0.404
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD


```
| Method      | Mean | Error |
|------------ |-----:|------:|
| GetUserById |   NA |    NA |
| LoginUser   |   NA |    NA |
| GetMetrics  |   NA |    NA |
| StreamUsers |   NA |    NA |

Benchmarks with issues:
  ApiBenchmarks.GetUserById: DefaultJob
  ApiBenchmarks.LoginUser: DefaultJob
  ApiBenchmarks.GetMetrics: DefaultJob
  ApiBenchmarks.StreamUsers: DefaultJob
