```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.3.2 (24D81) [Darwin 24.3.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 8.0.404
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  Job-IUVRCM : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD

IterationCount=10  LaunchCount=5  WarmupCount=3  

```
| Method        | Mean     | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|-------------- |---------:|---------:|---------:|------:|-------:|----------:|------------:|
| MediatR_Send  | 83.13 ns | 0.333 ns | 0.642 ns |  1.00 | 0.0459 |     384 B |        1.00 |
| Pipelink_Send | 39.10 ns | 0.179 ns | 0.340 ns |  0.47 | 0.0334 |     280 B |        0.73 |
