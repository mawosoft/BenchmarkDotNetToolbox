# [--whatif](#tab/tabid-1)
<pre>
// * What If Summary *

Console arguments: --runtimes net5.0 netcoreapp31 net48 --whatif

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19042.1237 (20H2/October2020Update), VM=VirtualBox
AMD Ryzen 5 3400G with Radeon Vega Graphics, 1 CPU, 4 logical and 4 physical cores
.NET SDK=5.0.401
  [Host] : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT


// Benchmarks1

IterationCount=3  LaunchCount=1  WarmupCount=3

|  Method |        Job |            Runtime |    Toolchain |     Mean | Error | Ratio |
|-------- |----------- |------------------- |------------- |---------:|------:|------:|
| Method1 | Job-PFBCDR |           .NET 5.0 |       net5.0 | 102.0 ms |    NA |  1.00 |
| Method2 | Job-PFBCDR |           .NET 5.0 |       net5.0 | 105.0 ms |    NA |  1.03 |
| Method3 | Job-PFBCDR |           .NET 5.0 |       net5.0 | 108.0 ms |    NA |  1.06 |
| Method1 | Job-GTPATI |      .NET Core 3.1 | netcoreapp31 | 100.0 ms |    NA |  0.98 |
| Method2 | Job-GTPATI |      .NET Core 3.1 | netcoreapp31 | 103.0 ms |    NA |  1.01 |
| Method3 | Job-GTPATI |      .NET Core 3.1 | netcoreapp31 | 106.0 ms |    NA |  1.04 |
| Method1 | Job-LLNYBD | .NET Framework 4.8 |        net48 | 101.0 ms |    NA |  0.99 |
| Method2 | Job-LLNYBD | .NET Framework 4.8 |        net48 | 104.0 ms |    NA |  1.02 |
| Method3 | Job-LLNYBD | .NET Framework 4.8 |        net48 | 107.0 ms |    NA |  1.05 |
</pre>
<pre>
// Benchmarks2

IterationCount=3  LaunchCount=1  WarmupCount=3

|  Method |        Job |            Runtime |    Toolchain | Prop1 | next |     Mean | Error | Ratio |
|-------- |----------- |------------------- |------------- |------ |----- |---------:|------:|------:|
| Method3 | Job-PFBCDR |           .NET 5.0 |       net5.0 | False |   44 | 125.0 ms |    NA |  1.00 |
| Method3 | Job-GTPATI |      .NET Core 3.1 | netcoreapp31 | False |   44 | 121.0 ms |    NA |  0.97 |
| Method3 | Job-LLNYBD | .NET Framework 4.8 |        net48 | False |   44 | 123.0 ms |    NA |  0.98 |
|         |            |                    |              |       |      |          |       |       |
| Method1 | Job-PFBCDR |           .NET 5.0 |       net5.0 | False |    ? | 113.0 ms |    NA |  1.00 |
| Method1 | Job-GTPATI |      .NET Core 3.1 | netcoreapp31 | False |    ? | 109.0 ms |    NA |  0.96 |
| Method1 | Job-LLNYBD | .NET Framework 4.8 |        net48 | False |    ? | 111.0 ms |    NA |  0.98 |
|         |            |                    |              |       |      |          |       |       |
| Method2 | Job-PFBCDR |           .NET 5.0 |       net5.0 | False |    ? | 119.0 ms |    NA |  1.00 |
| Method2 | Job-GTPATI |      .NET Core 3.1 | netcoreapp31 | False |    ? | 115.0 ms |    NA |  0.97 |
| Method2 | Job-LLNYBD | .NET Framework 4.8 |        net48 | False |    ? | 117.0 ms |    NA |  0.98 |
|         |            |                    |              |       |      |          |       |       |
| Method3 | Job-PFBCDR |           .NET 5.0 |       net5.0 |  True |   44 | 126.0 ms |    NA |  1.00 |
| Method3 | Job-GTPATI |      .NET Core 3.1 | netcoreapp31 |  True |   44 | 122.0 ms |    NA |  0.97 |
| Method3 | Job-LLNYBD | .NET Framework 4.8 |        net48 |  True |   44 | 124.0 ms |    NA |  0.98 |
|         |            |                    |              |       |      |          |       |       |
| Method1 | Job-PFBCDR |           .NET 5.0 |       net5.0 |  True |    ? | 114.0 ms |    NA |  1.00 |
| Method1 | Job-GTPATI |      .NET Core 3.1 | netcoreapp31 |  True |    ? | 110.0 ms |    NA |  0.96 |
| Method1 | Job-LLNYBD | .NET Framework 4.8 |        net48 |  True |    ? | 112.0 ms |    NA |  0.98 |
|         |            |                    |              |       |      |          |       |       |
| Method2 | Job-PFBCDR |           .NET 5.0 |       net5.0 |  True |    ? | 120.0 ms |    NA |  1.00 |
| Method2 | Job-GTPATI |      .NET Core 3.1 | netcoreapp31 |  True |    ? | 116.0 ms |    NA |  0.97 |
| Method2 | Job-LLNYBD | .NET Framework 4.8 |        net48 |  True |    ? | 118.0 ms |    NA |  0.98 |
</pre>
<pre>
// Benchmarks3

