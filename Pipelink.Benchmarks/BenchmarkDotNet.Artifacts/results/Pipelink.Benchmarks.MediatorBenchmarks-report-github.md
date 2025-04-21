```

BenchmarkDotNet v0.13.12, macOS 15.3.2 (24D81) [Darwin 24.3.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 8.0.404
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  Job-MYOBEA : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD

IterationCount=10  LaunchCount=5  WarmupCount=3  

```
| Method        | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| MediatR_Send  | 91.89 ns | 4.088 ns | 7.875 ns |  1.00 |    0.00 | 0.0459 |     384 B |        1.00 |
| Pipelink_Send | 38.66 ns | 0.351 ns | 0.685 ns |  0.42 |    0.04 | 0.0334 |     280 B |        0.73 |
