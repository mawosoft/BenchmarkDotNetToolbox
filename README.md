# BenchmarkDotNet Toolbox

An assortment of classes to make benchmarking with [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) even easier.

## Job Column Selection

```C#
public class JobColumnSelectionProvider : IColumnProvider {
    public JobColumnSelectionProvider(string filterExpression, bool showHiddenValuesInLegend = true);
}
```

By default, Job columns can take up quite a bit of space in the summary. If you run a `DryJob` and a `ShortRunJob`, the name is usually enough to identify them, but you will still get a lot of XxxCount columns to describe their respective details. The `JobColumnSelectionProvider` class offers a remedy for that:

```C#
ManualConfig config;
// ...
config.AddColumnProvider(new JobColumnSelectionProvider("-all +Job", showHiddenValuesInLegend: true));
```

<details>
  <summary>Parameters (click to expand)</summary>
  
| Parameter | Description |
|-----------|-------------|
| filterExpression         | A space-separated list of job column or category names, prefixed with `-` or `+` to exclude or include them. Initially, all columns are visible, just like with the default provider.<br>The filter expressions are processed sequentially, e.g. `-All +Job` will first hide all columns and then unhide the Job name column, and `-Run +RunStrategy` will hide the columns of the Run category and then unhide RunStrategy. The alias `All` refers to all columns, and both `Job` and `Id` can be used for the Job name column.<br>Categories can be specified either by their name or type; e.g. `Run` and `RunMode` are equivalent. Available categories are: `Environment`, `Gc`, `Run`, `Infrastructure`, `Accuracy`. See the [BenchmarkDotNet documentation](https://benchmarkdotnet.org/articles/configs/jobs.html) for details. |
| showHiddenValuesInLegend | True to include a compact display of hidden values as legend, false if not. |
</details>

<details>
  <summary>Associated classes</summary>
  
* `JobCharacteristicColumnWithLegend`, `JobColumnSelectionProvider`
</details>

<details>
  <summary>Sample output</summary>
  
BenchmarkDotNet's default output (`DefaultColumnProviders.Job`):
```
.NET SDK=5.0.400
  [Host] : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT DEBUG  [AttachedDebugger]

Toolchain=InProcessEmitToolchain

|  Method |        Job | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount |       Mean |     Error |    StdDev |
|-------- |----------- |--------------- |------------ |------------ |------------- |------------ |-----------:|----------:|----------:|
| Method1 | Job-IABCBG |        Default |     Default |     Default |           16 |     Default |   1.619 탎 | 0.0074 탎 | 0.0069 탎 |
| Method2 | Job-IABCBG |        Default |     Default |     Default |           16 |     Default |   3.259 탎 | 0.0204 탎 | 0.0181 탎 |
| Method1 |        Dry |              1 |           1 |   ColdStart |            1 |           1 | 190.000 탎 |        NA | 0.0000 탎 |
| Method2 |        Dry |              1 |           1 |   ColdStart |            1 |           1 | 192.800 탎 |        NA | 0.0000 탎 |

// * Legends *
  Mean   : Arithmetic mean of all measurements
  Error  : Half of 99.9% confidence interval
  StdDev : Standard deviation of all measurements
  1 탎   : 1 Microsecond (0.000001 sec)
```
With `JobColumnSelectionProvider`:
```
.NET SDK=5.0.400
  [Host] : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT DEBUG  [AttachedDebugger]

Toolchain=InProcessEmitToolchain

|  Method |        Job |       Mean |     Error |    StdDev |
|-------- |----------- |-----------:|----------:|----------:|
| Method1 | Job-IABCBG |   1.627 탎 | 0.0106 탎 | 0.0094 탎 |
| Method2 | Job-IABCBG |   3.310 탎 | 0.0595 탎 | 0.0557 탎 |
| Method1 |        Dry | 186.800 탎 |        NA | 0.0000 탎 |
| Method2 |        Dry | 209.500 탎 |        NA | 0.0000 탎 |

// * Legends *
  Job    : Job name. Some job columns have been hidden:
    Job-IABCBG: IterationCount=Default, LaunchCount=Default, RunStrategy=Default, UnrollFactor=16, WarmupCount=Default
           Dry: IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1
  Mean   : Arithmetic mean of all measurements
  Error  : Half of 99.9% confidence interval
  StdDev : Standard deviation of all measurements
  1 탎   : 1 Microsecond (0.000001 sec)
```
</details>

### To Be Continued...
