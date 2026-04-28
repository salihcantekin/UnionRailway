```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
11th Gen Intel Core i7-1165G7 2.80GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-BLBIKZ : .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=3  

```
| Method                           | Mean        | Error     | StdDev    | Median      | Gen0   | Allocated |
|--------------------------------- |------------:|----------:|----------:|------------:|-------:|----------:|
| &#39;Create success Rail&lt;int&gt;&#39;       |   0.7831 ns | 0.1836 ns | 0.1215 ns |   0.7958 ns |      - |         - |
| &#39;Create failure Rail&lt;int&gt;&#39;       |   4.4533 ns | 0.4278 ns | 0.2546 ns |   4.3507 ns | 0.0038 |      24 B |
| &#39;Create via implicit conversion&#39; |   0.0414 ns | 0.0676 ns | 0.0402 ns |   0.0180 ns |      - |         - |
| &#39;IsSuccess pattern&#39;              |   0.3009 ns | 0.0620 ns | 0.0369 ns |   0.2922 ns |      - |         - |
| &#39;Match pattern&#39;                  |   1.3663 ns | 0.0562 ns | 0.0372 ns |   1.3738 ns |      - |         - |
| &#39;TryGetValue pattern&#39;            |   0.1051 ns | 0.1248 ns | 0.0825 ns |   0.0680 ns |      - |         - |
| &#39;Map operation (success path)&#39;   |   1.5100 ns | 0.3766 ns | 0.2241 ns |   1.4062 ns |      - |         - |
| &#39;Map operation (error path)&#39;     |   5.6356 ns | 0.7622 ns | 0.3987 ns |   5.4671 ns | 0.0038 |      24 B |
| &#39;Bind operation (success path)&#39;  |  33.7535 ns | 1.0435 ns | 0.6902 ns |  33.7606 ns | 0.0063 |      40 B |
| &#39;Bind operation (error path)&#39;    |   6.2847 ns | 0.2907 ns | 0.1730 ns |   6.2648 ns | 0.0038 |      24 B |
| &#39;Chain multiple Map operations&#39;  |   7.1856 ns | 0.1987 ns | 0.1182 ns |   7.1626 ns |      - |         - |
| &#39;Chain multiple Bind operations&#39; |   6.1698 ns | 0.3673 ns | 0.2430 ns |   6.0616 ns |      - |         - |
| &#39;MapAsync operation&#39;             |  47.7163 ns | 1.3345 ns | 0.7941 ns |  47.5871 ns |      - |         - |
| &#39;BindAsync operation&#39;            | 100.2995 ns | 1.1389 ns | 0.6777 ns | 100.1852 ns | 0.0063 |      40 B |
| &#39;Create NotFound error&#39;          |   1.8112 ns | 0.0528 ns | 0.0314 ns |   1.8047 ns | 0.0038 |      24 B |
| &#39;Create Validation error&#39;        | 102.7516 ns | 2.6425 ns | 1.5725 ns | 102.7728 ns | 0.0739 |     464 B |
| &#39;Pattern match UnionError&#39;       |   8.8884 ns | 0.3121 ns | 0.1632 ns |   8.8467 ns | 0.0127 |      80 B |
| &#39;Simulate service call chain&#39;    |  19.9339 ns | 0.5486 ns | 0.3265 ns |  19.8439 ns | 0.0191 |     120 B |
| &#39;Error handling scenario&#39;        |   4.6067 ns | 0.0816 ns | 0.0486 ns |   4.5995 ns | 0.0038 |      24 B |
