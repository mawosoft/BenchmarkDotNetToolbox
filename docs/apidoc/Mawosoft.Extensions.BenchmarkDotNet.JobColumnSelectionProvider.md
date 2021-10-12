---
uid: Mawosoft.BenchmarkDotNetToolbox.JobColumnSelectionProvider
summary: An alternative to @"BenchmarkDotNet.Columns.DefaultColumnProviders.Job?displayProperty=nameWithType", with a user-defined selection of Job columns.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.JobColumnSelectionProvider.#ctor(System.String,System.Boolean)
syntax:
    parameters:
    - id: filterExpression
      description: |
        A space-separated list of job column or category names, prefixed with `-` or `+` to exclude or include them.
        See remarks for details.
    - id: showHiddenValuesInLegend
      description: |
        `true` to include a compact display of hidden values as legend, `false` if not.
        The default is `true`.
example:
- *content
seealso:
- linkType: HRef
  linkId: https://github.com/mawosoft/BenchmarkDotNetToolbox/tree/master/samples
  altText: Column Display Samples on GitHub
---

Use @"BenchmarkDotNet.Configs.ManualConfig.AddColumnProvider(BenchmarkDotNet.Columns.IColumnProvider[])?text=AddColumnProvider()"
if you are building a config from scratch.

```csharp
ManualConfig config = ManualConfig.CreateEmpty()
    // Add the DefaultColumnProviders you need except DefaultColumnProviders.Job
    .AddColumnProvider(DefaultColumnProviders.Descriptor /* add more... */)
    // Add a new JobColumnSelectionProvider that will show only the job name
    // and use the legend for hidden, non-common values.
    .AddColumnProvider(new JobColumnSelectionProvider("-all +Job", true));
    // Add other elements to the config...
```

If you are modifying an existing config, use
@"Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceColumnCategory(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Columns.IColumnProvider[])?text=ReplaceColumnCategory()",
one of the new config extension methods in *BenchmarkDotNetToolbox*.

```csharp
ManualConfig config = ManualConfig.Create(DefaultConfig.Instance)
    // Replace the default job columns with a new JobColumnSelectionProvider
    .ReplaceColumnCategory(new JobColumnSelectionProvider("-all +Job", true));
    // Make other changes to the config...
```

If you run otherwise identical jobs with different target frameworks, you can remove redundant columns and don't
need their values to be displayed in the legend.

```csharp
new JobColumnSelectionProvider("-Job -Toolchain", showHiddenValuesInLegend: false)
```

##### Sample Output

##### - Jobs with different Run characteristics

# ["-all +Job"](#tab/tabid-1)
<pre>
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
</pre>
# [BenchmarkDotNet Defaults](#tab/tabid-2)
<pre>
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
</pre>
***

##### - Jobs with different target frameworks

# ["-Job -Toolchain"](#tab/tabid-3)
<pre>
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
</pre>
# [BenchmarkDotNet Defaults](#tab/tabid-4)
<pre>
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
</pre>
***

---
uid: Mawosoft.BenchmarkDotNetToolbox.JobColumnSelectionProvider.GetColumns(BenchmarkDotNet.Reports.Summary)
summary: '@"BenchmarkDotNet.Columns.IColumnProvider" implementation.'
---