LaunchCount=1

|  Method |        Job |            Runtime |    Toolchain | IterationCount | RunStrategy | UnrollFactor | WarmupCount |     Mean | Error | Ratio |
|-------- |----------- |------------------- |------------- |--------------- |------------ |------------- |------------ |---------:|------:|------:|
| Method1 | Job-PFBCDR |           .NET 5.0 |       net5.0 |              3 |     Default |           16 |           3 | 129.0 ms |    NA |  1.00 |
| Method1 | Job-GTPATI |      .NET Core 3.1 | netcoreapp31 |              3 |     Default |           16 |           3 | 127.0 ms |    NA |  0.98 |
| Method1 | Job-LLNYBD | .NET Framework 4.8 |        net48 |              3 |     Default |           16 |           3 | 128.0 ms |    NA |  0.99 |
| Method1 |        Dry |           .NET 5.0 |      Default |              1 |   ColdStart |            1 |           1 | 130.0 ms |    NA |  1.01 |
|         |            |                    |              |                |             |              |             |          |       |       |
| Method2 | Job-PFBCDR |           .NET 5.0 |       net5.0 |              3 |     Default |           16 |           3 | 133.0 ms |    NA |  1.00 |
| Method2 | Job-GTPATI |      .NET Core 3.1 | netcoreapp31 |              3 |     Default |           16 |           3 | 131.0 ms |    NA |  0.98 |
| Method2 | Job-LLNYBD | .NET Framework 4.8 |        net48 |              3 |     Default |           16 |           3 | 132.0 ms |    NA |  0.99 |
| Method2 |        Dry |           .NET 5.0 |      Default |              1 |   ColdStart |            1 |           1 | 134.0 ms |    NA |  1.01 |
</pre>
# [--list flat](#tab/tabid-2)
Console arguments: --runtimes net5.0 netcoreapp31 net48 --list flat
<pre>
WhatifFilterSample.Benchmarks1.Method1
WhatifFilterSample.Benchmarks1.Method2
WhatifFilterSample.Benchmarks1.Method3
WhatifFilterSample.Benchmarks2.Method3
WhatifFilterSample.Benchmarks2.Method1
WhatifFilterSample.Benchmarks2.Method2
WhatifFilterSample.Benchmarks3.Method1
WhatifFilterSample.Benchmarks3.Method2
</pre>
# [--list tree](#tab/tabid-3)
Console arguments: --runtimes net5.0 netcoreapp31 net48 --list tree
<pre>
WhatifFilterSample
 ├─Benchmarks1
 │  ├─Method1
 │  ├─Method2
 │  └─Method3
 ├─Benchmarks2
 │  ├─Method3
 │  ├─Method1
 │  └─Method2
 └─Benchmarks3
    ├─Method1
    └─Method2
</pre>
***
