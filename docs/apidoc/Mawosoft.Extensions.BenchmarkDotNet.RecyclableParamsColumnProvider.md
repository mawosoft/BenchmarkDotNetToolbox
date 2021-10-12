---
uid: Mawosoft.BenchmarkDotNetToolbox.RecyclableParamsColumnProvider
summary: An alternative to @"BenchmarkDotNet.Columns.DefaultColumnProviders.Params?displayProperty=nameWithType" that displays parameters in recyclable columns corresponding to parameter position rather than name.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.RecyclableParamsColumnProvider.#ctor(System.Boolean,System.String)
syntax:
    parameters:
    - id: tryKeepParamName
      description: |
        If `true` and if all parameters at a position have the same name, that name will be used as column
        header. Otherwise, a generic, numbered column header will be used.
        The default is `true`.
    - id: genericName
      description: Prefix for the generic, numbered column header. The default is `"Param"`.
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
    // Add the DefaultColumnProviders you need except DefaultColumnProviders.Params
    .AddColumnProvider(DefaultColumnProviders.Descriptor /* add more... */)
    // Add a new RecyclableParamsColumnProvider
    .AddColumnProvider(new RecyclableParamsColumnProvider());
    // Add other elements to the config...
```


If you are modifying an existing config, use
@"Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceColumnCategory(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Columns.IColumnProvider[])?text=ReplaceColumnCategory()",
one of the new config extension methods in *BenchmarkDotNetToolbox*.

```csharp
ManualConfig config = ManualConfig.Create(DefaultConfig.Instance)
    // Replace the default param columns with a new RecyclableParamsColumnProvider
    .ReplaceColumnCategory(new RecyclableParamsColumnProvider());
    // Make other changes to the config...
```

##### Sample Output

# [Default Settings](#tab/tabid-1)
<pre>
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
</pre>
# [Custom Settings](#tab/tabid-2)
<pre>
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
</pre>
# [BenchmarkDotNet Defaults](#tab/tabid-3)
<pre>
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
</pre>
***


---
uid: Mawosoft.BenchmarkDotNetToolbox.RecyclableParamsColumnProvider.GetColumns(BenchmarkDotNet.Reports.Summary)
summary: '@"BenchmarkDotNet.Columns.IColumnProvider" implementation.'
---
