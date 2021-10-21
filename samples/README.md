# Samples

### Column Display Samples

<details>
  <summary>Sample Output</summary>

```
// ***** Samples Overview *****

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19042.1237 (20H2/October2020Update), VM=VirtualBox
AMD Ryzen 5 3400G with Radeon Vega Graphics, 1 CPU, 4 logical and 4 physical cores
.NET SDK=5.0.401
  [Host] : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
```
```
// *** Job Column Sample 1: Jobs with different run characteristics ***

// with BenchmarkDotNet defaults

Toolchain=InProcessEmitToolchain  

|  Method |        Job | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount |       Mean |     Error |    StdDev |
|-------- |----------- |--------------- |------------ |------------ |------------- |------------ |-----------:|----------:|----------:|
| Method1 | Job-QYOJAY |        Default |     Default |     Default |           16 |     Default |   1.701 μs | 0.0221 μs | 0.0206 μs |
| Method2 | Job-QYOJAY |        Default |     Default |     Default |           16 |     Default |   1.689 μs | 0.0075 μs | 0.0063 μs |
| Method1 |        Dry |              1 |           1 |   ColdStart |            1 |           1 | 255.200 μs |        NA | 0.0000 μs |
| Method2 |        Dry |              1 |           1 |   ColdStart |            1 |           1 | 239.700 μs |        NA | 0.0000 μs |

  Mean   : Arithmetic mean of all measurements
  Error  : Half of 99.9% confidence interval
  StdDev : Standard deviation of all measurements
  1 μs   : 1 Microsecond (0.000001 sec)
```
```
// with JobColumnSelectionProvider("-all +Job", showHiddenValuesInLegend: true)

Toolchain=InProcessEmitToolchain  

|  Method |        Job |       Mean |     Error |    StdDev |
|-------- |----------- |-----------:|----------:|----------:|
| Method1 | Job-QYOJAY |   1.700 μs | 0.0100 μs | 0.0094 μs |
| Method2 | Job-QYOJAY |   1.688 μs | 0.0100 μs | 0.0094 μs |
| Method1 |        Dry | 241.300 μs |        NA | 0.0000 μs |
| Method2 |        Dry | 203.100 μs |        NA | 0.0000 μs |

  Job    : Job name. Some job columns have been hidden:
    Job-QYOJAY: IterationCount=Default, LaunchCount=Default, RunStrategy=Default, UnrollFactor=16, WarmupCount=Default
           Dry: IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1
  Mean   : Arithmetic mean of all measurements
  Error  : Half of 99.9% confidence interval
  StdDev : Standard deviation of all measurements
  1 μs   : 1 Microsecond (0.000001 sec)
```
```
// *** Job Column Sample 2: Jobs with different target frameworks ***

  Job-NNZHLG : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
  Job-JFNSKS : .NET Core 3.1.19 (CoreCLR 4.700.21.41101, CoreFX 4.700.21.41603), X64 RyuJIT
  Job-DUPVIG : .NET Framework 4.8 (4.8.4400.0), X64 RyuJIT

// with BenchmarkDotNet defaults

IterationCount=1  LaunchCount=1  RunStrategy=ColdStart  
UnrollFactor=1  WarmupCount=1  

|  Method |        Job |            Runtime |     Toolchain |     Mean | Error | Ratio |
|-------- |----------- |------------------- |-------------- |---------:|------:|------:|
| Method1 | Job-YMVNBK |           .NET 5.0 |        net5.0 | 358.1 μs |    NA |  1.05 |
| Method1 | Job-QHJRYG |      .NET Core 3.1 | netcoreapp3.1 | 350.5 μs |    NA |  1.02 |
| Method1 | Job-BDEBJQ | .NET Framework 4.8 |         net48 | 342.4 μs |    NA |  1.00 |
|         |            |                    |               |          |       |       |
| Method2 | Job-YMVNBK |           .NET 5.0 |        net5.0 | 332.9 μs |    NA |  0.65 |
| Method2 | Job-QHJRYG |      .NET Core 3.1 | netcoreapp3.1 | 332.5 μs |    NA |  0.65 |
| Method2 | Job-BDEBJQ | .NET Framework 4.8 |         net48 | 512.5 μs |    NA |  1.00 |

  Mean  : Arithmetic mean of all measurements
  Error : Half of 99.9% confidence interval
  Ratio : Mean of the ratio distribution ([Current]/[Baseline])
  1 μs  : 1 Microsecond (0.000001 sec)
```
```
// with JobColumnSelectionProvider("-Job -Toolchain", showHiddenValuesInLegend: false)

IterationCount=1  LaunchCount=1  RunStrategy=ColdStart  
UnrollFactor=1  WarmupCount=1  

|  Method |            Runtime |     Mean | Error | Ratio |
|-------- |------------------- |---------:|------:|------:|
| Method1 |           .NET 5.0 | 435.8 μs |    NA |  1.28 |
| Method1 |      .NET Core 3.1 | 328.7 μs |    NA |  0.96 |
| Method1 | .NET Framework 4.8 | 340.7 μs |    NA |  1.00 |
|         |                    |          |       |       |
| Method2 |           .NET 5.0 | 385.9 μs |    NA |  1.15 |
| Method2 |      .NET Core 3.1 | 451.9 μs |    NA |  1.34 |
| Method2 | .NET Framework 4.8 | 336.2 μs |    NA |  1.00 |

  Mean  : Arithmetic mean of all measurements
  Error : Half of 99.9% confidence interval
  Ratio : Mean of the ratio distribution ([Current]/[Baseline])
  1 μs  : 1 Microsecond (0.000001 sec)
```
```
// *** Param Column Sample ***

// with BenchmarkDotNet defaults

Job=Dry  Toolchain=InProcessEmitToolchain  IterationCount=1  
LaunchCount=1  RunStrategy=ColdStart  UnrollFactor=1  
WarmupCount=1  

|  Method |  fooArg |  barArg |  bazArg |  buzzArg |     Mean | Error |
|-------- |-------- |-------- |-------- |--------- |---------:|------:|
| Method1 | fooval1 | barval1 |       ? |        ? | 410.8 μs |    NA |
| Method2 | fooval1 |       ? | bazval1 |        ? | 325.0 μs |    NA |
| Method3 | fooval1 |       ? |       ? | buzzval1 | 341.0 μs |    NA |
| Method1 | fooval2 | barval2 |       ? |        ? | 268.8 μs |    NA |
| Method2 | fooval2 |       ? | bazval2 |        ? | 310.9 μs |    NA |
| Method3 | fooval2 |       ? |       ? | buzzval2 | 414.2 μs |    NA |

  fooArg  : Value of the 'fooArg' parameter
  barArg  : Value of the 'barArg' parameter
  bazArg  : Value of the 'bazArg' parameter
  buzzArg : Value of the 'buzzArg' parameter
  Mean    : Arithmetic mean of all measurements
  Error   : Half of 99.9% confidence interval
  1 μs    : 1 Microsecond (0.000001 sec)
```
```
// with CombinedParamsColumn() // default formatting

Job=Dry  Toolchain=InProcessEmitToolchain  IterationCount=1  
LaunchCount=1  RunStrategy=ColdStart  UnrollFactor=1  
WarmupCount=1  

|  Method |                           Params |     Mean | Error |
|-------- |--------------------------------- |---------:|------:|
| Method1 |   fooArg=fooval1, barArg=barval1 | 382.9 μs |    NA |
| Method2 |   fooArg=fooval1, bazArg=bazval1 | 255.3 μs |    NA |
| Method3 | fooArg=fooval1, buzzArg=buzzval1 | 255.8 μs |    NA |
| Method1 |   fooArg=fooval2, barArg=barval2 | 262.6 μs |    NA |
| Method2 |   fooArg=fooval2, bazArg=bazval2 | 268.5 μs |    NA |
| Method3 | fooArg=fooval2, buzzArg=buzzval2 | 256.6 μs |    NA |

  Params : All parameter values
  Mean   : Arithmetic mean of all measurements
  Error  : Half of 99.9% confidence interval
  1 μs   : 1 Microsecond (0.000001 sec)
```
```
// with CombinedParamsColumn(formatNameValue: "{1}", separator: "; ")

Job=Dry  Toolchain=InProcessEmitToolchain  IterationCount=1  
LaunchCount=1  RunStrategy=ColdStart  UnrollFactor=1  
WarmupCount=1  

|  Method |            Params |     Mean | Error |
|-------- |------------------ |---------:|------:|
| Method1 |  fooval1; barval1 | 272.2 μs |    NA |
| Method2 |  fooval1; bazval1 | 272.0 μs |    NA |
| Method3 | fooval1; buzzval1 | 280.2 μs |    NA |
| Method1 |  fooval2; barval2 | 255.9 μs |    NA |
| Method2 |  fooval2; bazval2 | 371.8 μs |    NA |
| Method3 | fooval2; buzzval2 | 267.3 μs |    NA |

  Params : All parameter values
  Mean   : Arithmetic mean of all measurements
  Error  : Half of 99.9% confidence interval
  1 μs   : 1 Microsecond (0.000001 sec)
```
```
// with RecyclableParamsColumnProvider() // default settings

Job=Dry  Toolchain=InProcessEmitToolchain  IterationCount=1  
LaunchCount=1  RunStrategy=ColdStart  UnrollFactor=1  
WarmupCount=1  

|  Method |  fooArg |   Param2 |     Mean | Error |
|-------- |-------- |--------- |---------:|------:|
| Method1 | fooval1 |  barval1 | 244.4 μs |    NA |
| Method2 | fooval1 |  bazval1 | 223.3 μs |    NA |
| Method3 | fooval1 | buzzval1 | 238.0 μs |    NA |
| Method1 | fooval2 |  barval2 | 329.4 μs |    NA |
| Method2 | fooval2 |  bazval2 | 209.0 μs |    NA |
| Method3 | fooval2 | buzzval2 | 238.0 μs |    NA |

  fooArg : Value of the 'fooArg' parameter
  Param2 : Value of the parameter at position 2
  Mean   : Arithmetic mean of all measurements
  Error  : Half of 99.9% confidence interval
  1 μs   : 1 Microsecond (0.000001 sec)
```
```
// with RecyclableParamsColumnProvider(tryKeepParamName: false)

Job=Dry  Toolchain=InProcessEmitToolchain  IterationCount=1  
LaunchCount=1  RunStrategy=ColdStart  UnrollFactor=1  
WarmupCount=1  

|  Method |  Param1 |   Param2 |     Mean | Error |
|-------- |-------- |--------- |---------:|------:|
| Method1 | fooval1 |  barval1 | 270.3 μs |    NA |
| Method2 | fooval1 |  bazval1 | 318.1 μs |    NA |
| Method3 | fooval1 | buzzval1 | 278.5 μs |    NA |
| Method1 | fooval2 |  barval2 | 258.6 μs |    NA |
| Method2 | fooval2 |  bazval2 | 318.4 μs |    NA |
| Method3 | fooval2 | buzzval2 | 304.9 μs |    NA |

  Param1 : Value of the parameter at position 1
  Param2 : Value of the parameter at position 2
  Mean   : Arithmetic mean of all measurements
  Error  : Half of 99.9% confidence interval
  1 μs   : 1 Microsecond (0.000001 sec)
```
```
// *** ParamWrapper Sample ***

Job=Dry  Toolchain=InProcessEmitToolchain  IterationCount=1  
LaunchCount=1  RunStrategy=ColdStart  UnrollFactor=1  
WarmupCount=1  

|     Method |                  input |     Mean | Error |
|----------- |----------------------- |---------:|------:|
| NotWrapped | System.IO.MemoryStream | 452.0 μs |    NA |
| NotWrapped | System.IO.MemoryStream | 203.7 μs |    NA |
|    Wrapped |             big stream | 449.4 μs |    NA |
|    Wrapped |           small stream | 213.8 μs |    NA |

  input : Value of the 'input' parameter
  Mean  : Arithmetic mean of all measurements
  Error : Half of 99.9% confidence interval
  1 μs  : 1 Microsecond (0.000001 sec)
```
</details>

### What-If Filter Sample

<details>
  <summary>Sample Output</summary>

```
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

// ***** Addendum: BenchmarkDotNet --list option for comparison *****

Console arguments: --runtimes net5.0 netcoreapp31 net48 --list flat

WhatifFilterSample.Benchmarks1.Method1
WhatifFilterSample.Benchmarks1.Method2
WhatifFilterSample.Benchmarks1.Method3
WhatifFilterSample.Benchmarks2.Method3
WhatifFilterSample.Benchmarks2.Method1
WhatifFilterSample.Benchmarks2.Method2
WhatifFilterSample.Benchmarks3.Method1
WhatifFilterSample.Benchmarks3.Method2

Console arguments: --runtimes net5.0 netcoreapp31 net48 --list tree

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
```

</details>

### BenchmarkRunInfos Sample

Coming soon.

### ConfigExtensions Sample

Coming soon.
