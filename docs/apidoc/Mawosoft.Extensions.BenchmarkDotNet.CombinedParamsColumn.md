---
uid: Mawosoft.Extensions.BenchmarkDotNet.CombinedParamsColumn
summary: An alternative to @"BenchmarkDotNet.Columns.DefaultColumnProviders.Params?displayProperty=nameWithType" that displays all parameters in a single, customizable column.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.CombinedParamsColumn.#ctor(System.String,System.String,System.String,System.String)
syntax:
    content: public CombinedParamsColumn(string formatNameValue = "{0}={1}", string separator = ", ", string prefix = "", string suffix = "")
    parameters:
    - id: formatNameValue
      description: |
        A composite format string where the format item `{0}` will be replaced with the parameter name
        and the format item `{1}` with the parameter value.

        The default is `"{0}={1}"`.
    - id: separator
      description: The string to use as a separator between multiple formatted parameters. The default is `", "`.
    - id: prefix
      description: The string to use before the first formatted parameter. The default is an empty string.
    - id: suffix
      description: The string to use after the last formatted parameter. The default is an empty string.
example:
- *content
seealso:
- linkType: HRef
  linkId: https://github.com/mawosoft/Mawosoft.Extensions.BenchmarkDotNet/tree/master/samples
  altText: Column Display Samples on GitHub
---

Use @"BenchmarkDotNet.Configs.ManualConfig.AddColumn(BenchmarkDotNet.Columns.IColumn[])?text=AddColumn()"
if you are building a config from scratch.

```csharp
ManualConfig config = ManualConfig.CreateEmpty()
    // Add the DefaultColumnProviders you need except DefaultColumnProviders.Params
    .AddColumnProvider(DefaultColumnProviders.Descriptor /* add more... */)
    // Add a new CombinedParamsColumn
    .AddColumn(new CombinedParamsColumn());
    // Add other elements to the config...
```

If you are modifying an existing config, use
@"Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceColumnCategory(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Columns.IColumn[])?text=ReplaceColumnCategory()",
one of the new config extension methods in this library.

```csharp
ManualConfig config = ManualConfig.Create(DefaultConfig.Instance)
    // Replace the default param columns with a new CombinedParamsColumn
    .ReplaceColumnCategory(new CombinedParamsColumn());
    // Make other changes to the config...
```


Change the formatting to display values only, separated by semicolon.

```csharp
new CombinedParamsColumn("{1}", "; ")
```

##### Sample Output

# [Default Formatting](#tab/tabid-1)
<pre>
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
</pre>
# [Custom Formatting](#tab/tabid-2)
<pre>
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
uid: Mawosoft.Extensions.BenchmarkDotNet.CombinedParamsColumn.AlwaysShow
summary: '@"BenchmarkDotNet.Columns.IColumn" implementation.'
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.CombinedParamsColumn.Category
summary: '@"BenchmarkDotNet.Columns.IColumn" implementation.'
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.CombinedParamsColumn.ColumnName
summary: '@"BenchmarkDotNet.Columns.IColumn" implementation.'
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.CombinedParamsColumn.GetValue(BenchmarkDotNet.Reports.Summary,BenchmarkDotNet.Running.BenchmarkCase)
summary: '@"BenchmarkDotNet.Columns.IColumn" implementation.'
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.CombinedParamsColumn.GetValue(BenchmarkDotNet.Reports.Summary,BenchmarkDotNet.Running.BenchmarkCase,BenchmarkDotNet.Reports.SummaryStyle)
summary: '@"BenchmarkDotNet.Columns.IColumn" implementation.'
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.CombinedParamsColumn.Id
summary: '@"BenchmarkDotNet.Columns.IColumn" implementation.'
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.CombinedParamsColumn.IsAvailable(BenchmarkDotNet.Reports.Summary)
summary: '@"BenchmarkDotNet.Columns.IColumn" implementation.'
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.CombinedParamsColumn.IsDefault(BenchmarkDotNet.Reports.Summary,BenchmarkDotNet.Running.BenchmarkCase)
summary: '@"BenchmarkDotNet.Columns.IColumn" implementation.'
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.CombinedParamsColumn.IsNumeric
summary: '@"BenchmarkDotNet.Columns.IColumn" implementation.'
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.CombinedParamsColumn.Legend
summary: '@"BenchmarkDotNet.Columns.IColumn" implementation.'
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.CombinedParamsColumn.PriorityInCategory
summary: '@"BenchmarkDotNet.Columns.IColumn" implementation.'
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.CombinedParamsColumn.UnitType
summary: '@"BenchmarkDotNet.Columns.IColumn" implementation.'
---
