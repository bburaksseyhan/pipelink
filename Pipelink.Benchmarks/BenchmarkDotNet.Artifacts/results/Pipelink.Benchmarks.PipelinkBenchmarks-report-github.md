```

BenchmarkDotNet v0.13.12, macOS 15.3.2 (24D81) [Darwin 24.3.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 8.0.404
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD


```
| Method                     | Mean         | Error      | StdDev     | Gen0    | Allocated |
|--------------------------- |-------------:|-----------:|-----------:|--------:|----------:|
| Send_SimpleRequest         |     60.97 ns |   0.117 ns |   0.097 ns |  0.0353 |     296 B |
| Publish_SimpleNotification |    297.73 ns |   1.222 ns |   1.200 ns |  0.0334 |     280 B |
| Send_RequestWithBehavior   |     53.92 ns |   0.407 ns |   0.340 ns |  0.0353 |     296 B |
| SendStream_SimpleRequest   | 52,769.82 ns | 244.721 ns | 228.912 ns | 15.5640 |  130536 B |
