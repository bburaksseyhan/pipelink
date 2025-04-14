```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.3.2 (24D81) [Darwin 24.3.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 8.0.404
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD


```
| Method        | Mean        | Error      | StdDev     |
|-------------- |------------:|-----------:|-----------:|
| MediatR_Send  |    79.37 ns |   1.515 ns |   3.325 ns |
| Pipelink_Send | 7,434.62 ns | 358.445 ns | 993.250 ns |
