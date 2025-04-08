```

BenchmarkDotNet v0.13.12, macOS 15.3.2 (24D81) [Darwin 24.3.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 8.0.404
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD


```
| Method                     | Mean     | Error   | StdDev  | Gen0   | Allocated |
|--------------------------- |---------:|--------:|--------:|-------:|----------:|
| Send_SimpleRequest         |       NA |      NA |      NA |     NA |        NA |
| Publish_SimpleNotification | 259.3 ns | 5.20 ns | 9.50 ns | 0.0334 |     280 B |
| Send_RequestWithBehavior   |       NA |      NA |      NA |     NA |        NA |
| SendStream_SimpleRequest   | 320.4 ns | 1.84 ns | 1.63 ns | 0.1135 |     952 B |

Benchmarks with issues:
  PipelinkBenchmarks.Send_SimpleRequest: DefaultJob
  PipelinkBenchmarks.Send_RequestWithBehavior: DefaultJob
