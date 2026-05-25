```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8246/25H2/2025Update/HudsonValley2)
Intel Core i9-10885H CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method       | Count | Mean       | Error    | StdDev   | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------- |------ |-----------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| NativeDivide | 1000  |   797.6 ns |  9.72 ns |  7.59 ns |  1.00 |    0.01 |      62 B |         - |          NA |
| FastDivide   | 1000  |   484.8 ns |  7.42 ns |  6.94 ns |  0.61 |    0.01 |      61 B |         - |          NA |
| NativeModulo | 1000  | 1,013.0 ns | 19.50 ns | 19.15 ns |  1.27 |    0.03 |      74 B |         - |          NA |
| FastModulo   | 1000  |   690.2 ns | 10.46 ns | 16.59 ns |  0.87 |    0.02 |      60 B |         - |          NA |
