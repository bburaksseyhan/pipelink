```

BenchmarkDotNet v0.13.12, macOS 15.3.2 (24D81) [Darwin 24.3.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 8.0.404
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD


```
| Method                     | Mean        | Error     | StdDev      | Gen0   | Allocated |
|--------------------------- |------------:|----------:|------------:|-------:|----------:|
| Send_SimpleRequest         | 21,982.6 ns | 558.20 ns | 1,601.57 ns | 0.1831 |    1688 B |
| Publish_SimpleNotification |    112.6 ns |   0.50 ns |     0.42 ns | 0.0545 |     456 B |
| Send_RequestWithBehavior   | 20,199.6 ns | 402.66 ns |   672.75 ns | 0.1831 |    1688 B |
